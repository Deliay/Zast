using Zast.AyeRecorder.Script.Config;

namespace Zast.AyeRecorder.Config;

public class RecordConfig 
{
    public int Quality { get; set; }

    public Mode Mode { get; set; }

    public string RecordFileNameFormat { get; set; }

    public HashSet<long> RoomIds { get; set; }

    public static RecordConfig Default() => new()
    {
        Quality = 10000,
        Mode = Mode.m3u8_hls_ts_h264,
        RecordFileNameFormat = "{{roomId}}-{{recordingAt}}-{{title}}-{{quality}}",
    };
}
