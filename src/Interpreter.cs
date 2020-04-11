using System;
using System.Collections.Generic;
using shlox.Exceptions;

namespace shlox
{
    public class Interpreter : IExprVisitor<object>, IStmtVisitor<object>
    {
        private Environment _environment;

        public Interpreter()
        {
            _environment = new Environment();
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
                    _ => throw new RuntimeException(expr.Op, "Operands must be two numbers or two strings.")
                },
                _ => null
            };
        }

        public object VisitGroupingExpr(Grouping expr) => Evaluate(expr.Expression);

        public object VisitLiteralExpr(Literal expr) => expr.Value;

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

        public object VisitPrintStmt(Print stmt)
        {
            var value = Evaluate(stmt.Expr);
            Console.WriteLine(Stringify(value));
            return null;
        }

        public object VisitVarStmt(Var stmt)
        {
            var value = stmt.Initializer == null
                ? null
                : Evaluate(stmt.Initializer);

            _environment.Define(stmt.Name.Lexeme, value);
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

        private void ExecuteBlock(List<Stmt> statements, Environment environment)
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
    }
}
