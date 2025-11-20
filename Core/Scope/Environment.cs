using SharpLox.Core.Parsing;
using SharpLox.Core.Runtime;
using System.Collections.Generic;

namespace SharpLox.Core.Scope {
    public class Environment {
        public readonly Environment? Enclosing;
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public Environment() {
            Enclosing = null;
        }

        public Environment(Environment enclosing) {
            Enclosing = enclosing;
        }

        public object Get(Token name) {
            if (values.ContainsKey(name.Lexeme)) {
                return values[name.Lexeme];
            }
            if (Enclosing != null) return Enclosing.Get(name);

            throw new RuntimeError(name, "Undefined variable '" + name.Lexeme + "'.");
        }

        public void Assign(Token name, object value) {
            if (values.ContainsKey(name.Lexeme)) {
                values[name.Lexeme] = value;
                return;
            }
            if (Enclosing != null) {
                Enclosing.Assign(name, value);
                return;
            }
            throw new RuntimeError(name, "Undefined variable '" + name.Lexeme + "'.");
        }

        public void Define(string name, object value) {
            values[name] = value;
        }

        public Environment Ancestor(int distance) {
            Environment environment = this;
            for (int i = 0; i < distance; i++) {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                environment = environment.Enclosing;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            return environment;
        }

        public object GetAt(int distance, string name) {
            // Use TryGetValue or check ContainsKey to safely get value, assuming correct resolution
            if (Ancestor(distance).values.TryGetValue(name, out object val))
                return val;
            return null;
        }

        public void AssignAt(int distance, Token name, object value) {
            Ancestor(distance).values[name.Lexeme] = value;
        }
    }


}
