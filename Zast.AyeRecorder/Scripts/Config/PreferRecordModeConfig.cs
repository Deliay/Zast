using Spectre.Console;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Script.Config;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Script.Config;

public class PreferRecordModeConfig : IMenuItem
{
    public Type Category => typeof(ConfigScript);

    public string Name => "录制模式";

    public ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]选择优先录制模式，如果无法录制选择的码率，则按B站返回的推荐码率录制。[/]");

        var mode = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .AddChoices(
                "[teal]m3u8[/] [grey]无法录制原始流，但能支持特殊分区，手机、网页端播放使用这种模式[/]",
                "[teal]flv[/] [grey]应该是之前的兼容模式，未来B站应该会逐渐替换这种模式[/]"
            ));

        var config = context.Get<RecordConfig>();
        config.Mode = mode;

        return ValueTask.FromResult(prev);
    }
}
