using Discord;
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
        public const string DefaultPatchSpreadsheetID = "178For5MJYcNMq1LWZIPeEY9DsPfZ1ce-Vz9QShRtQlw";
        public const string PlatinumPlusSpreadsheetID = "1r0xBErHtqJYogyq4H3G52vPKmYiAaJcPOWNtWYTwNyM";
        public const string DiamondPlusSpreadsheetID = "1u2ZWN8nHh0gaqXwLyGeWQtIJNYxUo21iVQoR-p0c3ec";
        public const string MasterPlusSpreadsheetID = "1mj3eQ1XRghP81AlWXbYQcQaGhOReQnABJTXcgWnPSmM";
        public const string AllRanksSpreadsheetID = "1UjbTiUwulfqisKGa5NWOBBaRpoqeHZ8xFECdIrXSv88";

        public static Spreadsheet DefaultPatchSpreadsheet;
        public static Spreadsheet PlatinumPlusSpreadsheet;
        public static Spreadsheet DiamondPlusSpreadsheet;
        public static Spreadsheet MasterPlusSpreadsheet;
        public static Spreadsheet AllRanksSpreadsheet;

        public static SheetManager PlatinumPlusManager;
        public static SheetManager DiamondPlusManager;
        public static SheetManager MasterPlusManager;
        public static SheetManager AllRanksManager;

        public static Sheet NewPatchSheet;
        public static Sheet OlderPatchSheet;

        public static SheetsService Service;

        public static string[] Scopes = { SheetsService.Scope.Spreadsheets };

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
            AllRanks
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
                    case "all":
                        RetrieveAllPreviousPatchData();
                        break;
                    case "help":
                    case "h":
                        ShowHelp();
                        break;
                    case "exit":
                        return;
                }
            }
        }

        public static string GetAppropriateRankName(string rank) => $"{char.ToUpper(rank[0])}{rank.Replace("_plus", "+").Substring(1)}";
        public static void RetrieveData(GamePatch patch, string rank, SheetManager manager, int r, string captionFinalizer, HashSet<string> announcedRanks, bool retrieveTotalMatchCount = false, bool isFinalizerBeforeStats = false)
        {
            Console.WriteLine($"Getting {GetAppropriateRankName(rank)} Ekko {GetOrderedCaption()} from u.gg");
            try
            {
                var record = DataRetriever.GetStatRecord(patch, rank);
                Console.WriteLine($"Recording the stats in the spreadsheet");

                var spreadsheet = manager.Spreadsheet;

                if (!manager.Patches.Contains(patch))
                    manager.CreateNewPatch(patch, true);

                var values = new List<IList<object>>
                {
                    new List<object> { record.Mid.PickRateString,    record.OverallBanRateString, record.Mid.WinRateString,                           $"=C{r + 2} + D{r}" },
                    new List<object> { record.Jungle.PickRateString, null,                        record.Jungle.WinRateString,                        null                },
                    new List<object> { $"=C{r} + C{r + 1}",          null,                        $"=(C{r} * E{r} + C{r + 1} * E{r + 1}) / C{r + 2}", null                },
                };

                var request = Service.Spreadsheets.Values.Update(new ValueRange { Values = values, MajorDimension = "ROWS" }, spreadsheet.SpreadsheetId, $"{patch}!C{r}:F{r + 2}");
                request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                request.Execute();

                if (retrieveTotalMatchCount)
                {
                    var matchesValues = new List<IList<object>>
                    {
                        new List<object> { record.Mid.Matches    },
                        new List<object> { record.Jungle.Matches },
                        new List<object> { $"=G{r} + G{r + 1}"   },
                    };

                    request = Service.Spreadsheets.Values.Update(new ValueRange { Values = matchesValues, MajorDimension = "ROWS" }, spreadsheet.SpreadsheetId, $"{patch}!G{r}:G{r + 2}");
                    request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    request.Execute();
                }

                WriteLineWithColor("Successfully retrieved information", ConsoleColor.Green);

                if (announcedRanks.Contains(rank))
                    SendDiscordMessage(record, $"{GetAppropriateRankName(rank)} Ekko {GetOrderedCaption()}");

                WriteLineWithColor("", ConsoleColor.Gray);
            }
            catch (Exception e)
            {
                WriteLineWithColor($"Failed to retrieve patch {patch} information", ConsoleColor.Red);
                WriteLineWithColor($"Exception: {e.Message}", ConsoleColor.Red);
                WriteLineWithColor("", ConsoleColor.Gray);
            }

            string GetOrderedCaption() => isFinalizerBeforeStats ? $"{captionFinalizer} stats" : $"stats {captionFinalizer}";
        }
        public static void RetrieveData(GamePatch patch, int startingRow, string captionFinalizer, HashSet<string> announcedRanks, bool retrieveTotalMatchCount = false, bool isFinalizerBeforeStats = false)
        {
            RetrieveData(patch, PlatinumPlus, PlatinumPlusManager, startingRow, captionFinalizer, announcedRanks, retrieveTotalMatchCount, isFinalizerBeforeStats);
            RetrieveData(patch, DiamondPlus, DiamondPlusManager, startingRow, captionFinalizer, announcedRanks, retrieveTotalMatchCount, isFinalizerBeforeStats);
            RetrieveData(patch, MasterPlus, MasterPlusManager, startingRow, captionFinalizer, announcedRanks, retrieveTotalMatchCount, isFinalizerBeforeStats);
            RetrieveData(patch, AllRanks, AllRanksManager, startingRow, captionFinalizer, announcedRanks, retrieveTotalMatchCount, isFinalizerBeforeStats);
            Console.WriteLine("Completed retrieval");
        }

        public static void RetrieveTodayData()
        {
            Client.SetGameAsync("today's Ekko stats", type: ActivityType.Watching);

            var currentPatch = PlatinumPlusManager.Patches.Max;
            var today = DateTime.Now.Date;
            if (currentPatch.LastPatchDay < today) // new patch alert
            {
                currentPatch = currentPatch.GetNextPatch();
                PlatinumPlusManager.CreateNewPatch();
                MasterPlusManager.CreateNewPatch();
            }
            int daysOffset = (today - currentPatch.FirstPatchDay).Days;
            int row = 5 + daysOffset * 3;
            RetrieveData(currentPatch, row, today.ToString("dd/MM/yyyy"), DailyAnnouncedRanks);

            Client.SetGameAsync(null);
        }
        public static void RetrieveLastPatchData()
        {
            Client.SetGameAsync("last patch's Ekko stats", type: ActivityType.Watching);

            int index = PlatinumPlusManager.Patches.Count - 1;
            var today = DateTime.Now.Date;
            while (PlatinumPlusManager.Patches.ElementAt(index).FirstPatchDay > today)
                index--;
            var lastPatch = PlatinumPlusManager.Patches.ElementAt(index);
            RetrieveData(lastPatch, 2, lastPatch.PatchNumber.ToString(), BiweeklyAnnouncedRanks, true, true);

            Client.SetGameAsync(null);
        }
        public static void RetrieveAllPreviousPatchData()
        {
            var currentPatch = PlatinumPlusManager.Patches.Max;
            int lastAvailableMinor = PlatinumPlusManager.Patches.Max.PatchNumber.Minor - 4;
            while (currentPatch.PatchNumber.Minor > lastAvailableMinor)
            {
                currentPatch = currentPatch.GetPreviousPatch();
                Client.SetGameAsync($"patch {currentPatch} Ekko stats", type: ActivityType.Watching);
                RetrieveData(currentPatch, 2, currentPatch.PatchNumber.ToString(), new HashSet<string>(), true, true);
            }

            Client.SetGameAsync(null);
        }
        public static void ShowHelp()
        {
            WriteCommandHelp("today", "t", "Retrieves today's u.gg info about Platinum+, Diamond+, Master+ and All Ranks.");
            WriteCommandHelp("lastpatch", "lp", "Retrieves last patch's u.gg info about Platinum+, Diamond+, Master+ and All Ranks.");
            WriteCommandHelp("allavailablepatches", "app, all", "Retrieves last 4 patches' u.gg info about Platinum+, Diamond+, Master+ and All Ranks.");
            WriteCommandHelp("help", "h", "Shows this help message.");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteCommandHelp(string command, string commandAlts, string info)
        {
            WriteWithColor(command, ConsoleColor.Yellow);
            WriteWithColor($" [{commandAlts}]", ConsoleColor.Magenta);
            WriteWithColor(":", ConsoleColor.White);
            WriteLineWithColor($" {info}", ConsoleColor.Red);
        }

        public static void WriteWithColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
        }
        public static void WriteLineWithColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
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

            WriteLineWithColor("Sent Discord Message", ConsoleColor.Green);
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

        // TODO: Uncomment when sheets are created
        public static void InitializeSpreadsheets()
        {
            GetSpreadsheet(ref DefaultPatchSpreadsheet, DefaultPatchSpreadsheetID);
            GetSpreadsheet(ref PlatinumPlusSpreadsheet, PlatinumPlusSpreadsheetID);
            GetSpreadsheet(ref DiamondPlusSpreadsheet, DiamondPlusSpreadsheetID);
            GetSpreadsheet(ref MasterPlusSpreadsheet, MasterPlusSpreadsheetID);
            GetSpreadsheet(ref AllRanksSpreadsheet, AllRanksSpreadsheetID);
        }
        public static void InitializeSheetManagers()
        {
            NewPatchSheet = DefaultPatchSpreadsheet.Sheets.Where(s => s.Properties.Title == "Default New Patch Sheet").First();
            OlderPatchSheet = DefaultPatchSpreadsheet.Sheets.Where(s => s.Properties.Title == "Default Older Patch Sheet").First();

            GetSheetManager(ref PlatinumPlusManager, PlatinumPlusSpreadsheet);
            GetSheetManager(ref DiamondPlusManager, DiamondPlusSpreadsheet);
            GetSheetManager(ref MasterPlusManager, MasterPlusSpreadsheet);
            GetSheetManager(ref AllRanksManager, AllRanksSpreadsheet);

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

        public static void CreatePatchSheet(string spreadsheetID, GamePatch patch, bool isOlderPatch)
        {
            var request = new CopySheetToAnotherSpreadsheetRequest
            {
                DestinationSpreadsheetId = spreadsheetID,
            };
            var result = Service.Spreadsheets.Sheets.CopyTo(request, DefaultPatchSpreadsheetID, (isOlderPatch ? OlderPatchSheet : NewPatchSheet).Properties.SheetId ?? 0).Execute();
            result.Title = patch.ToString();
            if (!isOlderPatch)
                result.Index = 0;

            Service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request
                    {
                        UpdateSheetProperties = new UpdateSheetPropertiesRequest
                        {
                            Properties = result,
                            Fields = "*",
                        }
                    }
                },
            }, spreadsheetID).Execute();
        }
    }

    public delegate void DiscordMessageSender(StatRecord record, string caption);
}
