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
//    - Culture-invariant numeric formatting (decimal point) + selectable delimiter
//
//    Output format: DateTime<sep>Open<sep>High<sep>Low<sep>Close<sep>Volume
//
//    Author: J. Cornier
//    Version: 1.2
//    Last Updated: 2026-08-12
//
//    Changelog:
//    - v1.0: Initial release
//    - v1.1: Fix decimal/separator collision — prices now written with InvariantCulture
//            (decimal POINT), and the column delimiter is configurable (default ';').
//            Optional decimal-comma mode for French Excel without an import wizard.
//    - v1.2: Fix "Price Digits = auto" — the digit count is now read from the EXPORTED
//            symbol instead of the chart symbol the cBot is attached to (exporting
//            EURUSD from a US100 chart truncated prices to 2 decimals).
//
//    GitHub: https://github.com/jcornierfra/cTrader_Bot_JCO_OHLC_Exporter
//
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
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

    // Séparateur de colonnes. Défaut ';' : jamais en conflit avec un point décimal,
    // et ouvert directement par Excel FR sans assistant d'import.
    [Parameter("Column Delimiter", DefaultValue = ";")]
    public string Delimiter { get; set; }

    // Si true : prix en virgule décimale FR (25252,16) AVEC séparateur ';'.
    // Si false (défaut) : prix en point décimal invariant (25252.16), format machine standard.
    [Parameter("Decimal Comma (FR)", DefaultValue = false)]
    public bool DecimalComma { get; set; }

    // Nombre de décimales des prix. 0 = laisser cTrader décider selon le symbole.
    [Parameter("Price Digits (0=auto)", DefaultValue = 0, MinValue = 0, MaxValue = 8)]
    public int PriceDigits { get; set; }

    protected override void OnStart()
    {
        var resolvedPath = Path.IsPathRooted(OutputPath)
            ? OutputPath
            : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), OutputPath);

        var startDate = DateTime.Parse(StartDateStr, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal).ToUniversalTime();
        var endDate = DateTime.Parse(EndDateStr, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal).ToUniversalTime();

        // Garde-fou : si le séparateur est la virgule ET qu'on écrit en virgule décimale,
        // le fichier serait illisible (exactement le bug de la v1.0). On refuse.
        string sep = string.IsNullOrEmpty(Delimiter) ? ";" : Delimiter;
        if (DecimalComma && sep == ",")
        {
            Print("ERREUR: séparateur ',' incompatible avec 'Decimal Comma (FR)'. " +
                  "Choisissez ';' comme séparateur, ou désactivez Decimal Comma. Export annulé.");
            Stop();
            return;
        }

        // Nom du symbole à exporter : celui du paramètre, sinon celui du graphique.
        string symbolName = string.IsNullOrWhiteSpace(ExportSymbol) ? SymbolName : ExportSymbol.Trim();

        // Décimales en mode auto : celles du SYMBOLE EXPORTÉ, pas celles du graphique
        // auquel le cBot est attaché (bug v1.1 : EURUSD exporté depuis un chart US100
        // héritait des 2 décimales de l'indice au lieu de ses 5).
        int digits = PriceDigits;
        if (digits == 0)
        {
            var exportedSymbol = Symbols.GetSymbol(symbolName);
            if (exportedSymbol == null)
            {
                Print($"ERREUR: symbole '{symbolName}' introuvable. Export annulé.");
                Stop();
                return;
            }
            digits = exportedSymbol.Digits;
        }

        // Format numérique des prix : invariant (point) par défaut, ou FR (virgule) au choix.
        var priceCulture = DecimalComma ? CultureInfo.GetCultureInfo("fr-FR") : CultureInfo.InvariantCulture;
        string priceFmt = "F" + digits;

        // Chargement des barres pour le symbole/TF voulu
        var bars = MarketData.GetBars(ExportTimeFrame, symbolName);

        // Lazy loading : forcer le chargement jusqu'à couvrir la période demandée
        while (bars.Count > 0 && bars[0].OpenTime > startDate)
        {
            var loaded = bars.LoadMoreHistory();
            if (loaded == 0) break; // Plus d'historique disponible
        }

        int exported = 0;

        using (var writer = new StreamWriter(resolvedPath, false))
        {
            writer.WriteLine(string.Join(sep, "DateTime", "Open", "High", "Low", "Close", "Volume"));

            foreach (var bar in bars)
            {
                if (bar.OpenTime < startDate) continue;
                if (bar.OpenTime > endDate) break;

                // Date toujours en format invariant ISO (jamais localisée)
                string dt = bar.OpenTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                string o = bar.Open.ToString(priceFmt, priceCulture);
                string h = bar.High.ToString(priceFmt, priceCulture);
                string l = bar.Low.ToString(priceFmt, priceCulture);
                string c = bar.Close.ToString(priceFmt, priceCulture);
                string v = bar.TickVolume.ToString(CultureInfo.InvariantCulture);

                writer.WriteLine(string.Join(sep, dt, o, h, l, c, v));
                exported++;
            }
        }

        Print($"Export terminé : {exported} bougies {symbolName} {ExportTimeFrame} -> {resolvedPath} " +
              $"(sép='{sep}', décimale={(DecimalComma ? "virgule" : "point")}, {digits} déc." +
              $"{(PriceDigits > 0 ? " forcées" : " auto")})");
        Stop();
    }
}