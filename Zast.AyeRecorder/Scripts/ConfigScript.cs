using Spectre.Console;
using Zast.AyeRecorder.Config;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Script;

public class ConfigScript : IScript
{
    private readonly IEnumerable<IMenuItem> _configMenuItems;
    private readonly RecordConfigRepository _configRepository;

    public ConfigScript(
        IEnumerable<IMenuItem> configMenuItems,
        RecordConfigRepository configRepository
    )
    {
        this._configMenuItems = configMenuItems.Where(item => typeof(ConfigScript) == item.Category);
        this._configRepository = configRepository;
    }

    public string Name => "config";

    public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
    {
        if (!context.TryGet<RecordConfig>(out var setting))
        {
            context.Set(await _configRepository.Load(cancellationToken) ?? RecordConfig.Default());
        }
        else
        {
            await _configRepository.Save(context.Get<RecordConfig>(), cancellationToken);
        }

        Console.Clear();

        AnsiConsole.Write(new Panel(new FigletText("Aye Recorder").Centered().Color(Color.Aqua)));
        AnsiConsole.MarkupLine("[yellow]设置会立即生效[/]");

        return AnsiConsole.Prompt(new SelectionPrompt<IMenuItem>()
            .Title("设置...")
            .AddChoices(_configMenuItems)
            .UseConverter(c => c.Name));
    }
}
