using System.Collections.Generic;
namespace shlox
{
    public abstract class Stmt
    {
        public abstract T Accept<T>(IStmtVisitor<T> visitor);
    }

    public interface IStmtVisitor<T>
    {
        T VisitBlockStmt(Block stmt);
        T VisitExpressionStmt(Expression stmt);
        T VisitIfStmt(If stmt);
        T VisitPrintStmt(Print stmt);
        T VisitVarStmt(Var stmt);
        T VisitWhileStmt(While stmt);
    }

    public class Block : Stmt
    {
        public List<Stmt> Statements { get; }

        public Block(List<Stmt> statements)
        {
            Statements = statements;
        }

        public override T Accept<T>(IStmtVisitor<T> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }
    }

    public class Expression : Stmt
    {
        public Expr Expr { get; }

        public Expression(Expr expr)
        {
            Expr = expr;
        }

        public override T Accept<T>(IStmtVisitor<T> visitor)
        {
            return visitor.VisitExpressionStmt(this);
        }
    }

    public class If : Stmt
    {
        public Expr Condition { get; }
        public Stmt Thenbranch { get; }
        public Stmt Elsebranch { get; }

        public If(Expr condition, Stmt thenBranch, Stmt elseBranch)
        {
            Condition = condition;
            Thenbranch = thenBranch;
            Elsebranch = elseBranch;
        }

        public override T Accept<T>(IStmtVisitor<T> visitor)
        {
            return visitor.VisitIfStmt(this);
        }
    }

    public class Print : Stmt
    {
        public Expr Expr { get; }

        public Print(Expr expr)
        {
            Expr = expr;
        }

        public override T Accept<T>(IStmtVisitor<T> visitor)
        {
            return visitor.VisitPrintStmt(this);
        }
    }

    public class Var : Stmt
    {
        public Token Name { get; }
        public Expr Initializer { get; }

        public Var(Token name, Expr initializer)
        {
            Name = name;
            Initializer = initializer;
        }

        public override T Accept<T>(IStmtVisitor<T> visitor)
        {
            return visitor.VisitVarStmt(this);
        }
    }

    public class While : Stmt
    {
        public Expr Condition { get; }
        public Stmt Body { get; }

        public While(Expr condition, Stmt body)
        {
            Condition = condition;
            Body = body;
        }

        public override T Accept<T>(IStmtVisitor<T> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }
    }
}
