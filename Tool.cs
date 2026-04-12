using QuikSharp;
using System;
namespace GrokOptions
{
    public class Tool
    {
        Char separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

        Quik _quik;
        string name;
        string securityCode;
        string classCode;
        string sectype;
        string expdate;
        
        int days_to_mat;
        //   string clientCode;
        string accountID;
        string firmID;
        int lot;
        int priceAccuracy;
        double buydepo;
        double selldepo;
        decimal step;
        decimal stepPrice;
        decimal slip;
        decimal lastPrice;

        decimal value;
        double ask;
        double bid;


        #region Свойства
        /// <summary>
        /// Краткое наименование инструмента (бумаги)
        /// </summary>
        public string Name { get { return name; } }


        /// <summary>
        /// Шаг проскальзывания цены
        /// </summary>
        public decimal Slip { get { return slip; } }
        /// <summary>
        /// Код инструмента (бумаги)
        /// </summary>
        public string SecurityCode { get { return securityCode; } }
        /// <summary>
        /// Код класса инструмента (бумаги)
        /// </summary>
        public string ClassCode { get { return classCode; } }
        /// <summary>
        /// Счет клиента
        /// </summary>
        public string AccountID { get { return accountID; } }
        /// <summary>
        /// Код фирмы
        /// </summary>
        public string FirmID { get { return firmID; } }
        /// <summary>
        /// Количество акций в одном лоте
        /// Для инструментов класса SPBFUT = 1
        /// </summary>
        public int Lot { get { return lot; } }
        /// <summary>
        /// Точность цены (количество знаков после запятой)
        /// </summary>
        public int PriceAccuracy { get { return priceAccuracy; } }
        /// <summary>
        /// Шаг цены
        /// </summary>
        public decimal Step { get { return step; } }
        /// <summary>
        /// Стоимость шага цены
        /// </summary>
        public decimal StepPrice { get { return stepPrice; } }
        /// <summary>
        /// Номинал купона
        /// </summary>
        public decimal Value { get { return value; } }

        /// ///  /// <summary>
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
        /// <summary>
        /// Дата экспирации инструмента (бумаги)
        /// </summary>
        public string ExpDate
        { 
            get { return expdate; }
        }
        /// <summary>
        /// Тип инструмента (бумаги)
        /// </summary>
        public string SecType
        {
            get { return sectype; }
            set { sectype = value; }
        }
        /// <summary>
        /// Гарантийное обеспечение покуптеля
        /// </summary>
        public double BuyDepo { get { return buydepo; } }
        /// <summary>
        /// Гарантийное обеспечение продавца
        /// </summary>
        public double SellDepo { get { return selldepo; } }
        /// <summary>
        /// Цена последней сделки
        /// </summary>
        /// 
        public decimal LastPrice
        {
            get
            {
                lastPrice = Convert.ToDecimal(_quik.Trading.GetParamEx(classCode, securityCode, "LAST").Result.ParamValue.Replace('.', separator));
                return lastPrice;
            }
        }

        #endregion

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="_quik"></param>
        /// <param name="SecurityCode">Код инструмента</param>
        /// <param name="ClassCode">Код класса</param>
        public Tool(Quik quik, string SecurityCode, int koefSlip)
        {
            _quik = quik;
            GetBaseParam(quik, SecurityCode, koefSlip);


            ask = 0;
            bid = 0;


        }

        void GetBaseParam(Quik quik, string secCode, int _koefSlip)
        {
            try
            {
                securityCode = secCode;

                if (quik != null)
                {
                    classCode = quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,EQOB", secCode).Result;
                    if (classCode != null && classCode != "")
                    {
                        try
                        {
                            name = quik.Class.GetSecurityInfo(classCode, securityCode).Result.ShortName;
                            accountID = quik.Class.GetTradeAccount(classCode).Result;
                            firmID = quik.Class.GetClassInfo(classCode).Result.FirmId;
                            step = Convert.ToDecimal(quik.Trading.GetParamEx(classCode, securityCode, "SEC_PRICE_STEP").Result.ParamValue.Replace('.', separator));
                            stepPrice = Convert.ToDecimal(quik.Trading.GetParamEx(classCode, securityCode, "STEPPRICE").Result.ParamValue.Replace('.', separator));
                            priceAccuracy = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(classCode, securityCode, "SEC_SCALE").Result.ParamValue.Replace('.', separator)));

                            expdate = Convert.ToString(quik.Trading.GetParamEx(classCode, securityCode, "SEC_SCALE").Result.ParamValue.Replace('.', separator));
                            sectype = Convert.ToString(quik.Trading.GetParamEx(classCode, securityCode, "SECTYPE").Result.ParamValue.Replace('.', separator));
                            expdate = Convert.ToString(quik.Trading.GetParamEx(classCode, securityCode, "MAT_DATE").Result.ParamValue.Replace('.', separator));
                            days_to_mat = Convert.ToInt32(Convert.ToDecimal(quik.Trading.GetParamEx(classCode, securityCode, "DAYS_TO_MAT_DATE").Result.ParamValue.Replace('.', separator)));

                            value = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(classCode, securityCode, "SEC_FACE_VALUE").Result.ParamValue.Replace('.', separator)));

                            slip = _koefSlip * step;



                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Tool.GetBaseParam. Ошибка получения наименования для " + securityCode + ": " + e.Message);
                        }

                        if (classCode == "SPBFUT")
                        {
                            Console.WriteLine("Получаем 'selldepo/buydepo'.");
                            lot = 1;
                            selldepo = Convert.ToDouble(quik.Trading.GetParamEx(classCode, securityCode, "SELLDEPO").Result.ParamValue.Replace('.', separator));
                            buydepo = Convert.ToDouble(quik.Trading.GetParamEx(classCode, securityCode, "BUYDEPO").Result.ParamValue.Replace('.', separator));
                        }
                        else
                        {
                            Console.WriteLine("Получаем 'lot'.");
                            lot = Convert.ToInt32(Convert.ToDouble(quik.Trading.GetParamEx(classCode, securityCode, "LOTSIZE").Result.ParamValue.Replace('.', separator)));
                            selldepo = 0;
                            buydepo = 0;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Tool.GetBaseParam. Ошибка: classCode не определен.");
                        lot = 0;
                        selldepo = 0;
                        buydepo = 0;
                    }
                }
                else
                {
                    Console.WriteLine("Tool.GetBaseParam. quik = null !");
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Ошибка NullReferenceException в методе GetBaseParam: " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка в методе GetBaseParam: " + e.Message);
            }
        }
    }
}