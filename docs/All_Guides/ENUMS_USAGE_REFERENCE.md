# ENUMs Reference

> Every project-defined enum the CSharpMT4 API uses — what each one is, where it appears (a method **input** you pass or a reply **output** you read), and its values.

All enums live in the generated **`mt4_term_api`** namespace. Reference a member as `mt4_term_api.EnumName.Member`:

```csharp
using mt4_term_api;

var req = new OrderSendRequest { Symbol = "EURUSD", OperationType = OrderSendOperationType.OcOpBuy };
var bars = await service.BarsAsync("EURUSD", QUOTE_HISTORY_TIMEFRAME.QhbPeriodH1, from, to);
```

> Enums print with their C# member name via `ToString()`; the numeric value is the proto wire value.

## 🔎 Quick lookup — which methods touch enums

| Method | Input enum | Output enum |
| ------ | ---------- | ----------- |
| `AccountSummaryAsync` | — | `EnumAccountTradeMode`, `EnumAccountMarginMode` |
| `QuoteHistoryAsync` | `ENUM_QUOTE_HISTORY_TIMEFRAME` | — |
| `BarsAsync` | `QUOTE_HISTORY_TIMEFRAME` | — |
| `SymbolParamsManyAsync` | — | `SP_ENUM_SYMBOL_TRADE_MODE`, `SP_ENUM_SYMBOL_TRADE_EXECUTION`, `SP_ENUM_TRADE_CALC_MODE` |
| `ServerTimezoneAsync` | — | `SERVER_TIMEZONE_SOURCE` |
| `OpenedOrdersAsync` | `EnumOpenedOrderSortType` | `OpenedOrderType` (per order) |
| `OrdersHistoryAsync` | `EnumOrderHistorySortType` | `OpenedOrderType` (per order) |
| `OrderSendAsync` | `OrderSendOperationType` | — |
| `OrderCloseDeleteAsync` | — | `OrderCloseDeleteMode` |
| `OnTradeAsync` / `OnOpenedOrdersProfitAsync` | — | `MT4_SUB_ENUM_EVENT_GROUP_TYPE` (+ `SUB_ORDER_OPERATION_TYPE` per order) |

---

## 👤 Account Information

### `EnumAccountTradeMode` — `AccountSummaryData.AccountTradeMode` (out)
* `AccountTradeModeDemo = 0` · `AccountTradeModeContest = 1` · `AccountTradeModeReal = 2`

### `EnumAccountMarginMode` — `AccountSummaryData.AccountMarginMode` (out)
* `AccountMarginModeRetailNetting = 0` — one net position per symbol
* `AccountMarginModeExchange = 1` — exchange model
* `AccountMarginModeRetailHedging = 2` — hedging (the usual MT4 model)

---

## 📈 Symbol Information

### `ENUM_QUOTE_HISTORY_TIMEFRAME` — `QuoteHistoryAsync(timeframe)` (in)
Members are period codes (not minutes):
* `QhPeriodM1 = 0` · `QhPeriodM5 = 1` · `QhPeriodM15 = 2` · `QhPeriodM30 = 3`
* `QhPeriodH1 = 4` · `QhPeriodH4 = 5` · `QhPeriodD1 = 6` · `QhPeriodW1 = 7` · `QhPeriodMn1 = 8`

### `QUOTE_HISTORY_TIMEFRAME` — `BarsAsync(timeframe)` (in)
Members are the period **in minutes** (different enum from the one above — don't mix):
* `QhbPeriodM1 = 1` · `QhbPeriodM5 = 5` · `QhbPeriodM15 = 15` · `QhbPeriodM30 = 30`
* `QhbPeriodH1 = 60` · `QhbPeriodH4 = 240` · `QhbPeriodD1 = 1440` · `QhbPeriodW1 = 10080` · `QhbPeriodMn1 = 43200`
* `QhbPeriodUnspecified = 0` (do not use)

### `SP_ENUM_SYMBOL_TRADE_MODE` — `SymbolParamsManyInfo.TradeMode` (out)
* `SymbolTradeModeDisabled = 0` · `SymbolTradeModeLongonly = 1` · `SymbolTradeModeShortonly = 2` · `SymbolTradeModeCloseonly = 3` · `SymbolTradeModeFull = 4`

### `SP_ENUM_SYMBOL_TRADE_EXECUTION` — `SymbolParamsManyInfo.TradeExeMode` (out)
* `SymbolTradeExecutionRequest = 0` · `SymbolTradeExecutionInstant = 1` · `SymbolTradeExecutionMarket = 2` · `SymbolTradeExecutionExchange = 3`

### `SP_ENUM_TRADE_CALC_MODE` — `SymbolParamsManyInfo.TradeCalcMode` (out)
* `SymbolTradeMargineCalcModeForex = 0` · `SymbolTradeMargineCalcModeFutures = 1` · `SymbolTradeMargineCalcModeCfd = 2`

### `SERVER_TIMEZONE_SOURCE` — `ServerTimezoneData.Source` (out)
* `Unknown = 0` · `Live = 1` (authoritative) · `LastValidated = 2` (cached fallback)

---

## 📦 Orders & Trading

### `OrderSendOperationType` — `OrderSendRequest.OperationType` (in)
* `OcOpBuy = 0` · `OcOpSell = 1` — market
* `OcOpBuylimit = 2` · `OcOpSelllimit = 3` — pending limit
* `OcOpBuystop = 4` · `OcOpSellstop = 5` — pending stop

> Casing: pending members are `OcOpBuylimit` / `OcOpSelllimit` / `OcOpBuystop` / `OcOpSellstop` (lowercase after `Op`).

### `OpenedOrderType` — `OpenedOrderInfo.OrderType` / `HistoryOrderInfo.OrderType` (out)
* `OoOpBuy = 0` · `OoOpSell = 1` — market
* `OoOpBuylimit = 2` · `OoOpSelllimit = 3` — pending limit
* `OoOpBuystop = 4` · `OoOpSellstop = 5` — pending stop
* `OoOpBalance = 6` · `OoOpCredit = 7` — ledger entries (deposit/withdrawal, credit)

### `EnumOpenedOrderSortType` — `OpenedOrdersAsync(sortType)` (in)
* `SortByOpenTimeAsc = 0` (default) · `SortByOpenTimeDesc = 1` · `SortByOrderTicketIdAsc = 2` · `SortByOrderTicketIdDesc = 3`

### `EnumOrderHistorySortType` — `OrdersHistoryAsync(sortType)` (in)
* `HistorySortByOpenTimeAsc = 0` · `HistorySortByOpenTimeDesc = 1`
* `HistorySortByCloseTimeAsc = 2` · `HistorySortByCloseTimeDesc = 3` (default)
* `HistorySortByOrderTicketIdAsc = 4` · `HistorySortByOrderTicketIdDesc = 5`

### `OrderCloseDeleteMode` — `OrderCloseDeleteData.Mode` (out)
* `OcdMarketOrder = 0` — a market order was closed
* `OcdPendingOrder = 1` — a pending order was deleted

---

## 📡 Streaming

### `MT4_SUB_ENUM_EVENT_GROUP_TYPE` — `OnTradeData.Type` / `OnOpenedOrdersProfitData.Type` (out)
* `OrderProfit = 0` — periodic profit/price update for open orders
* `OrderUpdate = 1` — an order was created / modified / closed

### `SUB_ORDER_OPERATION_TYPE` — `OnOpenedOrdersProfitOrderInfo.Type` (out)
Per-order side inside a profit-stream update (mirrors `OpenedOrderType`):
* `SubOpBuy = 0` · `SubOpSell = 1` · `SubOpBuylimit = 2` · `SubOpSelllimit = 3` · `SubOpBuystop = 4` · `SubOpSellstop = 5`

---

## ⚠️ Errors

Errors are surfaced as `ApiExceptionMT4` (its `Error` carries the detail); they are **not** numeric return codes.

### `ErrorType` — `Error.ErrorType`
Classifies where an error came from, e.g.:
* `Undefined = 0` · `Mrpc = 1` · `MrpcValidation = 2` · `MrpcTimeout = 3`
* `TerminalApiTimeout = 4` · `TerminalApiGetParams = 5` · `TerminalApiSetResult = 6` · `TerminalApiUnexpected = 7`
* `MqlExecution = 8` · `MqlCustomExecution = 9`

### `MqlErrorCode`
The MT4/MQL trade error code set (many values) carried on execution errors — inspect `ApiExceptionMT4` fields (`MqlErrorCode` / `MqlErrorDescription`) when a trade is rejected. See **[Return Codes](RETURN_CODES_REFERENCE.md)**.

---

> **Not listed here** are terminal-management enums (`ChartTemplateMode`, `EA_PARAM_TYPE`, `TerminalType`, GUI/proxy enums, …) — they belong to terminal-control features outside the core trading/data API documented in these guides.
