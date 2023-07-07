using Zast.AyeRecorder.Script.Config;

namespace Zast.AyeRecorder.Config;

public class RecordConfig 
{
    public int Quality { get; set; }

    public Mode Mode { get; set; }

    public string WorkingDirectory { get; set; }

    public string RoomDirectoryNameFormat { get; set; }

    public string RecordFileNameFormat { get; set; }

    public List<long> RoomIds { get; set; }
}
