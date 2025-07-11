using System;
using System.IO;
using EAGLE.Library;

namespace CurrencyMonitorDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== USD Currency Monitoring System ===");
            Console.WriteLine("Monitoring USD against: NZD, AUD, GBP, CAD, EUR, NOK, SEK");
            Console.WriteLine();

            using var monitoringService = new CurrencyMonitoringService();
            var reportingService = new CurrencyReportingService(monitoringService);

            try
            {
                // Display latest exchange rates
                Console.WriteLine("Latest Exchange Rates:");
                Console.WriteLine("=====================");
                var latestRates = monitoringService.GetLatestRates();
                foreach (var rate in latestRates)
                {
                    Console.WriteLine($"1 USD = {rate.Rate:F4} {rate.TargetCurrency} (as of {rate.Timestamp:yyyy-MM-dd})");
                }
                Console.WriteLine();

                // Display trend analysis
                Console.WriteLine("Trend Analysis (from 1/1/2024 to present):");
                Console.WriteLine("============================================");
                var startDate = new DateTime(2024, 1, 1);
                var trends = monitoringService.AnalyzeAllTrends(startDate);
                
                foreach (var trend in trends)
                {
                    var direction = trend.IsStrengthening ? "↗ Strengthening" : "↘ Weakening";
                    var color = trend.IsStrengthening ? ConsoleColor.Green : ConsoleColor.Red;
                    
                    Console.Write($"{trend.CurrencyPair}: ");
                    Console.ForegroundColor = color;
                    Console.Write($"{direction} ({trend.PercentageChange:+0.00;-0.00}%)");
                    Console.ResetColor();
                    Console.WriteLine($" | Range: {trend.MinRate:F4} - {trend.MaxRate:F4} | Avg: {trend.AverageRate:F4}");
                }
                Console.WriteLine();

                // Display significant events
                Console.WriteLine("Significant Events:");
                Console.WriteLine("==================");
                var events = monitoringService.GetSignificantEvents(startDate);
                foreach (var evt in events)
                {
                    Console.WriteLine($"{evt.Date:yyyy-MM-dd}: {evt.Description} ({evt.EventType})");
                    Console.WriteLine($"  Affected Currencies: {string.Join(", ", evt.AffectedCurrencies)}");
                }
                Console.WriteLine();

                // Generate reports
                Console.WriteLine("Generating Reports:");
                Console.WriteLine("==================");
                var outputDir = Path.Combine(Environment.CurrentDirectory, "CurrencyReports");
                
                Console.WriteLine("Creating comprehensive currency monitoring report...");
                reportingService.GenerateComprehensiveReport(outputDir, startDate);
                
                Console.WriteLine($"Reports have been generated in: {outputDir}");
                Console.WriteLine("Reports created:");
                Console.WriteLine("- USD_Currency_Analysis_Report.txt (comprehensive analysis)");
                Console.WriteLine("- USD_Currency_Data.csv (historical data export)");
                Console.WriteLine();

                // Show ASCII chart example
                Console.WriteLine("Sample ASCII Chart (USD/EUR):");
                Console.WriteLine("=============================");
                var asciiChart = reportingService.CreateASCIIChart("EUR", startDate);
                Console.WriteLine(asciiChart);

                // Show detailed analysis for one currency as example
                Console.WriteLine("Detailed Analysis Example (USD/EUR):");
                Console.WriteLine("====================================");
                var eurTrend = monitoringService.AnalyzeTrend("EUR", startDate);
                Console.WriteLine($"Currency Pair: {eurTrend.CurrencyPair}");
                Console.WriteLine($"Period: {eurTrend.StartDate:yyyy-MM-dd} to {eurTrend.EndDate:yyyy-MM-dd}");
                Console.WriteLine($"Start Rate: {eurTrend.StartRate:F4}");
                Console.WriteLine($"End Rate: {eurTrend.EndRate:F4}");
                Console.WriteLine($"Change: {eurTrend.PercentageChange:+0.00;-0.00}% ({eurTrend.TrendDescription})");
                Console.WriteLine($"Min Rate: {eurTrend.MinRate:F4}");
                Console.WriteLine($"Max Rate: {eurTrend.MaxRate:F4}");
                Console.WriteLine($"Average Rate: {eurTrend.AverageRate:F4}");
                Console.WriteLine();

                Console.WriteLine("Currency monitoring demonstration completed successfully!");
                Console.WriteLine("Check the generated reports for detailed trend analysis.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}