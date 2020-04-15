using System.Text;
using System.Linq;

namespace shlox
{
    public class AstPrinter : IExprVisitor<string>, IStmtVisitor<string>
    {

        public AstPrinter()
        {

        }

        public string Print(Expr expr)
        {
            return expr.Accept(this);
        }

        public string VisitAssignExpr(Assign expr)
        {
            return Parenthesize("assign", expr);
        }

        public string VisitBinaryExpr(Binary expr)
        {
            return Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);
        }

        public string VisitBlockStmt(Block stmt)
        {
            throw new System.NotImplementedException();
        }

        public string VisitCallExpr(Call expr)
        {
            var args = expr.Arguments.Select(a => a.Accept(this));
            return expr.Callee.Accept(this) + $"({args.Aggregate((a,b) => $"{a}, {b}")})";
        }

        public string VisitExpressionStmt(Expression stmt)
        {
            throw new System.NotImplementedException();
        }

        public string VisitFunctionStmt(Function stmt)
        {
            throw new System.NotImplementedException();
        }

        public string VisitGroupingExpr(Grouping expr)
        {
            return Parenthesize("group", expr.Expression);
        }

        public string VisitIfStmt(If stmt)
        {
            throw new System.NotImplementedException();
        }

        public string VisitLiteralExpr(Literal expr)
        {
            if (expr.Value == null)
            {
                return "nil";
            }
            return expr.Value.ToString();
        }

        public string VisitLogicalExpr(Logical expr)
        {
            return Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);
        }

        public string VisitPrintStmt(Print stmt)
        {
            throw new System.NotImplementedException();
        }

        public string VisitReturnStmt(Return stmt)
        {
            throw new System.NotImplementedException();
        }

        public string VisitUnaryExpr(Unary expr)
        {
            return Parenthesize(expr.Op.Lexeme, expr.Right);
        }

        public string VisitVariableExpr(Variable expr)
        {
            return expr.ToString();
        }

        public string VisitVarStmt(Var stmt)
        {
            throw new System.NotImplementedException();
        }

        public string VisitWhileStmt(While stmt)
        {
            throw new System.NotImplementedException();
        }

        private string Parenthesize(string name, params Expr[] exprs)
        {
            var sb = new StringBuilder();
            sb.Append("(").Append(name);
            foreach (var expr in exprs)
            {
                sb.Append(" ");
                sb.Append(expr.Accept(this));
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}
