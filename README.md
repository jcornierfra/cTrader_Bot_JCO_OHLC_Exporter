# JCO OHLC Exporter

A cTrader cBot that exports historical OHLC candlestick data to a CSV file for a given symbol, timeframe, and date range.

## Features

- Export OHLC + tick volume data to CSV
- Configurable symbol, timeframe, start date and end date
- Automatic lazy-loading of historical bars to cover the full requested period
- Flexible output path: relative (saves next to the robot) or absolute
- UTC-based date handling
- Configurable column delimiter, with an optional French decimal-comma mode for Excel
- Price precision auto-detected from the **exported** symbol (5 decimals for EURUSD,
  3 for USDJPY, 2 for indices…), or forced manually

## Parameters

| Parameter             | Default             | Description                                                                                      |
|-----------------------|---------------------|--------------------------------------------------------------------------------------------------|
| Start Date            | `2024-01-01`        | Start of the export period (UTC, format `YYYY-MM-DD`)                                            |
| End Date              | `2024-12-31`        | End of the export period (UTC, format `YYYY-MM-DD`)                                              |
| TimeFrame             | `Hour`              | Timeframe of the candles to export (e.g. `Minute`, `Hour`, `Daily`)                              |
| Symbol                | `US100.cash`        | Symbol to export (must be available in your cTrader broker)                                      |
| Output Path           | `ohlc_export.csv`   | Output file path. If relative, saves next to the robot's compiled assembly                       |
| Column Delimiter      | `;`                 | Column separator. `;` opens directly in French Excel and never clashes with a decimal point      |
| Decimal Comma (FR)    | `false`             | `false`: prices as `1.09345` (machine standard). `true`: `1,09345` — requires a `;` delimiter    |
| Price Digits (0=auto) | `0`                 | Number of decimals for prices. `0` = taken from the exported symbol. Set 1–8 to force a value    |

> The robot refuses to run if `Decimal Comma (FR)` is enabled while the delimiter is `,`,
> since the resulting file would be unparsable.

## Output Format

The CSV file contains one row per candle with the following columns:

```text
DateTime;Open;High;Low;Close;Volume
```

Timestamps are always written in UTC as `yyyy-MM-dd HH:mm:ss`, independent of the
delimiter and decimal settings.

### Sample Output

| DateTime            | Open    | High    | Low     | Close   | Volume |
|---------------------|---------|---------|---------|---------|--------|
| 2024-01-02 00:00:00 | 16842.6 | 16842.9 | 16831.1 | 16839.4 | 3076   |
| 2024-01-02 01:00:00 | 16839.3 | 16850.9 | 16823.5 | 16826.8 | 6692   |
| 2024-01-02 02:00:00 | 16826.9 | 16831.3 | 16813.2 | 16820.8 | 4620   |
| 2024-01-02 03:00:00 | 16820.7 | 16825.3 | 16814.6 | 16819.7 | 3006   |
| 2024-01-02 04:00:00 | 16819.8 | 16825.4 | 16815.8 | 16820.2 | 2346   |

> See [ohlc_export_sample.csv](JCO%20OHLC%20Exporter/ohlc_export_sample.csv) for a full sample file
> (produced with v1.0, hence comma-separated).

## Usage

1. Import the robot into cTrader via **Automate > Open**
2. Configure the parameters (symbol, timeframe, date range, output path)
3. Run the robot on any chart — it will export and stop automatically
4. Retrieve the CSV from the configured output path

> **Note:** The output directory must exist before running the robot. cTrader will not create it automatically.
>
> **Note:** The chart the robot is attached to has no effect on the export — everything
> (data and price precision) comes from the `Symbol` parameter.

## Changelog

- **v1.2** — `Price Digits = auto` now reads the precision of the *exported* symbol
  instead of the chart symbol the robot is attached to (exporting EURUSD from a US100
  chart used to truncate prices to 2 decimals).
- **v1.1** — Prices written with a decimal **point** (InvariantCulture) and a configurable
  column delimiter (default `;`), fixing the collision between decimal commas and comma
  separators. Optional French decimal-comma mode.
- **v1.0** — Initial release.

## Requirements

- cTrader 4.x or later
- The target symbol must be available in your broker's instrument list

## Author

J. Cornier — v1.2 — 2026-08-12
