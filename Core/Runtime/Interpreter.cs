using SharpLox.Core.Interface;
using SharpLox.Core.Parsing;
using SharpLox.Core.Scope;
using SharpLox.Core.Stdlib;
using System;
using System.Collections.Generic;
using System.IO;
using Environment = SharpLox.Core.Scope.Environment;

namespace SharpLox.Core.Runtime {
    // Helper for Native functions
    public class NativeFunction : ISharpLoxCallable {
        private readonly int _arity;
        private readonly Func<Interpreter, List<object>, object> _call;

        public NativeFunction(int arity, Func<Interpreter, List<object>, object> call) {
            _arity = arity;
            _call = call;
        }
        public int Arity() => _arity;
        public object Call(Interpreter i, List<object> args) => _call(i, args);
        public override string ToString() => "<native fn>";
    }

    public class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object> {
        public readonly Environment Globals = new Environment();
        private Environment environment;
        private readonly Dictionary<Expr, int> locals = new Dictionary<Expr, int>();

        public Interpreter() {
            environment = Globals;

            // Clock
            Globals.Define("clock", new NativeFunction(0, (i, a) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0));

            // Sleep
            Globals.Define("sleep", new NativeFunction(1, (i, a) => {
                object obj = a[0];
                if (obj is double) {
                    System.Threading.Thread.Sleep(Intify(obj));
                    return "\n";
                } else {
                    Console.Error.WriteLine("Sleep function arguments can only be numbers.");
                    return "\n";
                }
            }));

            // Exit
            Globals.Define("exit", new NativeFunction(0, (i, a) => {
                SharpLox.ExitProgram();
                return null;
            }));

            // Print
            Globals.Define("print", new NativeFunction(1, (i, a) => {
                Console.WriteLine(Stringify(a[0]));
                return null;
            }));

            // Input
            Globals.Define("input", new NativeFunction(0, (i, a) => {
                try { return Console.ReadLine(); } catch { return null; }
            }));



            // ReadFile
            Globals.Define("readFile", new NativeFunction(1, (i, a) => {
                try {
                    return File.ReadAllText(Stringify(a[0]));
                } catch { return null; }
            }));

            // WriteFile
            Globals.Define("writeFile", new NativeFunction(2, (i, a) => {
                try {
                    File.WriteAllText(Stringify(a[0]), Stringify(a[1]));
                    return true;
                } catch { return false; }
            }));

            // AppendFile
            Globals.Define("appendFile", new NativeFunction(2, (i, a) => {
                try {
                    File.AppendAllText(Stringify(a[0]), "\n" + Stringify(a[1]));
                    return true;
                } catch { return false; }
            }));

            // FileExists
            Globals.Define("fileExists", new NativeFunction(1, (i, a) => {
                return File.Exists(Stringify(a[0]));
            }));

            // CreateFile
            Globals.Define("createFile", new NativeFunction(1, (i, a) => {
                string path = Stringify(a[0]);
                try {
                    if (!File.Exists(path)) {
                        File.Create(path).Close();
                        return true;
                    } else {
                        Console.Error.WriteLine(" '" + path + "' already exists.");
                        return false;
                    }
                } catch { return false; }
            }));

            // DeleteFile
            Globals.Define("deleteFile", new NativeFunction(1, (i, a) => {
                string path = Stringify(a[0]);
                if (File.Exists(path)) {
                    File.Delete(path);
                    return true;
                } else {
                    Console.Error.WriteLine(" '" + path + "' does not exist.");
                    return false;
                }
            }));

            // Len
            Globals.Define("len", new NativeFunction(1, (i, a) => {
                if (a[0] is string s) return (double)s.Length;
                Console.Error.WriteLine("Length function can only be used on Strings.");
                return "\n";
            }));

            // Lower
            Globals.Define("lower", new NativeFunction(1, (i, a) => {
                if (a[0] is string s) return s.ToLower();
                Console.Error.WriteLine("Lower case function can only be used on Strings.");
                return "\n";
            }));

            // Upper
            Globals.Define("upper", new NativeFunction(1, (i, a) => {
                if (a[0] is string s) return s.ToUpper();
                Console.Error.WriteLine("Upper case function can only be used on Strings.");
                return "\n";
            }));

            // Upper
            Globals.Define("toStr", new NativeFunction(1, (i, a) => {
                if (a[0] is double s) return s.ToString();
                Console.Error.WriteLine("Only numbers can be converted to strings.");
                return "\n";
            }));

            // Abs
            Globals.Define("abs", new NativeFunction(1, (i, a) => {
                if (a[0] is double d) return Math.Abs(d);
                Console.Error.WriteLine("Absolute function can only be used on Numbers.");
                return "\n";
            }));

            // Pow
            Globals.Define("pow", new NativeFunction(2, (i, a) => {
                if (a[0] is double d1 && a[1] is double d2) return Math.Pow(d1, d2);
                Console.Error.WriteLine("Power function can only be used on Numbers.");
                return "\n";
            }));
        }

        public void Interpret(List<Stmt> statements) {
            try {
                foreach (Stmt statement in statements) {
                    Execute(statement);
                }
            } catch (RuntimeError error) {
                SharpLox.RuntimeError(error);
            }
        }

        private void Execute(Stmt stmt) {
            stmt.Accept(this);
        }

        public void Resolve(Expr expr, int depth) {
            locals[expr] = depth;
        }

        public void ExecuteBlock(List<Stmt> statements, Environment environment) {
            Environment previous = this.environment;
            try {
                this.environment = environment;
                foreach (Stmt statement in statements) {
                    Execute(statement);
                }
            }
            finally {
                this.environment = previous;
            }
        }

        // -- Visitor Implementations --

        public object VisitLiteralExpr(Expr.Literal expr) => expr.Value;

        public object VisitLogicalExpr(Expr.Logical expr) {
            object left = Evaluate(expr.Left);
            if (expr.Operator.Type == TokenType.OR) {
                if (IsTruthy(left)) return left;
            } else {
                if (!IsTruthy(left)) return left;
            }
            return Evaluate(expr.Right);
        }

        public object VisitSetExpr(Expr.Set expr) {
            object @object = Evaluate(expr.Object);
            if (!(@object is SharpLoxInstance instance)) {
                throw new RuntimeError(expr.Name, "Only instances have fields.");
            }
            object value = Evaluate(expr.Value);
            instance.Set(expr.Name, value);
            return value;
        }

        public object VisitSuperExpr(Expr.Super expr) {
            int distance = locals[expr];
            SharpLoxClass superclass = (SharpLoxClass)environment.GetAt(distance, "super");
            SharpLoxInstance @object = (SharpLoxInstance)environment.GetAt(distance - 1, "this");
            SharpLoxFunction method = superclass.FindMethod(expr.Method.Lexeme);

            if (method == null) {
                throw new RuntimeError(expr.Method, "Undefined property '" + expr.Method.Lexeme + "'.");
            }
            return method.Bind(@object);
        }

        public object VisitThisExpr(Expr.This expr) {
            return LookUpVariable(expr.Keyword, expr);
        }

        public object VisitUnaryExpr(Expr.Unary expr) {
            object right = Evaluate(expr.Right);
            switch (expr.Operator.Type) {
                case TokenType.BANG: return !IsTruthy(right);
                case TokenType.MINUS:
                    CheckNumberOperand(expr.Operator, right);
                    return -(double)right;
            }
            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr) {
            return LookUpVariable(expr.Name, expr);
        }

        private object LookUpVariable(Token name, Expr expr) {
            if (locals.ContainsKey(expr)) {
                int distance = locals[expr];
                return environment.GetAt(distance, name.Lexeme);
            } else {
                return Globals.Get(name);
            }
        }

        public object VisitGroupingExpr(Expr.Grouping expr) {
            return Evaluate(expr.Expression);
        }

        public object VisitAssignExpr(Expr.Assign expr) {
            object value = Evaluate(expr.Value);
            if (locals.ContainsKey(expr)) {
                int distance = locals[expr];
                environment.AssignAt(distance, expr.Name, value);
            } else {
                Globals.Assign(expr.Name, value);
            }
            return value;
        }

        public object VisitBinaryExpr(Expr.Binary expr) {
            object left = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);

            switch (expr.Operator.Type) {
                case TokenType.BANG_EQUAL: return !IsEqual(left, right);
                case TokenType.EQUAL_EQUAL: return IsEqual(left, right);
                case TokenType.GREATER:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left > (double)right;
                case TokenType.GREATER_EQUAL:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left >= (double)right;
                case TokenType.LESS:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left < (double)right;
                case TokenType.LESS_EQUAL:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left <= (double)right;
                case TokenType.MINUS:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left - (double)right;
                case TokenType.PLUS:
                    if (left is double d1 && right is double d2) return d1 + d2;
                    if (left is string s1 && right is string s2) return s1 + s2;
                    throw new RuntimeError(expr.Operator, "Operands must be two numbers or two strings.");
                case TokenType.SLASH:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left / (double)right;
                case TokenType.STAR:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left * (double)right;
            }
            return null;
        }

        public object VisitCallExpr(Expr.Call expr) {
            object callee = Evaluate(expr.Callee);
            List<object> arguments = new List<object>();
            foreach (Expr argument in expr.Arguments) {
                arguments.Add(Evaluate(argument));
            }

            if (!(callee is ISharpLoxCallable)) {
                throw new RuntimeError(expr.Paren, "Can only call functions and classes.");
            }

            ISharpLoxCallable function = (ISharpLoxCallable)callee;
            if (arguments.Count != function.Arity()) {
                throw new RuntimeError(expr.Paren, "Expected " + function.Arity() + " arguments but got " + arguments.Count + ".");
            }
            return function.Call(this, arguments);
        }

        public object VisitGetExpr(Expr.Get expr) {
            object @object = Evaluate(expr.Object);
            if (@object is SharpLoxInstance instance) {
                return instance.Get(expr.Name);
            }
            throw new RuntimeError(expr.Name, "Only instances have properties.");
        }

        public object VisitBlockStmt(Stmt.Block stmt) {
            ExecuteBlock(stmt.Statements, new Environment(environment));
            return null;
        }

        public object VisitClassStmt(Stmt.Class stmt) {
            object superclass = null;
            if (stmt.Superclass != null) {
                superclass = Evaluate(stmt.Superclass);
                if (!(superclass is SharpLoxClass)) {
                    throw new RuntimeError(stmt.Superclass.Name, "Superclass must be a class.");
                }
            }
            environment.Define(stmt.Name.Lexeme, null);
            if (stmt.Superclass != null) {
                environment = new Environment(environment);
                environment.Define("super", superclass);
            }

            Dictionary<string, SharpLoxFunction> methods = new Dictionary<string, SharpLoxFunction>();
            foreach (Stmt.Function method in stmt.Methods) {
                SharpLoxFunction function = new SharpLoxFunction(method, environment, method.Name.Lexeme.Equals("init"));
                methods[method.Name.Lexeme] = function;
            }

            SharpLoxClass klass = new SharpLoxClass(stmt.Name.Lexeme, (SharpLoxClass)superclass, methods);
            if (stmt.Superclass != null) {
                environment = environment.Enclosing;
            }
            environment.Assign(stmt.Name, klass);
            return null;
        }

        public object VisitExpressionStmt(Stmt.Expression stmt) {
            Evaluate(stmt.Expr);
            return null;
        }

        public object VisitFunctionStmt(Stmt.Function stmt) {
            SharpLoxFunction function = new SharpLoxFunction(stmt, environment, false);
            environment.Define(stmt.Name.Lexeme, function);
            return null;
        }

        public object VisitIfStmt(Stmt.If stmt) {
            if (IsTruthy(Evaluate(stmt.Condition))) {
                Execute(stmt.ThenBranch);
            } else if (stmt.ElseBranch != null) {
                Execute(stmt.ElseBranch);
            }
            return null;
        }

        public object VisitImportStmt(Stmt.Import stmt) {
            object module = Evaluate(stmt.Module);
            if (!(module is string)) {
                throw new RuntimeError(stmt.Keyword, "Module name must be a string.");
            }
            string moduleName = (string)module;
            if (moduleName.StartsWith("std:")) {
                string library = moduleName.Split(':')[1];
                if (library.Equals("File")) {
                    SharpLox.Run(StandardLibrary.File);
                } else {
                    throw new RuntimeError(stmt.Keyword, "'" + moduleName + "' is not a standard library module.");
                }
                return null;
            }

            try {
                string source = File.ReadAllText(moduleName);
                SharpLox.Run(source);
            } catch (IOException) {
                throw new RuntimeError(stmt.Keyword, "Could not import module '" + module + "'.");
            }
            return null;
        }

        public object VisitReturnStmt(Stmt.Return stmt) {
            object value = null;
            if (stmt.Value != null) value = Evaluate(stmt.Value);
            throw new Return(value);
        }

        public object VisitVarStmt(Stmt.Var stmt) {
            object value = null;
            if (stmt.Initializer != null) {
                value = Evaluate(stmt.Initializer);
            }
            environment.Define(stmt.Name.Lexeme, value);
            return null;
        }

        public object VisitWhileStmt(Stmt.While stmt) {
            while (IsTruthy(Evaluate(stmt.Condition))) {
                Execute(stmt.Body);
            }
            return null;
        }

        private object Evaluate(Expr expr) {
            return expr.Accept(this);
        }

        private bool IsTruthy(object @object) {
            if (@object == null) return false;
            if (@object is bool b) return b;
            return true;
        }

        private bool IsEqual(object a, object b) {
            if (a == null && b == null) return true;
            if (a == null) return false;
            return a.Equals(b);
        }

        private void CheckNumberOperand(Token @operator, object operand) {
            if (operand is double) return;
            throw new RuntimeError(@operator, "Operand must be a number.");
        }

        private void CheckNumberOperands(Token @operator, object left, object right) {
            if (left is double && right is double) return;
            throw new RuntimeError(@operator, "Operands must be numbers.");
        }

        private string Stringify(object @object) {
            if (@object == null) return "nil";
            if (@object is double) {
                string text = @object.ToString();
                // C# ToString on doubles doesn't strictly add .0 like Java, 
                // but we can just return it. If we want to mimic Java's "remove .0":
                // Java 1.0 -> "1.0" -> "1"
                // C# 1.0 -> "1"
                return text;
            }
            return @object.ToString();
        }

        private int Intify(object @object) {
            if (@object == null) return 0;
            if (@object is double d) return (int)d;
            return 0;
        }
    }


}
