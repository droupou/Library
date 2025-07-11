using System;
using System.Linq;
using System.Threading.Tasks;
using EAGLE.Library.Financial;
using JetBrains.Annotations;

namespace EAGLE.Library.Financial;

[PublicAPI]
public class FinancialMonitoringExample
{
    /// <summary>
    /// Demonstrates basic usage of the financial monitoring system
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    public static async Task RunExampleAsync()
    {
        Console.WriteLine("Starting Financial Monitoring Example");
        
        try
        {
            // Create the financial data service
            var dataService = FinancialMonitoringFactory.CreateDataService();
            
            Console.WriteLine("=== MONITORED COMPANIES ===");
            var companies = dataService.GetMonitoredCompanies();
            foreach (var company in companies)
            {
                Console.WriteLine($"{company.Name} ({company.Symbol}) - {company.Exchange} - {company.Country}");
            }
            
            Console.WriteLine("\n=== SAMPLE FINANCIAL SUMMARY ===");
            
            // Get financial summary for a few sample companies
            var sampleSymbols = new[] { "SHOP.TO", "SAP.DE", "AAPL", "INFY.NS", "7974.T" };
            
            foreach (var symbol in sampleSymbols)
            {
                var summary = await dataService.GetFinancialSummaryAsync(symbol);
                if (summary != null)
                {
                    Console.WriteLine($"\n--- {summary.Company.Name} ({summary.Company.Symbol}) ---");
                    Console.WriteLine($"Exchange: {summary.Company.Exchange}");
                    Console.WriteLine($"Sector: {summary.Company.Sector}");
                    Console.WriteLine($"Country: {summary.Company.Country}");
                    
                    if (summary.CurrentPrice != null)
                    {
                        Console.WriteLine($"Current Price: {summary.CurrentPrice.Close:F2} {summary.Company.Currency}");
                        Console.WriteLine($"Daily Range: {summary.CurrentPrice.Low:F2} - {summary.CurrentPrice.High:F2}");
                        Console.WriteLine($"Volume: {summary.CurrentPrice.Volume:N0}");
                    }
                    
                    if (summary.LatestFinancials != null)
                    {
                        Console.WriteLine($"Revenue: {FormatCurrency(summary.LatestFinancials.Revenue, summary.Company.Currency)}");
                        Console.WriteLine($"Net Income: {FormatCurrency(summary.LatestFinancials.NetIncome, summary.Company.Currency)}");
                        Console.WriteLine($"Report Period: {summary.LatestFinancials.ReportPeriod}");
                    }
                    
                    if (summary.Outlook != null)
                    {
                        Console.WriteLine($"Analyst Rating: {summary.Outlook.AnalystRating}");
                        Console.WriteLine($"Market Sentiment: {summary.Outlook.MarketSentiment}");
                        if (summary.Outlook.TargetPrice.HasValue)
                        {
                            Console.WriteLine($"Target Price: {summary.Outlook.TargetPrice:F2} {summary.Company.Currency}");
                        }
                        if (summary.Outlook.PriceEarningsRatio.HasValue)
                        {
                            Console.WriteLine($"P/E Ratio: {summary.Outlook.PriceEarningsRatio:F2}");
                        }
                    }
                    
                    if (summary.Plans.Count > 0)
                    {
                        Console.WriteLine($"Company Plans ({summary.Plans.Count}):");
                        foreach (var plan in summary.Plans)
                        {
                            Console.WriteLine($"  - {plan.Title} ({plan.Status})");
                            Console.WriteLine($"    Type: {plan.PlanType}");
                            if (plan.InvestmentAmount.HasValue)
                            {
                                Console.WriteLine($"    Investment: {FormatCurrency(plan.InvestmentAmount, summary.Company.Currency)}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Could not retrieve data for {symbol}");
                }
            }
            
            Console.WriteLine("\n=== CONTINUOUS MONITORING DEMO ===");
            Console.WriteLine("Creating monitoring service (in real usage, this would run continuously)...");
            
            // Create monitoring service
            var monitoringService = FinancialMonitoringFactory.CreateMonitoringService();
            
            Console.WriteLine($"Monitoring service created. Monitored companies: {companies.Count}");
            Console.WriteLine("To start continuous monitoring, call: await monitoringService.StartMonitoringAsync()");
            Console.WriteLine("To get latest reports: await monitoringService.GetLatestReportsAsync()");
            Console.WriteLine("To get specific company report: await monitoringService.GetCompanyReportAsync(\"SHOP.TO\")");
            
            Console.WriteLine("Financial Monitoring Example completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Financial Monitoring Example: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Demonstrates continuous monitoring for a short period
    /// </summary>
    /// <param name="durationMinutes">How long to run the monitoring demo</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task RunContinuousMonitoringDemoAsync(int durationMinutes = 1)
    {
        Console.WriteLine($"Starting {durationMinutes}-minute continuous monitoring demo");
        
        try
        {
            var monitoringService = FinancialMonitoringFactory.CreateMonitoringService();
            
            // Start monitoring
            var cancellationTokenSource = new System.Threading.CancellationTokenSource();
            var monitoringTask = monitoringService.StartMonitoringAsync(cancellationTokenSource.Token);
            
            // Let it run for specified duration
            await Task.Delay(TimeSpan.FromMinutes(durationMinutes));
            
            // Check status
            Console.WriteLine($"Monitoring Status: {monitoringService.IsMonitoring}");
            Console.WriteLine($"Last Update: {monitoringService.LastUpdateTime}");
            
            // Get latest reports
            var reports = await monitoringService.GetLatestReportsAsync();
            Console.WriteLine($"Retrieved {reports.Count} company reports");
            
            // Stop monitoring
            cancellationTokenSource.Cancel();
            await monitoringService.StopMonitoringAsync();
            
            Console.WriteLine("Continuous monitoring demo completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in continuous monitoring demo: {ex.Message}");
            throw;
        }
    }
    
    private static string FormatCurrency(decimal? amount, string currency)
    {
        if (!amount.HasValue) return "N/A";
        
        return currency switch
        {
            "USD" or "CAD" or "BRL" => $"${amount:N0}",
            "EUR" => $"€{amount:N0}",
            "GBP" => $"£{amount:N0}",
            "JPY" => $"¥{amount:N0}",
            "INR" => $"₹{amount:N0}",
            "CHF" => $"CHF {amount:N0}",
            "SEK" => $"SEK {amount:N0}",
            _ => $"{currency} {amount:N0}"
        };
    }
}