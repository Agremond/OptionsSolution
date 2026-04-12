using QuikSharp;
using QuikSharp.DataStructures;
using System;
using MoexOptionsPricer;
//    Рассчет греков https:/forum.quik.ru/forum10/topic4401/
namespace GrokOptions
{
    public class Option
    {
        Char separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
        double ask;
        double bid;
        double delta;
        double gamma;
        double vega;
        double tetta;
        double theorprice;
        public double volatiity;
        public double HV;
        public double RV;
        public double askIV;
        public double midIV;
        public double bidIV;
        public string type;
        string ba;
        string securitycode;
        string classCode;
        string name;
        string expdate;
        int days_to_exp;
        int lot;
        int priceAccuracy;
        double buyDepo;
        double sellDepo;
        double step;
        double stepPrice;
        double slip;
        double lastPrice;
        int steps;
        double strike;
        BlackScholesImpliedVolatility bsi;

        #region Свойства
        /// <summary>
        /// Краткое наименование инструмента (бумаги)
        /// </summary>
        public string Name { get { return name; } }
        /// <summary>
        /// Дата экспирации
        /// </summary>
        public string ExpDate { get { return expdate; } }
        /// <summary>
        /// Значение грека Delta
        /// </summary>
        public double Delta
        {
            get
            { return delta; }
            set
            { delta = value; }
        }
        /// <summary>
        /// Значение грека Gamma
        /// </summary>
        public double Gamma
        {
            get
            { return gamma; }
            set
            { gamma = value; }
        }
        /// <summary>
        /// Значение грека Vega
        /// </summary>
        public double Vega
        {
            get
            { return vega; }
            set
            { vega = value; }
        }
        /// <summary>
        /// Значение грека Tetta
        /// </summary>
        public double Tetta
        {
            get
            { return tetta; }
            set
            { tetta = value; }
        }
        /// <summary>
        /// Волатильность
        /// </summary>
        public double Volatility { get { return volatiity; } }
        /// <summary>
        /// Теоретическая цена биржи
        /// </summary>
        public double Theorprice { get { return theorprice; } }
        /// <summary>
        /// Базовый актив
        /// </summary>
        public string BaseActive { get { return ba; } }
        /// <summary>
        /// Шаг проскальзывания цены
        /// </summary>
        public double Slip { get { return slip; } }
        /// <summary>
        /// Код инструмента (бумаги)
        /// </summary>
        public string seccode { get { return securitycode; } }
        /// <summary>
        /// Код класса инструмента (бумаги)
        /// </summary>
        public string ClassCode { get { return classCode; } }
        /// <summary>
        /// Лот
        /// </summary>
        public int Lot { get { return lot; } }
        /// <summary>
        /// Точность цены (количество знаков после запятой)
        /// </summary>
        public int PriceAccuracy { get { return priceAccuracy; } }
        /// <summary>
        /// Шаг цены
        /// </summary>
        public double Step { get { return step; } }
        /// <summary>
        /// Стоимость шага цены
        /// </summary>
        public double StepPrice { get { return stepPrice; } }
        /// <summary>
        /// Лучшее предложение
        /// </summary>
        public double Ask
        {
            get
            { return ask; }
            set
            { ask = value; }
        }

        /// <summary>
        /// Лучший спрос
        /// </summary>
        public double Bid
        {
            get
            { return bid; }
            set
            { bid = value; }
        }
        ///  /// <summary>
        /// Гарантийное обеспечение продавца (только для срочного рынка)
        /// для фондовой секции = 0
        /// </summary>
        ///  
        public double SellDepo { get { return sellDepo; } }
        ///  /// <summary>
        /// Гарантийное обеспечение покупателя (только для срочного рынка)
        /// для фондовой секции = 0
        /// </summary>
        ///  
        public double BuyDepo { get { return buyDepo; } }
        /// <summary>
        /// Цена последней сделки
        /// </summary>
        /// 
        public double LastPrice
        {
            get
            { return lastPrice; }
            set
            { lastPrice = value; }
        }

        public double Strike
        {
            get
            { return strike; }

        }
        /// <summary>
        /// IV Лучшего предложения
        /// </summary>
        public double AskIV
        {
            get
            { return askIV; }
        }

        /// <summary>
        /// IV Лучшего спроса
        /// </summary>
       public double BidIV
        {
            get
            { return bidIV; }
        }
#endregion

/// <summary>
/// Конструктор класса
/// </summary>
/// <param name="OptionBoard">Опцион</param>
public Option(OptionBoard option_brd)
        {
            securitycode = option_brd.Code;
            volatiity = option_brd.Volatility;
            ba = option_brd.OPTIONBASE;
            ask = option_brd.OFFER;
            bid = option_brd.BID;
            askIV = 0;
            bidIV = 0;
            name = option_brd.Name;
            type = option_brd.OPTIONTYPE;
            days_to_exp = option_brd.DAYSTOMATDATE;
            lastPrice = option_brd.LastPrice;
            theorprice = option_brd.TheorPrice;
            stepPrice = option_brd.StepPrice;
            step = option_brd.Step;
            expdate = option_brd.ExpDate;
            classCode = "SPBOPT";
            strike = option_brd.Strike;
            buyDepo = option_brd.BuyDepo;
            sellDepo = option_brd.SellDepo;
            steps = 100;
            bsi = new BlackScholesImpliedVolatility();
        }


        public void UpdatePrices(Quik _quik)
        {
            ask = Convert.ToDouble(_quik.Trading.GetParamEx(classCode, seccode, "OFFER").Result.ParamValue.Replace('.', separator));
            bid = Convert.ToDouble(_quik.Trading.GetParamEx(classCode, seccode, "BID").Result.ParamValue.Replace('.', separator));
           
        }
        public void CalcIV(double futuresPrice)
        {
            try
            {

                //double sigma = 0.173;
                //double F = 81265;
                //double K = 81500;
                //double T = 23.0 / 365;
                //double r = 0.16;
                //int steps = 100;
                //double callPrice = AmericanFuturesOptionPricer.PriceAmericanFuturesOption(F, K, T, r, sigma, steps, "call");  // ≈ Получил 1712.0

                //double putPrice = AmericanFuturesOptionPricer.PriceAmericanFuturesOption(F, K, T, r, sigma, steps, "put");   // ≈ Получил 1217.0

                //double ivCall = AmericanFuturesOptionPricer.CalculateImpliedVolatility(F, K, T, r, callPrice, steps, "call");

                //double ivPut = AmericanFuturesOptionPricer.CalculateImpliedVolatility( F, K, T, r, putPrice, steps, "put");
                //double parityDiff = callPrice - putPrice;
                //double theoreticalDiff = Math.Exp(-r * T) * (F - K);

                //Console.WriteLine($"Market diff: {parityDiff:F2}");
                //Console.WriteLine($"Theory diff: {theoreticalDiff:F2}");
                //Console.WriteLine($"Error: {Math.Abs(parityDiff - theoreticalDiff):F2}");

                //double callPrice = AmericanFuturesOptionPricer_BAW.PriceAmericanFuturesOption(F, K, T, r, sigma, "call");
                //double putPrice = AmericanFuturesOptionPricer_BAW.PriceAmericanFuturesOption(F, K, T, r, sigma, "put");

                double F = futuresPrice;
                double K = strike;     // страйк
                double T = days_to_exp / 365.0;
                double r = 0.16;     // 5%
                double sigma = 0.16; // 30%

              
                // Параметры контракта SI
                double contractMultiplier = 1;     // 1 пункт = 1 рубль
                double priceStep = 100;            // шаг цены = 100 пунктов

                askIV = BlackScholesImpliedVolatility.ImpliedVolatility(
                    ask,
                    F: F,
                    K: K,
                    T: T,
                    r: r,
                    type.ToLower() == "call" ? MoexOptionsPricer.OptionType.Call : MoexOptionsPricer.OptionType.Put,
                    contractMultiplier: contractMultiplier,
                    priceStep: priceStep,
                    isAmerican: true);

                bidIV = BlackScholesImpliedVolatility.ImpliedVolatility(
                    bid,
                    F: F,
                    K: K,
                    T: T,
                    r: r,
                    type.ToLower() == "call" ? MoexOptionsPricer.OptionType.Call : MoexOptionsPricer.OptionType.Put,
                    contractMultiplier: contractMultiplier,
                    priceStep: priceStep,
                    isAmerican: true);


                //askIV = bsi.(futuresPrice, strike, days_to_exp/365.0, sigma, ask, type.ToLower() == "call");
                //bidIV = OptionPricer.CalculateImpliedVolatility(futuresPrice, strike, days_to_exp/365.0, sigma, bid, type.ToLower() == "call");

                // Parity check
                //double marketDiff = callPrice - putPrice;
                //double theoryDiff = Math.Exp(-r * T) * (F - K);
                //Console.WriteLine($"Parity error: {marketDiff - theoryDiff:F4}");

            }
            catch (Exception ex)
            {
                ;
            }
           
        }

    }
}