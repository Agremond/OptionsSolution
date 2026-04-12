using DatabaseOperations;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using MathNet.Numerics.Statistics;
using QuikSharp;
using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GrokOptions
{
    public partial class Instrument : DevExpress.XtraEditors.XtraForm
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        public TelegramBotClient botClient = new TelegramBotClient("5088131315:AAEtWSZz4McGorHm6eqOtxxxCYM2AwJK-og");
        public ChatId telegramChatId = 0;
        

        bool updatedRow = false;
        public Series Series = null;
        Position pos;
        double tmp_cost = 0;
        public Tool ba = null;
        double current_strike = 0;
        double ba_lastprice = 0;
        int current_strike_Id = 0;
        double strike_step = 0;
        int STRIKES_ON_DESK = 0;
        TimeSpan worktime0_start = new TimeSpan(10, 00, 00);
        TimeSpan worktime0_end = new TimeSpan(13, 45, 00);
        TimeSpan worktime1_start = new TimeSpan(14, 05, 00);
        TimeSpan worktime1_end = new TimeSpan(18, 45, 00);
        TimeSpan worktime2_start = new TimeSpan(19, 05, 00);
        TimeSpan worktime2_end = new TimeSpan(23, 45, 00);
        DatabaseManager db;
        List<OptionBoard> Option_board = new List<OptionBoard>();
        List<Strategy> Strategies = new List<Strategy>();
        List<Strategy> Portfolio = new List<Strategy>();
        List<Position> positions = new List<Position>();
        List<Strike> strikes;
        List<double> cost_matrix;
        BindingSource bsOptionBoard = new BindingSource();
        BindingSource bsStrategies = new BindingSource();
        BindingSource bsPortfolio = new BindingSource();
        BindingSource bsPositions = new BindingSource();
        BindingSource bsSelectedStrategy = new BindingSource();

        DataTable tableOptionBoard = new DataTable();
        DataTable tableStrategies = new DataTable();
        DataTable tablePortfolio = new DataTable();
        DataTable tablePositions = new DataTable();
        DataTable tableSelectedStrategy = new DataTable();
        DataTable tableChart0 = new DataTable();

        int selected_strategy_Id = -1;
        // Statistical calculations
        const int numBuckets = 75;
        int sampleSize;
        double[] p = new double[numBuckets];
        double stdDev;
        DescriptiveStatistics statistics;
        string seriesTableName;
        Histogram histogram;
        double[] deltaDistribution = new double[] { 30, -4130, 2910, 680, -1040, 1280, -7280, -5000, 4260, 2340, 1610, 1160, -6650, -820, 3010, -2000, 1680, -2690, 1660, -5050, -850, -6200, -1110, 2910, 160, 2870, 2830, -1550, 60, 270, 170, 1600, 4630, 2130, 280, -150, 870, 1710, -110, -190, -2240, 90, 3500, -3120, 3140, -2360, -1390, 3340, -1790, 2330, 6120, 3300, 3780, 2150, -1820, -7140, 5310, 2940, -2920, -2620, 1170, -1320, 180, -12320, 2560, -440, -1850, 4640, 1540, -1910, -1320, 1940, -5800, -3190, 1800, 4870, 1460, -3890, 1810, -620, -4640, -3190, -1120, 2620, -1720, 1420, 5280, 4180, -1660, -2660, 2130, -3120, 1790, 2350, -2930, 980, 490, -1340, 10, -3240, -3370, 2840, 4800, 1850, 3040, 2780, -1400, -4200, 2580, 410, -870, -1100, 4950, -2990, 1320, 3000, 460, -1000, -1220, 2710, 950, 1450, 3160, 1610, 4870, -10, 2140, -960, -1860, -440, -480, -3920, -5780, 3180, -250, 5790, 940, 2240, -2360, -4680, 3000, 1780, 5600, 1490, 6520, -5130, 1670, -1430, -750, 6480, 2640, 1960, 6980, 1020, -1630, -4590, -990, 110, -30, -14550, -7810, -35930, -11180, 14220, 2510, 13790, -9760, 3830, 2270, -910, -1610, 10300, 3570, 2680, -5140, 110, 2340, -1380, 40, 210, 3530, -2370, 5060, 4070, -4180, -1870, -3770, -3380, 3480, -5930, -980, -640, -2440, 2130, -6600, 8390, 6750, 2420, 4360, 3050, 5940, 2440, -4180, 12760, -4370, -7040, 2150, 4790, -260, 350, -340, 2190, -720, -7060, 4180, -2350, 2910, 2920, 800, 4290, 1910, 50, 4820, 4140, 1140, -520, -690, -210, -3350, 550, -1350, 4000, 1220, 1540, -2620, -680, 9930, 370, 520, 340, 1480, 8420, 4270, -1570, 1150, -3110, -7260, -10300, 1960, -5670, -3900, -1190, 1510, -5740, 590, -9570, -4040, 1770 };

        string[] SpreadsNames = new string[] { "CallButterfly", "PutButterfly", "CallCondor", "PutCondor" };

        public Instrument(Series _series, Tool _ba, DatabaseManager _db)
        {
            InitializeComponent();
            Series = _series;
            ba = _ba;
            db = _db;


            Init();
          
        }

        void Init()
        {
            if (Series == null || ba == null)
                return;
            if (Series.Strikes?.Count > 2)
                strike_step = Math.Abs(Series.Strikes[0].strike - Series.Strikes[1].strike);
           

            STRIKES_ON_DESK = Series.Strikes?.Count ?? 0;
            gridOptionBoard.DataSource = bsOptionBoard;
            gridStrategies.DataSource = bsStrategies;
            gridSelectedStrategy.DataSource = bsSelectedStrategy;

            tableStrategies = InitStrategies("Strategies");
            tablePortfolio = InitPortfolio("Portfolio");
            tableSelectedStrategy = InitSelectedStrategy("SelectedStrategy");

            string tablename = ba.SecurityCode + Series?.ExpDate;
            tableOptionBoard = InitOptionBoard(tablename);
            bsOptionBoard.DataSource = tableOptionBoard;
            bsStrategies.DataSource = tableStrategies;
            bsSelectedStrategy.DataSource = tableSelectedStrategy;
            bsPortfolio.DataSource = tablePortfolio;

            if (!strategyChart.Series.Any(s => s.Name == "Series1"))
            {
                strategyChart.Series.Add(new DevExpress.XtraCharts.Series("Series1", DevExpress.XtraCharts.ViewType.Line));
                strategyChart.Series["Series1"].ArgumentDataMember = "X";
                strategyChart.Series["Series1"].ValueDataMembers.AddRange(new string[] { "Y" });
            }
            if (!strategyChart.Series.Any(s => s.Name == "Series2"))
            {
                strategyChart.Series.Add(new DevExpress.XtraCharts.Series("Series2", DevExpress.XtraCharts.ViewType.Line));
                strategyChart.Series["Series2"].ArgumentDataMember = "X";
                strategyChart.Series["Series2"].ValueDataMembers.AddRange(new string[] { "P_Y" });
            }
            if (!strategyChart.Series.Any(s => s.Name == "Series3"))
            {
                strategyChart.Series.Add(new DevExpress.XtraCharts.Series("Series3", DevExpress.XtraCharts.ViewType.Line));
                strategyChart.Series["Series3"].ArgumentDataMember = "X";
                strategyChart.Series["Series3"].ValueDataMembers.AddRange(new string[] { "Count" });
            }
            if (!strategyChart.Series.Any(s => s.Name == "BaseActive"))
            {
                strategyChart.Series.Add(new DevExpress.XtraCharts.Series("BaseActive", DevExpress.XtraCharts.ViewType.Line));
                strategyChart.Series["BaseActive"].ArgumentDataMember = "X";
                strategyChart.Series["BaseActive"].ValueDataMembers.AddRange(new string[] { "Y" });
            }

            statistics = new DescriptiveStatistics(deltaDistribution);
            sampleSize = deltaDistribution.Length;
            stdDev = statistics.StandardDeviation;
            histogram = new Histogram(deltaDistribution, numBuckets);
            viewStrategies.Columns["Id"].Visible = false;
            viewSelectedStrategy.Columns["str_Id"].Visible = false;
            gridStrategies.ContextMenuStrip = ctsAddToPortfolio;


            seriesTableName = ba.Name + Series.ExpDate.Replace(".", "_");
            CreateDatabase(seriesTableName);

            //botClient.OnError += OnError;
            //botClient.OnMessage += OnMessage;
            //botClient.OnUpdate += OnUpdate;

            //NotifyTelegram(null, $"Instrument initialized: {seriesTableName}");
            
            
        }
        private void CreateDatabase(string dbname)
        {
           
            try
            {
                string createTableQuery = string.Format(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{0}')
                CREATE TABLE {0} (
                    DateTime DATETIME2,
                    PutCode VARCHAR(50),
                    PutTeor FLOAT,
                    PutBid FLOAT,
                    PutAsk FLOAT,
                    PutBidIV FLOAT,
                    PutAskIV FLOAT,
                    Strike INT,
                    CallCode VARCHAR(50),
                    CallTeor FLOAT,
                    CallBid FLOAT,
                    CallAsk FLOAT,
                    CallBidIV FLOAT,
                    CallAskIV FLOAT,
                    Delta FLOAT,
                    Gamma FLOAT,
                    Theta FLOAT,
                    Vega FLOAT,
                    BALastPrice FLOAT,
                    CentralStrike INT
                );", dbname);

                db.ExecuteQuery(createTableQuery);
              
             
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving to database: {ex.Message}");
            }
        }



        //private async void NotifyTelegram(Strategy strategy, string messageType)
        //{
        //    if (strategy == null)
        //        return;

        //    try
        //    {
        //        string message = $"[{messageType}] Strategy: {strategy.Name} (ID: {strategy.Id})\n";
        //        message += $"Cost: {strategy.Cost:C2}, Profit: {strategy.Profit:C2}, P/L: {strategy.PlanPL:F2}\n";
        //        message += $"PoP: {strategy.ProbabilityOfProfit:F2}%, Mean: {strategy.Mean:F1}\n";
        //        message += $"Greeks - Delta: {strategy.Delta:F2}, Gamma: {strategy.Gamma:F2}, Theta: {strategy.Tetta:F2}, Vega: {strategy.Vega:F2}\n";
        //        message += "Members:\n";
        //        foreach (var member in strategy.Members)
        //        {
        //            message += $"{member.EntranceOrderQty}x {(member.IsFutures ? "Futures" : member.Option?.type)} {(member.IsFutures ? member.SecurityCode : member.Option?.Strike)}\n";
        //        }

        //        await SendMessage(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Failed to send Telegram message: {ex.Message}");
        //    }
        //}

        private void UpdateChart()
        {
            if (selected_strategy_Id < 0)
                return;

            List<Strategy> list_strategies = checkPortfolio.Checked ? Portfolio : Strategies;
            if (selected_strategy_Id >= list_strategies.Count)
                return;

            Strategy selectedStrategy = list_strategies[selected_strategy_Id];
            strategyChart.Series["Series1"].Points.Clear();
            strategyChart.Series["Series2"].Points.Clear();
            strategyChart.Series["Series3"].Points.Clear();

            foreach (DataRow row in selectedStrategy.ExpProfile.Rows)
            {
                double x = Convert.ToDouble(row["X"]);
                double y = Convert.ToDouble(row["Y"]);
                double p_y = Convert.ToDouble(row["P_Y"]);
                int count = Convert.ToInt32(row["Count"]);
                strategyChart.Series["Series1"].Points.AddPoint(x, y);
                strategyChart.Series["Series2"].Points.AddPoint(x, p_y);
                strategyChart.Series["Series3"].Points.AddPoint(x, count);
            }
        }

        private double getLongButterflyProfit(int middleStrike, int leftStrike)
        {
            return Math.Abs(middleStrike - leftStrike);
        }

        private double getLongButterflyCost(double leftPrice, double middlePrice, double rightPrice)
        {
            return leftPrice - 2 * middlePrice + rightPrice;
        }

        private double getLongCondorProfit(int rightMidStrike, int leftMidStrike)
        {
            return Math.Abs(rightMidStrike - leftMidStrike);
        }

        private double getLongCondorLoss(double leftAsk, double leftMidBid, double rightMidBid, double rightAsk)
        {
            return leftAsk - leftMidBid - rightMidBid + rightAsk;
        }

        private void timerRun_Tick(object sender, EventArgs e)
        {
            Run();
            
        }

        async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine(exception);
            await Task.Delay(2000, cts.Token);
        }

        //async Task OnMessage(Telegram.Bot.Types.Message msg, UpdateType type)
        //{
        //    var me = await botClient.GetMe();
        //    telegramChatId = msg.Chat;
        //    if (msg.Text is not { } text)
        //        Console.WriteLine($"Received a message of type {msg.Type}");
        //    else if (text.StartsWith("/"))
        //    {
        //        var space = text.IndexOf(' ');
        //        if (space < 0) space = text.Length;
        //        var command = text[..space].ToLower();
        //        if (command.LastIndexOf('@') is > 0 and int at) // it's a targeted command
        //            if (command[(at + 1)..].Equals(me.Username, StringComparison.OrdinalIgnoreCase))
        //                command = command[..at];
        //            else
        //                return; // command was not targeted at me
        //        await OnCommand(command, text[space..].TrimStart(), msg);
        //    }
        //    else
        //        await OnTextMessage(msg);
        //}


        //public async Task OnTextMessage(Telegram.Bot.Types.Message msg) // received a text message that is not a command
        //{
        //    await OnCommand("/start", "", msg); // for now we redirect to command /start
        //}

        //async Task OnCommand(string command, string args, Telegram.Bot.Types.Message msg)
        //{
        //    Console.WriteLine($"Received command: {command} {args}");
        //    switch (command)
        //    {
        //        case "/start":
        //            await botClient.SendMessage(msg.Chat, "Бот активирован!\n", parseMode: ParseMode.Html, linkPreviewOptions: true,

        //                replyMarkup: new ReplyKeyboardRemove()); // also remove keyboard to clean-up things
        //            telegramChatId = msg.Chat;

        //            break;

        //    }
        //}

        //public async Task SendMessage(string message)
        //{
        //    if (telegramChatId == null)
        //        return;
        //    await botClient.SendMessage(telegramChatId, message, parseMode: ParseMode.Html, linkPreviewOptions: true,

        //                   replyMarkup: new ReplyKeyboardRemove()); // also remove keyboard to clean-up things
        //}

        //async Task OnUpdate(Update update)
        //{
        //    switch (update)
        //    {
        //        case { CallbackQuery: { } callbackQuery }: await OnCallbackQuery(callbackQuery); break;
        //        case { PollAnswer: { } pollAnswer }: await OnPollAnswer(pollAnswer); break;
        //        default: Console.WriteLine($"Received unhandled update {update.Type}"); break;
        //    };
        //}

        //async Task OnCallbackQuery(CallbackQuery callbackQuery)
        //{
        //    await botClient.AnswerCallbackQuery(callbackQuery.Id, $"You selected {callbackQuery.Data}");
        //    await botClient.SendMessage(callbackQuery.Message!.Chat, $"Received callback from inline button {callbackQuery.Data}");
        //}

        //async Task OnPollAnswer(PollAnswer pollAnswer)
        //{
        //    if (pollAnswer.User != null)
        //        await botClient.SendMessage(pollAnswer.User.Id, $"You voted for option(s) id 1");
        //}

        void Run()
        {
            try
            {
                UpdateBaseAssetPrice();
                RefreshOptionBoard();
                
                label_lastprice.Text = ba_lastprice.ToString();
                if (tableOptionBoard.Rows.Count != 0)
                    label_currentstrike.Text = tableOptionBoard.Rows[current_strike_Id-1]["Strike"].ToString();

                strikes = Series?.Strikes ?? new List<Strike>();

                if (checkFindButterflyes.Checked)
                    FindButterflies();

                if (checkFindCondors.Checked)
                    FindCondors();

                UpdateStrategies();

                if (checkButTrackCondor.Checked && strikes.Count > 4)
                    TrackSpecialCondor();

                UpdatePortfolio();

                RefreshStrategies();
                RefreshPortfolio();
                viewStrategies.RefreshData();
                FillSelectedStrategyTable();

                if (selected_strategy_Id >= 0)
                {
                    UpdateChart();
                }
                strategyChart.Series["BaseActive"].Points.Clear();
                strategyChart.Series["BaseActive"].Points.AddPoint(ba_lastprice, -10000);
                strategyChart.Series["BaseActive"].Points.AddPoint(ba_lastprice, 10000);

                

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Run: {ex.Message}");
             //   NotifyTelegram(null, $"Error in Run: {ex.Message}");
            }

        }

        private void UpdateBaseAssetPrice()
        {
            ba_lastprice = (double)(ba?.LastPrice ?? 0);
        }

        private void FindButterflies()
        {
            if (strikes == null || STRIKES_ON_DESK < 3)
                return;

            cost_matrix = new List<double>();
            Parallel.For(0, STRIKES_ON_DESK - 2, i0 =>
            {
                double leftASKCall, leftASKPut, leftBidCall, leftBidPut;
                double mIdASKCall, mIdASKPut, mIdBidCall, mIdBidPut;
                double rightASKCall, rightASKPut, rightBidCall, rightBidPut;
                double costCall, profitCall;
                int leftStrike, mIdStrike, rightStrike;
                Strike left_strike_obj, mId_strike_obj, right_strike_obj;

                lock (tableOptionBoard)
                {
                    leftStrike = (int)tableOptionBoard.Rows[i0]["Strike"];
                    left_strike_obj = strikes.FirstOrDefault(n => n.strike == leftStrike);
                    if (left_strike_obj == null) return;

                    leftASKCall = CalculateAdjustedAsk(leftStrike, (double)tableOptionBoard.Rows[i0]["CallAsk"], (double)tableOptionBoard.Rows[i0]["PutAsk"], true);
                    leftBidCall = CalculateAdjustedBid(leftStrike, (double)tableOptionBoard.Rows[i0]["CallBid"], (double)tableOptionBoard.Rows[i0]["PutBid"], true);
                    leftASKPut = CalculateAdjustedAsk(leftStrike, (double)tableOptionBoard.Rows[i0]["CallAsk"], (double)tableOptionBoard.Rows[i0]["PutAsk"], false);
                    leftBidPut = CalculateAdjustedBid(leftStrike, (double)tableOptionBoard.Rows[i0]["CallBid"], (double)tableOptionBoard.Rows[i0]["PutBid"], false);

                    for (int i1 = i0 + 1; i1 < STRIKES_ON_DESK - 1; i1++)
                    {
                        mIdStrike = (int)tableOptionBoard.Rows[i1]["Strike"];
                        mId_strike_obj = strikes.FirstOrDefault(n => n.strike == mIdStrike);
                        if (mId_strike_obj == null) continue;

                        mIdASKCall = CalculateAdjustedAsk(mIdStrike, (double)tableOptionBoard.Rows[i1]["CallAsk"], (double)tableOptionBoard.Rows[i1]["PutAsk"], true);
                        mIdBidCall = CalculateAdjustedBid(mIdStrike, (double)tableOptionBoard.Rows[i1]["CallBid"], (double)tableOptionBoard.Rows[i1]["PutBid"], true);
                        mIdASKPut = CalculateAdjustedAsk(mIdStrike, (double)tableOptionBoard.Rows[i1]["CallAsk"], (double)tableOptionBoard.Rows[i1]["PutAsk"], false);
                        mIdBidPut = CalculateAdjustedBid(mIdStrike, (double)tableOptionBoard.Rows[i1]["CallBid"], (double)tableOptionBoard.Rows[i1]["PutBid"], false);

                        for (int i2 = i1 + 1; i2 < STRIKES_ON_DESK; i2++)
                        {
                            double Vleft = 2;
                            double VmId = 4;
                            double Vright = 2;
                            rightStrike = (int)tableOptionBoard.Rows[i2]["Strike"];
                            right_strike_obj = strikes.FirstOrDefault(n => n.strike == rightStrike);
                            if (right_strike_obj == null) continue;

                            rightASKCall = CalculateAdjustedAsk(rightStrike, (double)tableOptionBoard.Rows[i2]["CallAsk"], (double)tableOptionBoard.Rows[i2]["PutAsk"], true);
                            rightBidCall = CalculateAdjustedBid(rightStrike, (double)tableOptionBoard.Rows[i2]["CallBid"], (double)tableOptionBoard.Rows[i2]["PutBid"], true);
                            rightASKPut = CalculateAdjustedAsk(rightStrike, (double)tableOptionBoard.Rows[i2]["CallAsk"], (double)tableOptionBoard.Rows[i2]["PutAsk"], false);
                            rightBidPut = CalculateAdjustedBid(rightStrike, (double)tableOptionBoard.Rows[i2]["CallBid"], (double)tableOptionBoard.Rows[i2]["PutBid"], false);

                            List<double> local_cost_matrix = new List<double>();
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKCall, mIdBidCall, rightASKCall, Vleft, VmId, Vright));
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKPut, mIdBidPut, rightASKPut, Vleft, VmId, Vright));
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKPut, mIdBidPut / 2 + mIdBidCall / 2, rightASKCall, Vleft, VmId, Vright));
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKCall, mIdBidPut / 2 + mIdBidCall / 2, rightASKPut, Vleft, VmId, Vright));
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKPut, mIdBidCall, rightASKCall, Vleft, VmId, Vright));
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKPut, mIdBidCall, rightASKPut, Vleft, VmId, Vright));
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKCall, mIdBidPut, rightASKPut, Vleft, VmId, Vright));
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKCall, mIdBidPut, rightASKCall, Vleft, VmId, Vright));
                            local_cost_matrix.Add(CalculateButterflyCostVariant(leftASKPut, mIdBidPut, rightASKCall, Vleft, VmId, Vright));

                            costCall = local_cost_matrix.Max();
                            int strategy_type = local_cost_matrix.IndexOf(costCall);
                            profitCall = Math.Round(getLongButterflyProfit(mIdStrike, leftStrike) * (double)(ba.StepPrice / ba.Step), 0);

                            if (costCall > -1000)
                            {
                                Strategy temp_strategy = CreateButterflyStrategy(strategy_type, left_strike_obj, mId_strike_obj, right_strike_obj, Vleft, VmId, Vright, leftStrike, mIdStrike, rightStrike, profitCall, costCall);
                                if (temp_strategy != null)
                                {
                                    lock (Strategies)
                                    {
                                        int str_Id = Strategies.FindIndex(s => s.Id.Contains(temp_strategy.Id));
                                        if (str_Id == -1)
                                        {
                                            if (Strategies.Count < 100)
                                            {
                                                Strategies.Add(temp_strategy);
                                                str_Id = Strategies.Count - 1;
                                                Strategies[str_Id].MinStrike = Math.Min(Math.Min(leftStrike, mIdStrike), rightStrike);
                                                Strategies[str_Id].MaxStrike = Math.Max(Math.Max(leftStrike, mIdStrike), rightStrike);
                                                Strategies[str_Id].ProbabilityOfProfit = CalculateProbabilityOfProfit(Strategies[str_Id]);
                                            }
                                        }
                                        else
                                        {
                                            Strategies[str_Id].PlanPL = (costCall < 0) ? Math.Round((profitCall + costCall) / Math.Abs(costCall), 2) : 999;
                                            Strategies[str_Id].CalculateCost();
                                            Strategies[str_Id].Profit = profitCall + Strategies[str_Id].Cost;
                                            Strategies[str_Id].ProbabilityOfProfit = CalculateProbabilityOfProfit(Strategies[str_Id]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        private void FindCondors()
        {
            if (strikes == null || STRIKES_ON_DESK < 4)
                return;

            cost_matrix = new List<double>();
            Parallel.For(0, STRIKES_ON_DESK - 3, i0 =>
            {
                double leftASKCall, leftASKPut, leftBidCall, leftBidPut;
                double leftMidASKCall, leftMidASKPut, leftMidBidCall, leftMidBidPut;
                double rightMidASKCall, rightMidASKPut, rightMidBidCall, rightMidBidPut;
                double rightASKCall, rightASKPut, rightBidCall, rightBidPut;
                double cost, profit;
                int leftStrike, leftMidStrike, rightMidStrike, rightStrike;
                Strike left_strike_obj, left_mid_strike_obj, right_mid_strike_obj, right_strike_obj;

                lock (tableOptionBoard)
                {
                    leftStrike = (int)tableOptionBoard.Rows[i0]["Strike"];
                    left_strike_obj = strikes.FirstOrDefault(n => n.strike == leftStrike);
                    if (left_strike_obj == null) return;

                    leftASKCall = CalculateAdjustedAsk(leftStrike, (double)tableOptionBoard.Rows[i0]["CallAsk"], (double)tableOptionBoard.Rows[i0]["PutAsk"], true);
                    leftBidCall = CalculateAdjustedBid(leftStrike, (double)tableOptionBoard.Rows[i0]["CallBid"], (double)tableOptionBoard.Rows[i0]["PutBid"], true);
                    leftASKPut = CalculateAdjustedAsk(leftStrike, (double)tableOptionBoard.Rows[i0]["CallAsk"], (double)tableOptionBoard.Rows[i0]["PutAsk"], false);
                    leftBidPut = CalculateAdjustedBid(leftStrike, (double)tableOptionBoard.Rows[i0]["CallBid"], (double)tableOptionBoard.Rows[i0]["PutBid"], false);

                    for (int i1 = i0 + 1; i1 < STRIKES_ON_DESK - 2; i1++)
                    {
                        leftMidStrike = (int)tableOptionBoard.Rows[i1]["Strike"];
                        left_mid_strike_obj = strikes.FirstOrDefault(n => n.strike == leftMidStrike);
                        if (left_mid_strike_obj == null) continue;

                        leftMidASKCall = CalculateAdjustedAsk(leftMidStrike, (double)tableOptionBoard.Rows[i1]["CallAsk"], (double)tableOptionBoard.Rows[i1]["PutAsk"], true);
                        leftMidBidCall = CalculateAdjustedBid(leftMidStrike, (double)tableOptionBoard.Rows[i1]["CallBid"], (double)tableOptionBoard.Rows[i1]["PutBid"], true);
                        leftMidASKPut = CalculateAdjustedAsk(leftMidStrike, (double)tableOptionBoard.Rows[i1]["CallAsk"], (double)tableOptionBoard.Rows[i1]["PutAsk"], false);
                        leftMidBidPut = CalculateAdjustedBid(leftMidStrike, (double)tableOptionBoard.Rows[i1]["CallBid"], (double)tableOptionBoard.Rows[i1]["PutBid"], false);

                        for (int i2 = i1 + 1; i2 < STRIKES_ON_DESK - 1; i2++)
                        {
                            rightMidStrike = (int)tableOptionBoard.Rows[i2]["Strike"];
                            right_mid_strike_obj = strikes.FirstOrDefault(n => n.strike == rightMidStrike);
                            if (right_mid_strike_obj == null) continue;

                            rightMidASKCall = CalculateAdjustedAsk(rightMidStrike, (double)tableOptionBoard.Rows[i2]["CallAsk"], (double)tableOptionBoard.Rows[i2]["PutAsk"], true);
                            rightMidBidCall = CalculateAdjustedBid(rightMidStrike, (double)tableOptionBoard.Rows[i2]["CallBid"], (double)tableOptionBoard.Rows[i2]["PutBid"], true);
                            rightMidASKPut = CalculateAdjustedAsk(rightMidStrike, (double)tableOptionBoard.Rows[i2]["CallAsk"], (double)tableOptionBoard.Rows[i2]["PutAsk"], false);
                            rightMidBidPut = CalculateAdjustedBid(rightMidStrike, (double)tableOptionBoard.Rows[i2]["CallBid"], (double)tableOptionBoard.Rows[i2]["PutBid"], false);

                            for (int i3 = i2 + 1; i3 < STRIKES_ON_DESK; i3++)
                            {
                                double Vleft = 1, VleftMid = 1, VrightMid = 1, Vright = 1;
                                rightStrike = (int)tableOptionBoard.Rows[i3]["Strike"];
                                right_strike_obj = strikes.FirstOrDefault(n => n.strike == rightStrike);
                                if (right_strike_obj == null) continue;

                                rightASKCall = CalculateAdjustedAsk(rightStrike, (double)tableOptionBoard.Rows[i3]["CallAsk"], (double)tableOptionBoard.Rows[i3]["PutAsk"], true);
                                rightBidCall = CalculateAdjustedBid(rightStrike, (double)tableOptionBoard.Rows[i3]["CallBid"], (double)tableOptionBoard.Rows[i3]["PutBid"], true);
                                rightASKPut = CalculateAdjustedAsk(rightStrike, (double)tableOptionBoard.Rows[i3]["CallAsk"], (double)tableOptionBoard.Rows[i3]["PutAsk"], false);
                                rightBidPut = CalculateAdjustedBid(rightStrike, (double)tableOptionBoard.Rows[i3]["CallBid"], (double)tableOptionBoard.Rows[i3]["PutBid"], false);

                                List<double> local_cost_matrix = new List<double>();
                                local_cost_matrix.Add(getLongCondorLoss(leftASKCall, leftMidBidCall, rightMidBidCall, rightASKCall));
                                local_cost_matrix.Add(getLongCondorLoss(leftASKPut, leftMidBidPut, rightMidBidPut, rightASKPut));

                                cost = local_cost_matrix.Max();
                                int strategy_type = local_cost_matrix.IndexOf(cost);
                                profit = Math.Round(getLongCondorProfit(rightMidStrike, leftMidStrike) * (double)(ba.StepPrice / ba.Step), 0);

                                if (cost > -1000)
                                {
                                    Strategy temp_strategy = CreateCondorStrategy(strategy_type, left_strike_obj, left_mid_strike_obj, right_mid_strike_obj, right_strike_obj, Vleft, VleftMid, VrightMid, Vright, leftStrike, leftMidStrike, rightMidStrike, rightStrike, profit, cost);
                                    if (temp_strategy != null)
                                    {
                                        lock (Strategies)
                                        {
                                            int str_Id = Strategies.FindIndex(s => s.Id.Contains(temp_strategy.Id));
                                            if (str_Id == -1)
                                            {
                                                if (Strategies.Count < 100)
                                                {
                                                    Strategies.Add(temp_strategy);
                                                    str_Id = Strategies.Count - 1;
                                                    Strategies[str_Id].MinStrike = Math.Min(Math.Min(leftStrike, leftMidStrike), Math.Min(rightMidStrike, rightStrike));
                                                    Strategies[str_Id].MaxStrike = Math.Max(Math.Max(leftStrike, leftMidStrike), Math.Max(rightMidStrike, rightStrike));
                                                    Strategies[str_Id].ProbabilityOfProfit = CalculateProbabilityOfProfit(Strategies[str_Id]);
                                                }
                                            }
                                            else
                                            {
                                                Strategies[str_Id].PlanPL = (cost < 0) ? Math.Round((profit + cost) / Math.Abs(cost), 2) : 999;
                                                Strategies[str_Id].CalculateCost();
                                                Strategies[str_Id].Profit = profit + Strategies[str_Id].Cost;
                                                Strategies[str_Id].ProbabilityOfProfit = CalculateProbabilityOfProfit(Strategies[str_Id]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        private double CalculateAdjustedAsk(int strike, double callAsk, double putAsk, bool isCall)
        {
            if (double.IsNaN(callAsk) || double.IsNaN(putAsk))
                return 0;
            if (strike >= ba_lastprice)
            {
                return isCall ? callAsk : putAsk - Math.Abs(strike - ba_lastprice);
            }
            else
            {
                return isCall ? callAsk - Math.Abs(strike - ba_lastprice) : putAsk;
            }
        }

        private double CalculateAdjustedBid(int strike, double callBid, double putBid, bool isCall)
        {
            if (double.IsNaN(callBid) || double.IsNaN(putBid))
                return 0;
            if (strike >= ba_lastprice)
            {
                return isCall ? callBid : putBid - Math.Abs(strike - ba_lastprice);
            }
            else
            {
                return isCall ? callBid - Math.Abs(strike - ba_lastprice) : putBid;
            }
        }

        private double CalculateButterflyCostVariant(double left, double mid, double right, double Vleft, double VmId, double Vright)
        {
            if (left > 0 && mid > 0 && right > 0)
                return Math.Round(getLongButterflyCost(Vleft * left, VmId * mid, Vright * right) * (double)(ba.StepPrice / ba.Step), 0);
            return -9999999;
        }

        private Strategy CreateButterflyStrategy(int strategy_type, Strike left_strike_obj, Strike mId_strike_obj, Strike right_strike_obj, double Vleft, double VmId, double Vright, int leftStrike, int mIdStrike, int rightStrike, double profitCall, double costCall)
        {
            if (left_strike_obj == null || mId_strike_obj == null || right_strike_obj == null)
                return null;

            Strategy temp_strategy = new Strategy();
            temp_strategy.Name = "LongButterfly";
            temp_strategy.ExpDate = Series?.ExpDate ?? "";
            temp_strategy.Id = "L" + leftStrike.ToString() + "S" + mIdStrike.ToString() + "R" + rightStrike.ToString() + temp_strategy.Name;

            double accountSize = 100000;
            double riskTolerance = 0.02;
            int maxContracts = CalculatePositionSize(accountSize, riskTolerance, costCall);

            if (maxContracts == 0)
                return null;

            switch (strategy_type)
            {
                case 0:
                    temp_strategy.Name += "Call";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Call, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Call, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Call, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    break;
                case 1:
                    temp_strategy.Name += "Put";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Put, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Put, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Put, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    break;
                case 2:
                    temp_strategy.Name += "Iron";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Put, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Put, Operation.Sell, Math.Min((int)VmId / 2, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Call, Operation.Sell, Math.Min((int)VmId / 2, maxContracts));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Call, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    break;
                case 3:
                    temp_strategy.Name += "GutIron";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Call, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Call, Operation.Sell, Math.Min((int)VmId / 2, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Put, Operation.Sell, Math.Min((int)VmId / 2, maxContracts));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Put, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    break;
                case 4:
                    temp_strategy.Name += "CallSynLeft";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Put, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Call, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Call, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    AddFuturesPositionToStrategy(temp_strategy, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    break;
                case 5:
                    temp_strategy.Name += "PutSynMId";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Put, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Call, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Put, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    AddFuturesPositionToStrategy(temp_strategy, Operation.Buy, Math.Min((int)VmId, maxContracts * 2));
                    break;
                case 6:
                    temp_strategy.Name += "CallSynRight";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Call, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Call, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Put, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    AddFuturesPositionToStrategy(temp_strategy, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    break;
                case 7:
                    temp_strategy.Name += "PutSynLeft";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Call, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Put, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Put, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    AddFuturesPositionToStrategy(temp_strategy, Operation.Sell, Math.Min((int)Vleft, maxContracts));
                    break;
                case 8:
                    temp_strategy.Name += "CallSynMId";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Call, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Put, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Call, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    AddFuturesPositionToStrategy(temp_strategy, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    break;
                case 9:
                    temp_strategy.Name += "PutSynRight";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Put, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, mId_strike_obj.Put, Operation.Sell, Math.Min((int)VmId, maxContracts * 2));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Call, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    AddFuturesPositionToStrategy(temp_strategy, Operation.Sell, Math.Min((int)Vright, maxContracts));
                    break;
                default:
                    return null;
            }

            temp_strategy.PlanPL = (costCall < 0) ? Math.Round((profitCall + costCall) / Math.Abs(costCall), 2) : 999;
            temp_strategy.CalculateCost();
            temp_strategy.Profit = profitCall + temp_strategy.Cost;
            temp_strategy.ProbabilityOfProfit = CalculateProbabilityOfProfit(temp_strategy);

            return temp_strategy;
        }

        private Strategy CreateCondorStrategy(int strategy_type, Strike left_strike_obj, Strike left_mid_strike_obj, Strike right_mid_strike_obj, Strike right_strike_obj, double Vleft, double VleftMid, double VrightMid, double Vright, int leftStrike, int leftMidStrike, int rightMidStrike, int rightStrike, double profit, double cost)
        {
            if (left_strike_obj == null || left_mid_strike_obj == null || right_mid_strike_obj == null || right_strike_obj == null)
                return null;

            Strategy temp_strategy = new Strategy();
            temp_strategy.Name = "Condor";
            temp_strategy.ExpDate = Series?.ExpDate ?? "";
            temp_strategy.Id = "L" + leftStrike.ToString() + "LM" + leftMidStrike.ToString() + "RM" + rightMidStrike.ToString() + "R" + rightStrike.ToString() + temp_strategy.Name;

            double accountSize = 100000;
            double riskTolerance = 0.02;
            int maxContracts = CalculatePositionSize(accountSize, riskTolerance, cost);

            if (maxContracts == 0)
                return null;

            switch (strategy_type)
            {
                case 0:
                    temp_strategy.Name += "Call";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Call, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, left_mid_strike_obj.Call, Operation.Sell, Math.Min((int)VleftMid, maxContracts));
                    AddPositionToStrategy(temp_strategy, right_mid_strike_obj.Call, Operation.Sell, Math.Min((int)VrightMid, maxContracts));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Call, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    break;
                case 1:
                    temp_strategy.Name += "Put";
                    AddPositionToStrategy(temp_strategy, left_strike_obj.Put, Operation.Buy, Math.Min((int)Vleft, maxContracts));
                    AddPositionToStrategy(temp_strategy, left_mid_strike_obj.Put, Operation.Sell, Math.Min((int)VleftMid, maxContracts));
                    AddPositionToStrategy(temp_strategy, right_mid_strike_obj.Put, Operation.Sell, Math.Min((int)VrightMid, maxContracts));
                    AddPositionToStrategy(temp_strategy, right_strike_obj.Put, Operation.Buy, Math.Min((int)Vright, maxContracts));
                    break;
                default:
                    return null;
            }

            temp_strategy.PlanPL = (cost < 0) ? Math.Round((profit + cost) / Math.Abs(cost), 2) : 999;
            temp_strategy.CalculateCost();
            temp_strategy.Profit = profit + temp_strategy.Cost;
            temp_strategy.ProbabilityOfProfit = CalculateProbabilityOfProfit(temp_strategy);

            return temp_strategy;
        }

        private int CalculatePositionSize(double accountSize, double riskTolerance, double strategyCost)
        {
            if (strategyCost <= 0 || accountSize <= 0 || riskTolerance <= 0)
                return 0;

            double maxRisk = accountSize * riskTolerance;
            return (int)Math.Floor(maxRisk / Math.Abs(strategyCost));
        }

        private double CalculateProbabilityOfProfit(Strategy strategy)
        {
            if (strategy.MinStrike == 0 || strategy.MaxStrike == 0 || histogram == null)
                return 0;

            double profitableRangeStart = strategy.MinStrike;
            double profitableRangeEnd = strategy.MaxStrike;
            double totalProbability = 0;

            for (int i = 0; i < histogram.BucketCount; i++)
            {
                var bucket = histogram[i];
                if (bucket.LowerBound >= profitableRangeStart && bucket.UpperBound <= profitableRangeEnd)
                {
                    totalProbability += bucket.Count / (double)sampleSize;
                }
            }

            return Math.Round(totalProbability * 100, 2);
        }

        private DataTable SimulateStrategyScenarios(Strategy strategy, double priceRange)
        {
            DataTable scenarios = new DataTable();
            scenarios.Columns.Add("Price", typeof(double));
            scenarios.Columns.Add("Profit", typeof(double));

            double currentPrice = ba_lastprice;
            double step = priceRange * currentPrice / 50;

            for (double price = currentPrice * (1 - priceRange); price <= currentPrice * (1 + priceRange); price += step)
            {
                double profit = 0;
                foreach (var member in strategy.Members)
                {
                    if (member.IsFutures)
                        continue;
                    double optionPrice = member.Option.type.ToLower() == "call"
                        ? Math.Max(0, price - member.Option.Strike)
                        : Math.Max(0, member.Option.Strike - price);
                    profit += (member.Operation == Operation.Buy ? 1 : -1) * member.EntranceOrderQty * optionPrice;
                }

                DataRow row = scenarios.NewRow();
                row["Price"] = price;
                row["Profit"] = profit - strategy.Cost;
                scenarios.Rows.Add(row);
            }

            return scenarios;
        }

        private void LogTradeToDatabase(Strategy strategy)
        {
            try
            {
                var tradeData = new Dictionary<string, object>
                {
                    { "StrategyId", strategy.Id },
                    { "Name", strategy.Name },
                    { "Timestamp", DateTime.Now },
                    { "Cost", strategy.Cost },
                    { "Profit", strategy.Profit },
                    { "PlanPL", strategy.PlanPL },
                    { "PoP", strategy.ProbabilityOfProfit },
                    { "Delta", strategy.Delta },
                    { "Gamma", strategy.Gamma },
              //      { "Theta", strategy.Tetta },
                    { "Vega", strategy.Vega },
                    { "State", strategy.State.ToString() }
                };

                // TODO: Implement db.InsertTradeLog in DatabaseManager
                // db.InsertTradeLog("TradeLog", tradeData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to log trade: {ex.Message}");
                //NotifyTelegram(null, $"Failed to log trade: {ex.Message}");
            }
        }

        private void AddPositionToStrategy(Strategy strategy, Option option, Operation operation, int qty)
        {
            if (option == null || qty <= 0)
                return;
            pos = new Position
            {
                Option = option,
                Operation = operation,
                EntranceOrderQty = qty,
                PriceEntrance = operation == Operation.Buy ? (decimal)option.Ask : (decimal)option.Bid
            };
            strategy.Members.Add(pos);
        }

        private void AddFuturesPositionToStrategy(Strategy strategy, Operation operation, int qty)
        {
            if (qty <= 0 || ba == null)
                return;
            pos = new Position
            {
                IsFutures = true,
                Operation = operation,
                EntranceOrderQty = qty,
                SecurityCode = ba.SecurityCode,
                Tool = ba,
                PriceEntrance = operation == Operation.Buy ? (decimal)ba.Ask : (decimal)ba.Bid
            };
            strategy.Members.Add(pos);
        }

        private void UpdateStrategies()
        {
            Parallel.ForEach(Strategies, strategy =>
            {
                UpdateStrategyPrices(strategy);
                strategy.CalculateExpirationProfile(50, ba_lastprice, (double)(ba.StepPrice / ba.Step), histogram);
                strategy.CalculateCost();
                strategy.ProbabilityOfProfit = CalculateProbabilityOfProfit(strategy);

                if (checkFindLizzards.Checked)
                {
                    lock (Strategies)
                    {
                        FindLizzards(Strategies.IndexOf(strategy));
                    }
                }
            });
        }

        private void UpdateStrategyPrices(Strategy strategy)
        {
            if (strategy == null || strategy.Members == null)
                return;

            foreach (var member in strategy.Members)
            {
                if (!member.IsFutures)
                {
                    var temp_strike = strikes.FirstOrDefault(n => n.strike == member.Option?.Strike);
                    if (temp_strike != null)
                    {
                        if (member.Option.type.ToLower() == "call")
                        {
                            member.Option.Ask = temp_strike.Call.Ask;
                            member.Option.Bid = temp_strike.Call.Bid;
                            member.PriceEntrance = member.Operation == Operation.Buy ? (decimal)temp_strike.Call.Ask : (decimal)temp_strike.Call.Bid;
                        }
                        else if (member.Option.type.ToLower() == "put")
                        {
                            member.Option.Ask = temp_strike.Put.Ask;
                            member.Option.Bid = temp_strike.Put.Bid;
                            member.PriceEntrance = member.Operation == Operation.Buy ? (decimal)temp_strike.Put.Ask : (decimal)temp_strike.Put.Bid;
                        }
                    }
                }
                else
                {
                    if (member.Tool != null)
                    {
                        member.PriceEntrance = member.Operation == Operation.Buy ? (decimal)member.Tool.Ask : (decimal)member.Tool.Bid;
                    }
                }
            }
        }

        private void FindLizzards(int s1)
        {
            if (s1 < 0 || s1 >= Strategies.Count)
                return;

            for (int s2 = s1 + 1; s2 < Strategies.Count; s2++)
            {
                if ((Strategies[s1].Name.Contains("Put") && Strategies[s2].Name.Contains("Call")) ||
                    (Strategies[s1].Name.Contains("Call") && Strategies[s2].Name.Contains("Put")))
                {
                    if (Strategies[s1].Cost + Strategies[s2].Cost > -2000)
                    {
                        Strategy lizzard = new Strategy
                        {
                            Name = "Lizzard",
                            Id = Strategies[s1].Id + Strategies[s2].Id,
                            MinStrike = Math.Min(Strategies[s1].MinStrike, Strategies[s2].MinStrike),
                            MaxStrike = Math.Max(Strategies[s1].MaxStrike, Strategies[s2].MaxStrike)
                        };
                        lizzard.Members.AddRange(Strategies[s1].Members);
                        lizzard.Members.AddRange(Strategies[s2].Members);
                        lizzard.CalculateCost();
                        lizzard.CalculateExpirationProfile(50, ba_lastprice, (double)(ba.StepPrice / ba.Step), histogram);
                        lizzard.ProbabilityOfProfit = CalculateProbabilityOfProfit(lizzard);
                        if (lizzard.Mean > 3000 && Portfolio.Count < 100)
                        {
                            lock (Portfolio)
                            {
                                if (Portfolio.All(p => p.Id != lizzard.Id))
                                {
                                    Portfolio.Add(lizzard);
                                    LogTradeToDatabase(lizzard);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void TrackSpecialCondor()
        {
            if (strikes.Count <= 4)
                return;

            int Id_s = Portfolio.FindIndex(p => p.Id == "tracker1");
            if (Id_s != -1)
                Portfolio.RemoveAt(Id_s);
            tableSelectedStrategy.Clear();
            Strategy condor = new Strategy
            {
                Id = "tracker1",
                Name = "tracker1",
                State = State.Canceled
            };
            AddPositionToStrategy(condor, strikes[current_strike_Id - 1].Put, Operation.Buy, 2);
            AddPositionToStrategy(condor, strikes[current_strike_Id - 2].Put, Operation.Sell, 2);
            AddPositionToStrategy(condor, strikes[current_strike_Id - 4].Put, Operation.Sell, 2);
            AddPositionToStrategy(condor, strikes[current_strike_Id - 6].Put, Operation.Buy, 1);
            Portfolio.Add(condor);
            LogTradeToDatabase(condor);
        }

        private void UpdatePortfolio()
        {
            Parallel.ForEach(Portfolio, strategy =>
            {
                double oldCost = strategy.Cost;
                UpdateStrategyPrices(strategy);
                strategy.CalculateCost();
                if (Math.Abs(strategy.Cost - oldCost) / Math.Abs(oldCost > 0 ? oldCost : 1) > 0.05)
                {
                    //NotifyTelegram(strategy, "CostChange");
                }
                strategy.ProbabilityOfProfit = CalculateProbabilityOfProfit(strategy);
            });
        }

        void RefreshPortfolio()
        {
            if (!checkPortfolio.Checked)
                return;
            tablePortfolio.BeginLoadData();
            foreach (var strategy in Portfolio)
            {
                string id = strategy.Id;
                DataRow[] rows = tablePortfolio.Select($"Id = '{id}'");
                if (rows.Length == 0)
                {
                    DataRow portfolioRow = tablePortfolio.NewRow();
                    portfolioRow["Id"] = id;
                    UpdatePortfolioRow(portfolioRow, strategy);
                    tablePortfolio.Rows.Add(portfolioRow);
                }
                else
                {
                    UpdatePortfolioRow(rows[0], strategy);
                }
            }
            tablePortfolio.EndLoadData();
        }

        private void UpdatePortfolioRow(DataRow row, Strategy strategy)
        {
            //row["Delta"] = strategy.Delta;
            //row["Tetta"] = strategy.Tetta;
            row["Gamma"] = strategy.Gamma;
            row["Vega"] = strategy.Vega;
            row["Profit"] = strategy.Profit;
            row["Cost"] = strategy.Cost;
            row["planPL"] = strategy.PlanPL;
            row["State"] = strategy.State.ToString();
            row["Mean"] = Math.Round(strategy.Mean, 1);
            row["PoP"] = strategy.ProbabilityOfProfit;
        }

        void RefreshStrategies()
        {
            if (checkPortfolio.Checked)
                return;

            tableStrategies.BeginLoadData();
            foreach (var strategy in Strategies)
            {
                string id = strategy.Id;
                DataRow[] rows = tableStrategies.Select($"Id = '{id}'");
                if (rows.Length == 0)
                {
                    DataRow strategyRow = tableStrategies.NewRow();
                    strategyRow["Id"] = id;
                    strategyRow["Name"] = strategy.Name;
                    UpdateStrategyRow(strategyRow, strategy);
                    tableStrategies.Rows.Add(strategyRow);
                }
                else
                {
                    UpdateStrategyRow(rows[0], strategy);
                }
            }
            tableStrategies.EndLoadData();
        }

        private void UpdateStrategyRow(DataRow row, Strategy strategy)
        {
            row["P/L"] = strategy.PlanPL;
            row["Profit"] = strategy.Profit;
            row["Cost"] = strategy.Cost;
            row["Mean"] = Math.Round(strategy.Mean, 1);
            row["PoP"] = strategy.ProbabilityOfProfit;
        }

        void RefreshOptionBoard()
        {
            if (Series?.Strikes == null)
                return;
            
            tableOptionBoard.BeginLoadData();

            double halfstep = strike_step / 2.0;
            for (int Id = 0; Id < Series.Strikes.Count; Id++)
            {
                Strike strike = Series.Strikes[Id];
               

                if (Math.Abs(ba_lastprice - strike.strike) < halfstep)
                {
                    current_strike_Id = Id;
                    current_strike = strike.strike;
                }

                if (strike?.Call != null && strike?.Put != null)
                {
                    if (strike.Call.Strike >=  current_strike)
                        strike.Call.CalcIV(ba_lastprice);
                    if (strike.Put.Strike <= current_strike)
                        strike.Put.CalcIV(ba_lastprice);
                }

                DataRow[] rows = tableOptionBoard.Select($"Strike = {strike.strike}");
                DataRow sRow;
                if (rows.Length > 0)
                {
                    sRow = rows[0];
                    UpdateOptionBoardRow(sRow, strike);
                }
                else
                {
                    sRow = tableOptionBoard.NewRow();
                    sRow["PutCode"] = strike.Put?.seccode ?? "";
                    sRow["CallCode"] = strike.Call?.seccode ?? "";
                    UpdateOptionBoardRow(sRow, strike);
                    tableOptionBoard.Rows.Add(sRow);
                }
            }
            tableOptionBoard.EndLoadData();


           

        }

        private void UpdateOptionBoardRow(DataRow row, Strike strike)
        {

            row["DateTime"] = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
            row["PutTeor"] = strike.Put?.Theorprice ?? 0;
            row["PutBid"] = strike.Put?.Bid ?? 0;
            row["PutAsk"] = strike.Put?.Ask ?? 0;
            row["PutAskIV"] = Math.Round(strike.Put?.AskIV ?? 0, 3);
            row["PutBidIV"] = Math.Round(strike.Put?.bidIV ?? 0, 3);
            row["Strike"] = strike.strike;
            row["CallTeor"] = strike.Call?.Theorprice ?? 0;
            row["CallBid"] = strike.Call?.Bid ?? 0;
            row["CallAsk"] = strike.Call?.Ask ?? 0;
            row["CallAskIV"] = Math.Round(strike.Call?.AskIV ?? 0, 3);
            row["CallBidIV"] = Math.Round(strike.Call?.bidIV ?? 0, 3);
            row["Delta"] = 0;
            row["Gamma"] = 0;
            row["Theta"] = 0;
            row["Vega"] = 0;
            row["BALastPrice"] = ba.LastPrice;
            row["CentralStrike"] = current_strike;




        }

        DataTable InitPortfolio(string name)
        {
            DataTable dataTable = new DataTable(name);
            dataTable.Columns.Add("Id", typeof(string));
            dataTable.Columns.Add("Delta", typeof(double));
            dataTable.Columns.Add("Tetta", typeof(double));
            dataTable.Columns.Add("Gamma", typeof(double));
            dataTable.Columns.Add("Vega", typeof(double));
            dataTable.Columns.Add("Profit", typeof(double));
            dataTable.Columns.Add("Cost", typeof(double));
            dataTable.Columns.Add("planPL", typeof(double));
            dataTable.Columns.Add("State", typeof(string));
            dataTable.Columns.Add("Mean", typeof(double));
            dataTable.Columns.Add("PoP", typeof(double));
            dataTable.PrimaryKey = new[] { dataTable.Columns["Id"] };
            return dataTable;
        }

        DataTable InitStrategies(string name)
        {
            DataTable dataTable = new DataTable(name);
            dataTable.Columns.Add("Id", typeof(string));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("P/L", typeof(double));
            dataTable.Columns.Add("Mean", typeof(double));
            dataTable.Columns.Add("Profit", typeof(double));
            dataTable.Columns.Add("Cost", typeof(double));
            dataTable.Columns.Add("ExpDate", typeof(string));
            dataTable.Columns.Add("PoP", typeof(double));
            dataTable.PrimaryKey = new[] { dataTable.Columns["Id"] };
            return dataTable;
        }

        DataTable InitSelectedStrategy(string name)
        {
            DataTable dataTable = new DataTable(name);
            dataTable.Columns.Add("str_Id", typeof(string));
            dataTable.Columns.Add("Code", typeof(string));
            dataTable.Columns.Add("Type", typeof(string));
            dataTable.Columns.Add("Price", typeof(double));
            dataTable.Columns.Add("Quantity", typeof(double));
            dataTable.Columns.Add("D", typeof(double));
            dataTable.Columns.Add("G", typeof(double));
            dataTable.Columns.Add("T", typeof(double));
            dataTable.Columns.Add("V", typeof(double));
            dataTable.Columns.Add("SellDepo", typeof(double));
            dataTable.Columns.Add("BuyDepo", typeof(double));
            dataTable.Columns.Add("askIV", typeof(double));
            dataTable.Columns.Add("midIV", typeof(double));
            dataTable.Columns.Add("BidIV", typeof(double));
            dataTable.Columns.Add("IVteo", typeof(double));
            dataTable.Columns.Add("Shift_CS", typeof(int));
            dataTable.PrimaryKey = new[] { dataTable.Columns["Code"] };
            return dataTable;
        }

        DataTable InitOptionBoard(string tablename)
        {
            DataTable dataTable = new DataTable(tablename);
            dataTable.Columns.Add("DateTime", typeof(DateTime));
            dataTable.Columns.Add("PutCode", typeof(string));
            dataTable.Columns.Add("PutTeor", typeof(double));
            dataTable.Columns.Add("PutBid", typeof(double));
            dataTable.Columns.Add("PutAsk", typeof(double));
            dataTable.Columns.Add("PutBidIV", typeof(double));
            dataTable.Columns.Add("PutAskIV", typeof(double));
            dataTable.Columns.Add("Strike", typeof(int));
            dataTable.Columns.Add("CallCode", typeof(string));
            dataTable.Columns.Add("CallTeor", typeof(double));
            dataTable.Columns.Add("CallBid", typeof(double));
            dataTable.Columns.Add("CallAsk", typeof(double));
            dataTable.Columns.Add("CallBidIV", typeof(double));
            dataTable.Columns.Add("CallAskIV", typeof(double));
            dataTable.Columns.Add("Delta", typeof(double));
            dataTable.Columns.Add("Gamma", typeof(double));
            dataTable.Columns.Add("Theta", typeof(double));
            dataTable.Columns.Add("Vega", typeof(double));
            dataTable.Columns.Add("BALastPrice", typeof(double));
            dataTable.Columns.Add("CentralStrike", typeof(int));
            
            return dataTable;
        }

        private void viewStrategies_SelectionChanged(object sender, DevExpress.Data.SelectionChangedEventArgs e)
        {
            tableSelectedStrategy.Clear();
            string _Id;
            foreach (int i in viewStrategies.GetSelectedRows())
            {
                DataRow row = viewStrategies.GetDataRow(i);
                _Id = (string)row["Id"];
                selected_strategy_Id = (checkPortfolio.Checked ? Portfolio : Strategies).FindIndex(s => s.Id == _Id);
            }
            FillSelectedStrategyTable();
            UpdateChart();
            viewSelectedStrategy.RefreshData();
        }

        private void FillSelectedStrategyTable()
        {
            if (selected_strategy_Id < 0)
                return;
            int Id = -1;
            int central_strike = tableOptionBoard.Rows.Count / 2;
            double minX = double.MaxValue, maxX = double.MinValue;
            List<Strategy> list_strategies = checkPortfolio.Checked ? Portfolio : Strategies;

            if (list_strategies.Count > selected_strategy_Id)
            {
                foreach (Position member in list_strategies[selected_strategy_Id].Members)
                {
                    DataRow memberRow = tableSelectedStrategy.NewRow();
                    memberRow["str_Id"] = list_strategies[selected_strategy_Id].Id;

                    if (member.IsFutures)
                    {
                        memberRow["Code"] = member.SecurityCode;
                        memberRow["Type"] = "Futures";
                        memberRow["Price"] = member.PriceEntrance;
                        memberRow["Quantity"] = member.EntranceOrderQty;
                        memberRow["D"] = member.EntranceOrderQty;
                        memberRow["G"] = 0;
                        memberRow["T"] = 0;
                        memberRow["V"] = 0;
                        memberRow["SellDepo"] = member.Tool?.SellDepo ?? 0;
                        memberRow["BuyDepo"] = member.Tool?.BuyDepo ?? 0;
                        memberRow["askIV"] = 0;
                        memberRow["midIV"] = 0;
                        memberRow["BidIV"] = 0;
                    }
                    else
                    {
                        memberRow["Code"] = member.Option.seccode;
                        memberRow["Type"] = member.Option.type;
                        memberRow["Price"] = member.PriceEntrance;
                        memberRow["Quantity"] = member.EntranceOrderQty;
                        memberRow["D"] = member.Option.Delta;
                        memberRow["G"] = member.Option.Gamma;
                        memberRow["T"] = member.Option.Tetta;
                        memberRow["V"] = member.Option.Vega;
                        memberRow["SellDepo"] = member.Option.SellDepo;
                        memberRow["BuyDepo"] = member.Option.BuyDepo;
                        memberRow["askIV"] = member.Option.askIV;
                        memberRow["midIV"] = member.Option.midIV;
                        memberRow["BidIV"] = member.Option.bidIV;
                        if (member.Option.type == "Call")
                            memberRow["Shift_CS"] = member.Option.Strike - (int)tableOptionBoard.Rows[central_strike]["Strike"];
                        else
                            memberRow["Shift_CS"] = (int)tableOptionBoard.Rows[central_strike]["Strike"] - member.Option.Strike;

                        if (member.Option.Strike < minX) minX = member.Option.Strike;
                        if (member.Option.Strike > maxX) maxX = member.Option.Strike;
                    }
                    tableSelectedStrategy.Rows.Add(memberRow);
                }
            }
        }

        private void timerSaveData_Tick(object sender, EventArgs e)
        {
            TimeSpan curtime = DateTime.Now.TimeOfDay;
            if (!(curtime > worktime0_start && curtime < worktime0_end || curtime > worktime1_start && curtime < worktime1_end || curtime > worktime2_start && curtime < worktime2_end))
                return;
            if (current_strike != 0)
                db.SaveToDB(tableOptionBoard, seriesTableName);
        }

        private async void timerWatchDog_Tick(object sender, EventArgs e)
        {
          
        }
    }
}