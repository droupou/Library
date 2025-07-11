using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace EAGLE.Library.Financial;

[PublicAPI]
public class Company
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

[PublicAPI]
public class FinancialRecord
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal? Revenue { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? TotalAssets { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? ShareholderEquity { get; set; }
    public decimal? OperatingCashFlow { get; set; }
    public decimal? FreeCashFlow { get; set; }
    public string ReportPeriod { get; set; } = string.Empty; // Q1, Q2, Q3, Q4, Annual
    public DateTime LastUpdated { get; set; }
}

[PublicAPI]
public class StockPrice
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public decimal? AdjustedClose { get; set; }
    public DateTime LastUpdated { get; set; }
}

[PublicAPI]
public class MarketOutlook
{
    public string Symbol { get; set; } = string.Empty;
    public string AnalystRating { get; set; } = string.Empty; // Buy, Hold, Sell
    public decimal? TargetPrice { get; set; }
    public decimal? PriceEarningsRatio { get; set; }
    public decimal? PriceToBookRatio { get; set; }
    public decimal? DebtToEquityRatio { get; set; }
    public decimal? ReturnOnEquity { get; set; }
    public string MarketSentiment { get; set; } = string.Empty;
    public List<string> KeyRisks { get; set; } = new();
    public List<string> GrowthDrivers { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

[PublicAPI]
public class CompanyPlan
{
    public string Symbol { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty; // Strategic Plan, Investment Plan, etc.
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime AnnouncementDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public decimal? InvestmentAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Planned, In Progress, Completed
    public DateTime LastUpdated { get; set; }
}

[PublicAPI]
public class FinancialSummary
{
    public Company Company { get; set; } = new();
    public FinancialRecord? LatestFinancials { get; set; }
    public StockPrice? CurrentPrice { get; set; }
    public MarketOutlook? Outlook { get; set; }
    public List<CompanyPlan> Plans { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}