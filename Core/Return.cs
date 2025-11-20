using System;

namespace SharpLox.Core.Runtime {
    // Used for control flow return
    public class Return : Exception {
        public readonly object Value;

        public Return(object value) {
            Value = value;
        }
    }
}