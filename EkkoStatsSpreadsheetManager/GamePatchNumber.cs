using System;
using static System.Convert;

namespace EkkoStatsSpreadsheetManager
{
    public struct GamePatchNumber : IComparable<GamePatchNumber>
    {
        public const int SeasonPatchCount = 24;

        public int Major;
        public int Minor;
        public string AdditionalCharacters;

        public GamePatchNumber(int major, int minor, string additionalCharacters = "")
        {
            Major = major + (minor - 1) / SeasonPatchCount;
            Minor = (minor - 1) % SeasonPatchCount + 1;
            AdditionalCharacters = additionalCharacters;
        }

        public int CompareTo(GamePatchNumber other)
        {
            int comparison = Major.CompareTo(other.Major);
            if (comparison == 0)
            {
                comparison = Minor.CompareTo(other.Minor);
                if (comparison == 0)
                    return AdditionalCharacters.CompareTo(other.AdditionalCharacters);
            }
            return comparison;
        }

        public GamePatchNumber GetNextPatch() => new GamePatchNumber(Major, Minor + 1);
        public GamePatchNumber GetPreviousPatch() => new GamePatchNumber(Major, Minor - 1);

        public static bool operator >(GamePatchNumber left, GamePatchNumber right)
        {
            bool comparison = left.Major > right.Major;
            if (!comparison && left.Major == right.Major)
            {
                comparison = left.Minor > right.Minor;
                if (!comparison && left.Minor == right.Minor)
                    comparison = string.Compare(left.AdditionalCharacters, right.AdditionalCharacters) > 0;
            }
            return comparison;
        }
        public static bool operator <(GamePatchNumber left, GamePatchNumber right)
        {
            bool comparison = left.Major < right.Major;
            if (!comparison && left.Major == right.Major)
            {
                comparison = left.Minor < right.Minor;
                if (!comparison && left.Minor == right.Minor)
                    comparison = string.Compare(left.AdditionalCharacters, right.AdditionalCharacters) < 0;
            }
            return comparison;
        }
        public static bool operator ==(GamePatchNumber left, GamePatchNumber right) => left.Major == right.Major && left.Minor == right.Minor && left.AdditionalCharacters == right.AdditionalCharacters;
        public static bool operator !=(GamePatchNumber left, GamePatchNumber right) => left.Major != right.Major || left.Minor != right.Minor || left.AdditionalCharacters != right.AdditionalCharacters;

        public static GamePatchNumber Parse(string patch)
        {
            int i = 0;
            while (!char.IsDigit(patch[^(i + 1)]))
                i++;
            string additional = i > 0 ? patch.Substring(patch.Length - i, i) : "";
            string numberPatch = patch.Substring(0, patch.Length - i);
            var numbers = numberPatch.Split('.');
            return new GamePatchNumber(ToInt32(numbers[0]), ToInt32(numbers[1]), additional);
        }

        public string ToURLString() => $"{Major}_{Minor}{AdditionalCharacters}";
        public override string ToString() => $"{Major}.{Minor}{AdditionalCharacters}";
        public override bool Equals(object obj) => obj is GamePatchNumber number && this == number;
        public override int GetHashCode() => HashCode.Combine(Major, Minor, AdditionalCharacters);
    }
}
