using SharpLox.Core.Parsing;
using System.Collections.Generic;

namespace SharpLox.Core.Interface {
    public abstract class Stmt {
        public interface IVisitor<R> {
            R VisitBlockStmt(Block stmt);
            R VisitClassStmt(Class stmt);
            R VisitExpressionStmt(Expression stmt);
            R VisitFunctionStmt(Function stmt);
            R VisitIfStmt(If stmt);
            R VisitImportStmt(Import stmt);
            R VisitReturnStmt(Return stmt);
            R VisitVarStmt(Var stmt);
            R VisitWhileStmt(While stmt);
        }

        public abstract R Accept<R>(IVisitor<R> visitor);

        public class Block : Stmt {
            public Block(List<Stmt> statements) { Statements = statements; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitBlockStmt(this); }
            public readonly List<Stmt> Statements;
        }

        public class Class : Stmt {
            public Class(Token name, Expr.Variable superclass, List<Function> methods) { Name = name; Superclass = superclass; Methods = methods; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitClassStmt(this); }
            public readonly Token Name;
            public readonly Expr.Variable Superclass;
            public readonly List<Function> Methods;
        }

        public class Expression : Stmt {
            public Expression(Expr expression) { Expr = expression; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitExpressionStmt(this); }
            public readonly Expr Expr;
        }

        public class Function : Stmt {
            public Function(Token name, List<Token> parameters, List<Stmt> body) { Name = name; Params = parameters; Body = body; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitFunctionStmt(this); }
            public readonly Token Name;
            public readonly List<Token> Params;
            public readonly List<Stmt> Body;
        }

        public class If : Stmt {
            public If(Expr condition, Stmt thenBranch, Stmt elseBranch) { Condition = condition; ThenBranch = thenBranch; ElseBranch = elseBranch; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitIfStmt(this); }
            public readonly Expr Condition;
            public readonly Stmt ThenBranch;
            public readonly Stmt ElseBranch;
        }

        public class Import : Stmt {
            public Import(Token keyword, Expr module) { Keyword = keyword; Module = module; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitImportStmt(this); }
            public readonly Token Keyword;
            public readonly Expr Module;
        }

        public class Return : Stmt {
            public Return(Token keyword, Expr value) { Keyword = keyword; Value = value; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitReturnStmt(this); }
            public readonly Token Keyword;
            public readonly Expr Value;
        }

        public class Var : Stmt {
            public Var(Token name, Expr initializer) { Name = name; Initializer = initializer; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitVarStmt(this); }
            public readonly Token Name;
            public readonly Expr Initializer;
        }

        public class While : Stmt {
            public While(Expr condition, Stmt body) { Condition = condition; Body = body; }
            public override R Accept<R>(IVisitor<R> visitor) { return visitor.VisitWhileStmt(this); }
            public readonly Expr Condition;
            public readonly Stmt Body;
        }
    }
}