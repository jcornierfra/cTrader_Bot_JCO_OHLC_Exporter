// -------------------------------------------------------------------------------------------------
//
//    JCO OHLC Exporter - Historical OHLC Data Exporter for cTrader
//
//    This cBot exports historical OHLC candlestick data to a CSV file for a given
//    symbol and timeframe over a specified date range.
//
//    Features:
//    - Export OHLC + tick volume data to CSV
//    - Configurable symbol, timeframe, start date and end date
//    - Automatic lazy-loading of historical bars to cover the requested period
//    - Configurable output file path
//    - UTC-based date handling
//
//    Output format: DateTime,Open,High,Low,Close,Volume
//
//    Author: J. Cornier
//    Version: 1.0
//    Last Updated: 2026-04-05
//
//    Changelog:
//    - v1.0: Initial release
//
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using cAlgo.API;

[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
public class OHLCExporter : Robot
{
    [Parameter("Start Date", DefaultValue = "2024-01-01")]
    public string StartDateStr { get; set; }

    [Parameter("End Date", DefaultValue = "2024-12-31")]
    public string EndDateStr { get; set; }

    [Parameter("TimeFrame", DefaultValue = "Hour")]
    public TimeFrame ExportTimeFrame { get; set; }

    [Parameter("Symbol", DefaultValue = "US100.cash")]
    public string ExportSymbol { get; set; }

    [Parameter("Output Path", DefaultValue = "ohlc_export.csv")]
    public string OutputPath { get; set; }

    protected override void OnStart()
    {
        var resolvedPath = Path.IsPathRooted(OutputPath)
            ? OutputPath
            : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), OutputPath);

        var startDate = DateTime.Parse(StartDateStr, null,
            System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();
        var endDate = DateTime.Parse(EndDateStr, null,
            System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();

        // Chargement des barres pour le symbole/TF voulu
        var bars = MarketData.GetBars(ExportTimeFrame, ExportSymbol);

        // S'assurer que suffisamment de données sont chargées
        // (cTrader charge en lazy loading — on peut forcer via LoadMoreHistory)
        while (bars.Count > 0 && bars[0].OpenTime > startDate)
        {
            var loaded = bars.LoadMoreHistory();
            if (loaded == 0) break; // Plus d'historique disponible
        }

        int exported = 0;

        using (var writer = new StreamWriter(resolvedPath, false))
        {
            writer.WriteLine("DateTime,Open,High,Low,Close,Volume");

            foreach (var bar in bars)
            {
                if (bar.OpenTime < startDate) continue;
                if (bar.OpenTime > endDate) break;

                writer.WriteLine(
                    $"{bar.OpenTime:yyyy-MM-dd HH:mm:ss}," +
                    $"{bar.Open},{bar.High},{bar.Low},{bar.Close},{bar.TickVolume}"
                );
                exported++;
            }
        }

        Print($"Export terminé : {exported} bougies exportées vers {resolvedPath}");
        Stop();
    }
}