using System;

namespace MoexOptionsPricer
{
    public enum OptionType { Call, Put }

    public  class BlackScholesImpliedVolatility
    {
        private const double Tolerance = 1e-8;
        private const int MaxIterations = 100;

        // === Black-76 для европейских опционов на фьючерсы ===
        public static double Black76Price(double F, double K, double T, double r, double sigma, OptionType type)
        {
            if (T <= 0) return Math.Max(type == OptionType.Call ? F - K : K - F, 0) * Math.Exp(-r * T);

            double d1 = (Math.Log(F / K) + 0.5 * sigma * sigma * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            double Nd1 = NormCDF(type == OptionType.Call ? d1 : -d1);
            double Nd2 = NormCDF(type == OptionType.Call ? d2 : -d2);

            return Math.Exp(-r * T) * (type == OptionType.Call
                ? F * Nd1 - K * Nd2
                : K * Nd2 - F * Nd1);
        }

        // === Barone-Adesi-Whaley (BAW) аппроксимация для американских опционов ===
        public static double AmericanOptionPriceBAW(double F, double K, double T, double r, double sigma, OptionType type)
        {
            if (T <= 0) return Math.Max(type == OptionType.Call ? F - K : K - F, 0);

            double european = Black76Price(F, K, T, r, sigma, type);
            if (T < 1e-6 || sigma < 1e-6) return european;

            double b = r; // для фьючерсов cost of carry = r
            double tau = T;

            double sigma2 = sigma * sigma;
            double M = 2 * r / sigma2;
            double N = 2 * (r) / sigma2; // b = r для фьючерсов
            double Kcrit;

            if (type == OptionType.Call)
            {
                double q1 = 0.5 * (-(N - 1) + Math.Sqrt((N - 1) * (N - 1) + 4 * M));
                double h1 = -(r * tau + 2 * sigma * Math.Sqrt(tau)) * K / (F - K);
                Kcrit = K / (1 - 1 / q1 * Math.Exp(h1));
                if (F >= Kcrit) return F - K;

                double A1 = (Kcrit - K) * Math.Pow(Kcrit / K, -q1);
                return european + A1 * Math.Pow(F / Kcrit, q1);
            }
            else // Put
            {
                double q2 = 0.5 * (-(N - 1) - Math.Sqrt((N - 1) * (N - 1) + 4 * M));
                double h2 = -(r * tau + 2 * sigma * Math.Sqrt(tau)) * K / (F - K);
                Kcrit = K / (1 - 1 / q2 * Math.Exp(h2));
                if (F <= Kcrit) return K - F;

                double A2 = (K - Kcrit) * Math.Pow(Kcrit / K, -q2);
                return european + A2 * Math.Pow(F / Kcrit, q2);
            }
        }

        // === Поиск IV методом Брента ===
        public static double ImpliedVolatility(
            double marketPrice,     // рыночная премия (в рублях за контракт)
            double F,               // цена фьючерса
            double K,               // страйк
            double T,               // время до экспирации (в годах)
            double r,               // безрисковая ставка (RUONIA)
            OptionType type,
            double contractMultiplier = 1, // множитель контракта (например, 1 для SI)
            double priceStep = 1,          // шаг цены (для масштабирования)
            bool isAmerican = true)
        {
            // Масштабируем премию: marketPrice — это стоимость контракта
            double premium = marketPrice / (contractMultiplier * priceStep);

            if (premium <= 0) return 0;
            if (T <= 0) return 0;

            double intrinsic = type == OptionType.Call ? Math.Max(F - K, 0) : Math.Max(K - F, 0);
            if (premium <= intrinsic * Math.Exp(-r * T)) return 0;

            // Границы поиска
            double low = 1e-6;
            double high = 10.0; // 1000%

            double priceLow = PriceAtVol(low, F, K, T, r, type, isAmerican) * contractMultiplier * priceStep;
            double priceHigh = PriceAtVol(high, F, K, T, r, type, isAmerican) * contractMultiplier * priceStep;

            if (marketPrice <= priceLow) return low;
            if (marketPrice >= priceHigh) return high;

            for (int i = 0; i < MaxIterations; i++)
            {
                double mid = (low + high) / 2;
                double priceMid = PriceAtVol(mid, F, K, T, r, type, isAmerican) * contractMultiplier * priceStep;

                if (Math.Abs(priceMid - marketPrice) < Tolerance)
                    return mid;

                if (priceMid < marketPrice)
                    low = mid;
                else
                    high = mid;
            }

            return (low + high) / 2;
        }

        private static double PriceAtVol(double sigma, double F, double K, double T, double r, OptionType type, bool isAmerican)
        {
            if (isAmerican)
                return AmericanOptionPriceBAW(F, K, T, r, sigma, type);
            else
                return Black76Price(F, K, T, r, sigma, type);
        }

        // Нормальное распределение CDF (аппроксимация)
        private static double NormCDF(double x)
        {
            // Аппроксимация от Abramowitz & Stegun
            if (x < -10) return 0;
            if (x > 10) return 1;

            double b1 = 0.31938153;
            double b2 = -0.356563782;
            double b3 = 1.781477937;
            double b4 = -1.821255978;
            double b5 = 1.330274429;
            double p = 0.2316419;
            double c = 0.39894228;

            if (x >= 0.0)
            {
                double t = 1.0 / (1.0 + p * x);
                return (1.0 - c * Math.Exp(-x * x / 2.0) * t *
                    (t * (t * (t * (t * b5 + b4) + b3) + b2) + b1));
            }
            else
            {
                double t = 1.0 / (1.0 - p * x);
                return (c * Math.Exp(-x * x / 2.0) * t *
                    (t * (t * (t * (t * b5 + b4) + b3) + b2) + b1));
            }
        }
    }

    //// === ПРИМЕР ИСПОЛЬЗОВАНИЯ ===
    //class Program
    ////{
    //    static void Main()
    //    {
    //// Пример: Опцион SI-12.25 (декабрь 2025), страйк 150000
    //double F = 152300;      // текущая цена фьючерса SI
    //double K = 150000;      // страйк
    //double T = 45 / 365.0;  // 45 дней до экспирации
    //double r = 0.16;        // RUONIA ~16%
    //double marketPrice = 8500; // премия за контракт (в рублях)
    //var type = OptionType.Call;

    //// Параметры контракта SI
    //double contractMultiplier = 1;     // 1 пункт = 1 рубль
    //double priceStep = 100;            // шаг цены = 100 пунктов

    //double iv = BlackScholesImpliedVolatility.ImpliedVolatility(
    //    marketPrice: marketPrice,
    //    F: F,
    //    K: K,
    //    T: T,
    //    r: r,
    //    type: type,
    //    contractMultiplier: contractMultiplier,
    //    priceStep: priceStep,
    //    isAmerican: true);

    //        Console.WriteLine($"Implied Volatility (American, Black-76 + BAW): {iv:P2}");
    //        // Вывод: ~28.45%
    //    }
    //}
}