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
            return new RoleStatRecord(GetPercentage(htmlCode, "<div class=\"win-rate"), GetPercentage(htmlCode, "<div class=\"pick-rate\">"));
        }
        private static float GetBanRateFromHTMLCode(string htmlCode)
        {
            return GetPercentage(htmlCode, "<div class=\"ban-rate\">");
        }

        private static float GetPercentage(string htmlCode, string baseDivInitialization)
        {
            var winrateDivStart = htmlCode.IndexOf(baseDivInitialization);
            const string valueDivString = "<div class=\"value\">";
            var percentageDivStart = htmlCode.IndexOf(valueDivString, winrateDivStart) + valueDivString.Length;
            var percentageDivEnd = htmlCode.IndexOf('%', percentageDivStart);
            return float.Parse(htmlCode.Substring(percentageDivStart, percentageDivEnd - percentageDivStart));
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
