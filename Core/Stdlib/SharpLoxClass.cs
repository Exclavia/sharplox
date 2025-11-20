using SharpLox.Core.Interface;
using SharpLox.Core.Runtime;
using SharpLox.Core.Scope;
using System.Collections.Generic;

namespace SharpLox.Core.Stdlib {
    public class SharpLoxClass : ISharpLoxCallable {
        public readonly string Name;
        public readonly SharpLoxClass Superclass;
        private readonly Dictionary<string, SharpLoxFunction> methods;

        public SharpLoxClass(string name, SharpLoxClass superclass, Dictionary<string, SharpLoxFunction> methods) {
            Superclass = superclass;
            Name = name;
            this.methods = methods;
        }

        public SharpLoxFunction FindMethod(string name) {
            if (methods.ContainsKey(name)) {
                return methods[name];
            }
            if (Superclass != null) {
                return Superclass.FindMethod(name);
            }
            return null;
        }

        public override string ToString() {
            return Name;
        }

        public object Call(Interpreter interpreter, List<object> arguments) {
            SharpLoxInstance instance = new SharpLoxInstance(this);
            SharpLoxFunction initializer = FindMethod("init");
            if (initializer != null) {
                initializer.Bind(instance).Call(interpreter, arguments);
            }
            return instance;
        }

        public int Arity() {
            SharpLoxFunction initializer = FindMethod("init");
            if (initializer == null) return 0;
            return initializer.Arity();
        }
    }


}
