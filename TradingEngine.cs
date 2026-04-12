using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuikSharp;
using QuikSharp.DataStructures;
using QuikSharp.DataStructures.Transaction;

namespace GrokOptions
{
    public class TradingEngine
    {
        private readonly Quik _quik;
        private readonly Settings _settings;
        private readonly List<Position> _positions = new List<Position>();
        private readonly List<TransactionReply> _transactionReplies = new List<TransactionReply>();
        private readonly string _clientCode;
        private readonly char _decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
        private readonly object _lock = new object();

        public TradingEngine(Quik quik, Settings settings)
        {
            _quik = quik ?? throw new ArgumentNullException(nameof(quik));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _clientCode = _quik.Class.GetClientCode().Result;
            _quik.Events.OnTransReply += OnTransReply;
        }
        private double MaxDrawdownLimit = 0.1; // 10% от капитала
        private double Capital = 1000000; // Пример капитала, вынеси в Settings

        public double CalculateKellySizing(double probWin = 0.6, double odds = 2.0) // p=вероятность прибыли, odds=отношение profit/loss
        {
            return (probWin * odds - (1 - probWin)) / odds; // Kelly fraction
        }

        //private bool CheckLimitsBeforeOpen(Position pos, Strategy strategy)
        //{
        //    double sizing = CalculateKellySizing(strategy.ProbabilityOfProfit, 2.0); // Пример odds
        //    pos.Quantity = (int)(sizing * Capital / pos.EntryPrice); // Adjust size

        //    // Проверка drawdown: симулируем потенциальный DD
        //    double potentialDD = strategy.StressTest() / Capital;
        //    if (potentialDD > MaxDrawdownLimit)
        //    {
        //        Log("Position rejected: Exceeds max drawdown limit");
        //        return false;
        //    }
        //    return true;
        //}
        public async Task VerifyingPositionsAsync()
        {
            try
            {
                var trades = await _quik.Trading.GetTrades().ConfigureAwait(false);


                foreach (var position in _positions)
                {
                    await UpdateEntranceOrderAsync(position, trades);
                    await UpdateClosingOrderAsync(position, trades);
                }

            }
            catch (Exception ex)
            {
                LogError($"Ошибка в VerifyingPositions: {ex.Message}");
            }
        }

        private async Task UpdateEntranceOrderAsync(Position position, List<Trade> trades)
        {
            if (position.EntranceOrderID <= 0 || position.State == State.Canceled)
                return;

            try
            {
                var order = await _quik.Orders.GetOrder_by_transID(position.ClassCode, position.SecurityCode, position.EntranceOrderID)
                    .ConfigureAwait(false);

                if (order == null)
                {
                    LogWarning($"Входная заявка {position.EntranceOrderID} не найдена или отменена");
                    position.State = State.Canceled;
                    return;
                }

                position.EntranceOrderNumber = order.OrderNum;
                position.State = order.State;
                position.EntranceOrderBalance = order.Balance;

                if (order.Flags.HasFlag(OrderTradeFlags.Active))
                {
                    await HandleActiveOrderAsync(order, position);
                }

                if (order.Balance < order.Quantity)
                {
                    UpdatePositionFromTrades(position, trades, order, true);
                }

                if (position.ToolQty == position.EntranceOrderQty && order.Balance == 0)
                {
                    ClearEntranceOrder(position);
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка обработки входной заявки {position.EntranceOrderID}: {ex.Message}");
            }
        }

        private async Task UpdateClosingOrderAsync(Position position, List<Trade> trades)
        {
            if (position.ClosingOrderID <= 0 || position.State == State.Canceled)
                return;

            try
            {
                var order = await _quik.Orders.GetOrder_by_transID(position.ClassCode, position.SecurityCode, position.ClosingOrderID)
                    .ConfigureAwait(false);

                if (order == null)
                {
                    LogWarning($"Выходная заявка {position.ClosingOrderID} не найдена или отменена");
                    position.State = State.Canceled;
                    return;
                }

                position.ClosingOrderNumber = order.OrderNum;
                position.State = order.State;
                position.ClosingOrderBalance = order.Balance;

                if (order.Flags.HasFlag(OrderTradeFlags.Active))
                {
                    await HandleActiveOrderAsync(order, position);
                }

                if (order.Balance < order.Quantity)
                {
                    UpdatePositionFromTrades(position, trades, order, false);
                }

                if (position.ToolQty == 0 && order.Balance == 0)
                {
                    ClearClosingOrder(position);
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка обработки выходной заявки {position.ClosingOrderID}: {ex.Message}");
            }
        }

        private async Task HandleActiveOrderAsync(Order order, Position position)
        {
            if (_settings.LifeTimeOrder == "00:00:00")
                return;

            var addTime = _settings.LifeTimeOrder.Split(':');
            var duration = new TimeSpan(0, int.Parse(addTime[0]), int.Parse(addTime[1]), int.Parse(addTime[2]));
            var orderEndTime = ((DateTime)order.Datetime).Add(duration);

            if (DateTime.Now > orderEndTime)
            {
                await _quik.Orders.KillOrder(order).ConfigureAwait(false);
                LogInfo($"Отменена просроченная заявка: {order.OrderNum}");
            }
        }

        private void UpdatePositionFromTrades(Position position, List<Trade> trades, Order order, bool isEntrance)
        {
            decimal price = 0;
            int quantity = 0;

            foreach (var trade in trades.Where(t => t.OrderNum == order.OrderNum))
            {
                position.StockFee += trade.ExchangeComission;
                price = quantity == 0 ? (decimal)trade.Price : (quantity * price + trade.Quantity * (decimal)trade.Price) / (quantity + trade.Quantity);
                quantity += trade.Quantity;

                if (isEntrance)
                {
                    position.ToolQty += position.Operation == Operation.Buy ? trade.Quantity : -trade.Quantity;
                }
                else
                {
                    position.ToolQty += position.Operation == Operation.Buy ? -trade.Quantity : trade.Quantity;
                }
            }

            if (isEntrance)
            {
                position.PriceEntrance = price;
                position.DateTimeEntrance = (QuikDateTime)DateTime.Now;
            }
            else
            {
                position.PriceClosing = price;
                position.DateTimeClosing = (QuikDateTime)DateTime.Now;
            }
        }

        private void ClearEntranceOrder(Position position)
        {
            position.EntranceOrderNumber = 0;
            position.EntranceOrderID = 0;
            position.EntranceOrderQty = 0;
            position.EntranceOrderBalance = 0;
        }

        private void ClearClosingOrder(Position position)
        {
            position.ClosingOrderNumber = 0;
            position.ClosingOrderID = 0;
            position.ClosingOrderQty = 0;
            position.ClosingOrderBalance = 0;
            position.DateTimeClosing = (QuikDateTime)DateTime.Now;
        }

        private void OnTransReply(TransactionReply reply)
        {
            //if (reply.SecCode != "SiH2" || _transactionReplies.Contains(reply))
            //    return;
            if (_transactionReplies.Contains(reply))
                return;

            lock (_lock)
            {
                _transactionReplies.Add(reply);
                var position = _positions.FirstOrDefault(p => p.EntranceOrderID == reply.TransID || p.ClosingOrderID == reply.TransID);

                if (position != null)
                {
                    position.State = reply.Status == 3 ? State.Active : reply.Status > 3 ? State.Canceled : position.State;
                    position.Message = reply.ResultMsg;
                }
            }
        }

        private async Task<long> NewOrderAsync(Tool tool, Operation operation, decimal price, int qty)
        {
            if (_settings.RobotMode != RobotMode.Prod)
                return 0;

            var order = new Order
            {
                ClassCode = tool.ClassCode,
                SecCode = tool.SecurityCode,
                Operation = operation,
                Price = price,
                Quantity = qty,
                ClientCode = _clientCode,
                Account = tool.AccountID
            };

            try
            {
                return await _quik.Orders.CreateOrder(order).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError($"Не удалось создать заявку: {ex.Message}");
                return 0;
            }
        }

        public List<Position> GetPositions()
        {
            lock (_lock)
            {
                return new List<Position>(_positions);
            }
        }

        public async Task<long> OpenPositionAsync(Operation operation, decimal price, int qty, Tool tool, RobotMode robotMode)
        {
            var position = new Position
            {
                EntranceOrderQty = qty * tool.Lot,
                ToolQty = 0,
                DateTimeEntrance = (QuikDateTime)DateTime.Now,
                ClassCode = tool.ClassCode,
                SecurityCode = tool.SecurityCode,
                Operation = operation,
                RobotMode = 0,
                PositionID = _positions.Count
            };

            lock (_lock)
            {
                _positions.Add(position);
            }

            if (robotMode == RobotMode.Prod)
            {
                position.EntranceOrderID = await NewOrderAsync(tool, operation, price - _settings.KoefSlip, position.EntranceOrderQty);
                if (position.EntranceOrderID != 0)
                {
                    position.State = State.Active;
                    LogInfo($"Создана входная заявка ID: {position.EntranceOrderID}, Цена: {price}");
                }
                else
                {
                    LogError("Не удалось создать новую заявку");
                }
            }
            else
            {
                var rnd = new Random();
                position.EntranceOrderID = rnd.Next();
                position.EntranceOrderNumber = rnd.Next();
                position.PositionID = rnd.Next();
                position.PriceEntrance = price;
                LogInfo("Создана тестовая заявка");
            }

            return position.EntranceOrderID;
        }

        public async Task<long> ClosePositionAsync(long posId, decimal price, int qty, Tool tool)
        {

            var position = _positions.FirstOrDefault(p => p.PositionID == posId);
            if (position == null)
            {
                LogError($"Позиция {posId} не найдена");
                return 0;
            }

            if (_settings.RobotMode == RobotMode.Prod && position.ToolQty != 0)
            {
                var operation = position.Operation == Operation.Buy ? Operation.Sell : Operation.Buy;
                var adjustedPrice = operation == Operation.Buy ? price - _settings.KoefSlip : price + _settings.KoefSlip;
                position.ClosingOrderID = await NewOrderAsync(tool, operation, adjustedPrice, qty * tool.Lot);

                position.PriceClosing = price;
                position.DateTimeClosing = (QuikDateTime)DateTime.Now;
                position.ClosingOrderQty = qty * tool.Lot;
            }
            else
            {
                position.DateTimeClosing = (QuikDateTime)DateTime.Now;
                position.PriceClosing = price;
                position.ClosingOrderID = 999;
                position.ClosingOrderQty = qty * tool.Lot;
            }

            return position.ClosingOrderID;

        }

        private void LogInfo(string message) => Console.WriteLine($"[INFO] {message}");
        private void LogWarning(string message) => Console.WriteLine($"[WARNING] {message}");
        private void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
    }
}