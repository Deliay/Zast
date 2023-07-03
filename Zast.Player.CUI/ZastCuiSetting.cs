using Zast.Player.CUI.Util;

namespace Zast.Player.CUI
{
    public class ZastCuiSetting
    {
        public static List<(Action<bool, ZastCuiSetting>, string)> GetSettingItems(ZastCuiSetting setting)
        {
            return new()
            {
                ((v, s) => s.EnabeldWhisper = v, "启用Whisper进行实时直播语音识别 [red](不推荐，不稳定，效果差)[/]"),
                ((v, s) => s.EnabledAudio = v, "播放直播语音流 [grey]使用BASS[/]")
            };
        }

        public bool EnabeldWhisper { get; set; }

        public bool EnabledAudio { get; set; }
    }
}