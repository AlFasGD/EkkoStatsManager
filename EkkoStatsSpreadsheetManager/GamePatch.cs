using System;

namespace EkkoStatsSpreadsheetManager
{
    public class GamePatch : IComparable<GamePatch>
    {
        public GamePatchNumber PatchNumber;
        public DateTime FirstPatchDay;
        public DateTime LastPatchDay;

        public GamePatch(GamePatchNumber patchNumber, DateTime firstDay, DateTime lastDay)
        {
            PatchNumber = patchNumber;
            FirstPatchDay = firstDay;
            LastPatchDay = lastDay;
        }
        public GamePatch(GamePatchNumber patchNumber, DateTime firstDay, int patchDuration = 14)
            : this(patchNumber, firstDay, firstDay.AddDays(patchDuration)) { }

        public int CompareTo(GamePatch other) => PatchNumber.CompareTo(other.PatchNumber);

        public GamePatch GetNextPatch(int patchDuration = 14) => new GamePatch(PatchNumber.GetNextPatch(), LastPatchDay.AddDays(1), LastPatchDay.AddDays(patchDuration));

        public override string ToString() => PatchNumber.ToString();
        public string ToURLString() => PatchNumber.ToURLString();
    }
}
