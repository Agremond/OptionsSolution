using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;
using MathNet.Numerics.Statistics;

namespace GrokOptions
{
    public enum StrategyType
    {
        Custom,
        ButterflyCall,
        ButterflyPut,
        IronCondor
    }

    public enum OptionType
    {
        Call,
        Put
    }

    /// <summary>
    /// Represents a trading strategy with its parameters and components.
    /// </summary>
    public class Strategy
    {
        #region Constants
        private const double STRIKE_STEP = 250;
        private const double RANGE_OFFSET = 30000;
        private const double MAX_RANGE_EXTENSION = 40000;
        #endregion

        #region Properties
        public List<Position> Members { get; } = new List<Position>();
        public string Id { get; set; }
        public double Delta { get; set; }
        public double Theta { get; set; } // было Tetta
        public double Gamma { get; set; }
        public double Vega { get; set; }
        public string Name { get; set; }
        public double Profit { get; set; }
        public double Cost { get; private set; }
        public double PlanPL { get; set; }
        public double Mean { get; private set; }
        public string ExpDate { get; set; }
        public State State { get; set; }
        public double MinStrike { get; set; }
        public double MaxStrike { get; set; }
        public DataTable ExpProfile { get; private set; }
        public double ProbabilityOfProfit { get; set; }

        // Новые свойства для расширенной функциональности
        public StrategyType Type { get; set; } = StrategyType.Custom;
        public double UnderlyingPrice { get; set; }
        public double ImpliedVolatility { get; set; } = 0.20;
        public double RiskFreeRate { get; set; } = 0.08; // RUONIA / ключевая ставка
        public double TimeToExpirationYears { get; set; } = 0.083; // ~1 месяц
        public double PointValue { get; set; } = 1.0; // можно переопределять для разных инструментов
        #endregion

        #region Constructor
        public Strategy()
        {
            ExpProfile = InitializeExpirationProfile("ExpProfile");
            MinStrike = 0;
            MaxStrike = 0;
            Mean = 0;
            ProbabilityOfProfit = 0;
       
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the total cost of the strategy based on position orders.
        /// </summary>
        public void CalculateCost()
        {
            if (Members == null || Members.Count == 0)
            {
                Cost = 0;
                return;
            }
            double totalCost = 0;
            foreach (var position in Members)
            {
                if (position == null)
                    continue;

                if (!position.IsFutures)
                {
                    if (position.Option == null)
                        continue;
                    totalCost += position.EntranceOrderQty > 0
                        ? -position.Option.Ask * Math.Abs(position.EntranceOrderQty)
                        : position.Option.Bid * Math.Abs(position.EntranceOrderQty);
                }
                else
                {
                    if (position.Tool == null || string.IsNullOrEmpty(position.SecurityCode))
                        continue;
                    totalCost += position.EntranceOrderQty > 0
                        ? -position.Tool.Ask * Math.Abs(position.EntranceOrderQty)
                        : position.Tool.Bid * Math.Abs(position.EntranceOrderQty);
                }
            }
            Cost = totalCost;
        }

        /// <summary>
        /// Calculates the expiration profile with the specified scale (оригинальный метод).
        /// </summary>
        public void CalculateExpirationProfile(int scale, double baseAssetPrice, double pointPrice, Histogram histogram)
        {
            if (scale <= 0 || histogram == null || Members == null || MinStrike >= MaxStrike)
            {
                throw new ArgumentException("Invalid input parameters for expiration profile calculation.");
            }

            ExpProfile.Clear();
            Mean = 0;
            double width = (MaxStrike - MinStrike + MAX_RANGE_EXTENSION) / scale;
            double cumulativeMean = 0;
            int bucketCount = 0;
            int previousBucket = -1;

            for (int i = 0; i < width; i++)
            {
                var row = ExpProfile.NewRow();
                double x = i * scale + baseAssetPrice - RANGE_OFFSET;
                row["X"] = x;
                double y = CalculatePositionValue(x, baseAssetPrice, pointPrice);
                row["Y"] = y + Cost;

                double delta = x - baseAssetPrice;
                if (delta > histogram.LowerBound && delta < histogram.UpperBound)
                {
                    var bucket = histogram.GetBucketOf(delta);
                    double probability = bucket.Count / histogram.DataCount;
                    row["P_Y"] = (y + Cost) * probability;
                    row["Count"] = bucket.Count;

                    int currentBucket = histogram.GetBucketIndexOf(delta);
                    if (currentBucket != previousBucket)
                    {
                        Mean += bucketCount > 0
                            ? (y + Cost) * probability + cumulativeMean / bucketCount
                            : (y + Cost) * probability;
                        previousBucket = currentBucket;
                        cumulativeMean = 0;
                        bucketCount = 0;
                    }
                    else
                    {
                        cumulativeMean += (y + Cost) * probability;
                        bucketCount++;
                    }
                }
                else
                {
                    row["P_Y"] = 0;
                }
                ExpProfile.Rows.Add(row);
            }

            UpdateProbabilityOfProfit();
        }

     

        /// <summary>
        /// Monte-Carlo симуляция профиля экспирации (новый метод)
        /// </summary>
        public DataTable CalculateExpirationProfileMC(
            int simulations = 10000,
            double? overrideVol = null,
            double drift = 0.0,
            int steps = 252)
        {
            double vol = overrideVol ?? ImpliedVolatility;
            var rng = new MersenneTwister();
            var normal = new Normal(0, 1, rng);

            var finalPrices = new List<double>(simulations);

            Parallel.For(0, simulations, i =>
            {
                double price = UnderlyingPrice;
                double dt = TimeToExpirationYears / steps;

                for (int step = 0; step < steps; step++)
                {
                    double z = normal.Sample();
                    price *= Math.Exp((drift - 0.5 * vol * vol) * dt + vol * Math.Sqrt(dt) * z);
                }
                lock (finalPrices) { finalPrices.Add(price); }
            });

            // Построение таблицы
            ExpProfile.Clear();

            double min = finalPrices.Min();
            double max = finalPrices.Max();
            int bins = 80;
            double binSize = (max - min) / bins;

            var binCounts = new Dictionary<double, int>();

            foreach (var p in finalPrices)
            {
                double bin = Math.Floor((p - min) / binSize) * binSize + binSize / 2;
                binCounts.TryGetValue(bin, out int count);
                binCounts[bin] = count + 1;
            }

            double total = finalPrices.Count;

            foreach (var kv in binCounts.OrderBy(x => x.Key))
            {
                var row = ExpProfile.NewRow();
                row["X"] = kv.Key;
                row["Y"] = CalculatePositionValue(kv.Key, UnderlyingPrice, PointValue);
                row["P_Y"] = (kv.Value / total) * (double)row["Y"];
                row["Count"] = kv.Value;
                ExpProfile.Rows.Add(row);
            }

            UpdateProbabilityOfProfit();
            return ExpProfile;
        }

        /// <summary>
        /// Загрузка исторических лог-доходностей с MOEX (для VaR, гистограмм и т.д.)
        /// </summary>
        public async Task<List<double>> LoadHistoricalLogReturnsAsync(
            string securityCode = "Si",
            DateTime? from = null,
            DateTime? to = null)
        {
            from ??= DateTime.Today.AddYears(-2);
            to ??= DateTime.Today;

            string url = $"https://iss.moex.com/iss/history/engines/futures/markets/forts/boards/RU/securities/{securityCode}.json" +
                         $"?from={from:yyyy-MM-dd}&till={to:yyyy-MM-dd}&interval=24";

            using var client = new HttpClient();
            try
            {
                string json = await client.GetStringAsync(url);
                var doc = JsonDocument.Parse(json);

                var dataRows = doc.RootElement
                    .GetProperty("history")
                    .GetProperty("data")
                    .EnumerateArray();

                var closes = new List<double>();
                foreach (var row in dataRows)
                {
                    if (row.GetArrayLength() > 4 && row[4].ValueKind != JsonValueKind.Null)
                        closes.Add(row[4].GetDouble()); // CLOSE
                }

                var logReturns = new List<double>();
                for (int i = 1; i < closes.Count; i++)
                {
                    if (closes[i - 1] > 0)
                        logReturns.Add(Math.Log(closes[i] / closes[i - 1]));
                }

                return logReturns;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки MOEX данных: {ex.Message}");
                return new List<double>();
            }
        }

        /// <summary>
        /// Value at Risk (исторический метод)
        /// </summary>
        public async Task<double> CalculateVaRAsync(double confidence = 0.95)
        {
            var returns = await LoadHistoricalLogReturnsAsync();
            if (!returns.Any()) return 0;

            returns.Sort();
            int index = (int)(returns.Count * (1 - confidence));
            double worstReturn = returns[index];

            return -worstReturn * UnderlyingPrice * PointValue;
        }

        /// <summary>
        /// Stress-тест с повышенной волатильностью
        /// </summary>
        public DataTable StressTest(double volMultiplier = 1.5)
        {
            return CalculateExpirationProfileMC(overrideVol: ImpliedVolatility * volMultiplier);
        }

        #endregion

        #region Private Methods

        private DataTable InitializeExpirationProfile(string tableName)
        {
            var dataTable = new DataTable(tableName);
            dataTable.Columns.AddRange(new[]
            {
                new DataColumn("X", typeof(double)),
                new DataColumn("Y", typeof(double)),
                new DataColumn("P_Y", typeof(double)),
                new DataColumn("Count", typeof(int))
            });
            dataTable.PrimaryKey = new[] { dataTable.Columns["X"] };
            return dataTable;
        }

        private double CalculatePositionValue(double x, double baseAssetPrice, double pointPrice)
        {
            double value = 0;
            foreach (var position in Members)
            {
                if (position == null) continue;

                if (!position.IsFutures)
                {
                    if (position.Option == null) continue;
                    double strike = position.Option.Strike;
                    string type = position.Option.type?.ToLower() ?? "";

                    if (type == "call" && x > strike)
                        value += Math.Abs(strike - x) * position.EntranceOrderQty * pointPrice;
                    else if (type == "put" && x < strike)
                        value += Math.Abs(strike - x) * position.EntranceOrderQty * pointPrice;
                }
                else
                {
                    if (position.Tool == null) continue;
                    value += x < baseAssetPrice
                        ? -Math.Abs(baseAssetPrice - x) * position.EntranceOrderQty * pointPrice
                        : Math.Abs(baseAssetPrice - x) * position.EntranceOrderQty * pointPrice;
                }
            }
            return value;
        }

        private void UpdateProbabilityOfProfit()
        {
            if (ExpProfile == null || ExpProfile.Rows.Count == 0)
            {
                ProbabilityOfProfit = 0;
                return;
            }

            double positive = ExpProfile.AsEnumerable()
                .Count(r => r.Field<double>("Y") > 0);

            ProbabilityOfProfit = positive / ExpProfile.Rows.Count;
        }

        #endregion
    }
}