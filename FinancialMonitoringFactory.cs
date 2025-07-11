using System;
using System.Net.Http;
using EAGLE.Library.Financial;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EAGLE.Library.Financial;

[PublicAPI]
public static class FinancialMonitoringFactory
{
    /// <summary>
    /// Creates a financial monitoring service with default configuration
    /// </summary>
    /// <returns>Configured financial monitoring service</returns>
    public static IFinancialMonitoringService CreateMonitoringService()
    {
        var configuration = CreateDefaultConfiguration();
        var httpClient = new HttpClient();
        var financialDataService = new FinancialDataService(httpClient, configuration);
        
        return new FinancialMonitoringService(financialDataService, configuration);
    }
    
    /// <summary>
    /// Creates a financial monitoring service with custom configuration
    /// </summary>
    /// <param name="configuration">Custom configuration settings</param>
    /// <returns>Configured financial monitoring service</returns>
    public static IFinancialMonitoringService CreateMonitoringService(IConfiguration configuration)
    {
        var httpClient = new HttpClient();
        var financialDataService = new FinancialDataService(httpClient, configuration);
        
        return new FinancialMonitoringService(financialDataService, configuration);
    }
    
    /// <summary>
    /// Creates a financial data service for manual data retrieval
    /// </summary>
    /// <returns>Configured financial data service</returns>
    public static IFinancialDataService CreateDataService()
    {
        var configuration = CreateDefaultConfiguration();
        var httpClient = new HttpClient();
        
        return new FinancialDataService(httpClient, configuration);
    }
    
    /// <summary>
    /// Creates a financial data service with custom configuration
    /// </summary>
    /// <param name="configuration">Custom configuration settings</param>
    /// <returns>Configured financial data service</returns>
    public static IFinancialDataService CreateDataService(IConfiguration configuration)
    {
        var httpClient = new HttpClient();
        return new FinancialDataService(httpClient, configuration);
    }
    
    /// <summary>
    /// Configures services for dependency injection
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <param name="configuration">Configuration settings</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddFinancialMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddSingleton<IFinancialDataService, FinancialDataService>();
        services.AddSingleton<IFinancialMonitoringService, FinancialMonitoringService>();
        services.AddHostedService<FinancialMonitoringService>();
        
        return services;
    }
    
    /// <summary>
    /// Creates default configuration for financial monitoring
    /// </summary>
    /// <returns>Default configuration</returns>
    private static IConfiguration CreateDefaultConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();
        
        // Add default values
        configurationBuilder.AddInMemoryCollection(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("FinancialMonitoring:UpdateIntervalMinutes", "240"),
            new System.Collections.Generic.KeyValuePair<string, string>("FinancialMonitoring:EnableContinuousMonitoring", "true"),
            new System.Collections.Generic.KeyValuePair<string, string>("FinancialMonitoring:SaveReportsToDatabase", "true"),
            new System.Collections.Generic.KeyValuePair<string, string>("FinancialMonitoring:GenerateAlerts", "true"),
            new System.Collections.Generic.KeyValuePair<string, string>("FinancialMonitoring:ReportOutputPath", "./Reports"),
            new System.Collections.Generic.KeyValuePair<string, string>("FinancialMonitoring:DataRetentionDays", "365"),
            new System.Collections.Generic.KeyValuePair<string, string>("FinancialMonitoring:PriceChangeAlertThreshold", "0.05"),
            new System.Collections.Generic.KeyValuePair<string, string>("FinancialMonitoring:VolumeChangeAlertThreshold", "0.20")
        });
        
        return configurationBuilder.Build();
    }
}