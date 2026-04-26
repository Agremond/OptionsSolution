using System;

namespace MoexOptionsPricer
{
    public enum OptionType { Call, Put }

    public class BlackScholesImpliedVolatility
    {
        private const double Tolerance = 1e-10;
        private const int MaxIterations = 100;

        // ===================================================================
        // Black-76 модель (европейская цена в пунктах фьючерса)
        // ===================================================================
        public static double Black76Price(double F, double K, double T, double r, double sigma, OptionType type)
        {
            if (T <= 0)
                return Math.Max(type == OptionType.Call ? F - K : K - F, 0);

            if (sigma < 1e-10)
                return Math.Max(type == OptionType.Call ? F - K : K - F, 0);

            double d1 = (Math.Log(F / K) + 0.5 * sigma * sigma * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            double Nd1 = type == OptionType.Call ? NormCDF(d1) : NormCDF(-d1);
            double Nd2 = type == OptionType.Call ? NormCDF(d2) : NormCDF(-d2);

            double price = Math.Exp(-r * T) * (type == OptionType.Call
                ? F * Nd1 - K * Nd2
                : K * Nd2 - F * Nd1);

            return Math.Max(price, 0.0);
        }

        // ===================================================================
        // Barone-Adesi-Whaley для американских PUT (b = 0)
        // ===================================================================
        public static double AmericanOptionPriceBAW(double F, double K, double T, double r, double sigma, OptionType type)
        {
            if (T <= 0)
                return Math.Max(type == OptionType.Call ? F - K : K - F, 0);

            if (T < 1e-7 || sigma < 1e-8)
                return Black76Price(F, K, T, r, sigma, type);

            double european = Black76Price(F, K, T, r, sigma, type);

            if (type == OptionType.Call)
                return european;   // для фьючерсов Call американский ≈ европейский

            // American PUT
            double q2 = 0.5 * (-1.0 - Math.Sqrt(1.0 + 8.0 * r / (sigma * sigma)));
            if (q2 > -0.01) q2 = -0.01;   // защита

            double Fstar = FindCriticalPricePut(F, K, T, r, sigma, q2);

            if (F <= Fstar + 1e-6)
                return K - F;

            double d1Star = D1(Fstar, K, T, sigma);
            double A2 = (Fstar / q2) * (1.0 - Math.Exp(-r * T) * NormCDF(-d1Star));

            double earlyPremium = A2 * Math.Pow(F / Fstar, q2);
            return european + Math.Max(earlyPremium, 0.0);
        }

        private static double D1(double F, double K, double T, double sigma)
        {
            if (T <= 0) return 0;
            return (Math.Log(F / K) + 0.5 * sigma * sigma * T) / (sigma * Math.Sqrt(T));
        }

        private static double FindCriticalPricePut(double F, double K, double T, double r, double sigma, double q2)
        {
            double Fstar = K * 0.92;

            for (int i = 0; i < 40; i++)
            {
                double d1s = D1(Fstar, K, T, sigma);
                double putEur = Black76Price(Fstar, K, T, r, sigma, OptionType.Put);

                double left = K - Fstar;
                double right = putEur + (Fstar / q2) * (1.0 - Math.Exp(-r * T) * NormCDF(-d1s));

                double diff = left - right;
                if (Math.Abs(diff) < 1e-8) break;

                double pdf = NormPDF(d1s);
                double deriv = -1.0 + (1.0 / q2) * (1.0 - Math.Exp(-r * T) * NormCDF(-d1s))
                               - Math.Exp(-r * T) * pdf * Fstar / (sigma * Math.Sqrt(T) * q2);

                if (Math.Abs(deriv) < 1e-12) break;

                Fstar -= diff / deriv;

                if (Fstar < K * 0.01) Fstar = K * 0.01;
                if (Fstar > K) Fstar = K;
            }
            return Fstar;
        }

        // ===================================================================
        // Поиск Implied Volatility методом BRENT (самое точное решение)
        // ===================================================================
        public static double ImpliedVolatility(
            double premiumInPoints,     // Расч. премия с доски MOEX (в пунктах!)
            double F,
            double K,
            double T,                   // в годах
            double r,
            OptionType type,
            bool useAmerican = true)
        {
            if (premiumInPoints <= 0 || T <= 0) return 0.0;

            double intrinsic = type == OptionType.Call ? Math.Max(F - K, 0) : Math.Max(K - F, 0);
            if (premiumInPoints <= intrinsic * Math.Exp(-r * T) + 1e-8)
                return 0.0;

            // Функция ошибки: Price(σ) - marketPrice
            Func<double, double> objective = sigma =>
            {
                double price = PriceAtVol(sigma, F, K, T, r, type, useAmerican);
                return price - premiumInPoints;
            };

            double low = 1e-5;
            double high = 2.0;

            // Расширяем границы, если нужно
            while (objective(low) > 0 && low > 1e-10) low *= 0.5;
            while (objective(high) < 0 && high < 10) high *= 2;

            try
            {
                return BrentSolver(objective, low, high, Tolerance, MaxIterations);
            }
            catch
            {
                // fallback на бинарный поиск при редких сбоях
                return FallbackBisection(objective, low, high);
            }
        }

        // Реализация метода Брента (классическая)
        private static double BrentSolver(Func<double, double> f, double a, double b, double tol, int maxIter)
        {
            double fa = f(a);
            double fb = f(b);

            if (fa * fb > 0)
                throw new ArgumentException("Brent: границы должны давать разные знаки функции.");

            double c = a, fc = fa;
            double d = b - a, e = d;

            for (int iter = 0; iter < maxIter; iter++)
            {
                if (Math.Abs(fc) < Math.Abs(fb))
                {
                    a = b; b = c; c = a;
                    fa = fb; fb = fc; fc = fa;
                }

                double tol1 = 2.0 * double.Epsilon * Math.Abs(b) + 0.5 * tol;
                double xm = 0.5 * (c - b);

                if (Math.Abs(xm) <= tol1 || Math.Abs(fb) < 1e-12)
                    return b;

                if (Math.Abs(e) >= tol1 && Math.Abs(fa) > Math.Abs(fb))
                {
                    double s = fb / fa;
                    double p, q;
                    if (Math.Abs(a - c) < 1e-12)
                    {
                        p = 2.0 * xm * s;
                        q = 1.0 - s;
                    }
                    else
                    {
                        q = fa / fc;
                        double r = fb / fc;
                        p = s * (2.0 * xm * q * (q - r) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }

                    if (p > 0) q = -q; else p = -p;
                    double temp = e;
                    e = d;

                    if (Math.Abs(p) < Math.Abs(0.5 * temp * q) && p < q * (b - a) && p < q * (c - a))
                    {
                        d = p / q;
                    }
                    else
                    {
                        d = xm;
                        e = d;
                    }
                }
                else
                {
                    d = xm;
                    e = d;
                }

                a = b;
                fa = fb;

                if (Math.Abs(d) > tol1)
                    b += d;
                else
                    b += (xm > 0 ? tol1 : -tol1);

                fb = f(b);

                if ((fb > 0 && fc > 0) || (fb < 0 && fc < 0))
                {
                    c = a;
                    fc = fa;
                    d = b - a;
                    e = d;
                }
            }

            return b;   // возвращаем лучшее приближение
        }

        private static double FallbackBisection(Func<double, double> f, double low, double high)
        {
            for (int i = 0; i < 300; i++)
            {
                double mid = (low + high) / 2.0;
                if (Math.Abs(f(mid)) < 1e-8) return mid;
                if (f(mid) * f(low) < 0)
                    high = mid;
                else
                    low = mid;
            }
            return (low + high) / 2.0;
        }

        private static double PriceAtVol(double sigma, double F, double K, double T, double r, OptionType type, bool useAmerican)
        {
            if (useAmerican && type == OptionType.Put)
                return AmericanOptionPriceBAW(F, K, T, r, sigma, type);
            else
                return Black76Price(F, K, T, r, sigma, type);
        }

        // ===================================================================
        // Нормальное распределение (улучшенная версия)
        // ===================================================================
        private static double NormCDF(double x)
        {
            if (x < -10) return 0.0;
            if (x > 10) return 1.0;

            double absX = Math.Abs(x);
            double t = 1.0 / (1.0 + 0.2316419 * absX);
            double d = 0.39894228040143267794 * Math.Exp(-0.5 * x * x);

            double poly = t * (0.319381530 + t * (-0.356563782 + t * (1.781477937 +
                          t * (-1.821255978 + t * 1.330274429))));

            double result = d * poly;
            return x >= 0 ? 1.0 - result : result;
        }

        private static double NormPDF(double x)
        {
            return 0.3989422804014327 * Math.Exp(-0.5 * x * x);
        }
        public static void DiagnoseF()
        {
            Console.WriteLine("=== Диагностика F для страйка 73500, Call премия 2740 ===\n");
            double r = 0.15;
            double T = 25.0 / 365.0;
            double K = 73500;
            double marketPoints = 2740;

            for (int f = 75800; f <= 76200; f += 50)
            {
                double iv = ImpliedVolatility(marketPoints, f, K, T, r, OptionType.Call, false);
                double calc = Black76Price(f, K, T, r, iv, OptionType.Call);
                double intrinsic = Math.Max(f - K, 0);

                Console.WriteLine($"F = {f} | Intrinsic = {intrinsic} | Time value market ≈ {marketPoints - intrinsic} | Расч. IV = {iv * 100:F3}% | Теор. цена = {calc:F1}");
            }
        }
        // ===================================================================
        // Тестовый метод (обновлённый)
        // ===================================================================
        public static void RunTestRealMOEX()
        {
            Console.WriteLine("=== Тест на реальных данных MOEX (опционы на Si) — Brent ===\n");

            double F = 75850;        // ← измените на более точную цену фьючерса, если есть
            double r = 0.15;           // RUONIA или близкая ставка
            double T = 25.0 / 365.0;

            var data = new[]
            {
                new { Strike = 73500, CallPoints = 2739, PutPoints = 425, VolBoard = 16.132 },
                new { Strike = 75000, CallPoints = 1667, PutPoints = 852,  VolBoard = 15.429 },
                new { Strike = 76000, CallPoints = 1129, PutPoints = 1314, VolBoard = 15.363 },
                new { Strike = 77000, CallPoints = 750,  PutPoints = 1935, VolBoard = 15.695 },
                new { Strike = 78000, CallPoints = 491,  PutPoints = 2677, VolBoard = 16.194 }
            };

            foreach (var row in data)
            {
                double ivCall = ImpliedVolatility(row.CallPoints, F, row.Strike, T, r, OptionType.Call, false);
                double calcCall = Black76Price(F, row.Strike, T, r, ivCall, OptionType.Call);

                double ivPut = ImpliedVolatility(row.PutPoints, F, row.Strike, T, r, OptionType.Put, true);
                double calcPut = Black76Price(F, row.Strike, T, r, ivPut, OptionType.Put);

                Console.WriteLine($"Страйк {row.Strike:N0} | F={F:N0} | T={T*365:F0} дней | r={r*100:F2}%");
                Console.WriteLine($"   CALL  Доска: {row.CallPoints,6} пунктов → IV: {ivCall*100:F3}% (биржа {row.VolBoard:F3}%) | Теор: {calcCall:F2}");
                Console.WriteLine($"   PUT   Доска: {row.PutPoints,6} пунктов → IV: {ivPut*100:F3}%               | Теор: {calcPut:F2}\n");
            }

            Console.WriteLine("=== Тест завершён ===\n");
        }
    }
}