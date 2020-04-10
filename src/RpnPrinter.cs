using System;
using System.Text;

namespace shlox
{
    public class RpnPrinter : IExprVisitor<string>
    {
        public string Print(Expr expr)
        {
            return expr.Accept(this);
        }

        public string VisitAssignExpr(Assign expr)
        {
            throw new NotImplementedException();
        }

        public string VisitBinaryExpr(Binary expr)
        {
            return $"{expr.Left.Accept(this)} {expr.Right.Accept(this)} {expr.Op.Lexeme}";
            //return Rpn(expr.Left, expr.Right);
        }

        public string VisitGroupingExpr(Grouping expr)
        {
            return expr.Expression.Accept(this);
            //return Rpn(expr.Expression);
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
            return Rpn(expr.Right) + expr.Op.Lexeme;
        }

        public string VisitVariableExpr(Variable expr)
        {
            throw new NotImplementedException();
        }

        private string Rpn(params Expr[] exprs)
        {
            var sb = new StringBuilder();
            foreach (var expr in exprs)
            {
                sb.Append(" ");
                sb.Append(expr.Accept(this));
            }
            return sb.ToString();
        }
    }
}
