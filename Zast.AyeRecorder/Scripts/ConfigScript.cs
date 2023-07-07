using Spectre.Console;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Script.Config;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Script;

public class ConfigScript : IScript
{
    private readonly IEnumerable<IMenuItem> configMenuItems;
    private readonly RecordConfigRepository configRepository;

    public ConfigScript(
        IEnumerable<IMenuItem> configMenuItems,
        RecordConfigRepository configRepository
    )
    {
        this.configMenuItems = configMenuItems.Where(item => typeof(ConfigScript) == item.Category);
        this.configRepository = configRepository;
    }

    public string Name => "config";

    public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
    {
        if (!context.TryGet<RecordConfig>(out var setting))
        {
            context.Set(await configRepository.Load(cancellationToken) ?? new RecordConfig
            {
                Quality = 10000,
                Mode = Mode.m3u8_hls_ts_h264,
            });
        }
        else
        {
            await configRepository.Save(context.Get<RecordConfig>(), cancellationToken);
        }

        Console.Clear();

        AnsiConsole.Write(new Panel(new FigletText("Aye Recorder").Centered().Color(Color.Aqua)));
        AnsiConsole.MarkupLine("[yellow]设置会立即生效[/]");

        return AnsiConsole.Prompt(new SelectionPrompt<IMenuItem>()
            .Title("设置...")
            .AddChoices(configMenuItems)
            .UseConverter(c => c.Name));
    }
}
