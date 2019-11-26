using System;

namespace EkkoStatsSpreadsheetManager
{
    public class GamePatch : IComparable<GamePatch>
    {
        public const int DefaultPatchDuration = 14;

        public GamePatchNumber PatchNumber;
        public DateTime FirstPatchDay;
        public DateTime LastPatchDay;

        public GamePatch(GamePatchNumber patchNumber)
            : this(patchNumber, default) { }
        public GamePatch(GamePatchNumber patchNumber, DateTime firstDay, int patchDuration = DefaultPatchDuration)
            : this(patchNumber, firstDay, firstDay.AddDays(patchDuration - 1)) { }
        public GamePatch(GamePatchNumber patchNumber, DateTime firstDay, DateTime lastDay)
        {
            PatchNumber = patchNumber;
            FirstPatchDay = firstDay;
            LastPatchDay = lastDay;
        }

        public int CompareTo(GamePatch other) => PatchNumber.CompareTo(other.PatchNumber);

        public GamePatch GetNextPatch(int patchDuration = DefaultPatchDuration) => new GamePatch(PatchNumber.GetNextPatch(), LastPatchDay.AddDays(1), LastPatchDay.AddDays(patchDuration));
        public GamePatch GetPreviousPatch() => new GamePatch(PatchNumber.GetPreviousPatch(), FirstPatchDay.AddDays(-DefaultPatchDuration), LastPatchDay.AddDays(-1));

        public override string ToString() => PatchNumber.ToString();
        public string ToURLString() => PatchNumber.ToURLString();
    }
}
