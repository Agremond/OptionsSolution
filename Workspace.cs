using DevExpress.XtraBars;
using DevExpress.XtraTabbedMdi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using QuikSharp;
using QuikSharp.DataStructures;
using QuikSharp.DataStructures.Transaction;
//using Telegram.Bot;
//using Telegram.Bot.Polling;
//using Telegram.Bot.Types;
//using Telegram.Bot.Types.Enums;
//using Telegram.Bot.Types.ReplyMarkups;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DatabaseOperations;
using System.Data.SqlClient;
using DevExpress.XtraEditors;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.IO;

using Timer = System.Windows.Forms.Timer;
using System.Configuration;

namespace GrokOptions
{
    
    public partial class Workspace : DevExpress.XtraBars.ToolbarForm.ToolbarForm
    {
        DatabaseManager dbWriter;
        //public ChatId telegramChatId = 0;
        
        //TelegramBotClient botClient;
        TimeSpan worktime0_start = new TimeSpan(10, 00, 00);
        TimeSpan worktime0_end = new TimeSpan(13, 45, 00);
        TimeSpan worktime1_start = new TimeSpan(14, 05, 00);
        TimeSpan worktime1_end = new TimeSpan(18, 45, 00);
        TimeSpan worktime2_start = new TimeSpan(19, 05, 00);
        TimeSpan worktime2_end = new TimeSpan(23, 45, 00);
        const int STRIKES_ON_DESK = 10;
        
        const int CENTRAL_STRIKE = STRIKES_ON_DESK / 2;
        public static List<Tool> tools = new List<Tool>();
        Settings settings;
        public Journal fJournal = new Journal();
        List<Instrument> instruments = new List<Instrument>();
        List<OrderBook> toolsOrderBook = new List<OrderBook>();
        List<Trade> listTrades = new List<Trade>();
        List<Strategy> Strategies = new List<Strategy>();
        List<Series> Series = new List<Series>();
        DataSet datasetOptionBoards = new DataSet();
        TradingEngine t_engine = null;
        OrderBook orderbook;
        int timerTF = 0;
        bool started = false;
        static int OpenFormCount = 1;
        bool isServerConnected = false;
        public static Quik _quik;
        public int test = 0;

        
        private const string QUIK_PATH = @"C:\QUIK_VTB\info.exe";
        private const string QUIK_FOLDER_PATH = @"C:\QUIK_VTB";
        ProcessStartInfo startInfo = new ProcessStartInfo(QUIK_PATH);
        private Timer autoConnectTimer;

        double[] deltaDistribution = new double[] { 30, -4130, 2910, /* ...остальные значения... */ };
        string[] fts = new string[] { "RI", "Si", "BR" };
        string[] ftsLitera = new string[] { "F", "G", "H", "J", "K", "M", "N", "Q", "U", "V", "X", "Z" };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        void Init()
        {

            
            InitTelegramBot();

            settings = new Settings();
            timerRenewForm.Enabled = false;

           
            LaunchQuikIfNotRunning();
            InitializeAutoConnectTimer();
        }

        private void InitializeSecuritiesComboBox()
        {
            comboBoxSecurities = new ComboBoxEdit
            {
                Name = "comboBoxSecurities",
                Location = new Point(10, 10),
                Size = new Size(200, 20),
                Properties = { TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard }
            };
            this.Controls.Add(comboBoxSecurities);
            comboBoxSecurities.Properties.Items.Add("Загрузка инструментов...");
            comboBoxSecurities.SelectedIndex = 0;
            comboBoxSecurities.SelectedIndexChanged += ComboBoxSecurities_SelectedIndexChanged;
        }

        private async Task LoadSecuritiesAsync()
        {
            try
            {
                fJournal.Log("Quik connector", "Info", "Загрузка списка инструментов...");
                string[] securities = await _quik.Class.GetClassSecurities("SPBFUT");
                comboBoxSecurities.Properties.Items.Clear();
                foreach (var sec in securities)
                {
                    comboBoxSecurities.Properties.Items.Add(sec);
                }
                fJournal.Log("Quik connector", "Info", $"Загружено {securities.Length} инструментов.");
                if (securities.Length > 0)
                {
                    comboBoxSecurities.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                fJournal.Log("Quik connector", "Error", $"Ошибка загрузки инструментов: {ex.Message}");
            }
        }

        private void ComboBoxSecurities_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxSecurities.SelectedItem != null)
            {
                fJournal.Log("UI", "Info", $"Выбран инструмент: {comboBoxSecurities.SelectedItem}");
            }
        }

        private void LaunchQuikIfNotRunning()
        {
            Process[] quikProcesses = Process.GetProcessesByName("info"); // Обновлено на "info"
            if (quikProcesses.Length == 0)
            {
                try
                {
                    if (File.Exists(QUIK_PATH))
                    {
                        startInfo.WorkingDirectory = QUIK_FOLDER_PATH;
                        Process.Start(startInfo);
                        fJournal.Log("QUIK Launcher", "Info", $"QUIK запущен: {QUIK_PATH}");

                        // Динамическая проверка загрузки окна QUIK
                        for (int i = 0; i < 10; i++)
                        {
                            Thread.Sleep(1000);
                            quikProcesses = Process.GetProcessesByName("info");
                            if (quikProcesses.Length > 0 && quikProcesses[0].MainWindowHandle != IntPtr.Zero)
                            {
                                fJournal.Log("QUIK Launcher", "Info", "Окно QUIK успешно загружено.");
                                break;
                            }
                            if (i == 9) fJournal.Log("QUIK Launcher", "Warning", "Окно QUIK не загрузилось за 10 секунд.");
                        }
                    }
                    else
                    {
                        fJournal.Log("QUIK Launcher", "Error", $"info.exe не найден по пути: {QUIK_PATH}");
                    }
                }
                catch (Exception ex)
                {
                    fJournal.Log("QUIK Launcher", "Error", $"Ошибка запуска QUIK: {ex.Message}");
                }
            }
            else
            {
                fJournal.Log("QUIK Launcher", "Info", "QUIK уже запущен.");
            }
        }

        private void InitializeAutoConnectTimer()
        {
            autoConnectTimer = new Timer();
            autoConnectTimer.Interval = 30000; // 30 секунд
            autoConnectTimer.Tick += AutoConnectTimer_Tick;
            autoConnectTimer.Enabled = true;
        }

        private async void AutoConnectTimer_Tick(object sender, EventArgs e)
        {
            if (_quik == null) return;

            bool currentConnected = await _quik.Service.IsConnected();
            if (!currentConnected)
            {
                fJournal.Log("AutoConnect", "Warning", "Соединение потеряно. Пытаемся переподключиться...");
                LaunchQuikIfNotRunning();
                AutomateQuikConnect();
            }
            else
            {
                isServerConnected = true;
            }
        }

        private void AutomateQuikConnect()
        {
            try
            {
                Process[] quikProcesses = Process.GetProcessesByName("info"); // Обновлено на "info"
                if (quikProcesses.Length == 0)
                {
                    fJournal.Log("AutoConnect", "Error", "QUIK не запущен.");
                    //notifyTelegram("QUIK не запущен.");
                    return;
                }

                Process quikProcess = quikProcesses[0];
                if (quikProcess.HasExited || quikProcess.MainWindowHandle == IntPtr.Zero)
                {
                    fJournal.Log("AutoConnect", "Error", "Окно QUIK недоступно.");
                    return;
                }

                // Активируем главное окно QUIK
                SetForegroundWindow(quikProcess.MainWindowHandle);
                Thread.Sleep(500);

                // UI Automation: поиск меню "Сервис" и пункта "Подключение к серверу"
                AutomationElement quikWindow = AutomationElement.FromHandle(quikProcess.MainWindowHandle);
                if (quikWindow == null)
                {
                    fJournal.Log("AutoConnect", "Error", "Не удалось получить элемент UI Automation для окна QUIK.");
                    SendKeys.SendWait("^(q)");
                    fJournal.Log("AutoConnect", "Info", "Резервный механизм: отправлена комбинация Ctrl+Q.");
                    return;
                }

                // Повторные попытки поиска меню "Сервис"
                AutomationElement serviceMenu = null;
                //for (int i = 0; i < 3; i++)
                //{
                //    serviceMenu = quikWindow.FindFirst(
                //        TreeScope.Descendants,
                //        new AndCondition(
                //            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                //            new PropertyCondition(AutomationElement.NameProperty, "Система")
                //        )
                //    );
                //    if (serviceMenu != null) break;
                //    Thread.Sleep(1000);
                //    fJournal.Log("AutoConnect", "Info", $"Повторная попытка поиска меню 'Система' ({i + 1}/3)");
                //}

                if (serviceMenu == null)
                {
                    fJournal.Log("AutoConnect", "Error", "Меню 'Система' не найдено.");
                    SendKeys.Send("^q");
                    
                    fJournal.Log("AutoConnect", "Info", "Резервный механизм: отправлена комбинация Ctrl+Q.");
                    return;
                }

                ExpandCollapsePattern expandPattern = serviceMenu.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
                if (expandPattern != null)
                {
                    expandPattern.Expand();
                    fJournal.Log("AutoConnect", "Info", "Меню 'Система' развернуто.");
                    Thread.Sleep(200);
                }

                // Повторные попытки поиска пункта "Подключение к серверу"
                AutomationElement connectMenuItem = null;
                for (int i = 0; i < 3; i++)
                {
                    connectMenuItem = serviceMenu.FindFirst(
                        TreeScope.Descendants,
                        new AndCondition(
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                            new PropertyCondition(AutomationElement.NameProperty, "Установить соединение...")
                        )
                    );
                    if (connectMenuItem != null) break;
                    Thread.Sleep(1000);
                    fJournal.Log("AutoConnect", "Info", $"Повторная попытка поиска пункта 'Подключение к серверу' ({i + 1}/3)");
                }

                if (connectMenuItem == null)
                {
                    fJournal.Log("AutoConnect", "Error", "Пункт меню 'Подключение к серверу' не найден.");
                    SendKeys.SendWait("^(q)");
                    fJournal.Log("AutoConnect", "Info", "Резервный механизм: отправлена комбинация Ctrl+Q.");
                    return;
                }

                InvokePattern invokePattern = connectMenuItem.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                if (invokePattern != null)
                {
                    invokePattern.Invoke();
                    fJournal.Log("AutoConnect", "Info", "Выполнен клик по пункту 'Подключение к серверу'.");
                }
                else
                {
                    fJournal.Log("AutoConnect", "Error", "Не удалось выполнить действие Invoke для пункта меню.");
                    SendKeys.SendWait("^(q)");
                    fJournal.Log("AutoConnect", "Info", "Резервный механизм: отправлена комбинация Ctrl+Q.");
                }

                // Опционально: обработка окна авторизации (раскомментируйте и настройте логин/пароль)
                /*
                Thread.Sleep(500);
                AutomationElement loginWindow = quikWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));
                if (loginWindow != null)
                {
                    AutomationElement loginField = loginWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                    if (loginField != null)
                    {
                        ValuePattern valuePattern = loginField.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                        valuePattern.SetValue("your_login");
                        fJournal.Log("AutoConnect", "Info", "Введен логин.");
                    }
                    AutomationElement passwordField = loginWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit, 1));
                    if (passwordField != null)
                    {
                        ValuePattern valuePattern = passwordField.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                        valuePattern.SetValue("your_password");
                        fJournal.Log("AutoConnect", "Info", "Введен пароль.");
                    }
                    AutomationElement okButton = loginWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                    if (okButton != null)
                    {
                        InvokePattern okInvoke = okButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        okInvoke.Invoke();
                        fJournal.Log("AutoConnect", "Info", "Нажата кнопка OK в окне авторизации.");
                    }
                }
                */
            }
            catch (Exception ex)
            {
                fJournal.Log("AutoConnect", "Error", $"Ошибка UI Automation: {ex.Message}");
                SendKeys.SendWait("^(q)");
                fJournal.Log("AutoConnect", "Info", "Резервный механизм: отправлена комбинация Ctrl+Q.");
            }
        }

        void Run()
        {
            TimeSpan curtime = DateTime.Now.TimeOfDay;
            if (!(curtime > worktime0_start && curtime < worktime0_end || curtime > worktime1_start && curtime < worktime1_end || curtime > worktime2_start && curtime < worktime2_end))
            {
                fJournal.Log("Trading Engine", "Info", "Вне торговых часов, обновление пропущено.");
                return;
            }

            update_BA_BidAsk();
            for (int s = 0; s < Series.Count; s++)
                update_OptionsBidAsk(s);
        }

        //public void notifyTelegram(string message)
        //{
        //    if (telegramChatId != 0)
        //    {
        //        botClient.SendMessage(telegramChatId, message).GetAwaiter().GetResult();
        //    }
        //}

      
        void InitTelegramBot()
        {
            var token = "5088131315:AAEtWSZz4McGorHm6eqOtxxxCYM2AwJK-og";
            
            
            try
            {
                var cts = new CancellationTokenSource();
                //botClient = new TelegramBotClient(token, cancellationToken: cts.Token);
                //botClient.DeleteWebhook();          // you may comment this line if you find it unnecessary
                //botClient.DropPendingUpdates();     // you may comment this line if you find it unnecessary


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize Telegram bot: {ex.Message}");
            }
            
            //notifyTelegram("QUIK не запущен.");
        }

        void update_OptionsBidAsk(int s)
        {
            int orderbook_id = 0;
            for (int i = 0; i < Series[s].Strikes.Count; i++)
            {
                Strike str = Series[s].Strikes[i];
                lock (toolsOrderBook)
                {
                    orderbook_id = toolsOrderBook.IndexOf(toolsOrderBook.Where(n => (n.sec_code == str.Call.seccode)).FirstOrDefault());
                    if (orderbook_id != -1)
                    {
                        orderbook = toolsOrderBook[orderbook_id];
                        if (orderbook.bid != null)
                            Series[s].Strikes[i].Call.Bid = Convert.ToDouble(orderbook.bid[toolsOrderBook[orderbook_id].bid.Count() - 1].price);
                        if (orderbook.offer != null)
                            Series[s].Strikes[i].Call.Ask = Convert.ToDouble(orderbook.offer[0].price);
                    }

                    orderbook_id = toolsOrderBook.IndexOf(toolsOrderBook.Where(n => (n.sec_code == str.Put.seccode)).FirstOrDefault());
                    if (orderbook_id != -1)
                    {
                        orderbook = toolsOrderBook[orderbook_id];
                        if (orderbook.offer != null)
                            Series[s].Strikes[i].Put.Ask = Convert.ToDouble(orderbook.offer[0].price);
                        if (orderbook.bid != null)
                            Series[s].Strikes[i].Put.Bid = Convert.ToDouble(orderbook.bid[toolsOrderBook[orderbook_id].bid.Count() - 1].price);
                    }
                }
            }
        }

        void update_BA_BidAsk()
        {
            int orderbook_id = 0;
            lock (toolsOrderBook)
            {
                for (int i = 0; i < tools.Count; i++)
                {
                    orderbook_id = toolsOrderBook.IndexOf(toolsOrderBook.Where(n => (n.sec_code == tools[i].SecurityCode)).FirstOrDefault());
                    if (orderbook_id != -1)
                    {
                        tools[i].Bid = Convert.ToDouble(toolsOrderBook[orderbook_id].bid[toolsOrderBook[orderbook_id].bid.Count() - 1].price);
                        tools[i].Ask = Convert.ToDouble(toolsOrderBook[orderbook_id].offer[0].price);
                    }
                }
            }
        }

        private void MdiManager_PageAdded(object sender, MdiTabPageEventArgs e)
        {
            XtraMdiTabPage page = e.Page;
            page.Tooltip = "Tooltip for the page " + page.Text;
        }

        public Workspace()
        {
            InitializeComponent();
            Init();

            fJournal.Text = "Журнал системы";
            fJournal.MdiParent = this;
            fJournal.Show();
        }

        private async void barButtonItem1_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            

            if (comboBoxSecurities.SelectedItem != null)
            {
                string selectedSecurity = comboBoxSecurities.SelectedItem.ToString();
                tools.Clear();
                tools.Add(new Tool(_quik, selectedSecurity, settings.KoefSlip));
                fJournal.Log("Quik connector", "Info", $"Добавлен инструмент: {selectedSecurity}");
            }
            else
            {
                fJournal.Log("Quik connector", "Warning", "Инструмент не выбран, используем стандартный SiZ5");
                tools.Add(new Tool(_quik, "SiZ5", settings.KoefSlip));
            }

            List<OptionBoard> option_board = new List<OptionBoard>();
            for (int i = 0; i < tools.Count; i++)
            {
                int id;
                string temp;
                string _temp_str;
                string Nweek = "";
                string month;
                string year;
                int id_strike;
                int counter = 0;
                int strike_step = 0;
                Strike _strike;
                option_board = await _quik.Trading.GetOptionBoard("SPBOPT", tools[i].SecurityCode, "1");
                if(option_board.Count > 2)
                    strike_step = (int)(option_board[1].Strike - option_board[0].Strike);
                foreach (OptionBoard opt_member in option_board)
                {
                    Nweek = "";
                    id = Series.IndexOf(Series.Where(n => (n.ExpDate == opt_member.ExpDate && n.BaseActive == opt_member.OPTIONBASE)).FirstOrDefault());
                    if (id == -1)
                    {
                        Series.Add(new GrokOptions.Series());
                        id = Series.Count - 1;
                        Series[id].Strikes = new List<Strike>();
                        Series[id].ExpDate = opt_member.ExpDate;
                        Series[id].BaseActive = opt_member.OPTIONBASE;
                    }

                    int central_strike = Convert.ToInt32((tools[i].LastPrice / strike_step)) * strike_step;
                    temp = opt_member.Code.TrimStart(tools[i].SecurityCode.Substring(0, 2).ToCharArray());
                    if (opt_member.Code.EndsWith("5"))
                    {
                        _temp_str = temp.Substring(0, temp.Length - 3);
                        month = temp.Substring(temp.Length - 2, 1);
                        year = temp.Substring(temp.Length - 1, 1);
                    }
                    else
                    {
                        _temp_str = temp.Substring(0, temp.Length - 4);
                        month = temp.Substring(temp.Length - 3, 1);
                        year = temp.Substring(temp.Length - 2, 1);
                        Nweek = temp.Substring(temp.Length - 1, 1);
                    }
                    int strike = Convert.ToInt32(_temp_str);
                    int LowStrike = central_strike - strike_step * STRIKES_ON_DESK / 2;
                    int HighStrike = central_strike + strike_step * (STRIKES_ON_DESK) / 2;

                    if (LowStrike <= strike && strike <= HighStrike)
                    {
                        counter++;
                        _strike = new Strike();
                        _strike.strike = strike;
                        if (opt_member.OPTIONTYPE.ToLower() == "call")
                            _strike.Call = new Option(opt_member);
                        else
                            _strike.Put = new Option(opt_member);
                        id_strike = Series[id].Strikes.IndexOf(Series[id].Strikes.Where(s => s.strike == strike).FirstOrDefault());
                        if (id_strike == -1)
                        {
                            Series[id].Strikes.Add(_strike);
                            id_strike = Series[id].Strikes.Count - 1;
                        }
                        else
                        {
                            if (opt_member.OPTIONTYPE.ToLower() == "call")
                                Series[id].Strikes[id_strike].Call = new Option(opt_member);
                            else
                                Series[id].Strikes[id_strike].Put = new Option(opt_member);
                        }
                    }
                }
                fJournal.Log("Загрузчик опционов", "Debug", $"Добавлено страйков: {tools[i].SecurityCode} {counter}");
            }
            fJournal.Log("Загрузчик опционов", "Info", "Завершена обработка доски опционов.");

            string connectionString = "Server=localhost;Database=TestDB;Trusted_Connection=True;";
            dbWriter = new DatabaseManager(connectionString);
            bool isSubscribedToolOrderBook = false;

            if (started)
            {
                btnStartStop.Caption = "Start";
                btnStartStop.Enabled = true;
                timerRenewForm.Enabled = false;
                started = false;
            }
            else
            {
                int count = 0;
                started = true;
                btnStartStop.Caption = "Stop";

                for (int i = 0; i < tools.Count; i++)
                {
                    await _quik.OrderBook.Unsubscribe(tools[i].ClassCode, tools[i].SecurityCode);
                    await _quik.OrderBook.Subscribe(tools[i].ClassCode, tools[i].SecurityCode);
                    isSubscribedToolOrderBook = await _quik.OrderBook.IsSubscribed("SPBFUT", tools[i].SecurityCode);
                    if (!isSubscribedToolOrderBook)
                    {
                        fJournal.Log("Quik connector", "Info", $"Подписка на стакан не удалась: {tools[i].SecurityCode}");
                    }
                }

                foreach (Series ser in Series)
                {
                    count = 0;
                    foreach (Strike str in ser.Strikes)
                    {
                        if (str.Call != null)
                        {
                            await _quik.OrderBook.Unsubscribe(str.Call.ClassCode, str.Call.seccode);
                            await _quik.OrderBook.Subscribe(str.Call.ClassCode, str.Call.seccode);
                            isSubscribedToolOrderBook = await _quik.OrderBook.IsSubscribed(str.Call.ClassCode, str.Call.seccode);
                            if (!isSubscribedToolOrderBook)
                            {
                                fJournal.Log("Quik connector", "Info", $"Подписка на стакан не удалась: {str.Call.seccode}");
                            }
                            else
                                count++;
                        }
                    }
                    fJournal.Log("Quik connector", "Info", $"Серия: {ser.ExpDate} Подписались на: {count} Call опционов.");

                    count = 0;
                    foreach (Strike str in ser.Strikes)
                    {
                        if (str.Put != null)
                        {
                            await _quik.OrderBook.Unsubscribe(str.Put.ClassCode, str.Put.seccode);
                            await _quik.OrderBook.Subscribe(str.Put.ClassCode, str.Put.seccode);
                            isSubscribedToolOrderBook = await _quik.OrderBook.IsSubscribed(str.Put.ClassCode, str.Put.seccode);
                            if (!isSubscribedToolOrderBook)
                            {
                                fJournal.Log("Quik connector", "Info", $"Подписка на стакан не удалась: {str.Put.seccode}");
                            }
                            else
                                count++;
                        }
                    }
                    fJournal.Log("Quik connector", "Info", $"Серия: {ser.ExpDate} Подписались на: {count} Put опционов.");
                }

                _quik.Events.OnQuote += OnQuoteDo;
                _quik.Events.OnTrade += OnTradeDo;
                timerRenewForm.Enabled = true;
            }

            if (dbWriter != null)
                for (int s = 0; s < Series.Count; s++)
                {
                    int id_tools = tools.IndexOf(tools.Where(n => (n.SecurityCode == Series[s].BaseActive)).FirstOrDefault());
                    instruments.Add(new Instrument(Series[s], tools[id_tools], dbWriter));
                    instruments.Last().Text = tools[id_tools].Name + " " + Series[s].ExpDate;
                    instruments.Last().MdiParent = this;
                    instruments.Last().Show();
                }
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private async void barButtonConnect_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                fJournal.Log("Quik connector", "Info", "Подключаемся к терминалу Quik");
                _quik = new Quik(Quik.DefaultPort, new InMemoryStorage());
            }
            catch (Exception ex)
            {
                fJournal.Log("Quik connector", "Error", $"Ошибка инициализации объекта Quik: {ex.Message}");
                return;
            }

            if (_quik != null)
            {
                fJournal.Log("Quik connector", "Info", "Экземпляр Quik создан.");
                try
                {
                    fJournal.Log("Quik connector", "Info", "Получаем статус соединения с сервером");
                    isServerConnected = await _quik.Service.IsConnected();
                    if (isServerConnected)
                    {
                        fJournal.Log("Quik connector", "Info", "Соединение с сервером установлено.");
                        barButtonConnect.ImageOptions.Image = Properties.Resources.apply_16x16_on;
                        barButtonConnect.Enabled = false;
                        btnStartStop.Enabled = true;
                        t_engine = new TradingEngine(_quik, settings);
                        autoConnectTimer.Start();
                        fJournal.Log("AutoConnect", "Info", "Автоподключение активировано.");
                        await LoadSecuritiesAsync();
                    }
                    else
                    {
                        fJournal.Log("Quik connector", "Error", "Соединение с сервером НЕ установлено.");
                        AutomateQuikConnect(); // Попытка автоматического подключения
                    }
                }
                catch (Exception ex)
                {
                    fJournal.Log("Quik connector", "Error", $"Неудачная попытка получить статус соединения: {ex.Message}");
                }
            }
        }

        private void btnStartStop_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (started)
            {
                started = false;
                btnStartStop.Caption = "Start";
                timerRenewForm.Enabled = false;
                autoConnectTimer.Stop();
                fJournal.Log("Trading Engine", "Info", "Торговый движок и автоподключение остановлены.");
            }
            else
            {
                started = true;
                btnStartStop.Caption = "Stop";
                timerRenewForm.Enabled = true;
                autoConnectTimer.Start();
                fJournal.Log("Trading Engine", "Info", "Торговый движок и автоподключение запущены.");
            }
        }

        void OnQuoteDo(OrderBook quote)
        {
            lock (toolsOrderBook)
            {
                int orderbook_id = toolsOrderBook.IndexOf(toolsOrderBook.Where(n => n.sec_code == quote.sec_code).FirstOrDefault());
                if (orderbook_id != -1)
                {
                    toolsOrderBook[orderbook_id] = quote;
                }
                else
                {
                    toolsOrderBook.Add(new OrderBook());
                    toolsOrderBook[toolsOrderBook.Count - 1] = quote;
                }
            }
        }

        void OnTradeDo(Trade trade)
        {
            int i = GetIndexOfTool(trade.SecCode, trade.ClassCode);
            if (i == -1)
                return;

            int trade_id = listTrades.IndexOf(listTrades.Where(n => n.TradeNum == trade.TradeNum).FirstOrDefault());
            if (trade_id != -1)
                return;

            if (!trade.Flags.HasFlag(OrderTradeFlags.Active) && !trade.Flags.HasFlag(OrderTradeFlags.Canceled))
            {
                string time = ((DateTime)trade.QuikDateTime).ToString();
                string message = $"Время: {time} {trade.SecCode} Цена: {trade.Price} Q: {trade.Quantity}";
                //notifyTelegram(message);
            }
            listTrades.Add(trade);
        }

        int GetIndexOfTool(string SecCode, string ClassCode)
        {
            for (int s = 0; s < Series.Count; s++)
                for (int i = 0; i < Series[s].Strikes.Count; i++)
                    if (Series[s].Strikes[i].Call.seccode == SecCode || Series[s].Strikes[i].Put.seccode == SecCode)
                        return i;
            return -1;
        }

        private void timerRenewForm_Tick(object sender, EventArgs e)
        {
            timerTF += timerRenewForm.Interval;
            Run();
        }

        private void barConnectDB_ItemClick(object sender, ItemClickEventArgs e) { }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            autoConnectTimer?.Stop();
            base.OnFormClosing(e);
        }

       
    }
}