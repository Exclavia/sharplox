using SharpLox.Core.Interface;
using SharpLox.Core.Runtime;
using SharpLox.Core.Scope;
using System.Collections.Generic;

namespace SharpLox.Core.Stdlib {
    public class SharpLoxFunction : ISharpLoxCallable {
        private readonly Stmt.Function declaration;
        private readonly Environment closure;
        private readonly bool isInitializer;

        public SharpLoxFunction(Stmt.Function declaration, Environment closure, bool isInitializer) {
            this.isInitializer = isInitializer;
            this.closure = closure;
            this.declaration = declaration;
        }

        public SharpLoxFunction Bind(SharpLoxInstance instance) {
            Environment environment = new Environment(closure);
            environment.Define("this", instance);
            return new SharpLoxFunction(declaration, environment, isInitializer);
        }

        public override string ToString() {
            return "<fn " + declaration.Name.Lexeme + ">";
        }

        public int Arity() {
            return declaration.Params.Count;
        }

        public object Call(Interpreter interpreter, List<object> arguments) {
            Environment environment = new Environment(closure);
            for (int i = 0; i < declaration.Params.Count; i++) {
                environment.Define(declaration.Params[i].Lexeme, arguments[i]);
            }
            try {
                interpreter.ExecuteBlock(declaration.Body, environment);
            } catch (Return returnValue) {
                if (isInitializer) return closure.GetAt(0, "this");
                return returnValue.Value;
            }
            if (isInitializer) return closure.GetAt(0, "this");
            return null;
        }
    }


}
