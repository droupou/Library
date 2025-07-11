using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EAGLE.Library.Financial;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EAGLE.Library.Financial;

[PublicAPI]
public interface IFinancialMonitoringService
{
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    Task StopMonitoringAsync();
    Task<List<FinancialSummary>> GetLatestReportsAsync();
    Task<FinancialSummary?> GetCompanyReportAsync(string symbol);
    bool IsMonitoring { get; }
    DateTime? LastUpdateTime { get; }
}

[PublicAPI]
public class FinancialMonitoringService : BackgroundService, IFinancialMonitoringService
{
    private readonly IFinancialDataService _financialDataService;
    private readonly IConfiguration _configuration;
    private readonly List<FinancialSummary> _latestReports;
    private readonly Timer _updateTimer;
    private readonly object _lockObject = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsMonitoring { get; private set; }
    public DateTime? LastUpdateTime { get; private set; }

    public FinancialMonitoringService(IFinancialDataService financialDataService, IConfiguration configuration)
    {
        _financialDataService = financialDataService;
        _configuration = configuration;
        _latestReports = new List<FinancialSummary>();
        
        // Set up timer for periodic updates - default to every 4 hours
        var updateIntervalMinutes = _configuration.GetValue<int>("FinancialMonitoring:UpdateIntervalMinutes", 240);
        _updateTimer = new Timer(async _ => await UpdateAllFinancialDataAsync(), null, Timeout.Infinite, Timeout.Infinite);
        
        Console.WriteLine("Financial Monitoring Service initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartMonitoringAsync(stoppingToken);
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            IsMonitoring = true;
            
            Console.WriteLine("Starting financial monitoring service");
            
            // Initial data load
            await UpdateAllFinancialDataAsync();
            
            // Set up periodic updates
            var updateIntervalMinutes = _configuration.GetValue<int>("FinancialMonitoring:UpdateIntervalMinutes", 240);
            _updateTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(updateIntervalMinutes));
            
            // Keep the service running
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Financial monitoring service cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in financial monitoring service: {ex.Message}");
        }
        finally
        {
            IsMonitoring = false;
        }
    }

    public async Task StopMonitoringAsync()
    {
        try
        {
            Console.WriteLine("Stopping financial monitoring service");
            
            _cancellationTokenSource?.Cancel();
            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            IsMonitoring = false;
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping financial monitoring service: {ex.Message}");
        }
    }

    public async Task<List<FinancialSummary>> GetLatestReportsAsync()
    {
        lock (_lockObject)
        {
            return new List<FinancialSummary>(_latestReports);
        }
    }

    public async Task<FinancialSummary?> GetCompanyReportAsync(string symbol)
    {
        lock (_lockObject)
        {
            return _latestReports.Find(r => r.Company.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        }
    }

    private async Task UpdateAllFinancialDataAsync()
    {
        try
        {
            Console.WriteLine("Starting financial data update for all companies");
            
            var summaries = await _financialDataService.GetAllCompaniesFinancialSummariesAsync();
            
            lock (_lockObject)
            {
                _latestReports.Clear();
                _latestReports.AddRange(summaries);
                LastUpdateTime = DateTime.UtcNow;
            }
            
            Console.WriteLine($"Completed financial data update. {summaries.Count} companies updated");
            
            // Generate overview report
            await GenerateOverviewReportAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating financial data: {ex.Message}");
        }
    }

    private async Task GenerateOverviewReportAsync()
    {
        try
        {
            List<FinancialSummary> currentReports;
            lock (_lockObject)
            {
                currentReports = new List<FinancialSummary>(_latestReports);
            }

            if (currentReports.Count == 0)
            {
                Console.WriteLine("No financial data available for overview report");
                return;
            }

            var reportBuilder = new System.Text.StringBuilder();
            reportBuilder.AppendLine("=== FINANCIAL MONITORING OVERVIEW REPORT ===");
            reportBuilder.AppendLine($"Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
            reportBuilder.AppendLine($"Companies monitored: {currentReports.Count}");
            reportBuilder.AppendLine();

            // Group by sector
            var sectorGroups = currentReports
                .GroupBy(r => r.Company.Sector)
                .OrderBy(g => g.Key);

            foreach (var sectorGroup in sectorGroups)
            {
                reportBuilder.AppendLine($"## {sectorGroup.Key} Sector ({sectorGroup.Count()} companies)");
                
                foreach (var company in sectorGroup.OrderBy(c => c.Company.Name))
                {
                    reportBuilder.AppendLine($"### {company.Company.Name} ({company.Company.Symbol})");
                    reportBuilder.AppendLine($"  Exchange: {company.Company.Exchange} | Currency: {company.Company.Currency}");
                    
                    if (company.CurrentPrice != null)
                    {
                        reportBuilder.AppendLine($"  Current Price: {company.CurrentPrice.Close:F2} {company.Company.Currency}");
                        reportBuilder.AppendLine($"  Daily Range: {company.CurrentPrice.Low:F2} - {company.CurrentPrice.High:F2}");
                    }
                    
                    if (company.LatestFinancials != null)
                    {
                        reportBuilder.AppendLine($"  Latest Revenue: {FormatCurrency(company.LatestFinancials.Revenue, company.Company.Currency)}");
                        reportBuilder.AppendLine($"  Net Income: {FormatCurrency(company.LatestFinancials.NetIncome, company.Company.Currency)}");
                    }
                    
                    if (company.Outlook != null)
                    {
                        reportBuilder.AppendLine($"  Analyst Rating: {company.Outlook.AnalystRating}");
                        reportBuilder.AppendLine($"  Market Sentiment: {company.Outlook.MarketSentiment}");
                        if (company.Outlook.TargetPrice.HasValue)
                        {
                            reportBuilder.AppendLine($"  Target Price: {company.Outlook.TargetPrice:F2} {company.Company.Currency}");
                        }
                    }
                    
                    if (company.Plans.Count > 0)
                    {
                        reportBuilder.AppendLine($"  Active Plans: {company.Plans.Count}");
                        foreach (var plan in company.Plans.Take(2)) // Show first 2 plans
                        {
                            reportBuilder.AppendLine($"    - {plan.Title} ({plan.Status})");
                        }
                    }
                    
                    reportBuilder.AppendLine();
                }
            }

            var reportContent = reportBuilder.ToString();
            Console.WriteLine("Financial overview report generated successfully");
            
            // In a real implementation, you might save this to a file or send to stakeholders
            await SaveOverviewReportAsync(reportContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating overview report: {ex.Message}");
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

    private async Task SaveOverviewReportAsync(string reportContent)
    {
        try
        {
            // In a real implementation, save to database or file system
            // For now, just log that we would save it
            Console.WriteLine($"Overview report would be saved (length: {reportContent.Length} characters)");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving overview report: {ex.Message}");
        }
    }

    public override void Dispose()
    {
        _updateTimer?.Dispose();
        _cancellationTokenSource?.Dispose();
        base.Dispose();
    }
}