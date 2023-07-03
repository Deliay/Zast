namespace Zast.Player.CUI.Scripts.Scenes
{
    public class SettingScene : IMenuItem
    {
        public Type Category => typeof(MainMenuScene);

        public string Name => "设置...";

        public ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(prev);
        }
    }
}