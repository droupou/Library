using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using static EAGLE.Library.FileLogging;

namespace EAGLE.Library
{
    [PublicAPI]
    /// <summary>
    /// Service for creating graphical visualizations of currency trends and exchange rate data.
    /// </summary>
    public class CurrencyVisualizationService
    {
        private readonly CurrencyMonitoringService _monitoringService;

        public CurrencyVisualizationService(CurrencyMonitoringService monitoringService)
        {
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        }

        /// <summary>
        /// Creates a line chart showing USD exchange rate trends for all monitored currencies.
        /// </summary>
        /// <param name="outputPath">Path where the chart image will be saved</param>
        /// <param name="startDate">Start date for the chart data</param>
        /// <param name="endDate">End date for the chart data</param>
        /// <param name="width">Width of the output image in pixels</param>
        /// <param name="height">Height of the output image in pixels</param>
        public void CreateTrendChart(string outputPath, DateTime? startDate = null, DateTime? endDate = null, 
            int width = 1200, int height = 800)
        {
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.Clear(Color.White);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Define chart area
            var margin = 60;
            var chartArea = new Rectangle(margin, margin, width - 2 * margin, height - 2 * margin);
            
            // Get all data
            var allData = new List<ExchangeRate>();
            foreach (var currency in CurrencyMonitoringService.MonitoredCurrencies)
            {
                allData.AddRange(_monitoringService.GetHistoricalData(currency, startDate, endDate));
            }

            if (!allData.Any()) return;

            // Calculate value ranges
            var minDate = allData.Min(d => d.Timestamp);
            var maxDate = allData.Max(d => d.Timestamp);
            var minRate = allData.Min(d => d.Rate);
            var maxRate = allData.Max(d => d.Rate);
            
            // Add some padding to the rate range
            var rateRange = maxRate - minRate;
            minRate -= rateRange * 0.1m;
            maxRate += rateRange * 0.1m;

            // Draw axes
            using var axisPen = new Pen(Color.Black, 2);
            graphics.DrawLine(axisPen, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom); // X-axis
            graphics.DrawLine(axisPen, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom); // Y-axis

            // Draw title
            using var titleFont = new Font("Arial", 16, FontStyle.Bold);
            var title = "USD Exchange Rate Trends";
            var titleSize = graphics.MeasureString(title, titleFont);
            graphics.DrawString(title, titleFont, Brushes.Black, 
                (width - titleSize.Width) / 2, 10);

            // Define colors for different currencies
            var colors = new[] { Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Purple, Color.Brown, Color.Pink };

            // Draw lines for each currency
            for (int i = 0; i < CurrencyMonitoringService.MonitoredCurrencies.Length; i++)
            {
                var currency = CurrencyMonitoringService.MonitoredCurrencies[i];
                var data = _monitoringService.GetHistoricalData(currency, startDate, endDate);
                
                if (!data.Any()) continue;

                var color = colors[i % colors.Length];
                using var pen = new Pen(color, 2);
                
                var points = new List<PointF>();
                foreach (var point in data)
                {
                    var x = chartArea.Left + (float)((point.Timestamp - minDate).TotalDays / (maxDate - minDate).TotalDays * chartArea.Width);
                    var y = chartArea.Bottom - (float)(((decimal)point.Rate - minRate) / (maxRate - minRate) * chartArea.Height);
                    points.Add(new PointF(x, y));
                }

                if (points.Count > 1)
                {
                    graphics.DrawLines(pen, points.ToArray());
                }

                // Draw legend
                var legendY = 30 + i * 20;
                graphics.DrawLine(pen, 10, legendY, 30, legendY);
                using var legendFont = new Font("Arial", 10);
                graphics.DrawString($"USD/{currency}", legendFont, Brushes.Black, 35, legendY - 8);
            }

            // Mark significant events
            var events = _monitoringService.GetSignificantEvents(startDate, endDate);
            using var eventPen = new Pen(Color.Red, 3) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            
            foreach (var evt in events)
            {
                if (evt.Date >= minDate && evt.Date <= maxDate)
                {
                    var x = chartArea.Left + (float)((evt.Date - minDate).TotalDays / (maxDate - minDate).TotalDays * chartArea.Width);
                    graphics.DrawLine(eventPen, x, chartArea.Top, x, chartArea.Bottom);
                    
                    using var eventFont = new Font("Arial", 8);
                    var eventText = evt.Description;
                    var textSize = graphics.MeasureString(eventText, eventFont);
                    graphics.DrawString(eventText, eventFont, Brushes.Red, x - textSize.Width / 2, chartArea.Top - 20);
                }
            }

            // Export the chart
            try
            {
                bitmap.Save(outputPath, ImageFormat.Png);
                WriteLog($"Currency trend chart saved to: {outputPath}", "CurrencyVisualization");
            }
            catch (Exception ex)
            {
                WriteLog($"Error saving chart: {ex.Message}", "CurrencyVisualization");
                throw;
            }
        }

        /// <summary>
        /// Creates a bar chart showing percentage changes for all currencies over a specified period.
        /// </summary>
        public void CreateTrendComparisonChart(string outputPath, DateTime? startDate = null, DateTime? endDate = null,
            int width = 1000, int height = 600)
        {
            var trends = _monitoringService.AnalyzeAllTrends(startDate, endDate);
            
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.Clear(Color.White);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Define chart area
            var margin = 80;
            var chartArea = new Rectangle(margin, margin, width - 2 * margin, height - 2 * margin);
            
            // Draw title
            using var titleFont = new Font("Arial", 16, FontStyle.Bold);
            var title = "USD Currency Strength/Weakness Comparison";
            var titleSize = graphics.MeasureString(title, titleFont);
            graphics.DrawString(title, titleFont, Brushes.Black, 
                (width - titleSize.Width) / 2, 10);

            // Calculate ranges
            var maxChange = Math.Max(Math.Abs(trends.Min(t => t.PercentageChange)), Math.Abs(trends.Max(t => t.PercentageChange)));
            var barWidth = chartArea.Width / trends.Count;

            // Draw axes
            using var axisPen = new Pen(Color.Black, 2);
            var zeroY = chartArea.Top + chartArea.Height / 2;
            graphics.DrawLine(axisPen, chartArea.Left, zeroY, chartArea.Right, zeroY); // Zero line
            graphics.DrawLine(axisPen, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom); // Y-axis

            // Draw bars
            for (int i = 0; i < trends.Count; i++)
            {
                var trend = trends[i];
                var x = chartArea.Left + i * barWidth + barWidth * 0.1f;
                var barHeight = Math.Abs((float)(trend.PercentageChange / maxChange * chartArea.Height / 2));
                var y = trend.PercentageChange >= 0 ? zeroY - barHeight : zeroY;
                
                var color = trend.PercentageChange >= 0 ? Color.Green : Color.Red;
                using var brush = new SolidBrush(color);
                
                graphics.FillRectangle(brush, x, y, barWidth * 0.8f, barHeight);
                
                // Draw currency label
                using var labelFont = new Font("Arial", 8);
                var label = trend.CurrencyPair.Replace("USD/", "");
                var labelSize = graphics.MeasureString(label, labelFont);
                graphics.DrawString(label, labelFont, Brushes.Black, 
                    x + (barWidth * 0.8f - labelSize.Width) / 2, chartArea.Bottom + 5);
                
                // Draw percentage value
                var valueText = $"{trend.PercentageChange:+0.0;-0.0}%";
                var valueSize = graphics.MeasureString(valueText, labelFont);
                var valueY = trend.PercentageChange >= 0 ? y - valueSize.Height - 2 : y + barHeight + 2;
                graphics.DrawString(valueText, labelFont, Brushes.Black, 
                    x + (barWidth * 0.8f - valueSize.Width) / 2, valueY);
            }

            // Export the chart
            try
            {
                bitmap.Save(outputPath, ImageFormat.Png);
                WriteLog($"Currency comparison chart saved to: {outputPath}", "CurrencyVisualization");
            }
            catch (Exception ex)
            {
                WriteLog($"Error saving comparison chart: {ex.Message}", "CurrencyVisualization");
                throw;
            }
        }

        /// <summary>
        /// Creates a detailed chart for a single currency showing trend with significant events marked.
        /// </summary>
        public void CreateSingleCurrencyChart(string currency, string outputPath, DateTime? startDate = null, 
            DateTime? endDate = null, int width = 1000, int height = 600)
        {
            var data = _monitoringService.GetHistoricalData(currency, startDate, endDate);
            
            if (!data.Any())
            {
                throw new ArgumentException($"No data available for currency {currency}");
            }

            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.Clear(Color.White);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Define chart area
            var margin = 60;
            var chartArea = new Rectangle(margin, margin, width - 2 * margin, height - 2 * margin);
            
            // Calculate value ranges
            var minDate = data.Min(d => d.Timestamp);
            var maxDate = data.Max(d => d.Timestamp);
            var minRate = data.Min(d => d.Rate);
            var maxRate = data.Max(d => d.Rate);
            
            // Add some padding to the rate range
            var rateRange = maxRate - minRate;
            minRate -= rateRange * 0.1m;
            maxRate += rateRange * 0.1m;

            // Draw title
            using var titleFont = new Font("Arial", 16, FontStyle.Bold);
            var title = $"USD/{currency} Exchange Rate Trend";
            var titleSize = graphics.MeasureString(title, titleFont);
            graphics.DrawString(title, titleFont, Brushes.Black, 
                (width - titleSize.Width) / 2, 10);

            // Draw axes
            using var axisPen = new Pen(Color.Black, 2);
            graphics.DrawLine(axisPen, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom); // X-axis
            graphics.DrawLine(axisPen, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom); // Y-axis

            // Draw main trend line
            using var trendPen = new Pen(Color.Blue, 2);
            var points = new List<PointF>();
            
            foreach (var point in data)
            {
                var x = chartArea.Left + (float)((point.Timestamp - minDate).TotalDays / (maxDate - minDate).TotalDays * chartArea.Width);
                var y = chartArea.Bottom - (float)(((decimal)point.Rate - minRate) / (maxRate - minRate) * chartArea.Height);
                points.Add(new PointF(x, y));
            }

            if (points.Count > 1)
            {
                graphics.DrawLines(trendPen, points.ToArray());
            }

            // Mark significant events
            var events = _monitoringService.GetSignificantEvents(startDate, endDate);
            using var eventBrush = new SolidBrush(Color.Red);
            
            foreach (var evt in events)
            {
                if (evt.Date >= minDate && evt.Date <= maxDate)
                {
                    // Find the rate closest to the event date
                    var eventRate = data
                        .OrderBy(d => Math.Abs((d.Timestamp - evt.Date).TotalDays))
                        .FirstOrDefault();

                    if (eventRate != null)
                    {
                        var x = chartArea.Left + (float)((evt.Date - minDate).TotalDays / (maxDate - minDate).TotalDays * chartArea.Width);
                        var y = chartArea.Bottom - (float)(((decimal)eventRate.Rate - minRate) / (maxRate - minRate) * chartArea.Height);
                        
                        // Draw diamond marker
                        var markerSize = 6;
                        var diamond = new PointF[]
                        {
                            new PointF(x, y - markerSize),
                            new PointF(x + markerSize, y),
                            new PointF(x, y + markerSize),
                            new PointF(x - markerSize, y)
                        };
                        graphics.FillPolygon(eventBrush, diamond);
                    }
                }
            }

            // Export the chart
            try
            {
                bitmap.Save(outputPath, ImageFormat.Png);
                WriteLog($"Single currency chart for {currency} saved to: {outputPath}", "CurrencyVisualization");
            }
            catch (Exception ex)
            {
                WriteLog($"Error saving single currency chart: {ex.Message}", "CurrencyVisualization");
                throw;
            }
        }

        /// <summary>
        /// Generates a comprehensive report with multiple charts and trend analysis.
        /// </summary>
        public void GenerateComprehensiveReport(string outputDirectory, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var start = startDate ?? new DateTime(2024, 1, 1);
            var end = endDate ?? DateTime.Now;
            
            WriteLog($"Generating comprehensive currency report for period {start:yyyy-MM-dd} to {end:yyyy-MM-dd}", "CurrencyVisualization");

            // Generate main trend chart
            var mainChartPath = Path.Combine(outputDirectory, "USD_Currency_Trends.png");
            CreateTrendChart(mainChartPath, start, end);

            // Generate comparison chart
            var comparisonChartPath = Path.Combine(outputDirectory, "USD_Currency_Comparison.png");
            CreateTrendComparisonChart(comparisonChartPath, start, end);

            // Generate individual currency charts
            foreach (var currency in CurrencyMonitoringService.MonitoredCurrencies)
            {
                var currencyChartPath = Path.Combine(outputDirectory, $"USD_{currency}_Trend.png");
                CreateSingleCurrencyChart(currency, currencyChartPath, start, end);
            }

            WriteLog($"Comprehensive currency report generated in directory: {outputDirectory}", "CurrencyVisualization");
        }
    }
}