using System;
using System.Linq;
using System.Collections.Generic;
using shlox.Exceptions;

namespace shlox
{
    public class Interpreter : IExprVisitor<object>, IStmtVisitor<object>
    {
        public Environment Globals { get; }
        private Environment _environment;

        public Interpreter()
        {
            Globals = new Environment();
            _environment = Globals;
            Globals.Define("clock", new Clock());
        }

        public void Interpret(IEnumerable<Stmt> statements)
        {
            try
            {
                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            catch (RuntimeException e)
            {
                Program.RuntimeError(e);
            }
        }

        public object VisitBinaryExpr(Binary expr)
        {
            var left = Evaluate(expr.Left);
            var right = Evaluate(expr.Right);

            return expr.Op.Type switch
            {
                TokenType.GREATER when CheckNumberOperands(expr.Op, left, right) => (double)left > (double)right,
                TokenType.GREATER_EQUAL when CheckNumberOperands(expr.Op, left, right) => (double)left >= (double)right,
                TokenType.LESS when CheckNumberOperands(expr.Op, left, right) => (double)left < (double)right,
                TokenType.LESS_EQUAL when CheckNumberOperands(expr.Op, left, right) => (double)left <= (double)right,
                TokenType.BANG_EQUAL => !IsEqual(left, right),
                TokenType.EQUAL_EQUAL => IsEqual(left, right),
                TokenType.MINUS when CheckNumberOperands(expr.Op, left, right) => (double)left - (double)right,
                TokenType.SLASH when CheckNumberOperands(expr.Op, left, right) => left switch
                {
                    double _ when right is double rd && rd == 0.0 => throw new RuntimeException(expr.Op, "Division by zero not allowed."),
                    double ld when right is double rd => ld / rd
                },
                TokenType.SLASH when CheckNumberOperands(expr.Op, left, right) => (double)left / (double)right,
                TokenType.STAR when CheckNumberOperands(expr.Op, left, right) => (double)left * (double)right,
                TokenType.PLUS => left switch
                {
                    double ld when right is string rs => $"{ld}{rs}",
                    string ls when right is double ld => $"{ls}{ld}",
                    double ld when right is double rd => ld + rd,
                    string ls when right is string rs => ls + rs,
                    _ => throw new RuntimeException(expr.Op,
                        "Operands must be two numbers or two strings.")
                },
                _ => null
            };
        }

        public object VisitCallExpr(Call expr)
        {
            if (!(Evaluate(expr.Callee) is ICallable func))
            {
                throw new RuntimeException(expr.Paren,
                    "Can only call functions and classes.");
            }
            var arguments = expr.Arguments.Select(a => Evaluate(a)).ToList();
            if (arguments.Count != func.Arity())
            {
                throw new RuntimeException(expr.Paren,
                    $"Expected {func.Arity()} arguments but got {arguments.Count()}.");
            }
            return func.Call(this, arguments);
        }

        public object VisitGroupingExpr(Grouping expr) => Evaluate(expr.Expression);

        public object VisitLiteralExpr(Literal expr) => expr.Value;

        public object VisitLogicalExpr(Logical expr)
        {
            var left = Evaluate(expr.Left);
            if (expr.Op.Type == TokenType.OR)
            {
                if (IsTruthy(left)) return left;
            }
            else
            {
                if (!IsTruthy(left)) return left;
            }
            return Evaluate(expr.Right);
        }

        public object VisitUnaryExpr(Unary expr)
        {
            var right = Evaluate(expr.Right);
            return expr.Op.Type switch
            {
                TokenType.MINUS when CheckNumberOperand(expr.Op, right) => -(double)right,
                TokenType.BANG => !IsTruthy(right),
                _ => null,
            };
        }

        public object VisitVariableExpr(Variable expr) => _environment.Get(expr.Name);

        private bool CheckNumberOperand(Token op, object operand)
        {
            if (operand is double) return true;
            throw new RuntimeException(op, "Operand must be a number.");
        }


        private bool CheckNumberOperands(Token op, object left, object right)
        {
            if (left is double && right is double) return true;
            throw new RuntimeException(op, "Operands must be numbers.");
        }

        private bool IsTruthy(object obj)
        {
            if (obj is null) return false;
            if (obj is bool b) return b;
            return true;
        }

        private bool IsEqual(object a, object b)
        {
            if (a is null && b is null) return true;
            if (a is null) return false;
            return a.Equals(b);
        }

        private string Stringify(object obj)
        {
            if (obj is null) return "nil";
            if (obj is double)
            {
                var text = obj.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text.Substring(text.Length - 2);
                }
                return text;
            }
            return obj.ToString();
        }

        private object Evaluate(Expr expr) => expr.Accept(this);

        private void Execute(Stmt stmt) => stmt.Accept(this);

        public object VisitExpressionStmt(Expression stmt)
        {
            Evaluate(stmt.Expr);
            return null;
        }

        public object VisitFunctionStmt(Function stmt)
        {
            var function = new LoxFunction(stmt);
            _environment.Define(stmt.Name.Lexeme, function);
            return null;
        }

        public object VisitPrintStmt(Print stmt)
        {
            var value = Evaluate(stmt.Expr);
            Console.WriteLine(Stringify(value));
            return null;
        }

        public object VisitReturnStmt(Return stmt)
        {
            var value = stmt.Value is null ? null : Evaluate(stmt.Value);
            throw new ReturnException(value);
        }

        public object VisitVarStmt(Var stmt)
        {
            var value = stmt.Initializer == null
                ? null
                : Evaluate(stmt.Initializer);

            _environment.Define(stmt.Name.Lexeme, value);
            return null;
        }

        public object VisitWhileStmt(While stmt)
        {
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.Body);
            }
            return null;
        }

        public object VisitAssignExpr(Assign expr)
        {
            var value = Evaluate(expr.Value);
            _environment.Assign(expr.Name, value);
            return null;
        }

        public object VisitBlockStmt(Block stmt)
        {
            ExecuteBlock(stmt.Statements, new Environment(_environment));
            return null;
        }

        public object VisitIfStmt(If stmt)
        {
            if (IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.Thenbranch);
            }
            else if (stmt.Elsebranch != null)
            {
                Execute(stmt.Elsebranch);
            }
            return null;
        }

        public void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            var previous = _environment;
            try
            {
                _environment = environment;
                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                _environment = previous;
            }
        }

        private class Clock : ICallable
        {
            public int Arity() => 0;

            public object Call(Interpreter interpreter, List<object> arguments)
                => (double)DateTime.Now.Ticks;

            public override string ToString() => "<native fn>";
        }
    }
}
