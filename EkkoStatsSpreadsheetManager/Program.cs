using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static EkkoStatsSpreadsheetManager.URLRanks;

namespace EkkoStatsSpreadsheetManager
{
    public static class Program
    {
        public const string ApplicationName = "Ekko Stats Spreadsheet Manager";
        public const string NewPatchSpreadsheetID = "178For5MJYcNMq1LWZIPeEY9DsPfZ1ce-Vz9QShRtQlw";
        public const string PlatinumPlusSpreadsheetID = "1r0xBErHtqJYogyq4H3G52vPKmYiAaJcPOWNtWYTwNyM";
        public const string DiamondPlusSpreadsheetID = "TODO: Add ID";
        public const string MasterPlusSpreadsheetID = "1mj3eQ1XRghP81AlWXbYQcQaGhOReQnABJTXcgWnPSmM";
        public const string AllRanksSpreadsheetID = "TODO: Add ID";

        public static Spreadsheet NewPatchSpreadsheet;
        public static Spreadsheet PlatinumPlusSpreadsheet;
        public static Spreadsheet DiamondPlusSpreadsheet; // not yet
        public static Spreadsheet MasterPlusSpreadsheet;
        public static Spreadsheet AllRanksSpreadsheet; // not yet

        public static SheetManager PlatinumPlusManager;
        public static SheetManager DiamondPlusManager; // not yet
        public static SheetManager MasterPlusManager;
        public static SheetManager AllRanksManager; // not yet

        public static Sheet NewPatchSheet;

        public static SheetsService Service;

        public static string[] Scopes = { SheetsService.Scope.Spreadsheets, SheetsService.Scope.Drive, SheetsService.Scope.DriveFile };

        // Bot stuff
        public static string ClientID, ClientSecret, Token;
        public static ulong ServerID = 245660829054271489;
        public static ulong DailyEkkoStatsID = 628904684182175744;
        public static SocketTextChannel DailyEkkoStatsChannel;
        public static DiscordSocketClient Client;

        public static HashSet<string> DailyAnnouncedRanks = new HashSet<string>
        {
            PlatinumPlus
        };
        public static HashSet<string> BiweeklyAnnouncedRanks = new HashSet<string>
        {
            PlatinumPlus,
            DiamondPlus,
            MasterPlus,
        };

        public static void Main(string[] args)
        {
            InitializeDiscordBot();
            InitializeService();
            InitializeSpreadsheets();
            InitializeSheetManagers();

            Console.WriteLine("Ready for input");

            while (true)
            {
                var line = Console.ReadLine();
                var split = line.Split(' ');
                var command = split[0].ToLower();
                var arguments = split.Skip(1);
                switch (command)
                {
                    case "today":
                    case "t":
                        RetrieveTodayData();
                        break;
                    case "lastpatch":
                    case "lp":
                        RetrieveLastPatchData();
                        break;
                    case "allpreviouspatches":
                    case "app":
                        RetrieveAllPreviousPatchData();
                        break;
                    case "help":
                    case "h":
                        ShowHelp();
                        break;
                }
            }
        }

        public static string GetAppropriateRankName(string rank) => $"{char.ToUpper(rank[0])}{rank.Replace("_plus", "+").Substring(1)}";
        public static void RetrieveData(GamePatch patch, string rank, Spreadsheet spreadsheet, int r, string captionFinalizer, HashSet<string> announcedRanks)
        {
            Console.WriteLine($"Getting {GetAppropriateRankName(rank)} Ekko stats {captionFinalizer} from u.gg");
            var record = DataRetriever.GetStatRecord(rank);
            Console.WriteLine($"Recording the stats in the spreadsheet");

            var values = new List<IList<object>>
            {
                new List<object> { record.Mid.PickRateString,      record.OverallBanRateString, record.Mid.WinRateString,                           $"=C{r + 2} + D{r}" },
                new List<object> { record.Jungle.PickRateString,   null,                        record.Jungle.WinRateString,                        null                },
                new List<object> { $"=C{r} + C{r + 1}",            null,                        $"=(C{r} * E{r} + C{r + 1} * E{r + 1}) / C{r + 2}", null                },
            };

            var request = Service.Spreadsheets.Values.Update(new ValueRange { Values = values, MajorDimension = "ROWS" }, spreadsheet.SpreadsheetId, $"{patch}!C{r}:F{r + 2}");
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();

            Console.WriteLine();

            if (announcedRanks.Contains(rank))
                SendDiscordMessage(record, $"{GetAppropriateRankName(rank)} Ekko stats {captionFinalizer}");
        }
        public static void RetrieveData(GamePatch patch, int startingRow, string captionFinalizer, HashSet<string> announcedRanks)
        {
            RetrieveData(patch, PlatinumPlus, PlatinumPlusSpreadsheet, startingRow, captionFinalizer, announcedRanks);
            RetrieveData(patch, MasterPlus, MasterPlusSpreadsheet, startingRow, captionFinalizer, announcedRanks);
            Console.WriteLine("Completed retrieval");
        }

        public static void RetrieveTodayData()
        {
            var currentPatch = PlatinumPlusManager.Patches.Max;
            var today = DateTime.Now.Date;
            if (currentPatch.LastPatchDay < DateTime.Now.Date) // new patch alert
            {
                currentPatch = currentPatch.GetNextPatch();
                PlatinumPlusManager.CreateNewPatch();
                MasterPlusManager.CreateNewPatch();
            }
            int daysOffset = (today - currentPatch.FirstPatchDay).Days;
            int row = 5 + daysOffset * 3;
            RetrieveData(currentPatch, row, today.ToString("dd/MM/yyyy"), DailyAnnouncedRanks);
        }
        public static void RetrieveLastPatchData()
        {

        }
        public static void RetrieveAllPreviousPatchData()
        {

        }
        public static void ShowHelp()
        {

        }

        public static void SendDiscordMessage(StatRecord record, string caption)
        {
            DailyEkkoStatsChannel.SendMessageAsync(
@$"{caption}
```cs
     Role |  JNG  |  MID  |  OVR
---------------------------------
Pick Rate | {record.Jungle.PickRate,4:N1}  | {record.Mid.PickRate,4:N1}  | {record.OverallPickRate,4:N1}
 Win Rate | {record.Jungle.WinRate:N2} | {record.Mid.WinRate:N2} | {record.OverallWinRate:N2}
 Ban Rate |  N/A  |  N/A  | {record.OverallBanRate,4:N1}
```"
            );
        }

        public static async void InitializeDiscordBot()
        {
            ReadDiscordBotCredentials();

            Client = new DiscordSocketClient();
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            Client.Ready += GetGuildInfo;
        }

        public static async Task GetGuildInfo()
        {
            var guild = Client.GetGuild(ServerID);
            DailyEkkoStatsChannel = guild.GetTextChannel(DailyEkkoStatsID);
        }
        public static void ReadDiscordBotCredentials()
        {
            var lines = File.ReadAllLines("botCredentials.txt");
            foreach (var l in lines)
            {
                var split = l.Split('=');
                switch (split[0])
                {
                    case "clientID":
                        ClientID = split[1];
                        break;
                    case "clientSecret":
                        ClientSecret = split[1];
                        break;
                    case "token":
                        Token = split[1];
                        break;
                }
            }
        }
        public static void InitializeService()
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None
                    //new FileDataStore(credPath, true)
                ).Result;
                Console.WriteLine($"Credential file saved to: {credPath}");
            }

            // Create Google Sheets API service.
            Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public static void InitializeSpreadsheets()
        {
            GetSpreadsheet(ref NewPatchSpreadsheet, NewPatchSpreadsheetID);
            GetSpreadsheet(ref PlatinumPlusSpreadsheet, PlatinumPlusSpreadsheetID);
            GetSpreadsheet(ref MasterPlusSpreadsheet, MasterPlusSpreadsheetID);
        }
        public static void InitializeSheetManagers()
        {
            NewPatchSheet = NewPatchSpreadsheet.Sheets[0];

            GetSheetManager(ref PlatinumPlusManager, PlatinumPlusSpreadsheet);
            GetSheetManager(ref MasterPlusManager, MasterPlusSpreadsheet);

            Console.WriteLine();
        }

        public static void GetSpreadsheet(ref Spreadsheet spreadsheet, string id) => spreadsheet = Service.Spreadsheets.Get(id).Execute();
        public static void GetSheetManager(ref SheetManager sheetManager, Spreadsheet spreadsheet)
        {
            Console.WriteLine();
            Console.WriteLine(spreadsheet.Properties.Title);
            sheetManager = new SheetManager(Service, spreadsheet);
            sheetManager.PatchSheetCreationRequested += CreatePatchSheet;
        }

        public static void CreatePatchSheet(string spreadsheetID, GamePatch patch)
        {
            var request = new CopySheetToAnotherSpreadsheetRequest
            {
                DestinationSpreadsheetId = spreadsheetID,
            };
            Service.Spreadsheets.Sheets.CopyTo(request, NewPatchSpreadsheetID, NewPatchSheet.Properties.SheetId ?? 0);
        }
    }

    public delegate void DiscordMessageSender(StatRecord record, string caption);
}
