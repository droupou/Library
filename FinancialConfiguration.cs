using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace EAGLE.Library.Financial;

[PublicAPI]
public class FinancialMonitoringConfiguration
{
    public int UpdateIntervalMinutes { get; set; } = 240; // 4 hours default
    public bool EnableContinuousMonitoring { get; set; } = true;
    public bool SaveReportsToDatabase { get; set; } = true;
    public bool GenerateAlerts { get; set; } = true;
    public string ReportOutputPath { get; set; } = "./Reports";
    public int DataRetentionDays { get; set; } = 365;
    
    // API Configuration
    public string? AlphaVantageApiKey { get; set; }
    public string? YahooFinanceApiKey { get; set; }
    public string? FinnhubApiKey { get; set; }
    public string? QuandlApiKey { get; set; }
    
    // Alert thresholds
    public decimal PriceChangeAlertThreshold { get; set; } = 0.05m; // 5% change
    public decimal VolumeChangeAlertThreshold { get; set; } = 0.20m; // 20% change
    
    // Companies to monitor (can be overridden by configuration)
    public List<string> CustomCompanySymbols { get; set; } = new();
    
    public void Validate()
    {
        if (UpdateIntervalMinutes < 1)
        {
            throw new ArgumentException("UpdateIntervalMinutes must be greater than 0");
        }
        
        if (DataRetentionDays < 1)
        {
            throw new ArgumentException("DataRetentionDays must be greater than 0");
        }
        
        if (PriceChangeAlertThreshold < 0 || PriceChangeAlertThreshold > 1)
        {
            throw new ArgumentException("PriceChangeAlertThreshold must be between 0 and 1");
        }
        
        if (VolumeChangeAlertThreshold < 0)
        {
            throw new ArgumentException("VolumeChangeAlertThreshold must be greater than or equal to 0");
        }
    }
}