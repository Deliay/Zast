using Zast.Player.CUI.Util;

namespace Zast.Player.CUI
{
    public class ZastCuiSettingRepository : JsonRepository<ZastCuiSetting>
    {
        public ZastCuiSettingRepository() : base("setting.json")
        {
        }
    }
}