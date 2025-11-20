using SharpLox.Core.Parsing;
using SharpLox.Core.Runtime;
using SharpLox.Core.Stdlib;
using System.Collections.Generic;

namespace SharpLox.Core.Scope {
    public class SharpLoxInstance {
        private SharpLoxClass klass;
        private readonly Dictionary<string, object> fields = new Dictionary<string, object>();

        public SharpLoxInstance(SharpLoxClass klass) {
            this.klass = klass;
        }

        public object Get(Token name) {
            if (fields.ContainsKey(name.Lexeme)) {
                return fields[name.Lexeme];
            }

            SharpLoxFunction method = klass.FindMethod(name.Lexeme);
            if (method != null) return method.Bind(this);

            throw new RuntimeError(name, "Undefined property '" + name.Lexeme + "'.");
        }

        public void Set(Token name, object value) {
            fields[name.Lexeme] = value;
        }

        public override string ToString() {
            return klass.Name + " instance";
        }
    }


}
