using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace GrokOptions
{
    public class Backtester
    {
        public List<double> Simulate(Strategy strategy, List<(DateTime Date, double UnderlyingPrice)> historicalData)
        {
            List<double> pnlHistory = new List<double>();
            double currentPnl = 0;

            foreach (var dataPoint in historicalData)
            {
                // Симуляция: обновляем underlying, рассчитываем profile на "экспирацию" (end of day)
                strategy.UnderlyingPrice = dataPoint.UnderlyingPrice;
                var dailyProfile = strategy.CalculateExpirationProfileMC();
                currentPnl += dailyProfile.Rows.Cast<DataRow>().Average(row => (double)row["Y"]); // Средний P&L (упрощённо)
                pnlHistory.Add(currentPnl);
            }

            return pnlHistory; // Для дальнейшего анализа (e.g., Sharpe = mean(ret) / std(ret) * sqrt(252))
        }

        public List<(DateTime, double)> LoadFromCsv(string filePath)
        {
            return File.ReadAllLines(filePath).Skip(1).Select(line =>
            {
                var parts = line.Split(',');
                return (DateTime.Parse(parts[0]), double.Parse(parts[1])); // Date, Close
            }).ToList();
        }

        // Или из БД: используй DatabaseOperations для SELECT
    }

    // Использование: var backtester = new Backtester(); var data = await strategy.LoadHistoricalReturnsAsync(); // Конвертируй в (Date, Price)
}
