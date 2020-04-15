using System;
using System.Collections.Generic;
using System.Linq;

namespace shlox
{
    public class Resolver : IExprVisitor<object>, IStmtVisitor<object>
    {
        private FunctionType _currentFunction = FunctionType.None;
        private readonly Stack<Dictionary<string, bool>> _scopes;
        private readonly Interpreter _interpreter;

        public Resolver(Interpreter interpreter)
        {
            _scopes = new Stack<Dictionary<string, bool>>();
            _interpreter = interpreter;
        }

        public object VisitAssignExpr(Assign expr)
        {
            Resolve(expr.Value);
            ResolveLocal(expr, expr.Name);
            return null;
        }

        public object VisitBinaryExpr(Binary expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object VisitBlockStmt(Block stmt)
        {
            BeginScope();
            Resolve(stmt.Statements);
            EndScope();
            return null;
        }

        public object VisitCallExpr(Call expr)
        {
            Resolve(expr.Callee);
            foreach (var arg in expr.Arguments)
            {
                Resolve(arg);
            }
            return null;
        }

        public object VisitExpressionStmt(Expression stmt)
        {
            Resolve(stmt.Expr);
            return null;
        }

        public object VisitFunctionStmt(Function stmt)
        {
            Declare(stmt.Name);
            Define(stmt.Name);
            ResolveFunction(stmt, FunctionType.None);
            return null;
        }

        public object VisitGroupingExpr(Grouping expr)
        {
            Resolve(expr.Expression);
            return null;
        }

        public object VisitIfStmt(If stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.Thenbranch);
            if (stmt.Elsebranch != null)
            {
                Resolve(stmt.Elsebranch);
            }
            return null;
        }

        public object VisitLiteralExpr(Literal expr) => null;

        public object VisitLogicalExpr(Logical expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object VisitPrintStmt(Print stmt)
        {
            Resolve(stmt.Expr);
            return null;
        }

        public object VisitReturnStmt(Return stmt)
        {
            if (_currentFunction is FunctionType.None)
            {
                Program.Error(stmt.Keyword, "Cannot return from top-level.");
            }
            if (stmt.Value is null)
            {
                return null;
            }
            Resolve(stmt.Value);
            return null;
        }

        public object VisitUnaryExpr(Unary expr)
        {
            Resolve(expr.Right);
            return null;
        }

        public object VisitVariableExpr(Variable expr)
        {
            if (_scopes.Count > 0 && !_scopes.Peek()[expr.Name.Lexeme])
            {
                Program.Error(expr.Name,
                    "Cannot read local variable in its own initializer");
            }
            ResolveLocal(expr, expr.Name);
            return null;
        }

        public object VisitVarStmt(Var stmt)
        {
            Declare(stmt.Name);
            if (stmt.Initializer != null)
            {
                Resolve(stmt.Initializer);
            }
            Define(stmt.Name);
            return null;
        }

        public object VisitWhileStmt(While stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.Body);
            return null;
        }

        public void Resolve(IEnumerable<Stmt> statements)
        {
            foreach (var statement in statements)
            {
                Resolve(statement);
            }
        }

        private void Resolve(Expr expr) => expr.Accept(this);

        private void Resolve(Stmt statement) => statement.Accept(this);

        private void ResolveLocal(Expr expr, Token name)
        {
            int i = 0;
            foreach (var scope in _scopes.ToArray().Reverse())
            {
                if (scope.ContainsKey(name.Lexeme))
                {
                    _interpreter.Resolve(expr, i);
                    i++;
                    return;
                }
            }            
        }

        private void ResolveFunction(Function function, FunctionType functionType)
        {
            var enclosingFunction = _currentFunction;
            _currentFunction = functionType;
            BeginScope();
            foreach (var param in function.Parameters)
            {
                Declare(param);
                Define(param);
            }
            Resolve(function.Body);
            EndScope();
            _currentFunction = enclosingFunction;
        }

        private void BeginScope() => _scopes.Push(new Dictionary<string, bool>());

        private void EndScope() => _scopes.Pop();

        private void Declare(Token name)
        {
            if (_scopes.Count == 0) return;
            var scope = _scopes.Peek();
            if (scope.ContainsKey(name.Lexeme))
            {
                Program.Error(name, "Variable with the same name already declared in this scope.");
            }
            scope[name.Lexeme] = false;
        }

        private void Define(Token name)
        {
            if (_scopes.Count == 0) return;
            _scopes.Peek()[name.Lexeme] = true;
        }

        private enum FunctionType
        {
            None,
            Function
        }
    }
}
