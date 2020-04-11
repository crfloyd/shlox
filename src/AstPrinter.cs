using System.Text;

namespace shlox
{
    public class AstPrinter : IExprVisitor<string>
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
            throw new System.NotImplementedException();
        }

        public string VisitBinaryExpr(Binary expr)
        {
            return Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);
        }

        public string VisitGroupingExpr(Grouping expr)
        {
            return Parenthesize("group", expr.Expression);
        }

        public string VisitLiteralExpr(Literal expr)
        {
            if (expr.Value == null)
            {
                return "nil";
            }
            return expr.Value.ToString();
        }

        public string VisitUnaryExpr(Unary expr)
        {
            return Parenthesize(expr.Op.Lexeme, expr.Right);
        }

        public string VisitVariableExpr(Variable expr)
        {
            return expr.ToString();
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
