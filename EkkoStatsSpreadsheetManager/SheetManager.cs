using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EkkoStatsSpreadsheetManager
{
    public class SheetManager
    {
        private SortedSet<GamePatch> patches;

        public SheetsService Service;
        public Spreadsheet Spreadsheet;

        public SortedSet<GamePatch> Patches => new SortedSet<GamePatch>(patches);

        public event Action<string, GamePatch, bool> PatchSheetCreationRequested;

        public SheetManager(SheetsService service, Spreadsheet spreadsheet)
        {
            Service = service;
            Spreadsheet = spreadsheet;
            GetPatches();
        }

        public void CreateNewPatch(int patchDuration = 14) => CreateNewPatch(patches.Max.GetNextPatch(patchDuration));
        public void CreateNewPatch(GamePatch patch, bool isOlderPatch = false)
        {
            patches.Add(patch);
            PatchSheetCreationRequested?.Invoke(Spreadsheet.SpreadsheetId, patch, isOlderPatch);
        }

        private void GetPatches()
        {
            patches = new SortedSet<GamePatch>();
            foreach (var s in Spreadsheet.Sheets)
            {
                var number = GamePatchNumber.Parse(s.Properties.Title);
                var values = Service.Spreadsheets.Values.Get(Spreadsheet.SpreadsheetId, $"{s.Properties.Title}!A5:A").Execute();

                Console.Write($"\n{number}");

                var patch = new GamePatch(number);
                patches.Add(patch);

                if (values.Values == null)
                    continue;

                var firstDay = DateTime.MaxValue;
                var lastDay = DateTime.MinValue;
                foreach (var d in values.Values)
                {
                    if (d.Count == 0)
                        continue;

                    var day = DateTime.ParseExact(d[0].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    if (day < firstDay)
                        firstDay = day;
                    if (day > lastDay)
                        lastDay = day;
                }
                patch.FirstPatchDay = firstDay;
                patch.LastPatchDay = lastDay;
                Console.Write($": {firstDay:dd/MM/yyyy} - {lastDay:dd/MM/yyyy}");
            }
            Console.WriteLine();
        }
    }
}
