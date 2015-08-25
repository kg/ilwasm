using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wasm.Module {
    [AttributeUsage(AttributeTargets.Method)]
    public class ExportAttribute : Attribute {
        public readonly string Name;

        public ExportAttribute (string name = null) {
            Name = name;
        }
    }
}
