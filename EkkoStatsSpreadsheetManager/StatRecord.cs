namespace EkkoStatsSpreadsheetManager
{
    public class StatRecord
    {
        public RoleStatRecord Mid, Jungle;
        public float OverallBanRate;

        public float OverallPickRate => Mid.PickRate + Jungle.PickRate;
        public float OverallWinRate => (Mid.WeightedWinRate + Jungle.WeightedWinRate) / OverallPickRate;
        public float OverallPresenceRate => OverallPickRate + OverallBanRate;

        public string OverallBanRateString => $"{OverallBanRate:N1}%";

        public StatRecord(RoleStatRecord mid, RoleStatRecord jungle, float overallBanRate) => (Mid, Jungle, OverallBanRate) = (mid, jungle, overallBanRate);
    }
}
