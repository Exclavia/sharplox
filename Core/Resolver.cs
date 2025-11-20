using SharpLox.Core.Interface;
using SharpLox.Core.Parsing;
using System.Collections.Generic;

namespace SharpLox.Core.Runtime {
    class Resolver : Expr.IVisitor<object>, Stmt.IVisitor<object> {
        private readonly Interpreter interpreter;
        private readonly Stack<Dictionary<string, bool>> scopes = new Stack<Dictionary<string, bool>>();
        private FunctionType currentFunction = FunctionType.NONE;
        private ClassType currentClass = ClassType.NONE;

        private enum FunctionType { NONE, FUNCTION, INITIALIZER, METHOD }
        private enum ClassType { NONE, CLASS, SUBCLASS }

        public Resolver(Interpreter interpreter) {
            this.interpreter = interpreter;
        }

        public void Resolve(List<Stmt> statements) {
            foreach (Stmt statement in statements) {
                Resolve(statement);
            }
        }

        private void Resolve(Stmt stmt) {
            stmt.Accept(this);
        }

        private void Resolve(Expr expr) {
            expr.Accept(this);
        }

        private void ResolveFunction(Stmt.Function function, FunctionType type) {
            FunctionType enclosingFunction = currentFunction;
            currentFunction = type;
            BeginScope();
            foreach (Token param in function.Params) {
                Declare(param);
                Define(param);
            }
            Resolve(function.Body);
            EndScope();
            currentFunction = enclosingFunction;
        }

        private void BeginScope() {
            scopes.Push(new Dictionary<string, bool>());
        }

        private void EndScope() {
            scopes.Pop();
        }

        private void Declare(Token name) {
            if (scopes.Count == 0) return;
            var scope = scopes.Peek();
            if (scope.ContainsKey(name.Lexeme)) {
                SharpLox.Error(name, "Already a variable with this name in this scope.");
            }
            scope[name.Lexeme] = false;
        }

        private void Define(Token name) {
            if (scopes.Count == 0) return;
            scopes.Peek()[name.Lexeme] = true;
        }

        private void ResolveLocal(Expr expr, Token name) {
            // Copy stack to array to iterate index
            var scopeArray = scopes.ToArray();
            // Stack enumerates from top (recent) to bottom (old), which matches the loop logic.
            // However, scopes.get(i) in Java accesses by index where 0 is bottom.
            // C# ToArray() returns Last pushed at index 0.
            // So scopeArray[0] is the current scope.

            for (int i = 0; i < scopeArray.Length; i++) {
                if (scopeArray[i].ContainsKey(name.Lexeme)) {
                    interpreter.Resolve(expr, i);
                    return;
                }
            }
        }

        public object VisitBlockStmt(Stmt.Block stmt) {
            BeginScope();
            Resolve(stmt.Statements);
            EndScope();
            return null;
        }

        public object VisitClassStmt(Stmt.Class stmt) {
            ClassType enclosingClass = currentClass;
            currentClass = ClassType.CLASS;
            Declare(stmt.Name);
            Define(stmt.Name);
            if (stmt.Superclass != null && stmt.Name.Lexeme.Equals(stmt.Superclass.Name.Lexeme)) {
                SharpLox.Error(stmt.Superclass.Name, "A class can't inherit from itself.");
            }

            if (stmt.Superclass != null) {
                currentClass = ClassType.SUBCLASS;
                Resolve(stmt.Superclass);
            }

            if (stmt.Superclass != null) {
                BeginScope();
                scopes.Peek()["super"] = true;
            }

            BeginScope();
            scopes.Peek()["this"] = true;

            foreach (Stmt.Function method in stmt.Methods) {
                FunctionType declaration = FunctionType.METHOD;
                if (method.Name.Lexeme.Equals("init")) {
                    declaration = FunctionType.INITIALIZER;
                }
                ResolveFunction(method, declaration);
            }

            EndScope();
            if (stmt.Superclass != null) EndScope();
            currentClass = enclosingClass;
            return null;
        }

        public object VisitExpressionStmt(Stmt.Expression stmt) {
            Resolve(stmt.Expr);
            return null;
        }

        public object VisitFunctionStmt(Stmt.Function stmt) {
            Declare(stmt.Name);
            Define(stmt.Name);
            ResolveFunction(stmt, FunctionType.FUNCTION);
            return null;
        }

        public object VisitIfStmt(Stmt.If stmt) {
            Resolve(stmt.Condition);
            Resolve(stmt.ThenBranch);
            if (stmt.ElseBranch != null) Resolve(stmt.ElseBranch);
            return null;
        }

        public object VisitImportStmt(Stmt.Import stmt) {
            Resolve(stmt.Module);
            return null;
        }

        public object VisitReturnStmt(Stmt.Return stmt) {
            if (currentFunction == FunctionType.NONE) {
                SharpLox.Error(stmt.Keyword, "Can't return from top-level code.");
            }
            if (stmt.Value != null) {
                if (currentFunction == FunctionType.INITIALIZER) {
                    SharpLox.Error(stmt.Keyword, "Can't return a value from an initializer.");
                }
                Resolve(stmt.Value);
            }
            return null;
        }

        public object VisitVarStmt(Stmt.Var stmt) {
            Declare(stmt.Name);
            if (stmt.Initializer != null) {
                Resolve(stmt.Initializer);
            }
            Define(stmt.Name);
            return null;
        }

        public object VisitWhileStmt(Stmt.While stmt) {
            Resolve(stmt.Condition);
            Resolve(stmt.Body);
            return null;
        }

        public object VisitAssignExpr(Expr.Assign expr) {
            Resolve(expr.Value);
            ResolveLocal(expr, expr.Name);
            return null;
        }

        public object VisitBinaryExpr(Expr.Binary expr) {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object VisitCallExpr(Expr.Call expr) {
            Resolve(expr.Callee);
            foreach (Expr argument in expr.Arguments) {
                Resolve(argument);
            }
            return null;
        }

        public object VisitGetExpr(Expr.Get expr) {
            Resolve(expr.Object);
            return null;
        }

        public object VisitGroupingExpr(Expr.Grouping expr) {
            Resolve(expr.Expression);
            return null;
        }

        public object VisitLiteralExpr(Expr.Literal expr) {
            return null;
        }

        public object VisitLogicalExpr(Expr.Logical expr) {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object VisitSetExpr(Expr.Set expr) {
            Resolve(expr.Value);
            Resolve(expr.Object);
            return null;
        }

        public object VisitSuperExpr(Expr.Super expr) {
            if (currentClass == ClassType.NONE) {
                SharpLox.Error(expr.Keyword, "Can't use 'super' outside of a class.");
            } else if (currentClass != ClassType.SUBCLASS) {
                SharpLox.Error(expr.Keyword, "Can't use 'super' in a class with no superclass.");
            }
            ResolveLocal(expr, expr.Keyword);
            return null;
        }

        public object VisitThisExpr(Expr.This expr) {
            if (currentClass == ClassType.NONE) {
                SharpLox.Error(expr.Keyword, "Can't use 'this' outside of a class.");
                return null;
            }
            ResolveLocal(expr, expr.Keyword);
            return null;
        }

        public object VisitUnaryExpr(Expr.Unary expr) {
            Resolve(expr.Right);
            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr) {
            if (scopes.Count > 0 && scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool defined) && !defined) {
                SharpLox.Error(expr.Name, "Can't read local variable in its own initializer.");
            }
            ResolveLocal(expr, expr.Name);
            return null;
        }
    }


}