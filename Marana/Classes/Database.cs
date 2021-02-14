﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data;
using MySql.Data.MySqlClient;

using Skender.Stock.Indicators;

namespace Marana {

    public class Database {
        public Settings _Settings;

        public Database(Settings settings) {
            _Settings = settings;
        }

        public enum ColumnsDaily {
            ID,
            Asset,
            Symbol,
            Date,
            Open,
            High,
            Low,
            Close,
            Volume,
            SMA7,
            SMA20,
            SMA50,
            SMA100,
            SMA200,
            EMA7,
            EMA20,
            EMA50,
            DEMA7,
            DEMA20,
            DEMA50,
            TEMA7,
            TEMA20,
            TEMA50,
            RSI,
            MACD,
            MACD_Histogram,
            MACD_Signal,
            BollingerBands_Center,
            BollingerBands_Upper,
            BollingerBands_Lower,
            BollingerBands_Percent,
            BollingerBands_ZScore,
            BollingerBands_Width,
        };

        public string ConnectionStr {
            get {
                return $"server={_Settings.Database_Server}; user={_Settings.Database_Username}; "
                    + $"database={_Settings.Database_Schema}; port={_Settings.Database_Port}; "
                   + $"password={_Settings.Database_Password}";
            }
        }

        public async Task Init() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                using (MySqlCommand cmd = new MySqlCommand(
                        @"CREATE TABLE IF NOT EXISTS `Validity` (
                            `ID` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            `Item` VARCHAR(64) NOT NULL,
                            `Updated` DATETIME
                            ) AUTO_INCREMENT = 1;",
                        connection))
                    await cmd.ExecuteNonQueryAsync();

                using (MySqlCommand cmd = new MySqlCommand(
                        @"CREATE TABLE IF NOT EXISTS `Assets` (
                            `ID` VARCHAR(64) PRIMARY KEY,
                            `Symbol` VARCHAR(10) NOT NULL,
                            `Class` VARCHAR (16),
                            `Exchange` VARCHAR (16),
                            `Status` VARCHAR (16),
                            `Tradeable` BOOLEAN,
                            `Marginable` BOOLEAN,
                            `Shortable` BOOLEAN,
                            `EasyToBorrow` BOOLEAN
                            );",
                        connection))
                    await cmd.ExecuteNonQueryAsync();

                using (MySqlCommand cmd = new MySqlCommand(
                        $@"CREATE TABLE IF NOT EXISTS `Daily` (
                            `ID` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            `Asset` VARCHAR(64),
                            `Symbol` VARCHAR(16),
                            `Date` DATE NOT NULL,
                            `Open` DECIMAL(16, 6),
                            `High` DECIMAL(16, 6),
                            `Low` DECIMAL(16, 6),
                            `Close` DECIMAL(16, 6),
                            `Volume` BIGINT,
                            `SMA7` DECIMAL(16, 6),
                            `SMA20` DECIMAL(16, 6),
                            `SMA50` DECIMAL(16, 6),
                            `SMA100` DECIMAL(16, 6),
                            `SMA200` DECIMAL(16, 6),
                            `EMA7` DECIMAL(16, 6),
                            `EMA20` DECIMAL(16, 6),
                            `EMA50` DECIMAL(16, 6),
                            `DEMA7` DECIMAL(16, 6),
                            `DEMA20` DECIMAL(16, 6),
                            `DEMA50` DECIMAL(16, 6),
                            `TEMA7` DECIMAL(16, 6),
                            `TEMA20` DECIMAL(16, 6),
                            `TEMA50` DECIMAL(16, 6),
                            `RSI` DECIMAL(16, 6),
                            `MACD` DECIMAL(16, 6),
                            `MACD_Histogram` DECIMAL(16, 6),
                            `MACD_Signal` DECIMAL(16, 6),
                            `BollingerBands_Center` DECIMAL(16, 6),
                            `BollingerBands_Upper` DECIMAL(16, 6),
                            `BollingerBands_Lower` DECIMAL(16, 6),
                            `BollingerBands_Percent` DECIMAL(16, 6),
                            `BollingerBands_ZScore` DECIMAL(16, 6),
                            `BollingerBands_Width` DECIMAL(16, 6),
                            INDEX(`Asset`));",
                        connection))
                    await cmd.ExecuteNonQueryAsync();

                using (MySqlCommand cmd = new MySqlCommand(
                        $@"CREATE TABLE IF NOT EXISTS `Strategies` (
                            `Name` VARCHAR(256) PRIMARY KEY,
                            `Query` TEXT
                            );",
                        connection))
                    await cmd.ExecuteNonQueryAsync();

                await connection.CloseAsync();
            }
        }

        public async Task<List<Data.Asset>> GetAssets() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return new List<Data.Asset>();
                }

                List<Data.Asset> assets = new List<Data.Asset>();

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                           @"SELECT * FROM `Assets` ORDER BY Symbol;",
                           connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read())
                                assets.Add(new Data.Asset() {
                                    ID = rdr.GetString("ID"),
                                    Symbol = rdr.GetString("Symbol"),
                                    Class = rdr.GetString("Class"),
                                    Exchange = rdr.GetString("Exchange"),
                                    Status = rdr.GetString("Status"),
                                    Tradeable = rdr.GetBoolean("Tradeable"),
                                    Marginable = rdr.GetBoolean("Marginable"),
                                    Shortable = rdr.GetBoolean("Shortable"),
                                    EasyToBorrow = rdr.GetBoolean("EasyToBorrow")
                                });
                        }
                    }

                    await connection.CloseAsync();
                    return assets;
                } catch (Exception ex) {
                    await connection.CloseAsync();
                    return new List<Data.Asset>();
                }
            }
        }

        public async Task<Data.Daily> GetData_Daily(Data.Asset asset) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return null;
                }

                string table = $"Daily:{asset.ID}";
                Data.Daily ds = new Data.Daily() { Asset = asset };

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"SELECT * FROM `Daily` WHERE Asset = '{asset.ID}';",
                            connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read()) {
                                Data.Daily.Price p = new Data.Daily.Price();
                                p.Date = rdr.IsDBNull(ColumnsDaily.Date.GetHashCode()) ? p.Date : rdr.GetDateTime("Date");
                                p.Open = rdr.IsDBNull(ColumnsDaily.Open.GetHashCode()) ? p.Open : rdr.GetDecimal("Open");
                                p.High = rdr.IsDBNull(ColumnsDaily.High.GetHashCode()) ? p.High : rdr.GetDecimal("High");
                                p.Low = rdr.IsDBNull(ColumnsDaily.Low.GetHashCode()) ? p.Low : rdr.GetDecimal("Low");
                                p.Close = rdr.IsDBNull(ColumnsDaily.Close.GetHashCode()) ? p.Close : rdr.GetDecimal("Close");
                                p.Volume = rdr.IsDBNull(ColumnsDaily.Volume.GetHashCode()) ? p.Volume : rdr.GetInt64("Volume");

                                Data.Daily.Metric m = new Data.Daily.Metric();
                                m.SMA7 = rdr.IsDBNull(ColumnsDaily.SMA7.GetHashCode()) ? m.SMA7 : rdr.GetDecimal("SMA7");
                                m.SMA20 = rdr.IsDBNull(ColumnsDaily.SMA20.GetHashCode()) ? m.SMA20 : rdr.GetDecimal("SMA20");
                                m.SMA50 = rdr.IsDBNull(ColumnsDaily.SMA50.GetHashCode()) ? m.SMA50 : rdr.GetDecimal("SMA50");
                                m.SMA100 = rdr.IsDBNull(ColumnsDaily.SMA100.GetHashCode()) ? m.SMA100 : rdr.GetDecimal("SMA100");
                                m.SMA200 = rdr.IsDBNull(ColumnsDaily.SMA200.GetHashCode()) ? m.SMA200 : rdr.GetDecimal("SMA200");
                                m.EMA7 = rdr.IsDBNull(ColumnsDaily.EMA7.GetHashCode()) ? m.EMA7 : rdr.GetDecimal("EMA7");
                                m.EMA20 = rdr.IsDBNull(ColumnsDaily.EMA20.GetHashCode()) ? m.EMA20 : rdr.GetDecimal("EMA20");
                                m.EMA50 = rdr.IsDBNull(ColumnsDaily.EMA50.GetHashCode()) ? m.EMA50 : rdr.GetDecimal("EMA50");
                                m.DEMA7 = rdr.IsDBNull(ColumnsDaily.DEMA7.GetHashCode()) ? m.DEMA7 : rdr.GetDecimal("DEMA7");
                                m.DEMA20 = rdr.IsDBNull(ColumnsDaily.DEMA20.GetHashCode()) ? m.DEMA20 : rdr.GetDecimal("DEMA20");
                                m.DEMA50 = rdr.IsDBNull(ColumnsDaily.DEMA50.GetHashCode()) ? m.DEMA50 : rdr.GetDecimal("DEMA50");
                                m.TEMA7 = rdr.IsDBNull(ColumnsDaily.TEMA7.GetHashCode()) ? m.TEMA7 : rdr.GetDecimal("TEMA7");
                                m.TEMA20 = rdr.IsDBNull(ColumnsDaily.TEMA20.GetHashCode()) ? m.TEMA20 : rdr.GetDecimal("TEMA20");
                                m.TEMA50 = rdr.IsDBNull(ColumnsDaily.TEMA50.GetHashCode()) ? m.TEMA50 : rdr.GetDecimal("TEMA50");
                                m.RSI = rdr.IsDBNull(ColumnsDaily.RSI.GetHashCode()) ? m.RSI : rdr.GetDecimal("RSI");
                                m.MACD = new MacdResult();
                                m.MACD.Macd = rdr.IsDBNull(ColumnsDaily.MACD.GetHashCode()) ? m.MACD.Macd : rdr.GetDecimal("MACD");
                                m.MACD.Histogram = rdr.IsDBNull(ColumnsDaily.MACD_Histogram.GetHashCode()) ? m.MACD.Histogram : rdr.GetDecimal("MACD_Histogram");
                                m.MACD.Signal = rdr.IsDBNull(ColumnsDaily.MACD_Signal.GetHashCode()) ? m.MACD.Signal : rdr.GetDecimal("MACD_Signal");

                                m.BB = new BollingerBandsResult();
                                m.BB.Sma = rdr.IsDBNull(ColumnsDaily.BollingerBands_Center.GetHashCode()) ? m.BB.Sma : rdr.GetDecimal("BollingerBands_Center");
                                m.BB.UpperBand = rdr.IsDBNull(ColumnsDaily.BollingerBands_Upper.GetHashCode()) ? m.BB.UpperBand : rdr.GetDecimal("BollingerBands_Upper");
                                m.BB.LowerBand = rdr.IsDBNull(ColumnsDaily.BollingerBands_Lower.GetHashCode()) ? m.BB.LowerBand : rdr.GetDecimal("BollingerBands_Lower");
                                m.BB.PercentB = rdr.IsDBNull(ColumnsDaily.BollingerBands_Percent.GetHashCode()) ? m.BB.PercentB : rdr.GetDecimal("BollingerBands_Percent");
                                m.BB.ZScore = rdr.IsDBNull(ColumnsDaily.BollingerBands_ZScore.GetHashCode()) ? m.BB.ZScore : rdr.GetDecimal("BollingerBands_ZScore");
                                m.BB.Width = rdr.IsDBNull(ColumnsDaily.BollingerBands_Width.GetHashCode()) ? m.BB.Width : rdr.GetDecimal("BollingerBands_Width");

                                p.Metric = m;
                                m.Price = p;

                                ds.Prices.Add(p);
                                ds.Metrics.Add(m);
                            }
                        }
                    }

                    await connection.CloseAsync();
                    return ds;
                } catch (Exception ex) {
                    await connection.CloseAsync();
                    return null;
                    // TO-DO: log errors to error log
                }
            }
        }

        public async Task<DateTime> GetValidity(string item) {
            DateTime result = new DateTime();

            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return new DateTime();
                }

                using (MySqlCommand cmd = new MySqlCommand(
                        $@"SELECT `Updated`
                            FROM `Validity`
                            WHERE `Item` = '{item}';",
                        connection))
                    result = await cmd.ExecuteScalarAsync() != null ? (DateTime)(await cmd.ExecuteScalarAsync()) : new DateTime();

                await connection.CloseAsync();
            }

            return result;
        }

        public async Task<DateTime> GetValidity_Assets()
            => await GetValidity("Assets");

        public async Task<DateTime> GetValidity_Daily(Data.Asset asset)
            => await GetValidity($"Daily:{asset.ID}");

        public async Task<decimal> GetSize() {
            await Init();                                     // Cannot get size of a schema with no tables!

            decimal size = 0;

            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return 0;
                }

                List<string> tables = new List<string>();
                using (MySqlCommand cmd = new MySqlCommand(
                    $@"SELECT table_name
                        FROM information_schema.tables
                        WHERE table_schema = '{_Settings.Database_Schema}';",
                    connection)) {
                    using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                        while (rdr.Read())
                            tables.Add(rdr.GetString(0));
                    }
                }

                string analyzelist = String.Join(", ",
                    tables.Select(t => $"`{MySqlHelper.EscapeString(_Settings.Database_Schema)}`.`{MySqlHelper.EscapeString(t)}`"));

                using (MySqlCommand cmd = new MySqlCommand($@"ANALYZE TABLE {analyzelist};",
                    connection)) {
                    await cmd.ExecuteNonQueryAsync();
                }

                using (MySqlCommand cmd = new MySqlCommand(
                    $@"SELECT table_schema `{_Settings.Database_Schema}`,
                            ROUND(SUM(data_length + index_length) / 1024 / 1024, 1) 'size'
                        FROM information_schema.tables
                        GROUP BY table_schema;",
                    connection)) {
                    using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                        while (rdr.Read()) {
                            size += rdr.GetDecimal("size");
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return size;
        }

        public async Task SetAssets(List<Data.Asset> assets) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                if (assets.Count == 0) {
                    await SetValidity("Assets");
                    await connection.CloseAsync();
                    return;
                }

                // Delete data that will be updated or rewritten

                string deletes = String.Join(" OR ",
                    assets.Select(a =>
                    $"ID = '{a.ID}'"));
                using (MySqlCommand cmd = new MySqlCommand(
                        $@"DELETE FROM `Assets` WHERE {deletes};",
                        connection)) {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Insert new or updated data

                string inserts = String.Join(", ",
                    assets.Select(a =>
                    $"('{MySqlHelper.EscapeString(a.ID)}', "
                    + $"'{MySqlHelper.EscapeString(a.Symbol)}', "
                    + $"'{MySqlHelper.EscapeString(a.Class)}', "
                    + $"'{MySqlHelper.EscapeString(a.Exchange)}',"
                    + $"'{MySqlHelper.EscapeString(a.Status)}', "
                    + $"'{MySqlHelper.EscapeString(a.Tradeable.GetHashCode().ToString())}', "
                    + $"'{MySqlHelper.EscapeString(a.Marginable.GetHashCode().ToString())}', "
                    + $"'{MySqlHelper.EscapeString(a.Shortable.GetHashCode().ToString())}', "
                    + $"'{MySqlHelper.EscapeString(a.EasyToBorrow.GetHashCode().ToString())}')"));
                using (MySqlCommand cmd = new MySqlCommand(
                        $@"INSERT INTO `Assets` ( ID, Symbol, Class, Exchange, Status, Tradeable, Marginable, Shortable, EasyToBorrow ) "
                        + $"VALUES {inserts};",
                        connection)) {
                    await cmd.ExecuteNonQueryAsync();
                }

                await SetValidity("Assets");
                await connection.CloseAsync();
            }
        }

        public async Task SetData_Daily(object dataset)
            => await SetData_Daily((Data.Daily)dataset);

        public async Task SetData_Daily(Data.Daily dataset) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                // Delete data that will be updated or rewritten

                using (MySqlCommand cmd = new MySqlCommand(
                        $@"DELETE FROM `Daily`
                            WHERE Asset = '{dataset.Asset.ID}';",
                        connection)) {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Insert the data into the table

                if (dataset.Prices.Count == 0) {
                    await SetValidity($"Daily:{dataset.Asset.ID}");
                    await connection.CloseAsync();
                    return;
                }

                string values = String.Join(", ",
                    dataset.Prices.Select(v =>
                        $@"(
                        '{MySqlHelper.EscapeString(dataset.Asset.ID)}',
                        '{MySqlHelper.EscapeString(dataset.Asset.Symbol)}',
                        '{MySqlHelper.EscapeString(v.Date.ToString("yyyy-MM-dd"))}',
                        '{MySqlHelper.EscapeString(v.Open.ToString())}',
                        '{MySqlHelper.EscapeString(v.High.ToString())}',
                        '{MySqlHelper.EscapeString(v.Low.ToString())}',
                        '{MySqlHelper.EscapeString(v.Close.ToString())}',
                        '{MySqlHelper.EscapeString(v.Volume.ToString())}',
                        {(v.Metric?.SMA7 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA7.ToString())}'" : "null")},
                        {(v.Metric?.SMA20 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA20.ToString())}'" : "null")},
                        {(v.Metric?.SMA50 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA50.ToString())}'" : "null")},
                        {(v.Metric?.SMA100 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA100.ToString())}'" : "null")},
                        {(v.Metric?.SMA200 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA200.ToString())}'" : "null")},
                        {(v.Metric?.EMA7 != null ? $"'{MySqlHelper.EscapeString(v.Metric.EMA7.ToString())}'" : "null")},
                        {(v.Metric?.EMA20 != null ? $"'{MySqlHelper.EscapeString(v.Metric.EMA20.ToString())}'" : "null")},
                        {(v.Metric?.EMA50 != null ? $"'{MySqlHelper.EscapeString(v.Metric.EMA50.ToString())}'" : "null")},
                        {(v.Metric?.DEMA7 != null ? $"'{MySqlHelper.EscapeString(v.Metric.DEMA7.ToString())}'" : "null")},
                        {(v.Metric?.DEMA20 != null ? $"'{MySqlHelper.EscapeString(v.Metric.DEMA20.ToString())}'" : "null")},
                        {(v.Metric?.DEMA50 != null ? $"'{MySqlHelper.EscapeString(v.Metric.DEMA50.ToString())}'" : "null")},
                        {(v.Metric?.TEMA7 != null ? $"'{MySqlHelper.EscapeString(v.Metric.TEMA7.ToString())}'" : "null")},
                        {(v.Metric?.TEMA20 != null ? $"'{MySqlHelper.EscapeString(v.Metric.TEMA20.ToString())}'" : "null")},
                        {(v.Metric?.TEMA50 != null ? $"'{MySqlHelper.EscapeString(v.Metric.TEMA50.ToString())}'" : "null")},
                        {(v.Metric?.RSI != null ? $"'{MySqlHelper.EscapeString(v.Metric.RSI.ToString())}'" : "null")},
                        {(v.Metric?.MACD?.Macd != null ? $"'{MySqlHelper.EscapeString(v.Metric.MACD.Macd.ToString())}'" : "null")},
                        {(v.Metric?.MACD?.Histogram != null ? $"'{MySqlHelper.EscapeString(v.Metric.MACD.Histogram.ToString())}'" : "null")},
                        {(v.Metric?.MACD?.Signal != null ? $"'{MySqlHelper.EscapeString(v.Metric.MACD.Signal.ToString())}'" : "null")},
                        {(v.Metric?.BB?.Sma != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.Sma.ToString())}'" : "null")},
                        {(v.Metric?.BB?.UpperBand != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.UpperBand.ToString())}'" : "null")},
                        {(v.Metric?.BB?.LowerBand != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.LowerBand.ToString())}'" : "null")},
                        {(v.Metric?.BB?.PercentB != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.PercentB.ToString())}'" : "null")},
                        {(v.Metric?.BB?.ZScore != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.ZScore.ToString())}'" : "null")},
                        {(v.Metric?.BB?.Width != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.Width.ToString())}'" : "null")}
                        )"));

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"INSERT INTO `Daily` (
                                    Asset, Symbol, Date, Open, High, Low, Close, Volume,
                                    SMA7, SMA20, SMA50, SMA100, SMA200,
                                    EMA7, EMA20, EMA50, DEMA7, DEMA20, DEMA50, TEMA7, TEMA20, TEMA50,
                                    RSI,
                                    MACD, MACD_Histogram, MACD_Signal,
                                    BollingerBands_Center, BollingerBands_Upper, BollingerBands_Lower, BollingerBands_Percent, BollingerBands_ZScore, BollingerBands_Width
                                    ) VALUES {values};",
                            connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await SetValidity($"Daily:{dataset.Asset.ID}");
                } catch (Exception ex) {
                    // TO-DO: log errors to error log
                } finally {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task SetValidity(string item) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                bool oldvalidity = false;
                using (MySqlCommand cmd = new MySqlCommand(
                        $@"SELECT `ID` FROM `Validity` WHERE `Item` = '{item}';",
                        connection))
                    oldvalidity = cmd.ExecuteScalar() != null;

                // Use UTC time- in case client and server in different time zones

                if (oldvalidity) {
                    using (MySqlCommand cmd = new MySqlCommand(
                            @"UPDATE `Validity`
                                SET `Updated` = ?updated
                                WHERE (`Item` = ?item);",
                            connection)) {
                        cmd.Parameters.AddWithValue("?updated", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("?item", item);
                        await cmd.ExecuteNonQueryAsync();
                    }
                } else {
                    using (MySqlCommand cmd = new MySqlCommand(
                            @"INSERT INTO `Validity` (
                                `Item`, Updated
                                ) VALUES (
                                ?item, ?updated
                                );",
                            connection)) {
                        cmd.Parameters.AddWithValue("?updated", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("?item", item);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await connection.CloseAsync();
            }
        }

        public async Task<bool> Wipe() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    return false;
                }

                try {
                    using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                        await cmd.ExecuteNonQueryAsync();

                    List<string> tables = new List<string>();
                    using (MySqlCommand cmd = new MySqlCommand(
                        $@"SELECT table_name
                        FROM information_schema.tables
                        WHERE table_schema = '{_Settings.Database_Schema}';",
                        connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read())
                                tables.Add(rdr.GetString(0));
                        }
                    }

                    string droplist = String.Join(", ",
                        tables.Select(t => $"`{MySqlHelper.EscapeString(t)}`"));
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"DROP TABLE IF EXISTS {droplist};",
                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                        await cmd.ExecuteNonQueryAsync();

                    await Init();
                    await connection.CloseAsync();
                    return true;
                } catch (Exception ex) {
                    return false;
                }
            }
        }
    }
}