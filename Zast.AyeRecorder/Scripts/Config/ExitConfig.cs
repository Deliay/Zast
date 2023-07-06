using Zast.AyeRecorder.Script;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Config;

public class ExitConfig : IMenuItem
{
    public Type Category => typeof(ConfigScript);

    public string Name => "退出...";

    public ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
    {
        Console.Clear();
        return ValueTask.FromResult<IScript>(default!);
    }
}