# MT4Account — Master Overview

> One page to **orient fast**: what lives where, how to choose the right API, and jump links to every **group overview** and **method spec** in the low-level (`MT4Account`) docs set.

---

## 🚦 Start here — Group Overviews

<div class="hx-cards hx-cards-4" markdown="0">

  <a href="../Account/Account_Overview/" class="hx-gc hx-teal">
    <div class="hx-gcTop"></div>
    <div class="hx-gcBody">
      <div class="hx-gcTag">Group</div>
      <div class="hx-gcTitle">Account</div>
      <p class="hx-gcDesc">Snapshot, trade stats, unrealized P/L, equity history.</p>
      <div class="hx-gcCount">5 methods</div>
    </div>
  </a>

  <a href="../Market%20Info/Market_Info_Overview/" class="hx-gc hx-blue">
    <div class="hx-gcTop"></div>
    <div class="hx-gcBody">
      <div class="hx-gcTag">Group</div>
      <div class="hx-gcTitle">Market Info</div>
      <p class="hx-gcDesc">Quotes, symbols, history/bars, tick values, timezone.</p>
      <div class="hx-gcCount">9 methods</div>
    </div>
  </a>

  <a href="../Orders/Orders_Overview/" class="hx-gc hx-purple">
    <div class="hx-gcTop"></div>
    <div class="hx-gcBody">
      <div class="hx-gcTag">Group</div>
      <div class="hx-gcTitle">Orders</div>
      <p class="hx-gcDesc">Open orders & history, send / modify / close / close-by.</p>
      <div class="hx-gcCount">7 methods</div>
    </div>
  </a>

  <a href="../Streaming/Streaming_Overview/" class="hx-gc hx-green">
    <div class="hx-gcTop"></div>
    <div class="hx-gcBody">
      <div class="hx-gcTag">Group</div>
      <div class="hx-gcTitle">Streaming</div>
      <p class="hx-gcDesc">Ticks, trades, profit &amp; ticket snapshots, connect state.</p>
      <div class="hx-gcCount">5 methods</div>
    </div>
  </a>

</div>

📖 **[Stream management guide](../All_Guides/GRPC_STREAM_MANAGEMENT.md)** — patterns, cancellation, reconnection.

> MT4 is intentionally leaner than MT5: **no Market Depth (DOM)**, no `OrderCheck`/`OrderCalcMargin`, and it works with **orders** (not netted positions). Account/symbol values come back as whole-object replies rather than per-property getters.

---

## 🧭 How to pick an API

| If you need…                         | Go to…       | Typical calls                                                            |
| ------------------------------------ | ------------ | ------------------------------------------------------------------------ |
| Account snapshot / performance       | Account      | `AccountSummaryAsync`, `TradeStatsAsync`, `EquityHistoryAsync`           |
| Quotes & symbol properties           | Market Info  | `QuoteAsync`, `QuoteManyAsync`, `SymbolParamsManyAsync`, `SymbolsAsync`  |
| History / candles                    | Market Info  | `QuoteHistoryAsync`, `BarsAsync`                                         |
| Sizing inputs (tick value, timezone) | Market Info  | `TickValueWithSizeAsync`, `ServerTimezoneAsync`                         |
| Current orders & history             | Orders       | `OpenedOrdersAsync`, `OpenedOrdersTicketsAsync`, `OrdersHistoryAsync`    |
| Trading operations                   | Orders       | `OrderSendAsync`, `OrderModifyAsync`, `OrderCloseDeleteAsync`, `OrderCloseByAsync` |
| Real-time updates                    | Streaming    | `OnSymbolTickAsync`, `OnTradeAsync`, `OnOpenedOrdersProfitAsync`, `OnConnectStateAsync` |

---

## 🔌 Usage pattern (gRPC)

Every data method exists in **two versions**: **Async** (recommended) and a **Sync** twin (rare scenarios).

```csharp
// Async (recommended)
var summary = await account.AccountSummaryAsync();
var quote   = await account.QuoteAsync("EURUSD");

// Sync twin (blocking)
var summary2 = account.AccountSummary();
var quote2   = account.Quote("EURUSD");
```

Every method follows the same shape:

* **Proto Service/Method:** `Service.Method(Request) → Reply`
* **C# wrapper:** `await mt4Account.MethodAsync(...)` (or the sync `Method()`)
* **Reply payload:** `Reply.Data` (proto-generated object)
* **Errors:** failures throw `ApiExceptionMT4` (the gateway error is surfaced, not a numeric code); transient gRPC errors are retried with reconnection.

Timestamps are **UTC** (`google.protobuf.Timestamp`). Streaming methods return **`IAsyncEnumerable<T>`** — consume with `await foreach` and always pass a `CancellationToken`.

---

# 📚 Full Index · All Method Specs

---

## 📄 Account

* **Overview:** [Account_Overview.md](./Account/Account_Overview.md)

* [AccountSummary.md](./Account/AccountSummary.md) — complete account snapshot
* [TradeStats.md](./Account/TradeStats.md) — aggregated trade statistics
* [TradeUnrealizedPL.md](./Account/TradeUnrealizedPL.md) — per-order unrealized P/L extrema
* [EquityHistory.md](./Account/EquityHistory.md) — equity curve points
* [TradeStatsEquityHistory.md](./Account/TradeStatsEquityHistory.md) — stats + chart + trades in one call

---

## 📊 Market Info

* **Overview:** [Market_Info_Overview.md](./Market%20Info/Market_Info_Overview.md)

* [AllSymbols.md](./Market%20Info/AllSymbols.md) — all instruments (names + indices)
* [Quote.md](./Market%20Info/Quote.md) — latest bid/ask for one symbol
* [QuotesMany.md](./Market%20Info/QuotesMany.md) — snapshot quotes for several symbols
* [QuoteHistory.md](./Market%20Info/QuoteHistory.md) — historical quotes (`ENUM_QUOTE_HISTORY_TIMEFRAME`)
* [Bars.md](./Market%20Info/Bars.md) — OHLC bars (`QUOTE_HISTORY_TIMEFRAME`)
* [SymbolInfo.md](./Market%20Info/SymbolInfo.md) — lightweight symbol info (digits/spread/bid)
* [SymbolParams.md](./Market%20Info/SymbolParams.md) — full symbol parameters (+ trade/exec enums)
* [TickValues.md](./Market%20Info/TickValues.md) — tick value / size / contract size
* [SymbolSelect.md](./Market%20Info/SymbolSelect.md) — add/remove in Market Watch
* [ServerTimezone.md](./Market%20Info/ServerTimezone.md) — server UTC offset (`SERVER_TIMEZONE_SOURCE`)

---

## 📦 Orders (state + trading)

* **Overview:** [Orders_Overview.md](./Orders/Orders_Overview.md)

### Current state & history

* [OpenedOrders.md](./Orders/OpenedOrders.md) — full details of open orders (`OpenedOrderType`)
* [OpenedOrderTickets.md](./Orders/OpenedOrderTickets.md) — ticket IDs only (lightweight)
* [OrdersHistory.md](./Orders/OrdersHistory.md) — closed orders in a range (`EnumOrderHistorySortType`)

### Trade lifecycle

* [OrderSend.md](./Orders/OrderSend.md) — send market/pending (`OrderSendOperationType`)
* [ModifyOrder.md](./Orders/ModifyOrder.md) — modify SL/TP/price/expiration
* [CloseOrder.md](./Orders/CloseOrder.md) — close/delete by ticket (`OrderCloseDeleteMode`)
* [CloseByOrder.md](./Orders/CloseByOrder.md) — close one order by an opposite one

---

## 📡 Streaming

* **Overview:** [Streaming_Overview.md](./Streaming/Streaming_Overview.md)

* [RealTimeQuotes.md](./Streaming/RealTimeQuotes.md) — live ticks (`OnSymbolTick`)
* [StreamTradeUpdates.md](./Streaming/StreamTradeUpdates.md) — trade events (`MT4_SUB_ENUM_EVENT_GROUP_TYPE`)
* [StreamOpenedOrderProfits.md](./Streaming/StreamOpenedOrderProfits.md) — floating P/L snapshots
* [StreamOpenedOrderTickets.md](./Streaming/StreamOpenedOrderTickets.md) — ticket-set snapshots
* [StreamConnectState.md](./Streaming/StreamConnectState.md) — connection-state heartbeat

---

## 🎯 Quick navigation by use case

| I want to… | Use this method |
|-----------|-----------------|
| Get a full account snapshot | [AccountSummaryAsync](./Account/AccountSummary.md) |
| Read performance / drawdowns | [TradeStatsAsync](./Account/TradeStats.md) |
| Draw an equity curve | [EquityHistoryAsync](./Account/EquityHistory.md) |
| Get the current price | [QuoteAsync](./Market%20Info/Quote.md) |
| List all symbols | [SymbolsAsync](./Market%20Info/AllSymbols.md) |
| Add a symbol to Market Watch | [SymbolSelectAsync](./Market%20Info/SymbolSelect.md) |
| Load candles for a chart | [BarsAsync](./Market%20Info/Bars.md) |
| Get tick value for sizing | [TickValueWithSizeAsync](./Market%20Info/TickValues.md) |
| List open orders | [OpenedOrdersAsync](./Orders/OpenedOrders.md) |
| Pull order history | [OrdersHistoryAsync](./Orders/OrdersHistory.md) |
| Open a market/pending order | [OrderSendAsync](./Orders/OrderSend.md) |
| Modify SL/TP | [OrderModifyAsync](./Orders/ModifyOrder.md) |
| Close / delete an order | [OrderCloseDeleteAsync](./Orders/CloseOrder.md) |
| Stream live prices | [OnSymbolTickAsync](./Streaming/RealTimeQuotes.md) |
| React to trade events | [OnTradeAsync](./Streaming/StreamTradeUpdates.md) |
| Watch connection health | [OnConnectStateAsync](./Streaming/StreamConnectState.md) |

---

## 🏗️ API architecture (3 layers)

### Layer 1 — `MT4Account` (low-level)

Direct proto/gRPC calls. Build a `Request`, read a `Reply.Data`. Full control, highest verbosity.
**Location:** `package/Helpers/MT4Account.cs`. **Docs:** this folder (you are here).

### Layer 2 — `MT4Service`

Thin typed facade over `MT4Account`: forwards the data methods 1:1 and **adds convenience getters**
(`GetBalanceAsync`, `GetSpreadAsync`, `GetDigitsAsync`, …) that have no single low-level call.
It is also the surface the Sugar layer extends. **Docs:** [MT4Service API](../API_Reference/MT4Service.md).

### Layer 3 — `MT4Sugar`

High-level convenience on top of `MT4Service`: snapshots, normalization, risk-based sizing, market
orders with SL/TP in points, bulk close, tick/trade streams. **Docs:** [MT4Sugar API](../API_Reference/MT4Sugar.md).

> **Rule of thumb:** low-level 1:1 wrappers (`Quote`, `AccountSummary`, `OrderSend`) look identical at the
> Service layer because the Service just forwards them — the Service's value is the **getters + Sugar surface**.

---

## ⚠️ Important notes

* **Demo first.** Always test on a demo account before going live.
* **Re-query before acting.** Equity/floating P/L and open-order lists are snapshots.
* **UTC everywhere.** Prefer the `*Utc` timestamp fields; convert only for display.
* **Streams are open-ended.** Always pass a `CancellationToken`; cancellation completes them gracefully.
* **Broker rules.** Respect `Digits`, `VolumeMin/Max/Step` and minimum stop distances (see Market Info).

---

_Low-level foundation (`MT4Account`) → typed facade (`MT4Service`) → convenience (`MT4Sugar`)._
