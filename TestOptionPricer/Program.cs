using MoexOptionsPricer;


Console.WriteLine("=== Тестирование ценообразования и IV ===");

BlackScholesImpliedVolatility.DiagnoseF();
BlackScholesImpliedVolatility.RunTestRealMOEX();

//// Пример реальных данных MOEX (примерные значения)
//double F = 95000;           // цена фьючерса (например, Si или RI)
//double K = 95000;           // страйк
//double T = 91.0 / 365.0;            // 3 месяца до экспирации
//double r = 0.12;            // RUONIA ≈ 12%
//double marketPriceCall = 250;  // премия колла в рублях за контракт
//double marketPricePut = 220;  // премия пута

//int multiplier = 1000;      // для Si — 1000 USD
//double priceStep = 1;       // шаг цены

//Console.WriteLine($"\nF={F}, K={K}, T={T * 365:F0} дней, r={r * 100:F2}%");

//// 1. Black-76 (европейский) — рекомендуется как база
//double sigmaCall = BlackScholesImpliedVolatility.ImpliedVolatility(
//    marketPriceCall, F, K, T, r, OptionType.Call, multiplier, priceStep, false);

//double priceCall = BlackScholesImpliedVolatility.Black76Price(F, K, T, r, sigmaCall, OptionType.Call);

//Console.WriteLine($"\nBlack-76 Call IV: {sigmaCall * 100:F4}%");
//Console.WriteLine($"Calculated Call price: {priceCall * multiplier * priceStep:F2} (market: {marketPriceCall})");

//// 2. BAW (американский) — ваш текущий
//double sigmaCallBaw = BlackScholesImpliedVolatility.ImpliedVolatility(
//    marketPriceCall, F, K, T, r, OptionType.Call, multiplier, priceStep, true);

//Console.WriteLine($"BAW Call IV: {sigmaCallBaw * 100:F4}%");

//// 3. То же для Put
//double sigmaPut = BlackScholesImpliedVolatility.ImpliedVolatility(
//    marketPricePut, F, K, T, r, OptionType.Put, multiplier, priceStep, false);

//Console.WriteLine($"\nBlack-76 Put IV: {sigmaPut * 100:F4}%");

//// Тест на глубокий ITM / OTM
//TestDeepITM(F, K * 0.9, T, r, multiplier);
//TestDeepOTM(F, K * 1.1, T, r, multiplier);

//Console.ReadLine();

//static void TestDeepITM(double F, double K, double T, double r, int mult)
//{
//    Console.WriteLine($"\n=== Deep ITM Call (F={F}, K={K}) ===");
//    double priceEur = BlackScholesImpliedVolatility.Black76Price(F, K, T, r, 0.2, OptionType.Call);
//    double priceBaw = BlackScholesImpliedVolatility.AmericanOptionPriceBAW(F, K, T, r, 0.2, OptionType.Call);

//    Console.WriteLine($"European: {priceEur * mult:F2} | BAW: {priceBaw * mult:F2}");
//    Console.WriteLine($"Intrinsic: {Math.Max(F - K, 0) * mult:F2}");
//}

//static void TestDeepOTM(double F, double K, double T, double r, int mult)
//{
//    Console.WriteLine($"\n=== Deep OTM Put (F={F}, K={K}) ===");
//    double priceEur = BlackScholesImpliedVolatility.Black76Price(F, K, T, r, 0.2, OptionType.Put);
//    double priceBaw = BlackScholesImpliedVolatility.AmericanOptionPriceBAW(F, K, T, r, 0.2, OptionType.Put);

//    Console.WriteLine($"European: {priceEur * mult:F2} | BAW: {priceBaw * mult:F2}");
//}