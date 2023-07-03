using Zast.Player.CUI.Util;

namespace Zast.Player.CUI.Bilibili
{
    public struct CookieInfo
    {
        public string Name { get; set; }

        public long Uid { get; set; }

        public string Cookie { get; set; }
    }

    public class CookieStore : JsonRepository<CookieInfo>
    {
        public CookieStore() : base("cookie.json")
        {
        }
    }
}