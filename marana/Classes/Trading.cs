﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Trading {
        private Program Program;
        private Settings Settings => Program.Settings;
        private Database Database => Program.Database;
        private Library Library => Program.Library;
        private API.Alpaca Alpaca => Program.Alpaca;

        public Trading(Program p) {
            Program = p;
        }

        public enum OrderResult {
            Success,
            Fail,
            FailInsufficientFunds
        }

        public async Task<decimal?> GetAvailableCash(Data.Format format) {
            object result;

            decimal cash;
            result = await Alpaca.GetTradeableCash(format);
            if (result is decimal r) {
                cash = r;
            } else {
                return null;
            }

            List<Data.Order> orders;
            result = await Alpaca.GetOrders_OpenBuy(format);
            if (result is List<Data.Order> pml) {
                orders = pml;
            } else {
                return null;
            }

            decimal marked = 0m;

            List<Data.Asset> assets = (await Library.GetAssets())?.Where(a => orders.Any(o => o.Symbol == a.Symbol)).ToList();
            Dictionary<string, decimal?> prices = await Library.GetLastPrices(assets);

            for (int i = 0; i < orders.Count; i++) {
                Data.Asset asset = assets.Find(a => a.Symbol == orders[i].Symbol);

                if (asset == null || prices == null || !prices.ContainsKey(asset.ID))
                    return null;

                marked += orders[i].Quantity * prices[asset.ID] ?? 999999m;
            }

            return cash - marked;
        }

        public async Task RunAutomation(Data.Format format, DateTime day) {
            Prompt.WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            Prompt.WriteLine($">>> Running automated rules for {format} instructions\n");

            List<Data.Instruction> instructions = (await Database.GetInstructions())?.Where(i => i.Format == format).ToList();
            List<Data.Strategy> strategies = await Database.GetStrategies();

            List<Data.Asset> assets = await Library.GetAssets();
            List<Data.Position> positions;
            List<Data.Order> orders;

            object result;

            result = await Alpaca.GetPositions(format);
            if (result is List<Data.Position> pmldp) {
                positions = pmldp;
            } else {
                Prompt.WriteLine("Unable to retrieve current trade positions from Alpaca API. Aborting.\n");
                return;
            }

            result = await Alpaca.GetOrders_OpenBuy(format);
            if (result is List<Data.Order> pmldo) {
                orders = pmldo;
            } else {
                Prompt.WriteLine("Unable to retrieve current open orders from Alpaca API. Aborting.\n");
                return;
            }

            if (assets == null || assets.Count == 0) {
                Prompt.WriteLine("Unable to retrieve asset list from database.\n");
                return;
            }

            if (instructions == null || instructions.Count == 0) {
                Prompt.WriteLine("No automated instruction rules found in database. Aborting.\n");
                return;
            }

            if (strategies == null || strategies.Count == 0) {
                Prompt.WriteLine("No automation strategies found in database. Aborting.\n");
                return;
            }

            for (int i = 0; i < instructions.Count; i++) {
                Data.Strategy strategy = strategies.Find(s => s.Name == instructions[i].Strategy);
                Data.Asset asset = assets.Find(a => a.Symbol == instructions[i].Symbol);
                Data.Position position = positions.Find(p => p.Symbol == instructions[i].Symbol);
                Data.Order order = orders.Find(o => o.Symbol == instructions[i].Symbol && o.Quantity == instructions[i].Quantity);

                Prompt.WriteLine($"\n[{i + 1:0000} / {instructions.Count:0000}] {instructions[i].Name} ({instructions[i].Format}): "
                    + $"{instructions[i].Symbol} x {instructions[i].Quantity} @ {instructions[i].Strategy} ({instructions[i].Frequency})");

                if (strategy == null) {
                    Prompt.WriteLine($"Strategy '{instructions[i].Strategy}' not found in database. Aborting.\n");
                    continue;
                }

                if (asset == null) {
                    Prompt.WriteLine($"Asset '{instructions[i].Symbol}' not found in database. Aborting.\n");
                    continue;
                }

                if (instructions[i].Frequency == Data.Frequency.Daily) {
                    if (!instructions[i].Active) {
                        Prompt.WriteLine($"Instruction marked as 'Inactive'. Skipping.\n");
                    } else if (instructions[i].Active) {
                        await RunAutomation_Daily(format, instructions[i], strategy, day, asset, position, order);
                    }
                }
            }

            Prompt.WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            Prompt.WriteLine($">>> Completed running automated rules for {format} instructions\n");
        }

        public async Task RunAutomation_Daily(
                Data.Format format, Data.Instruction instruction, Data.Strategy strategy,
                DateTime day, Data.Asset asset, Data.Position position, Data.Order order,
                bool useMargin = false) {
            // Get last market close to ensure most up-to-date data exists
            // And that today's potential data exists (since SQL query interprets to query for today's prices!)
            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await Alpaca.GetTime_LastMarketClose();
            if (result is DateTime r) {
                lastMarketClose = r;
            }

            // Check data validity (last update time) to ensure it is most recent
            DateTime validity = await Database.GetValidity_Daily(asset);

            if (validity.CompareTo(lastMarketClose) < 0) {              // If data is invalid
                Prompt.WriteLine("Latest market data for this symbol needs updating. Updating now.");
                await Library.Update_TSD(new List<Data.Asset>() { asset });
                await Task.Delay(10000);                         // Allow Library update's database threads to get ahead
                validity = await Database.GetValidity_Daily(asset);
            }

            if (validity.CompareTo(lastMarketClose) > 0) {       // If validity is current, data is valid
                bool? toBuy = await Database.ScalarQuery(await Strategy.Interpret(strategy.Entry, instruction.Symbol, day));
                bool? toSellGain = await Database.ScalarQuery(await Strategy.Interpret(strategy.ExitGain, instruction.Symbol, day));
                bool? toSellLoss = await Database.ScalarQuery(await Strategy.Interpret(strategy.ExitStopLoss, instruction.Symbol, day));

                if (!toBuy.HasValue || !toSellGain.HasValue || !toSellLoss.HasValue) {
                    Prompt.WriteLine($"Error detected in SQL query. Please validate queries. Skipping.");
                    return;
                }

                if (toBuy.Value && (toSellGain.Value || toSellLoss.Value)) {   // Cannot simultaneously buy and sell ... erroneous queries?
                    Prompt.WriteLine("Buy AND Sell triggers met- doing nothing. Check strategy for errors?");
                    return;
                }

                if (toBuy.Value) {
                    // Warning: API buy/sell orders use negative quantity to indicate short positions
                    // And/or may just throw exceptions when attempting to sell to negative

                    if (position != null && position.Quantity > 0) {
                        Prompt.WriteLine("  Buy trigger detected; active position already exists; doing nothing.");
                    } else if (order != null) {
                        Prompt.WriteLine("  Buy trigger detected; identical open buy order already exists; doing nothing.");
                    } else if (order == null && (position == null || position.Quantity <= 0)) {
                        Prompt.WriteLine("  Buy trigger detected; no current position owned; placing Buy order.");

                        // If not using margin trading
                        // Ensure there is (as best as can be approximated) enough cash in account for transaction

                        decimal? availableCash = await GetAvailableCash(format);

                        if (!useMargin && availableCash == null) {
                            Prompt.WriteLine("    Error calculating available cash; instructed not to trade on margin; aborting.");
                            return;
                        }

                        decimal? lastPrice = (await Library.GetLastPrice(asset)).Close;
                        decimal? orderPrice = instruction.Quantity * lastPrice;

                        if (!useMargin && (lastPrice == null || orderPrice == null)) {
                            Prompt.WriteLine("    Error calculating estimated cost of buy order; unable to determine if margin trading needed; aborting.");
                            return;
                        }

                        if (!useMargin && (availableCash < orderPrice)) {
                            Prompt.WriteLine($"    Available cash ${availableCash:n0} insufficient for buy order ${orderPrice:n0}; aborting.");
                            return;
                        }

                        Prompt.WriteLine($"    Available cash ${availableCash:n0} sufficient for buy order ${orderPrice:n0}.");

                        switch (await Alpaca.PlaceOrder_BuyMarket(instruction.Format, instruction.Symbol, instruction.Quantity)) {
                            case OrderResult.Success:
                                Prompt.WriteLine(">> Order successfully placed.", ConsoleColor.Green);
                                break;

                            case OrderResult.Fail:
                                Prompt.WriteLine(">> Order placement unsuccessful.", ConsoleColor.Red);
                                break;

                            case OrderResult.FailInsufficientFunds:
                                Prompt.WriteLine(">> Order placement unsuccessful; Insufficient available funds.", ConsoleColor.Red);
                                break;
                        }
                    }
                } else if (toSellGain.Value || toSellLoss.Value) {
                    string msgSellQuery = toSellGain.Value && toSellLoss.Value ? "Both" : (toSellGain.Value ? "Gain" : (toSellLoss.Value ? "Stop-Loss" : ""));

                    if (position == null || position.Quantity <= 0) {
                        Prompt.WriteLine($"  Sell trigger detected; {msgSellQuery}; no current position owned; doing nothing.");
                    } else if (position != null && position.Quantity > 0) {
                        Prompt.WriteLine($"  Sell trigger detected; {msgSellQuery}; active position found; placing Sell order.");
                        // Sell position.quantity in case position.Quantity != instruction.Quantity
                        switch (await Alpaca.PlaceOrder_SellMarket(instruction.Format, instruction.Symbol, position.Quantity)) {
                            case OrderResult.Success:
                                Prompt.WriteLine(">> Order successfully placed.", ConsoleColor.Green);
                                break;

                            case OrderResult.Fail:
                                Prompt.WriteLine(">> Order placement unsuccessful.", ConsoleColor.Red);
                                break;
                        }
                    }
                } else {
                    Prompt.WriteLine("  No triggers detected.");
                }
            }
        }
    }
}