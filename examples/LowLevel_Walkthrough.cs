// File: examples/LowLevel_Walkthrough.cs
// Goal: sequentially exercise low-level MT4Account methods (read-only first, short streaming demos, optional trading).
// Safety: trading is OFF by default (ENABLE_TRADING=false). Turn it on only on demo/sandbox.
// Inputs: expects a connected MetaRPC.CSharpMT4.MT4Account instance (constructed/connected outside, see Program.cs).

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MetaRPC.CSharpMT4; 
using mt4_term_api;        

namespace MetaRPC.CSharpMT4.Examples
{
    public static class MT4Account_RunAllLowLevel
    {
// ---------===== Streaming sampling settings (configurable via ENV) =====---------
       
// STREAM_SAMPLE_TIME — how long to listen to a stream before stopping.
//   • Reads seconds from env var STREAM_SAMPLE_SECONDS (default: 6 seconds).
//   • Example: set STREAM_SAMPLE_SECONDS=15 to listen for 15 seconds.
//
// STREAM_SAMPLE_ITEMS — maximum number of events to print per stream.
//   • Reads integer from env var STREAM_SAMPLE_ITEMS (default: 10 items).
//   • Example: set STREAM_SAMPLE_ITEMS=25 to print up to 25 items.
//
// Why: keeps demo output short and prevents endless streaming in examples.
// You can tune these per run without changing code.

private static readonly TimeSpan STREAM_SAMPLE_TIME =
    TimeSpan.FromSeconds(ParseDoubleEnv("STREAM_SAMPLE_SECONDS", 6.0));

private static readonly int STREAM_SAMPLE_ITEMS =
    ParseIntEnv("STREAM_SAMPLE_ITEMS", 10);

// Optional: environment override examples
// PowerShell:
//   $env:STREAM_SAMPLE_SECONDS = "15"
//   $env:STREAM_SAMPLE_ITEMS  = "25"
// Bash/cmd (Linux/macOS or Windows cmd):
//   export STREAM_SAMPLE_SECONDS=15
//   export STREAM_SAMPLE_ITEMS=25


        public static async Task<int> RunAllAsync(MT4Account acc, CancellationToken ct)
        {

            // -----------------------------------------------
            // 0) ENV / runtime settings
            // -----------------------------------------------

// ENV: runtime knobs for the demo
// BASE_CHART_SYMBOL  - main symbol (default: EURUSD)
// SYMBOLS            - comma-separated list for bulk calls
// ENABLE_TRADING     - safety switch (default: false; trading block is skipped)
// TRADE_LOT          - lot size for trading demo when ENABLE_TRADING=true
// Values are printed below so you can verify what was actually picked up.

            Section("ENV");
            var baseSymbol = Env("BASE_CHART_SYMBOL", "EURUSD");
            var symbolsCsv = Env("SYMBOLS", "EURUSD,GBPUSD,USDJPY");
            var symbols = symbolsCsv.Split(',')
                                    .Select(s => s.Trim())
                                    .Where(s => s.Length > 0)
                                    .Distinct()
                                    .ToArray();
            var enableTrading = EnvBool("ENABLE_TRADING", false);
            var lotStr = Env("TRADE_LOT", "0.01");
            var lot = double.TryParse(lotStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var l) ? l : 0.01;

            Console.WriteLine($"BASE_CHART_SYMBOL = {baseSymbol}");
            Console.WriteLine($"SYMBOLS           = {string.Join(", ", symbols)}");
            Console.WriteLine($"ENABLE_TRADING    = {enableTrading}");
            Console.WriteLine($"TRADE_LOT         = {lot.ToString("G", CultureInfo.InvariantCulture)}");
            Console.WriteLine();

            // -----------------------------------------------
            // 1) AccountSummary
            // -----------------------------------------------

// ACCOUNT SUMMARY (unary RPC)
// - Fetches a snapshot of the trading account (balance, equity, currency, leverage, trade mode, etc).
// - deadline: null → use channel defaults; cancellation is controlled by the provided token.
// - Safe to call early to verify readiness of the terminal/session.
// - If this succeeds but history/streams fail, the issue is usually on the MT4/EA side (chart/DLL/autotrading).

            Section("AccountSummary");
            try
            {
                var summary = await acc.AccountSummaryAsync(deadline: null, cancellationToken: ct);
                Dump(summary, "AccountSummary");
            }
            catch (Exception ex) { Warn("AccountSummaryAsync", ex); }

            // -----------------------------------------------
            // 2) Symbols catalog / detailed symbol parameters
            // -----------------------------------------------

// SYMBOL CATALOG + PARAMS
// 1) SymbolsAsync(): returns the list of available instrument names (e.g., EURUSD.m).
//    Use this to discover exact broker-specific names/suffixes.
// 2) SymbolParamsManyAsync(symbolName: null): returns metadata (lot step, min/max lot, digits, etc.).
//    Pass symbolName (e.g., "EURUSD") to filter and reduce payload.
// Notes:
// - We print only the head of collections (DumpHead) to avoid flooding the console.
// - deadline=null → rely on channel defaults; cancellation controlled by ct.
// - If you see empty results for a filter, the symbol name likely differs (check SymbolsAsync first).

            Section("Symbols / SymbolParamsMany");
            try
            {
                var syms = await acc.SymbolsAsync(deadline: null, cancellationToken: ct);
                DumpHead(syms, "Symbols (head)", 20);

                var spm = await acc.SymbolParamsManyAsync(symbolName: null, deadline: null, cancellationToken: ct);
                DumpHead(spm, "SymbolParamsMany (head)", 20);
            }
            catch (Exception ex) { Warn("Symbols*/SymbolParamsMany*", ex); }

            // -----------------------------------------------
            // 3) Quotes R/W
            // -----------------------------------------------

// QUOTES (top-of-book snapshot)
// - QuoteAsync(baseSymbol): single instrument (Bid/Ask/High/Low/DateTime).
// - QuoteManyAsync(symbols): batch pricing for multiple instruments in one RPC.
// Notes:
// * Ensure broker-specific symbol names (EURUSD vs EURUSD.m).
// * deadline=null → use channel default timeouts; cancel via ct.
// * Prefer QuoteMany for efficiency when you need multiple symbols.

            Section("Quotes");
            try
            {
                var q1 = await acc.QuoteAsync(symbol: baseSymbol, deadline: null, cancellationToken: ct);
                Dump(q1, $"Quote({baseSymbol})");

                var qMany = await acc.QuoteManyAsync(symbols: symbols, deadline: null, cancellationToken: ct);
                DumpHead(qMany, "QuoteMany (head)", 20);
            }
            catch (Exception ex) { Warn("Quote*/QuoteMany*", ex); }

            // -----------------------------------------------
            // 4) Quote history R/O
            // -----------------------------------------------

// QUOTE HISTORY (bars/aggregates)
// - Fetches OHLC bars for [symbol, timeframe] within [from..to] (UTC).
// - Timeframe enum: ENUM_QUOTE_HISTORY_TIMEFRAME → C# PascalCase (e.g., QhPeriodM5, QhPeriodH1).
// - If you see MqlExecutionError/InternalChartOpen, the MT4 side likely blocked
//   the internal chart open (enable AutoTrading/DLL, ensure symbol is available).
// - Use smaller ranges or set a per-call deadline if the server is slow.
//   Example: var dl = DateTime.UtcNow.AddSeconds(10); ... QuoteHistoryAsync(..., deadline: dl, ...);

            Section("QuoteHistory");
            try
            {
                var to = DateTime.UtcNow;
                var from = to.AddHours(-4);
                // Proto: ENUM_QUOTE_HISTORY_TIMEFRAME { QH_PERIOD_M1=0, QH_PERIOD_M5=1, ... }
                // C#: PascalCase enum values => QhPeriodM5, QhPeriodH1, ...
                var timeframe = ENUM_QUOTE_HISTORY_TIMEFRAME.QhPeriodM5;

                var hist = await acc.QuoteHistoryAsync(
                    symbol: baseSymbol,
                    timeframe: timeframe,
                    from: from,
                    to: to,
                    deadline: null,
                    cancellationToken: ct);
                DumpHead(hist, $"QuoteHistory({baseSymbol}, M5) (head)", 50);
            }
            catch (Exception ex) { Warn("QuoteHistoryAsync", ex); }

            // -----------------------------------------------
            // 5) TickValue with lot sizes
            // -----------------------------------------------

// TICK VALUE vs LOT SIZE (risk helper)
// - Returns per-symbol tick value info computed by the terminal (currency-normalized).
// - Input: symbolNames[]; server provides tick value based on broker settings (contract size, digits).
// - Use this to size SL/TP and calculate $ per point/pip for a given lot.
// - Notes: ensure broker-specific symbol names (EURUSD vs EURUSD.m). deadline=null; cancel via ct.
// - Prefer passing a small symbol set to reduce payload.

            Section("TickValueWithSize");
            try
            {
                var tv = await acc.TickValueWithSizeAsync(symbolNames: symbols, deadline: null, cancellationToken: ct);
                DumpHead(tv, "TickValueWithSize (head)", 20);
            }
            catch (Exception ex) { Warn("TickValueWithSizeAsync", ex); }

            // -----------------------------------------------
            // 6) Opened orders / their tickets (lists)
            // -----------------------------------------------

// OPENED ORDERS + TICKETS
// - OpenedOrdersAsync(sortType): full details of all open positions & pending orders.
// - OpenedOrdersTicketsAsync(): lightweight list of order tickets (IDs) only.
// Notes:
// * Investor (read-only) login can read these; trading still disabled.
// * Empty result simply means nothing is currently open.
// * EnumOpenedOrderSortType values are PascalCase (e.g., SortByOpenTimeAsc).

            Section("OpenedOrders / OpenedOrdersTickets");
            try
            {
                var opened = await acc.OpenedOrdersAsync(
                    sortType: EnumOpenedOrderSortType.SortByOpenTimeAsc,
                    deadline: null,
                    cancellationToken: ct);
                DumpHead(opened, "OpenedOrders (head)", 20);

                var tickets = await acc.OpenedOrdersTicketsAsync(deadline: null, cancellationToken: ct);
                DumpHead(tickets, "OpenedOrdersTickets (head)", 20);
            }
            catch (Exception ex) { Warn("OpenedOrders*/OpenedOrdersTickets*", ex); }

            // -----------------------------------------------
            // 7) Orders history (time-bounded, no paging)
            // -----------------------------------------------

// ORDERS HISTORY (closed/cancelled orders)
// - Fetches history within [from..to], sorted by close time DESC.
// - Investor (read-only) login is fine for reading.
// - If empty: likely no activity in the window or timezone mismatch.
//   Consider aligning [from..to] to MT4 server time via UtcServerTimeShiftMinutes.
// - For large ranges use paging: set itemsPerPage > 0 and increment page.
// - deadline=null → use channel defaults; cancellation via ct.

            Section("OrdersHistory");
            try
            {
                var from7 = DateTime.UtcNow.AddDays(-7);
                var oh = await acc.OrdersHistoryAsync(
                    sortType: EnumOrderHistorySortType.HistorySortByCloseTimeDesc,
                    from: from7,
                    to: DateTime.UtcNow,
                    page: null,
                    itemsPerPage: null,
                    deadline: null,
                    cancellationToken: ct);
                DumpHead(oh, "OrdersHistory (head)", 20);
            }
            catch (Exception ex) { Warn("OrdersHistoryAsync", ex); }

            // -----------------------------------------------
            // 8) Streaming demo sections
            // -----------------------------------------------

// STREAM: OnSymbolTick
// - Live ticks for the provided symbols[].
// - Empty stream = market closed / wrong symbol names / MT4 blocked internal chart (enable AutoTrading/DLL).
// - Cancellation via ct; SampleStream limits by time/items.

            Section("Streaming: OnSymbolTick");
            try
            {
                var tickStream = acc.OnSymbolTickAsync(symbols: symbols, ct: ct);
                await SampleStream(tickStream, "OnSymbolTick", STREAM_SAMPLE_ITEMS, STREAM_SAMPLE_TIME, ct);
            }
            catch (Exception ex) { Warn("OnSymbolTickAsync", ex); }

// STREAM: OnTrade
// - Emits order/position events (created/modified/closed).
// - Silence is normal if no trades happen during the sample window.
// - Investor login can still observe events; no trading is performed here.

            Section("Streaming: OnTrade");
            try
            {
                var tradeStream = acc.OnTradeAsync(ct: ct);
                await SampleStream(tradeStream, "OnTrade", STREAM_SAMPLE_ITEMS, STREAM_SAMPLE_TIME, ct);
            }
            catch (Exception ex) { Warn("OnTradeAsync", ex); }

// STREAM: OnOpenedOrdersProfit / OnOpenedOrdersTickets
// - Poll-like streams every intervalMs for open orders profit and tickets snapshot.
// - Will be empty if no open orders exist.
// - If you see InternalChartOpen errors, MT4 side blocks internal chart ops (enable AutoTrading/DLL).

            Section("Streaming: OnOpenedOrdersProfit / OnOpenedOrdersTickets");
            try
            {
                var profitStream = acc.OnOpenedOrdersProfitAsync(intervalMs: 1000, ct: ct);
                await SampleStream(profitStream, "OnOpenedOrdersProfit", STREAM_SAMPLE_ITEMS, STREAM_SAMPLE_TIME, ct);

                var ticketsStream = acc.OnOpenedOrdersTicketsAsync(intervalMs: 1000, ct: ct);
                await SampleStream(ticketsStream, "OnOpenedOrdersTickets", STREAM_SAMPLE_ITEMS, STREAM_SAMPLE_TIME, ct);
            }
            catch (Exception ex) { Warn("OnOpenedOrders*Async", ex); }

            // -----------------------------------------------------
            // 9) TRADING DEMO (guarded by ENABLE_TRADING=false)
            // -----------------------------------------------------

// Flow:
// 1) Read a fresh quote (Ask/Bid) → pick price.
// 2) Compute naive SL/TP (pure demo; do NOT use in production).
// 3) Send market BUY (OrderSend) with optional price/SL/TP.
// 4) If ticket received → OrderModify (tweak SL/TP) → OrderCloseDelete.
//
// Requirements on MT4 side:
// - Real (not investor) trading password.
// - AutoTrading ON, "Allow automated trading", "Allow DLL imports" enabled.
// - Symbol must be tradeable for this account.
//
// Notes:
// - Slippage is in points (MT4). Tune it per broker.
// - Always round lot to broker step and respect min/max lot.
// - Respect stop level & freeze level; otherwise expect InvalidStops or Requote.
// - If Quote returns zeros → skip sending.

            if (enableTrading)
{
    Section("TRADING (guarded)");
    try
    {
        // 1) Fresh quote → choose price (prefer Ask for BUY)
        var q    = await acc.QuoteAsync(symbol: baseSymbol, deadline: null, cancellationToken: ct);
        var ask  = GetPropDouble(q, "Ask") ?? 0.0;
        var bid  = GetPropDouble(q, "Bid") ?? 0.0;
        var price = ask > 0 ? ask : (bid > 0 ? bid : 0.0);
        if (price <= 0) { Console.WriteLine("No valid price; skip trading."); goto TRADING_DONE; }

        // 2) Pull symbol params (digits/lot limits/stop levels)
var spMany = await acc.SymbolParamsManyAsync(symbolName: baseSymbol, deadline: null, cancellationToken: ct);

// Extract first item from possible wrapper/enumerable (no impossible pattern-matching)
object? sp = null;

// 1) Try to treat as IEnumerable via object (always compiles)
System.Collections.IEnumerable? en = null;
object spManyObj = spMany!;
en = spManyObj as System.Collections.IEnumerable;

// 2) If it's a protobuf "wrapper", extract the first collection-like property
if (en == null)
{
    var prop = spMany.GetType().GetProperties()
        .FirstOrDefault(p => typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType)
                             && p.PropertyType != typeof(string));
    en = prop?.GetValue(spMany) as System.Collections.IEnumerable;
}

// 3) We take the first element as an object
if (en != null)
{
    sp = System.Linq.Enumerable.Cast<object?>(en).FirstOrDefault();
}
else
{
    Console.WriteLine("[WARN] SymbolParamsMany: collection not found inside wrapper.");
}

// Now we can safely read the required fields from 'sp'
var digits   = GetPropDouble(sp, "Digits") ?? 5.0;
var pointSz  = Math.Pow(10.0, -Math.Max(0.0, digits));
var minLot   = GetPropDouble(sp, "MinLot")  ?? 0.01;
var maxLot   = GetPropDouble(sp, "MaxLot")  ?? 100.0;
var lotStep  = GetPropDouble(sp, "LotStep") ?? 0.01;
// The stop-level field name in protos varies — try several variants:
var stopLvlP = GetPropDouble(sp, "StopsLevelPoints")
            ?? GetPropDouble(sp, "StopLevelPoints")
            ?? GetPropDouble(sp, "StopsLevel")
            ?? 0.0;
   
        // 3) Normalize lot to broker constraints
        double normLot = Math.Round((lot / lotStep)) * lotStep;
        normLot = Math.Max(minLot, Math.Min(maxLot, normLot));

        // 4) Demo SL/TP around price, but respect stop level
        var basePoint = pointSz > 0 ? pointSz : (GuessPointSize(baseSymbol) ?? 0.0001);
        var sl = price - 100 * basePoint;
        var tp = price + 100 * basePoint;

        // Enforce minimal distances from price (stop level + small safety margin)
        var minDistPts = stopLvlP + 5;                 // +5 points of safety
        var minDist    = minDistPts * basePoint;
        if ((price - sl) < minDist) sl = price - minDist;
        if ((tp - price) < minDist) tp = price + minDist;

        // 5) Send market BUY (with per-call deadline)
        var dl = DateTime.UtcNow.AddSeconds(10);

        var sendReq = new OrderSendRequest
        {
            Symbol        = baseSymbol,
            OperationType = OrderSendOperationType.OcOpBuy,
            Volume        = normLot,
            Slippage      = 5,             // points; tune per broker
            Comment       = "RunAllLowLevel BUY",
            MagicNumber   = 777
        };
        sendReq.Price      = price;
        sendReq.Stoploss   = sl;
        sendReq.Takeprofit = tp;

        var sendRes = await acc.OrderSendAsync(sendReq, deadline: dl, cancellationToken: ct);
        Dump(sendRes, "OrderSend BUY (result)");

        var ticket = GetPropLong(sendRes, "Ticket");
        if (ticket.HasValue)
        {
            // 6) Slight SL/TP tweak via OrderModify
            var modDl  = DateTime.UtcNow.AddSeconds(10);
            var modReq = new OrderModifyRequest { OrderTicket = (int)ticket.Value };

            var tweak = 50 * basePoint; // move by 50 points
            var newSl = sl - tweak; if ((price - newSl) < minDist) newSl = price - minDist;
            var newTp = tp + tweak; if ((newTp - price) < minDist) newTp = price + minDist;

            modReq.NewStopLoss   = newSl;
            modReq.NewTakeProfit = newTp;

            var modRes = await acc.OrderModifyAsync(modReq, deadline: modDl, cancellationToken: ct);
            Dump(modRes, "OrderModify (result)");

            // 7) Close (market) / delete (pending) by broker logic
            var closeDl  = DateTime.UtcNow.AddSeconds(10);
            var closeReq = new OrderCloseDeleteRequest { OrderTicket = (int)ticket.Value };
            var closeRes = await acc.OrderCloseDeleteAsync(closeReq, deadline: closeDl, cancellationToken: ct);
            Dump(closeRes, "OrderCloseDelete (result)");
        }
        else
        {
            Console.WriteLine("No ticket returned after OrderSend; skipping modify/close.");
        }

        TRADING_DONE: ;
    }
    catch (Exception ex) { Warn("TRADING*", ex); }
}

Section("DONE");
return 0;

        }

        // -----------------------------------------------
        // Helpers: logging / ENV / dump
        // -----------------------------------------------

// Prints a pretty section header to the console:
// (blank line, then "-------- <title> --------") for readable logs.
        private static void Section(string title)
        {
            Console.WriteLine();
            Console.WriteLine(new string('-', 8) + " " + title + " " + new string('-', 8));
        }


// Logs a non-fatal warning in yellow:
// [WARN] <label>: <ExceptionType>: <Message>
// Saves/restores the console color so subsequent output isn't tinted.
        private static void Warn(string label, Exception ex)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {label}: {ex.GetType().Name}: {ex.Message}");
            Console.ForegroundColor = c;
        }


// ENV getter: returns the environment variable by key,
// or 'fallback' if it's not defined (null). Note: empty string is returned as-is.
        private static string Env(string key, string fallback = "") =>
            Environment.GetEnvironmentVariable(key) ?? fallback;


// Reads a boolean from ENV.
// true if value is "1", "true", or "yes" (case-insensitive); otherwise false.
// If var is missing, uses 'fallback' (default: false).
        private static bool EnvBool(string key, bool fallback = false)
        {
            var v = Env(key, fallback ? "true" : "false");
            return v.Equals("1") || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }


// Reads a double from ENV using invariant culture (decimal point '.'),
// returns 'fallback' if missing or unparsable.
        private static double ParseDoubleEnv(string key, double fallback)
        {
            var v = Env(key, "");
            return double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : fallback;
        }


// Reads an int from ENV; returns 'fallback' if missing or unparsable.
// Uses invariant culture. "10" → 10, "10.5" → fallback, ""/null → fallback.
        private static int ParseIntEnv(string key, int fallback)
{
    var v = (Env(key, "") ?? "").Trim();
    return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
        ? i
        : fallback;
}


// Pretty-prints an object to the console.
// - [label]
// - null → "<null>"
// - string → printed as-is
// - IEnumerable (not IDictionary) → prints up to 20 items via ToLine()
// - otherwise: prints all public properties as "Name: Value" (Value via ToLine())
// Notes:
// * maxDepth param is currently unused.
// * Dictionaries are not expanded.
// * Swallows property getter exceptions to avoid breaking logs.
        private static void Dump(object? obj, string label, int maxDepth = 2)
        {
            Console.WriteLine($"[{label}]");
            if (obj is null) { Console.WriteLine("  <null>"); return; }
            if (obj is string s) { Console.WriteLine("  " + s); return; }

            if (obj is IEnumerable enumerable && obj is not IDictionary)
            {
                int i = 0;
                foreach (var item in enumerable)
                {
                    if (i++ >= 20) { Console.WriteLine("  ..."); break; }
                    Console.WriteLine($"  - {ToLine(item)}");
                }
                return;
            }

            var t = obj.GetType();
            var props = t.GetProperties();
            if (props.Length == 0) { Console.WriteLine("  " + obj); return; }
            foreach (var p in props)
            {
                object? val = null;
                try { val = p.GetValue(obj); } catch { /* ignore */ }
                Console.WriteLine($"  {p.Name}: {ToLine(val)}");
            }
        }


// Prints only the head of a collection:
// - [label], then up to 'maxItems' elements via ToLine(...), followed by "..." if truncated.
// - Handles both plain IEnumerable and protobuf wrapper objects
//   (picks the first IEnumerable-like property inside the wrapper).
// - Null → "<null>", non-collection → "<not an IEnumerable>".
// Notes:
// * Skips IDictionary expansion.
// * Be mindful of lazy enumerables (iteration may trigger work).
        private static void DumpHead(object? collection, string label, int maxItems = 10)
        {
            Console.WriteLine($"[{label}]");
            if (collection is null) { Console.WriteLine("  <null>"); return; }

            static bool IsSeq(object x) => x is IEnumerable && x is not string && x is not IDictionary;

            if (IsSeq(collection))
            {
                int i = 0;
                foreach (var item in (IEnumerable)collection)
                {
                    if (i++ >= maxItems) { Console.WriteLine("  ..."); break; }
                    Console.WriteLine($"  - {ToLine(item)}");
                }
                return;
            }

            // Protobuf wrappers often contain the actual items inside a sub-collection property.
            var t = collection.GetType();
            var prop = t.GetProperties()
                        .FirstOrDefault(p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                                           && p.PropertyType != typeof(string));
            if (prop?.GetValue(collection) is IEnumerable en)
            {
                int i = 0;
                foreach (var item in en)
                {
                    if (i++ >= maxItems) { Console.WriteLine("  ..."); break; }
                    Console.WriteLine($"  - {ToLine(item)}");
                }
                return;
            }

            Console.WriteLine("  <not an IEnumerable>");
        }


// ToLine: single-line, human-friendly formatting for logs.
// - null → "<null>"
// - string → as-is
// - IEnumerable (not IDictionary) → first 5 items via ToLine, then "..." if truncated
// - primitives → ToString()
// - decimal/double/float → "G" with InvariantCulture
// - DateTime → "u" format
// - otherwise: prefer one of {SymbolName, Symbol, Name, Ticket, Id} and print
//   TypeName{Prop=Value}; fallback to TypeName if none found.
        private static string ToLine(object? obj)
        {
            if (obj is null) return "<null>";
            if (obj is string s) return s;
            if (obj is IEnumerable e && obj is not IDictionary)
            {
                var head = new List<string>();
                int i = 0;
                foreach (var it in e)
                {
                    if (i++ >= 5) { head.Add("..."); break; }
                    head.Add(ToLine(it));
                }
                return "[" + string.Join(", ", head) + "]";
            }
            var t = obj.GetType();
            if (t.IsPrimitive) return obj.ToString() ?? "";
            if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
                return Convert.ToDouble(obj, CultureInfo.InvariantCulture).ToString("G", CultureInfo.InvariantCulture);
            if (t == typeof(DateTime)) return ((DateTime)obj).ToString("u");

            // Prefer human-friendly fields if present
            var prop = t.GetProperty("SymbolName")
                   ?? t.GetProperty("Symbol")
                   ?? t.GetProperty("Name")
                   ?? t.GetProperty("Ticket")
                   ?? t.GetProperty("Id");
            if (prop != null)
            {
                try { return $"{t.Name}{{{prop.Name}={prop.GetValue(obj)}}}"; } catch { /* ignore */ }
            }
            return t.Name;
        }


// SampleStream: non-blocking sampler for async streams.
// - Reads up to 'maxItems' OR until 'maxTime' elapses, whichever comes first.
// - Uses a linked CTS with CancelAfter(maxTime) so the parent token remains intact.
// - Prints each item via ToLine(...); OperationCanceledException is expected on timeout.
// - Any other exception is logged as a warning; finally prints the collected count.
        private static async Task SampleStream<T>(
            IAsyncEnumerable<T> stream,
            string label,
            int maxItems,
            TimeSpan maxTime,
            CancellationToken parentCt)
        {
            Console.WriteLine($"[{label}] sampling up to {maxItems} items or {maxTime.TotalSeconds:F0}s...");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);
            cts.CancelAfter(maxTime);

            var count = 0;
            try
            {
                await foreach (var item in stream.WithCancellation(cts.Token))
                {
                    Console.WriteLine($"  -> {ToLine(item)}");
                    if (++count >= maxItems) break;
                }
            }
            catch (OperationCanceledException) { /* expected on timeout */ }
            catch (Exception ex) { Warn($"{label} (stream)", ex); }

            Console.WriteLine($"[{label}] collected {count} item(s).");
        }

        // Simple heuristic for SL/TP distances per symbol class
        private static double? GuessPointSize(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return null;
            symbol = symbol.ToUpperInvariant();
            if (symbol.EndsWith("JPY")) return 0.01;
            if (symbol.Contains("XAU") || symbol.Contains("GOLD")) return 0.1;
            return 0.0001;
        }


// Reflection helper: try to read property 'name' from 'obj' and convert to double.
// - null obj / missing property → null
// - Uses Convert.ToDouble(..., InvariantCulture); non-numeric or bad format → null
// - Works for boxed int/float/decimal/double and numeric strings like "1.23".
        private static double? GetPropDouble(object? obj, string name)
        {
            if (obj is null) return null;
            var p = obj.GetType().GetProperty(name);
            if (p == null) return null;
            try
            {
                var v = p.GetValue(obj);
                return v == null ? null : Convert.ToDouble(v, CultureInfo.InvariantCulture);
            }
            catch { return null; }
        }


// Reflection helper: read property 'name' and convert to long.
// - null obj / missing property → null
// - Convert.ToInt64 with InvariantCulture; overflow/format → null
// - Note: non-integer numerics get rounded (banker's rounding).
        private static long? GetPropLong(object? obj, string name)
        {
            if (obj is null) return null;
            var p = obj.GetType().GetProperty(name);
            if (p == null) return null;
            try
            {
                var v = p.GetValue(obj);
                return v == null ? null : Convert.ToInt64(v, CultureInfo.InvariantCulture);
            }
            catch { return null; }
        }
    }
}
