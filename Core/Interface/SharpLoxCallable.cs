using SharpLox.Core.Runtime;
using System.Collections.Generic;

namespace SharpLox.Core.Interface {
    public interface ISharpLoxCallable {
        int Arity();
        object Call(Interpreter interpreter, List<object> arguments);
    }


}
