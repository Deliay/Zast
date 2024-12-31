using Zast.AyeRecorder.Scripts.Config;

namespace Zast.AyeRecorder.Config;

public class RecordConfig 
{
    public int Quality { get; set; } = 10000;

    public Mode Mode { get; set; } = Mode.streaming;

    public string RecordFileNameFormat { get; set; } = "{{roomId}}-{{recordingAt}}-{{title}}-{{quality}}";

    public HashSet<long> RoomIds { get; set; } = new();
    
    public string? Cookie { get; set; }

    public static RecordConfig Default() => new()
    {
        Quality = 10000,
        Mode = Mode.streaming,
        RecordFileNameFormat = "{{roomId}}-{{recordingAt}}-{{title}}-{{quality}}",
        RoomIds = new(),
    };
}
