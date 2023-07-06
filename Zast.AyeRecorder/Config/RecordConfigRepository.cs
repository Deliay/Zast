using Zast.BuildingBlocks.Util;

namespace Zast.AyeRecorder.Config;

public class RecordConfigRepository : JsonRepository<RecordConfig>
{
    public RecordConfigRepository() : base("setting.json")
    {
    }
}