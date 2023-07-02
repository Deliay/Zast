using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Scripts
{
    public class ScriptManager
    {
        private readonly ScriptContext context;

        public IScript CurrentScript { get; private set; }

        public ScriptManager()
        {
            this.context = new ScriptContext();
        }

        public async ValueTask RunAsync(IScript script, CancellationToken cancellationToken)
        {
            CurrentScript = script;
            IScript lastScript = null!;
            while (CurrentScript != null)
            {
                var next = await CurrentScript.Show(lastScript, context, cancellationToken);
                lastScript = CurrentScript;
                CurrentScript = next;
            }
        }
    }
}
