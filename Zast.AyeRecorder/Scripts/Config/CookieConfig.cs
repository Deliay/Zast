using Spectre.Console;
using Zast.AyeRecorder.Config;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Scripts.Config;

public class CookieConfig(RecordConfigRepository configRepository) : IMenuItem
{
    public Type Category { get; } = typeof(CookieConfig);
    public string Name => "登录...";
    public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
    {
        var config = await configRepository.Load(cancellationToken) ?? RecordConfig.Default();

        var cookie = AnsiConsole.Prompt(new TextPrompt<string>("请粘贴您的cookie:"));
        
        config.Cookie = cookie;
        
        await configRepository.Save(config, cancellationToken);

        return prev;
    }

}