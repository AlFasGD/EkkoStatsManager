using System.Globalization;
using System.Net;
using static EkkoStatsSpreadsheetManager.URLRoles;

namespace EkkoStatsSpreadsheetManager
{
    public static class DataRetriever
    {
        public const string BaseURL = "https://u.gg/lol/champions/ekko/build";

        public static RoleStatRecord GetRoleStatRecord(string rank, string role) => GetRoleStatRecordFromHTMLCode(GetHTML(rank, role));
        public static RoleStatRecord GetRoleStatRecord(GamePatch patch, string rank, string role) => GetRoleStatRecordFromHTMLCode(GetHTML(patch, rank, role));
        public static float GetBanRate(string rank) => GetBanRateFromHTMLCode(GetHTML(GetURL(rank)));
        public static float GetBanRate(GamePatch patch, string rank) => GetBanRateFromHTMLCode(GetHTML(patch, rank));

        public static StatRecord GetStatRecord(string rank) => new StatRecord(GetRoleStatRecord(rank, Middle), GetRoleStatRecord(rank, Jungle), GetBanRate(rank));
        public static StatRecord GetStatRecord(GamePatch patch, string rank) => new StatRecord(GetRoleStatRecord(patch, rank, Middle), GetRoleStatRecord(patch, rank, Jungle), GetBanRate(patch, rank));

        private static RoleStatRecord GetRoleStatRecordFromHTMLCode(string htmlCode)
        {
            return new RoleStatRecord(GetPercentage(htmlCode, "win-rate"), GetPercentage(htmlCode, "pick-rate"), GetInt(htmlCode, "matches"));
        }
        private static float GetBanRateFromHTMLCode(string htmlCode)
        {
            return GetPercentage(htmlCode, "ban-rate");
        }

        private static float GetPercentage(string htmlCode, string baseDivClassName) => float.Parse(GetValue(htmlCode, baseDivClassName).Replace("%", ""));
        private static int GetInt(string htmlCode, string baseDivClassName) => int.Parse(GetValue(htmlCode, baseDivClassName), NumberStyles.AllowThousands);
        private static string GetValue(string htmlCode, string baseDivClassName)
        {
            var winrateDivStart = htmlCode.IndexOf($"<div class=\"{baseDivClassName}");
            const string valueDivString = "<div class=\"value\">";
            var valueDivStart = htmlCode.IndexOf(valueDivString, winrateDivStart) + valueDivString.Length;
            var valueDivEnd = htmlCode.IndexOf("</div>", valueDivStart);
            return htmlCode.Substring(valueDivStart, valueDivEnd - valueDivStart);
        }

        private static string GetHTML(GamePatch patch, string rank, string role = "jungle") => GetHTML(GetURL(patch, rank, role));
        private static string GetHTML(string rank, string role = "jungle") => GetHTML(GetURL(rank, role));
        private static string GetHTML(string url)
        {
            using var client = new WebClient();
            return client.DownloadString(url);
        }
        private static string GetURL(string rank, string role = "jungle") => $"{BaseURL}?rank={rank}&role={role}";
        private static string GetURL(GamePatch patch, string rank, string role = "jungle") => $"{BaseURL}?rank={rank}&role={role}&patch={patch.ToURLString()}";
    }
}
