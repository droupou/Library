using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using static EAGLE.Library.FileLogging;

namespace EAGLE.Library
{
    [PublicAPI]
    /// <summary>
    /// Service for creating text-based reports of currency trends and exchange rate data.
    /// </summary>
    public class CurrencyReportingService
    {
        private readonly CurrencyMonitoringService _monitoringService;

        public CurrencyReportingService(CurrencyMonitoringService monitoringService)
        {
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        }

        /// <summary>
        /// Generates a comprehensive text-based currency report.
        /// </summary>
        public void GenerateComprehensiveReport(string outputDirectory, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var start = startDate ?? new DateTime(2024, 1, 1);
            var end = endDate ?? DateTime.Now;
            
            WriteLog($"Generating comprehensive currency report for period {start:yyyy-MM-dd} to {end:yyyy-MM-dd}", "CurrencyReporting");

            var reportPath = Path.Combine(outputDirectory, "USD_Currency_Analysis_Report.txt");
            var csvPath = Path.Combine(outputDirectory, "USD_Currency_Data.csv");

            GenerateTextReport(reportPath, start, end);
            GenerateCSVData(csvPath, start, end);

            WriteLog($"Comprehensive currency report generated in directory: {outputDirectory}", "CurrencyReporting");
        }

        private void GenerateTextReport(string filePath, DateTime start, DateTime end)
        {
            var report = new StringBuilder();
            
            report.AppendLine("===========================================================");
            report.AppendLine("USD CURRENCY MONITORING SYSTEM - COMPREHENSIVE REPORT");
            report.AppendLine("===========================================================");
            report.AppendLine($"Report Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Analysis Period: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
            report.AppendLine($"Monitored Currencies: {string.Join(", ", CurrencyMonitoringService.MonitoredCurrencies)}");
            report.AppendLine();

            // Current exchange rates
            report.AppendLine("CURRENT EXCHANGE RATES");
            report.AppendLine("======================");
            var latestRates = _monitoringService.GetLatestRates();
            foreach (var rate in latestRates)
            {
                report.AppendLine($"1 USD = {rate.Rate:F4} {rate.TargetCurrency} (as of {rate.Timestamp:yyyy-MM-dd})");
            }
            report.AppendLine();

            // Trend analysis
            report.AppendLine("TREND ANALYSIS");
            report.AppendLine("==============");
            var trends = _monitoringService.AnalyzeAllTrends(start, end);
            
            report.AppendLine("Currency Performance Summary:");
            report.AppendLine("-----------------------------");
            
            foreach (var trend in trends.OrderByDescending(t => t.PercentageChange))
            {
                var direction = trend.IsStrengthening ? "STRENGTHENING" : "WEAKENING";
                var symbol = trend.IsStrengthening ? "↗" : "↘";
                
                report.AppendLine($"{trend.CurrencyPair}: {symbol} {direction} ({trend.PercentageChange:+0.00;-0.00}%)");
                report.AppendLine($"  Start Rate: {trend.StartRate:F4}");
                report.AppendLine($"  End Rate: {trend.EndRate:F4}");
                report.AppendLine($"  Range: {trend.MinRate:F4} - {trend.MaxRate:F4}");
                report.AppendLine($"  Average: {trend.AverageRate:F4}");
                report.AppendLine($"  Volatility: {((trend.MaxRate - trend.MinRate) / trend.AverageRate * 100):F2}%");
                report.AppendLine();
            }

            // Performance rankings
            report.AppendLine("PERFORMANCE RANKINGS");
            report.AppendLine("====================");
            
            var strengthening = trends.Where(t => t.IsStrengthening).OrderByDescending(t => t.PercentageChange).ToList();
            var weakening = trends.Where(t => !t.IsStrengthening).OrderBy(t => t.PercentageChange).ToList();
            
            if (strengthening.Any())
            {
                report.AppendLine("USD Strengthening Against:");
                report.AppendLine("--------------------------");
                for (int i = 0; i < strengthening.Count; i++)
                {
                    var trend = strengthening[i];
                    var targetCurrency = trend.CurrencyPair.Replace("USD/", "");
                    report.AppendLine($"{i + 1}. {targetCurrency}: +{trend.PercentageChange:F2}%");
                }
                report.AppendLine();
            }

            if (weakening.Any())
            {
                report.AppendLine("USD Weakening Against:");
                report.AppendLine("----------------------");
                for (int i = 0; i < weakening.Count; i++)
                {
                    var trend = weakening[i];
                    var targetCurrency = trend.CurrencyPair.Replace("USD/", "");
                    report.AppendLine($"{i + 1}. {targetCurrency}: {trend.PercentageChange:F2}%");
                }
                report.AppendLine();
            }

            // Significant events
            report.AppendLine("SIGNIFICANT EVENTS");
            report.AppendLine("==================");
            var events = _monitoringService.GetSignificantEvents(start, end);
            
            if (events.Any())
            {
                foreach (var evt in events.OrderBy(e => e.Date))
                {
                    report.AppendLine($"Date: {evt.Date:yyyy-MM-dd}");
                    report.AppendLine($"Event: {evt.Description}");
                    report.AppendLine($"Type: {evt.EventType}");
                    report.AppendLine($"Affected Currencies: {string.Join(", ", evt.AffectedCurrencies)}");
                    report.AppendLine();
                }
            }
            else
            {
                report.AppendLine("No significant events recorded for this period.");
                report.AppendLine();
            }

            // Market analysis
            report.AppendLine("MARKET ANALYSIS");
            report.AppendLine("===============");
            
            var avgVolatility = trends.Average(t => (double)((t.MaxRate - t.MinRate) / t.AverageRate * 100));
            var strongestGainer = trends.OrderByDescending(t => t.PercentageChange).First();
            var strongestLoser = trends.OrderBy(t => t.PercentageChange).First();
            
            report.AppendLine($"Average Market Volatility: {avgVolatility:F2}%");
            report.AppendLine($"Strongest Performer: {strongestGainer.CurrencyPair.Replace("USD/", "")} ({strongestGainer.PercentageChange:+F2}%)");
            report.AppendLine($"Weakest Performer: {strongestLoser.CurrencyPair.Replace("USD/", "")} ({strongestLoser.PercentageChange:F2}%)");
            report.AppendLine();
            
            var stableCount = trends.Count(t => Math.Abs(t.PercentageChange) < 1);
            var volatileCount = trends.Count(t => ((t.MaxRate - t.MinRate) / t.AverageRate * 100) > 5);
            
            report.AppendLine($"Market Conditions:");
            report.AppendLine($"- Stable currencies (< 1% change): {stableCount}");
            report.AppendLine($"- Volatile currencies (> 5% volatility): {volatileCount}");
            report.AppendLine();

            report.AppendLine("===========================================================");
            report.AppendLine("END OF REPORT");
            report.AppendLine("===========================================================");

            File.WriteAllText(filePath, report.ToString());
        }

        private void GenerateCSVData(string filePath, DateTime start, DateTime end)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Date,Currency,Rate,BaseCurrency,Source");

            var allData = _monitoringService.GetAllHistoricalData(start, end);
            
            foreach (var rate in allData.OrderBy(r => r.Timestamp).ThenBy(r => r.TargetCurrency))
            {
                csv.AppendLine($"{rate.Timestamp:yyyy-MM-dd},{rate.TargetCurrency},{rate.Rate:F4},{rate.BaseCurrency},{rate.Source}");
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        /// <summary>
        /// Creates a simple ASCII chart for a single currency.
        /// </summary>
        public string CreateASCIIChart(string currency, DateTime? startDate = null, DateTime? endDate = null, int width = 60, int height = 20)
        {
            var data = _monitoringService.GetHistoricalData(currency, startDate, endDate);
            
            if (!data.Any())
            {
                return $"No data available for {currency}";
            }

            var rates = data.Select(d => (double)d.Rate).ToArray();
            var minRate = rates.Min();
            var maxRate = rates.Max();
            var range = maxRate - minRate;

            var chart = new StringBuilder();
            chart.AppendLine($"USD/{currency} Trend Chart ({data.First().Timestamp:MM/dd} - {data.Last().Timestamp:MM/dd})");
            chart.AppendLine(new string('=', width + 10));

            // Create the chart
            for (int row = height - 1; row >= 0; row--)
            {
                var threshold = minRate + (range * row / (height - 1));
                chart.Append($"{threshold:F3} |");

                for (int col = 0; col < width && col < rates.Length; col++)
                {
                    var pointIndex = (int)((double)col / width * (rates.Length - 1));
                    var rate = rates[pointIndex];
                    
                    if (rate >= threshold - range / (height * 2) && rate <= threshold + range / (height * 2))
                    {
                        chart.Append("*");
                    }
                    else
                    {
                        chart.Append(" ");
                    }
                }
                chart.AppendLine();
            }

            chart.AppendLine($"      +{new string('-', width)}");
            chart.AppendLine($"       {data.First().Timestamp:MM/dd}{new string(' ', width - 10)}{data.Last().Timestamp:MM/dd}");

            return chart.ToString();
        }
    }
}