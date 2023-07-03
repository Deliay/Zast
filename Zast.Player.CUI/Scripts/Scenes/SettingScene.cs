using Spectre.Console;

namespace Zast.Player.CUI.Scripts.Scenes
{
    public class SettingScene : IMenuItem
    {
        private readonly ZastCuiSettingRepository settingRepository;

        public SettingScene(ZastCuiSettingRepository settingRepository)
        {
            this.settingRepository = settingRepository;
        }

        public Type Category => typeof(MainMenuScene);

        public string Name => "设置...";

        public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine();
            var setting = await settingRepository.Load(cancellationToken) ?? new ZastCuiSetting();

            var prompt = new MultiSelectionPrompt<KeyValuePair<string, (Action<ZastCuiSetting, bool>, Func<ZastCuiSetting, bool>, string)>>()
                .AddChoices(ZastCuiSetting.SettingItems)
                .UseConverter(pair => pair.Value.Item3)
                .NotRequired();

            foreach (var alreadySelected in ZastCuiSetting.SettingItems.Where(s => s.Value.Item2(setting)))
            {
                prompt.Select(alreadySelected);
            }

            var newlySelected = AnsiConsole.Prompt(prompt).ToHashSet();

            foreach (var option in ZastCuiSetting.SettingItems)
            {
                option.Value.Item1(setting, newlySelected.Contains(option));
            }

            await settingRepository.Save(setting, cancellationToken);

            return prev;
        }
    }
}