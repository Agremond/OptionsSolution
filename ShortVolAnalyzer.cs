using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json; // или System.Text.Json, если используешь его
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using MathNet.Numerics.Statistics;
using QuikSharp;
using QuikSharp.DataStructures;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OptionsSolution
{
    public class ShortVolAnalyzer
    {
        private readonly TradingEngine _tradingEngine; // или Quik _quik, если напрямую
        private readonly Instrument _instrument;       // ссылка на текущую форму/инструмент (Si)
        private readonly TelegramBotClient _telegram; // существующий бот из Instrument.cs
        private readonly HttpClient _httpClient = new HttpClient();

        // Настраиваемые параметры (можно вынести в Settings.cs позже)
        public double IvThresholdPoints { get; set; } = 4.0;      // пунктов выше theo vol
        public double IvToRvMultiplier { get; set; } = 1.35;      // IV > RV * multiplier
        public double SigmaThreshold { get; set; } = 1.8;         // в сигмах от среднего спреда
        public int MinDaysToExp { get; set; } = 7;
        public int MaxDaysToExp { get; set; } = 45;
        public double StrikeRangePercent { get; set; } = 0.20;    // ±20% от ATM

        public ShortVolAnalyzer(TradingEngine tradingEngine, Instrument instrument, TelegramBotClient telegram)
        {
            _tradingEngine = tradingEngine;
            _instrument = instrument;
            _telegram = telegram;
        }

        /// <summary>
        /// Главный метод анализа — вызывать по таймеру (каждые 30-60 сек в торговое время)
        /// </summary>
        public async Task AnalyzeAsync()
        {
            if (!IsTradingTime()) return;

            var siFuture = await GetCurrentSiFuturePriceAsync(); // через QUIK
            if (siFuture <= 0) return;

            var series = GetRelevantSeries(); // ближние 1-3 экспирации
            if (series == null || !series.Any()) return;

            foreach (var ser in series)
            {
                var daysToExp = (ser.ExpirationDate - DateTime.Today).TotalDays;
                if (daysToExp < MinDaysToExp || daysToExp > MaxDaysToExp) continue;

                var atmStrikes = GetAtmStrikes(siFuture, ser, StrikeRangePercent);

                var marketIv = CalculateMarketIv(atmStrikes, ser);           // через AmericanFuturesOptionPricer
                var theoVol = await GetTheoVolFromMoexApiAsync(ser);         // REST API НКЦ
                var rv30d = await GetRealizedVolAsync(30);                    // уже есть в Strategy.cs

                var signal = EvaluateSignal(marketIv, theoVol, rv30d, daysToExp);

                if (signal.IsHighVol)
                {
                    var structure = SuggestShortVolStructure(atmStrikes, ser, marketIv);
                    await SendTelegramSignalAsync(signal, structure, ser);
                }
            }
        }

        private bool IsTradingTime()
        {
            // используй существующий код из Instrument.cs (worktime0/1/2 или твои проверки сессий)
            return true; // заглушка — замени на реальную проверку
        }

        private async Task<double> GetCurrentSiFuturePriceAsync()
        {
            // Через TradingEngine или _quik.GetParamEx("Si", "LAST") или стакан
            // Пример: return await _tradingEngine.GetLastPriceAsync("Si");
            return 0; // заглушка
        }

        private List<Series> GetRelevantSeries()
        {
            // Используй существующий код загрузки серий для Si из Instrument/Workspace
            return new List<Series>();
        }

        private List<Strike> GetAtmStrikes(double futurePrice, Series series, double rangePercent)
        {
            double minStrike = futurePrice * (1 - rangePercent);
            double maxStrike = futurePrice * (1 + rangePercent);

            return series.Strikes
                .Where(s => s.Strike >= minStrike && s.Strike <= maxStrike)
                .OrderBy(s => s.Strike)
                .ToList();
        }

        private double CalculateMarketIv(List<Strike> atmStrikes, Series series)
        {
            // Используем твой AmericanFuturesOptionPricer
            // Берём ATM или 25-delta, считаем IV по mid-price
            // Пример:
            var pricer = new AmericanFuturesOptionPricer();
            // pricer.ImpliedVolatility(...) для нескольких страйков → среднее или по 25-delta
            return 0.25; // заглушка — реализуй через существующий метод
        }

        private async Task<double> GetTheoVolFromMoexApiAsync(Series series)
        {
            // Пример вызова бесплатного Option Calc API
            try
            {
                string url = $"https://iss.moex.com/iss/apps/option-calc/v1/volatilityCurve?asset=Si&series={series.Code}";
                var response = await _httpClient.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<dynamic>(response);
                // Парсим поле с теоретической волатильностью (ATM или кривая)
                return (double)data?.volatility ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetRealizedVolAsync(int days)
        {
            // Уже реализовано в Strategy.cs (LoadHistoricalLogReturnsAsync)
            // Используй существующий метод
            return 0.20; // заглушка
        }

        private (bool IsHighVol, double ExcessPoints, double Sigma) EvaluateSignal(
            double marketIv, double theoVol, double rv, double daysToExp)
        {
            double excess = marketIv - theoVol;
            // Здесь можно добавить расчёт исторического среднего спреда и сигм (нужен небольшой кэш)
            bool highVol = excess > IvThresholdPoints && marketIv > rv * IvToRvMultiplier;

            return (highVol, excess, 0);
        }

        private object SuggestShortVolStructure(List<Strike> atmStrikes, Series series, double iv)
        {
            // Используй логику поиска кондоров/спредов из твоего проекта
            // Предлагаем Short Strangle или Iron Condor на ±10-15% 
            return new { Description = "Short Strangle: Sell OTM Call + Sell OTM Put", Credit = "≈450-550 руб." };
        }

        private async Task SendTelegramSignalAsync(object signal, object structure, Series series)
        {
            string message = $"🚨 SHORT VOL SIGNAL Si\n" +
                             $"IV market: { /* marketIv */ }% | Theo NCC: { /* theo */ }% | RV30: { /* rv */ }%\n" +
                             $"Экспирация: {series.ExpirationDate:dd.MM.yyyy}\n" +
                             $"Структура: {structure}\n" +
                             $"Превышение: +{ /* excess */ } пп";

            await _telegram.SendMessageAsync(/* chatId */, message);
        }
    }
}