using System;
using JetBrains.Annotations;

namespace EAGLE.Library
{
    [PublicAPI]
    /// <summary>
    /// Represents an exchange rate data point for a specific currency pair at a point in time.
    /// </summary>
    public class ExchangeRate
    {
        /// <summary>
        /// The date and time of this exchange rate measurement.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The base currency (e.g., "USD").
        /// </summary>
        public string BaseCurrency { get; set; } = string.Empty;

        /// <summary>
        /// The target currency (e.g., "EUR", "GBP", etc.).
        /// </summary>
        public string TargetCurrency { get; set; } = string.Empty;

        /// <summary>
        /// The exchange rate value (how many units of target currency equal 1 unit of base currency).
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// Optional description or source of the exchange rate.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Gets the currency pair identifier (e.g., "USD/EUR").
        /// </summary>
        public string CurrencyPair => $"{BaseCurrency}/{TargetCurrency}";
    }

    [PublicAPI]
    /// <summary>
    /// Represents a significant event that occurred on a specific date that may affect exchange rates.
    /// </summary>
    public class SignificantEvent
    {
        /// <summary>
        /// The date when the event occurred.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Description of the event.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The type or category of the event.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Currencies that might be affected by this event.
        /// </summary>
        public string[] AffectedCurrencies { get; set; } = Array.Empty<string>();
    }

    [PublicAPI]
    /// <summary>
    /// Represents trend analysis results for a currency over a time period.
    /// </summary>
    public class CurrencyTrend
    {
        /// <summary>
        /// The currency pair being analyzed.
        /// </summary>
        public string CurrencyPair { get; set; } = string.Empty;

        /// <summary>
        /// The start date of the trend analysis period.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end date of the trend analysis period.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// The starting rate at the beginning of the period.
        /// </summary>
        public decimal StartRate { get; set; }

        /// <summary>
        /// The ending rate at the end of the period.
        /// </summary>
        public decimal EndRate { get; set; }

        /// <summary>
        /// The percentage change over the period.
        /// </summary>
        public decimal PercentageChange { get; set; }

        /// <summary>
        /// The minimum rate during the period.
        /// </summary>
        public decimal MinRate { get; set; }

        /// <summary>
        /// The maximum rate during the period.
        /// </summary>
        public decimal MaxRate { get; set; }

        /// <summary>
        /// The average rate during the period.
        /// </summary>
        public decimal AverageRate { get; set; }

        /// <summary>
        /// Indicates if the trend shows strength (true) or weakness (false) for the base currency.
        /// </summary>
        public bool IsStrengthening => PercentageChange > 0;

        /// <summary>
        /// A text description of the trend (e.g., "Strengthening", "Weakening", "Stable").
        /// </summary>
        public string TrendDescription 
        { 
            get
            {
                if (Math.Abs(PercentageChange) < 0.5m) return "Stable";
                return IsStrengthening ? "Strengthening" : "Weakening";
            }
        }
    }
}