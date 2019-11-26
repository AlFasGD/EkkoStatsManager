namespace EkkoStatsSpreadsheetManager
{
    public struct RoleStatRecord
    {
        public float WinRate;
        public float PickRate;
        public MatchesCount Matches;

        public float WeightedWinRate => WinRate * PickRate;

        public string WinRateString => $"{WinRate:N2}%";
        public string PickRateString => $"{PickRate:N1}%";

        public RoleStatRecord(float winRate, float pickRate, int matches) => (WinRate, PickRate, Matches) = (winRate, pickRate, matches);
    }
}
