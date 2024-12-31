using Spectre.Console;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Script;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Scripts.Config;

public partial class PreferModeConfig : IMenuItem
{
    public Type Category => typeof(ConfigScript);

    public string Name => "录制模式";


    private static string Convert(Mode mode)
    {
        return mode switch
        {
            Mode.m3u8_hls_ts_h265 => "[teal]m3u8 - hls - ts - h265[/]",
            Mode.m3u8_hls_ts_h264 => "[teal]m3u8 - hls - ts - h264[/]",
            Mode.m3u8_hls_fmp4_h265 => "[teal]m3u8 - hls - fmp4 - h265[/]",
            Mode.m3u8_hls_fmp4_h264 => "[teal]m3u8 - hls - fmp4 - h264[/]",
            Mode.streaming => "[teal]streaming - flv - h264[/] [grey]注：这个选项是之前的兼容模式，可以录制到原始流，未来B站应该会逐渐替换这种模式[/]",
            _ => throw new InvalidDataException(),
        };
    }

    public ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]录制模式优先级（优先高码率）：选择的模式 -> streaming -> ts+h265->h264 -> fmp4+h265->h264[/]");
        AnsiConsole.MarkupLine("[red]注：为保证录制完整及兼容性，ts/fmp4等录制封装格式总是输出为flv[/]");
        AnsiConsole.MarkupLine("[red]注：flv并不能直接扔进pr中剪辑，但却能比mp4等格式拥有更好的存储可靠性。[/]");

        Mode mode = AnsiConsole.Prompt(new SelectionPrompt<Mode>()
            .AddChoices(Enum.GetValues<Mode>())
            .UseConverter(Convert));

        var config = context.Get<RecordConfig>();
        config.Mode = mode;

        return ValueTask.FromResult(prev);
    }
}
