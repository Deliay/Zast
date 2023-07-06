using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.BuildingBlocks.Scripts;

namespace Zast.Player.CUI.Scripts.Scenes
{
    public class ExitScene : IMenuItem
    {
        public string Name => "退出";
        public Type Category => typeof(MainMenuScene);

        public ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<IScript>(null!);
        }
    }
}
