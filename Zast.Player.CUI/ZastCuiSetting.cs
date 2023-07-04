using Zast.Player.CUI.Util;

namespace Zast.Player.CUI
{
    public class ZastCuiSetting
    {
        public readonly static IReadOnlyDictionary<string, (Action<ZastCuiSetting, bool>, Func<ZastCuiSetting, bool>, string)> SettingItems
            = new Dictionary<string, (Action<ZastCuiSetting, bool>, Func<ZastCuiSetting, bool>, string)>()
        {
            //{ "EnabeldWhisper", ((s, v) => s.EnabeldWhisper = v, s => s.EnabeldWhisper, "(实验) 启用Whisper进行实时直播语音识别 [red](不推荐)[/]") },
            { "EnabledAudio", ((s, v) => s.EnabledAudio = v, s => s.EnabledAudio, "(实验) 播放直播语音流 [grey]使用BASS[/]") },
            { "DisableWebExport", ((s, v) => s.DisableWebExport = v, s => s.DisableWebExport, "禁用本地网页弹幕姬") },
        };

        public bool EnabeldWhisper { get; set; }

        public bool EnabledAudio { get; set; }

        public bool DisableWebExport { get; set; }
    }
}