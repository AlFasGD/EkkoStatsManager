using System;

namespace EkkoStatsSpreadsheetManager
{
    public struct MatchesCount
    {
        public int Count;

        public MatchesCount(int count) => Count = count;

        public static implicit operator int(MatchesCount count) => count.Count;
        public static implicit operator MatchesCount(int count) => new MatchesCount(count);

        public override string ToString() => Count.ToString();
        public string ToCompactString()
        {
            if (Count >= 1000000)
                return $"{GetCompactValueRepresentation(1000000)}m";
            if (Count >= 10000)
                return $"{GetCompactValueRepresentation(1000)}k";
            return $"{Count,4} ";
        }
        private string GetCompactValueRepresentation(int divisor)
        {
            return ((double)Count / divisor).ToString($"N{2 - (int)Math.Log10(Count / divisor)},4");
        }
    }
}
