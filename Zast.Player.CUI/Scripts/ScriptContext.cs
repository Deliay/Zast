using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Scripts
{
    public class ScriptContext
    {
        private readonly Dictionary<Type, object> context;

        public ScriptContext()
        {
            this.context = new Dictionary<Type, object>();
        }

        public bool TryGet<T>(out T value)
        {
            var res = context.TryGetValue(typeof(T), out var rawValue);
            if (rawValue != default) value = (T)rawValue!;
            else value = default!;
            return res;
        }

        public void Set<T>(T value)
        {
            context.Add(typeof(T), value!);
        }
    }
}
