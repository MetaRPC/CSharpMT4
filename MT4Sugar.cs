using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mt4_term_api;

namespace MetaRPC.CSharpMT4
{
    /// <summary>
    /// High-level convenience helpers on top of <see cref="MT4Service"/> (mirrors the MT5Sugar layer, bounded to MT4).
    /// MT4 has no Market Depth/DOM, no OrderCheck/margin RPCs and uses orders (not positions), so those MT5-only
    /// sugar methods are intentionally absent. Methods are grouped into #regions by the canonical API areas.
    /// </summary>
    public static class MT4Sugar
    {
        #region DTOs
        /// <summary>A compact snapshot of the trading account.</summary>
        public sealed class AccountSnapshot
        {
            /// <summary>Account login number.</summary>
            public long Login;
            /// <summary>Account balance in deposit currency.</summary>
            public double Balance;
            /// <summary>Account equity (balance + floating P/L).</summary>
            public double Equity;
            /// <summary>Margin currently used.</summary>
            public double Margin;
            /// <summary>Free margin available for new trades.</summary>
            public double FreeMargin;
            /// <summary>Margin level, percent.</summary>
            public double MarginLevel;
            /// <summary>Deposit currency (e.g. "USD").</summary>
            public string Currency = "";
            /// <summary>Human-readable one-line summary.</summary>
            public override string ToString()
                => $"#{Login} Balance={Balance} Equity={Equity} Margin={Margin} Free={FreeMargin} Level={MarginLevel} {Currency}";
        }

        /// <summary>A compact snapshot of a symbol: top-of-book plus key trading parameters.</summary>
        public sealed class SymbolSnapshot
        {
            /// <summary>Symbol name.</summary>
            public string Symbol = "";
            /// <summary>Current bid price.</summary>
            public double Bid;
            /// <summary>Current ask price.</summary>
            public double Ask;
            /// <summary>Price digits.</summary>
            public int Digits;
            /// <summary>Point size (10^-digits).</summary>
            public double Point;
            /// <summary>Current spread in points.</summary>
            public double SpreadPoints;
            /// <summary>Minimum tradable volume (lots).</summary>
            public double VolumeMin;
            /// <summary>Maximum tradable volume (lots).</summary>
            public double VolumeMax;
            /// <summary>Volume step (lots).</summary>
            public double VolumeStep;
            /// <summary>Human-readable one-line summary.</summary>
            public override string ToString()
                => $"{Symbol} Bid={Bid} Ask={Ask} Digits={Digits} Point={Point} Spread={SpreadPoints}pt Vol[{VolumeMin}..{VolumeMax}/{VolumeStep}]";
        }
        #endregion

        #region Account_Information
        /// <summary>Builds an <see cref="AccountSnapshot"/> from a single AccountSummary round trip.</summary>
        /// <param name="svc">The service.</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A populated account snapshot.</returns>
        public static async Task<AccountSnapshot> GetAccountSnapshotAsync(this MT4Service svc, DateTime? deadline = null, CancellationToken ct = default)
        {
            var s = await svc.AccountSummaryAsync(deadline, ct);
            return new AccountSnapshot
            {
                Login = (long)s.AccountLogin,
                Balance = s.AccountBalance,
                Equity = s.AccountEquity,
                Margin = s.AccountMargin,
                FreeMargin = s.AccountMarginFree,
                MarginLevel = s.AccountMarginLevel,
                Currency = s.AccountCurrency,
            };
        }
        #endregion

        #region Symbol_Information
        /// <summary>Builds a <see cref="SymbolSnapshot"/> (quote + digits/point/spread/volume limits).</summary>
        /// <param name="svc">The service.</param>
        /// <param name="symbol">Symbol name (e.g. "EURUSD").</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A populated symbol snapshot.</returns>
        public static async Task<SymbolSnapshot> GetSymbolSnapshotAsync(this MT4Service svc, string symbol, DateTime? deadline = null, CancellationToken ct = default)
        {
            var q = await svc.QuoteAsync(symbol, deadline, ct);
            var digits = await svc.GetDigitsAsync(symbol, deadline, ct);
            var point = Math.Pow(10, -digits);
            return new SymbolSnapshot
            {
                Symbol = symbol,
                Bid = q.Bid,
                Ask = q.Ask,
                Digits = digits,
                Point = point,
                SpreadPoints = point > 0 ? (q.Ask - q.Bid) / point : 0,
                VolumeMin = await svc.GetVolumeMinAsync(symbol, deadline, ct),
                VolumeMax = await svc.GetVolumeMaxAsync(symbol, deadline, ct),
                VolumeStep = await svc.GetVolumeStepAsync(symbol, deadline, ct),
            };
        }

        /// <summary>Ensures the symbol is present in Market Watch (SymbolSelect with select=true).</summary>
        /// <param name="svc">The service.</param>
        /// <param name="symbol">Symbol name.</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task EnsureSelectedAsync(this MT4Service svc, string symbol, DateTime? deadline = null, CancellationToken ct = default)
            => await svc.SymbolSelectAsync(symbol, true, deadline, ct);

        /// <summary>Rounds a price to the symbol's digits.</summary>
        /// <param name="svc">The service.</param>
        /// <param name="symbol">Symbol name.</param>
        /// <param name="price">Raw price.</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The price rounded to the symbol's digits.</returns>
        public static async Task<double> NormalizePriceAsync(this MT4Service svc, string symbol, double price, DateTime? deadline = null, CancellationToken ct = default)
            => Math.Round(price, await svc.GetDigitsAsync(symbol, deadline, ct));

        /// <summary>Clamps and snaps a volume to the symbol's min/max/step.</summary>
        /// <param name="svc">The service.</param>
        /// <param name="symbol">Symbol name.</param>
        /// <param name="volume">Raw volume (lots).</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A broker-valid volume.</returns>
        public static async Task<double> NormalizeVolumeAsync(this MT4Service svc, string symbol, double volume, DateTime? deadline = null, CancellationToken ct = default)
        {
            var min = await svc.GetVolumeMinAsync(symbol, deadline, ct);
            var max = await svc.GetVolumeMaxAsync(symbol, deadline, ct);
            var step = await svc.GetVolumeStepAsync(symbol, deadline, ct);
            var v = step > 0 ? Math.Round(volume / step) * step : volume;
            return Math.Max(min, Math.Min(max > 0 ? max : volume, v));
        }

        /// <summary>Converts points to pips (on 3/5-digit symbols 1 pip = 10 points).</summary>
        /// <param name="svc">The service.</param>
        /// <param name="symbol">Symbol name.</param>
        /// <param name="points">Value in points.</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The value in pips.</returns>
        public static async Task<double> PointsToPipsAsync(this MT4Service svc, string symbol, double points, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.GetDigitsAsync(symbol, deadline, ct);
            return (d == 3 || d == 5) ? points / 10.0 : points;
        }

        /// <summary>A price at <paramref name="offsetPoints"/> from the current Ask (fromAsk=true) or Bid; offset may be negative.</summary>
        /// <param name="svc">The service.</param>
        /// <param name="symbol">Symbol name.</param>
        /// <param name="fromAsk">Use Ask as the base price when true, Bid when false.</param>
        /// <param name="offsetPoints">Offset in points (negative moves below the base).</param>
        /// <param name="deadline">Optional RPC deadline.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The normalized offset price.</returns>
        public static async Task<double> PriceFromOffsetPointsAsync(this MT4Service svc, string symbol, bool fromAsk, double offsetPoints, DateTime? deadline = null, CancellationToken ct = default)
        {
            var q = await svc.QuoteAsync(symbol, deadline, ct);
            var pt = await svc.GetPointAsync(symbol, deadline, ct);
            var basePrice = fromAsk ? q.Ask : q.Bid;
            return await svc.NormalizePriceAsync(symbol, basePrice + offsetPoints * pt, deadline, ct);
        }
        #endregion

        #region Positions_And_Orders (read)
        /// <summary>Order history for the last <paramref name="window"/> up to now.</summary>
        public static Task<OrdersHistoryData> OrdersHistoryLastAsync(this MT4Service svc, TimeSpan window, DateTime? deadline = null, CancellationToken ct = default)
            => svc.OrdersHistoryAsync(DateTime.UtcNow - window, DateTime.UtcNow, deadline: deadline, ct: ct);

        /// <summary>Number of open orders (optionally for one symbol).</summary>
        public static async Task<int> GetOrderCountAsync(this MT4Service svc, string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            var q = d.OrderInfos.AsEnumerable();
            if (!string.IsNullOrEmpty(symbol)) q = q.Where(o => o.Symbol == symbol);
            return q.Count();
        }

        /// <summary>Total floating profit of open orders (optionally for one symbol).</summary>
        public static async Task<double> GetTotalProfitLossAsync(this MT4Service svc, string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            var q = d.OrderInfos.AsEnumerable();
            if (!string.IsNullOrEmpty(symbol)) q = q.Where(o => o.Symbol == symbol);
            return q.Sum(o => o.Profit);
        }

        /// <summary>Total open volume in lots (optionally for one symbol).</summary>
        public static async Task<double> TotalLotsAsync(this MT4Service svc, string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            return d.OrderInfos.Where(o => string.IsNullOrEmpty(symbol) || o.Symbol == symbol).Sum(o => o.Lots);
        }

        /// <summary>Open orders for a symbol.</summary>
        public static async Task<List<OpenedOrderInfo>> GetOrdersBySymbolAsync(this MT4Service svc, string symbol, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            return d.OrderInfos.Where(o => o.Symbol == symbol).ToList();
        }

        /// <summary>Finds an open order by ticket, or null.</summary>
        public static async Task<OpenedOrderInfo?> GetOrderByTicketAsync(this MT4Service svc, long ticket, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            return d.OrderInfos.FirstOrDefault(o => o.Ticket == ticket);
        }

        /// <summary>Open orders currently in profit (optionally for one symbol).</summary>
        public static async Task<List<OpenedOrderInfo>> GetProfitableOrdersAsync(this MT4Service svc, string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            return d.OrderInfos.Where(o => o.Profit > 0 && (string.IsNullOrEmpty(symbol) || o.Symbol == symbol)).ToList();
        }

        /// <summary>Open orders currently at a loss (optionally for one symbol).</summary>
        public static async Task<List<OpenedOrderInfo>> GetLosingOrdersAsync(this MT4Service svc, string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            return d.OrderInfos.Where(o => o.Profit < 0 && (string.IsNullOrEmpty(symbol) || o.Symbol == symbol)).ToList();
        }

        /// <summary>The most recent closed order within <paramref name="window"/>, or null.</summary>
        public static async Task<HistoryOrderInfo?> GetLastClosedOrderAsync(this MT4Service svc, TimeSpan window, DateTime? deadline = null, CancellationToken ct = default)
        {
            var h = await svc.OrdersHistoryAsync(DateTime.UtcNow - window, DateTime.UtcNow, deadline: deadline, ct: ct);
            return h.OrdersInfo.FirstOrDefault();
        }
        #endregion

        #region Trading
        private static bool IsMarket(OpenedOrderType t) => t == OpenedOrderType.OoOpBuy || t == OpenedOrderType.OoOpSell;
        private static bool IsPending(OpenedOrderType t) => t is OpenedOrderType.OoOpBuylimit or OpenedOrderType.OoOpBuystop or OpenedOrderType.OoOpSelllimit or OpenedOrderType.OoOpSellstop;

        private static async Task<OrderSendData> SendMarketAsync(MT4Service svc, string symbol, OrderSendOperationType op, double volume, double? sl, double? tp, int slippage, int magic, string comment, DateTime? deadline, CancellationToken ct)
        {
            var q = await svc.QuoteAsync(symbol, deadline, ct);
            var vol = await svc.NormalizeVolumeAsync(symbol, volume, deadline, ct);
            var req = new OrderSendRequest
            {
                Symbol = symbol,
                OperationType = op,
                Volume = vol,
                Slippage = slippage,
                Comment = comment,
                MagicNumber = magic,
            };
            req.Price = op == OrderSendOperationType.OcOpBuy ? q.Ask : q.Bid;
            if (sl.HasValue) req.Stoploss = sl.Value;
            if (tp.HasValue) req.Takeprofit = tp.Value;
            return await svc.OrderSendAsync(req, deadline, ct);
        }

        /// <summary>Sends a market BUY (volume auto-normalized, price = current Ask). SL/TP are absolute prices.</summary>
        public static async Task<OrderSendData> BuyMarketAsync(this MT4Service svc, string symbol, double volume, double? sl = null, double? tp = null, int slippage = 5, int magic = 0, string comment = "MT4Sugar", DateTime? deadline = null, CancellationToken ct = default)
            => await SendMarketAsync(svc, symbol, OrderSendOperationType.OcOpBuy, volume, sl, tp, slippage, magic, comment, deadline, ct);

        /// <summary>Sends a market SELL (volume auto-normalized, price = current Bid). SL/TP are absolute prices.</summary>
        public static async Task<OrderSendData> SellMarketAsync(this MT4Service svc, string symbol, double volume, double? sl = null, double? tp = null, int slippage = 5, int magic = 0, string comment = "MT4Sugar", DateTime? deadline = null, CancellationToken ct = default)
            => await SendMarketAsync(svc, symbol, OrderSendOperationType.OcOpSell, volume, sl, tp, slippage, magic, comment, deadline, ct);

        /// <summary>Market BUY with SL/TP expressed in points from the current Ask.</summary>
        public static async Task<OrderSendData> BuyMarketPointsAsync(this MT4Service svc, string symbol, double volume, double? slPoints = null, double? tpPoints = null, int slippage = 5, int magic = 0, string comment = "MT4Sugar", DateTime? deadline = null, CancellationToken ct = default)
        {
            var q = await svc.QuoteAsync(symbol, deadline, ct);
            var pt = await svc.GetPointAsync(symbol, deadline, ct);
            double? sl = slPoints.HasValue ? q.Ask - slPoints.Value * pt : (double?)null;
            double? tp = tpPoints.HasValue ? q.Ask + tpPoints.Value * pt : (double?)null;
            return await svc.BuyMarketAsync(symbol, volume, sl, tp, slippage, magic, comment, deadline, ct);
        }

        /// <summary>Market SELL with SL/TP expressed in points from the current Bid.</summary>
        public static async Task<OrderSendData> SellMarketPointsAsync(this MT4Service svc, string symbol, double volume, double? slPoints = null, double? tpPoints = null, int slippage = 5, int magic = 0, string comment = "MT4Sugar", DateTime? deadline = null, CancellationToken ct = default)
        {
            var q = await svc.QuoteAsync(symbol, deadline, ct);
            var pt = await svc.GetPointAsync(symbol, deadline, ct);
            double? sl = slPoints.HasValue ? q.Bid + slPoints.Value * pt : (double?)null;
            double? tp = tpPoints.HasValue ? q.Bid - tpPoints.Value * pt : (double?)null;
            return await svc.SellMarketAsync(symbol, volume, sl, tp, slippage, magic, comment, deadline, ct);
        }

        /// <summary>Modifies an order's SL/TP (absolute prices; null leaves the field unchanged).</summary>
        public static Task<OrderModifyData> ModifySlTpAsync(this MT4Service svc, int ticket, double? sl = null, double? tp = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var req = new OrderModifyRequest { OrderTicket = ticket };
            if (sl.HasValue) req.NewStopLoss = sl.Value;
            if (tp.HasValue) req.NewTakeProfit = tp.Value;
            return svc.OrderModifyAsync(req, deadline, ct);
        }

        /// <summary>Closes a market order / deletes a pending order by ticket.</summary>
        public static Task<OrderCloseDeleteData> CloseByTicketAsync(this MT4Service svc, int ticket, DateTime? deadline = null, CancellationToken ct = default)
            => svc.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = ticket }, deadline, ct);

        /// <summary>Closes/deletes ALL open orders (optionally one symbol). Returns how many were closed.</summary>
        public static async Task<int> CloseAllAsync(this MT4Service svc, string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            var items = d.OrderInfos.Where(o => string.IsNullOrEmpty(symbol) || o.Symbol == symbol).ToList();
            int n = 0;
            foreach (var o in items) { try { await svc.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = (int)o.Ticket }, deadline, ct); n++; } catch { } }
            return n;
        }

        /// <summary>Closes only market (buy/sell) orders. Returns how many were closed.</summary>
        public static async Task<int> CloseAllMarketAsync(this MT4Service svc, string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            var items = d.OrderInfos.Where(o => IsMarket(o.OrderType) && (string.IsNullOrEmpty(symbol) || o.Symbol == symbol)).ToList();
            int n = 0;
            foreach (var o in items) { try { await svc.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = (int)o.Ticket }, deadline, ct); n++; } catch { } }
            return n;
        }

        /// <summary>Deletes only pending (limit/stop) orders. Returns how many were deleted.</summary>
        public static async Task<int> CancelAllPendingAsync(this MT4Service svc, string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
        {
            var d = await svc.OpenedOrdersAsync(deadline: deadline, ct: ct);
            var items = d.OrderInfos.Where(o => IsPending(o.OrderType) && (string.IsNullOrEmpty(symbol) || o.Symbol == symbol)).ToList();
            int n = 0;
            foreach (var o in items) { try { await svc.OrderCloseDeleteAsync(new OrderCloseDeleteRequest { OrderTicket = (int)o.Ticket }, deadline, ct); n++; } catch { } }
            return n;
        }

        /// <summary>
        /// Volume for a fixed money risk over a stop distance (points), best-effort from TickValueWithSize.
        /// valuePerPoint ~= TradeTickValue (per lot); volume = riskMoney / (stopPoints * valuePerPoint), then normalized.
        /// </summary>
        public static async Task<double> CalcVolumeForRiskAsync(this MT4Service svc, string symbol, double stopPoints, double riskMoney, DateTime? deadline = null, CancellationToken ct = default)
        {
            if (stopPoints <= 0 || riskMoney <= 0) return await svc.GetVolumeMinAsync(symbol, deadline, ct);
            var tv = await svc.TickValueWithSizeAsync(new[] { symbol }, deadline, ct);
            var info = tv.Infos.FirstOrDefault(i => i.SymbolName == symbol) ?? tv.Infos.FirstOrDefault();
            double valuePerPoint = info?.TradeTickValue ?? 0;
            if (valuePerPoint <= 0) return await svc.GetVolumeMinAsync(symbol, deadline, ct);
            var raw = riskMoney / (stopPoints * valuePerPoint);
            return await svc.NormalizeVolumeAsync(symbol, raw, deadline, ct);
        }

        /// <summary>Market BUY sized to risk <paramref name="riskMoney"/> over <paramref name="stopPoints"/>; SL placed that far below Ask.</summary>
        public static async Task<OrderSendData> BuyMarketByRiskAsync(this MT4Service svc, string symbol, double stopPoints, double riskMoney, int slippage = 5, int magic = 0, string comment = "MT4Sugar", DateTime? deadline = null, CancellationToken ct = default)
        {
            var vol = await svc.CalcVolumeForRiskAsync(symbol, stopPoints, riskMoney, deadline, ct);
            var q = await svc.QuoteAsync(symbol, deadline, ct);
            var point = await svc.GetPointAsync(symbol, deadline, ct);
            double sl = await svc.NormalizePriceAsync(symbol, q.Ask - stopPoints * point, deadline, ct);
            return await svc.BuyMarketAsync(symbol, vol, sl, null, slippage, magic, comment, deadline, ct);
        }

        /// <summary>Market SELL sized to risk <paramref name="riskMoney"/> over <paramref name="stopPoints"/>; SL placed that far above Bid.</summary>
        public static async Task<OrderSendData> SellMarketByRiskAsync(this MT4Service svc, string symbol, double stopPoints, double riskMoney, int slippage = 5, int magic = 0, string comment = "MT4Sugar", DateTime? deadline = null, CancellationToken ct = default)
        {
            var vol = await svc.CalcVolumeForRiskAsync(symbol, stopPoints, riskMoney, deadline, ct);
            var q = await svc.QuoteAsync(symbol, deadline, ct);
            var point = await svc.GetPointAsync(symbol, deadline, ct);
            double sl = await svc.NormalizePriceAsync(symbol, q.Bid + stopPoints * point, deadline, ct);
            return await svc.SellMarketAsync(symbol, vol, sl, null, slippage, magic, comment, deadline, ct);
        }
        #endregion

        #region Subscriptions
        /// <summary>Live tick stream for a single symbol.</summary>
        public static IAsyncEnumerable<OnSymbolTickData> ReadTicks(this MT4Service svc, string symbol, CancellationToken ct = default)
            => svc.OnSymbolTickAsync(new[] { symbol }, ct);

        /// <summary>Live trade-event stream (order create/modify/close).</summary>
        public static IAsyncEnumerable<OnTradeData> ReadTrades(this MT4Service svc, CancellationToken ct = default)
            => svc.OnTradeAsync(ct);
        #endregion
    }
}
