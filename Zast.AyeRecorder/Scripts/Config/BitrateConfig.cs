using Spectre.Console;
using Zast.AyeRecorder.Script.Config;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Script.Config;

public class BitrateConfig : IMenuItem
{
    public Type Category => typeof(ConfigScript);

    public string Name => "码率设置";

    public static string Convert(int qn) => qn switch 
    {
        10000 => "原画",
        80 => "流畅",
        150 => "高清",
        250 => "超清",
        400 => "蓝光",
        20000 => "4K",
        30000 => "杜比",
        _ => throw new InvalidDataException(),

    } + $"({qn})";

    public ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]选择优先录制码率，如果无法录制选择的码率，则按B站返回的推荐码率录制。[/]");
        var qn = AnsiConsole.Prompt(new SelectionPrompt<int>()
            .AddChoices(10000, 80, 150, 250, 400, 20000, 30000)
            .UseConverter(Convert));

        return ValueTask.FromResult(prev);
    }
}
