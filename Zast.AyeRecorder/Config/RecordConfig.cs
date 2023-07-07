using Zast.AyeRecorder.Script.Config;

namespace Zast.AyeRecorder.Config;

public class RecordConfig 
{
    public int Quality { get; set; } = 10000;

    public Mode Mode { get; set; } = Mode.m3u8_hls_ts_h264;

    public string RecordFileNameFormat { get; set; } = "{{roomId}}-{{recordingAt}}-{{title}}-{{quality}}";

    public HashSet<long> RoomIds { get; set; } = new();

    public static RecordConfig Default() => new()
    {
        Quality = 10000,
        Mode = Mode.m3u8_hls_ts_h264,
        RecordFileNameFormat = "{{roomId}}-{{recordingAt}}-{{title}}-{{quality}}",
        RoomIds = new(),
    };
}
