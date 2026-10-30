# Glossary

> Terms and concepts used across the CSharpMT4 SDK and these docs. Short, practical definitions — follow the links for detail.

---

## 🏛️ Architecture

**MT4Account** — the **low-level** layer. Direct gRPC/protobuf calls: you build a `Request` and read a `Reply.Data`. Full control, most verbose. Source: `package/Helpers/MT4Account.cs`. Docs: [MT4Account](../MT5Account/MT4Account.Master.Overview.md).

**MT4Service** — the **mid-level** layer. A typed facade over `MT4Account` that forwards the data methods 1:1 and adds **convenience getters**. Docs: [MT4Service](../MT4Service/MT4Service.Overview.md).

**MT4Sugar** — the **high-level** layer. Extension methods on `MT4Service`: snapshots, normalization, risk sizing, one-line market orders, bulk close. Docs: [MT4Sugar](../MT4Sugar/MT4Sugar.Overview.md).

**Layer rule of thumb** — Sugar for one-liners → Service for typed data → Account for raw request/reply.

---

## 🔌 gRPC & protobuf

**gRPC** — the RPC protocol the SDK speaks to the MetaRPC gateway over HTTP/2 + TLS.

**Protobuf** — the message format. Every call has a generated `XxxRequest` and `XxxReply`; the payload lives in `Reply.Data` (an `XxxData` object).

**Service / Method** — the gRPC service (e.g. `mt4_term_api.AccountHelper`) and its method (e.g. `AccountSummary(AccountSummaryRequest) → AccountSummaryReply`).

**Unary vs streaming** — most calls are **unary** (one request → one reply). Subscriptions are **server-streaming**: the SDK exposes them as `IAsyncEnumerable<T>` consumed with `await foreach`.

**Timestamp** — `google.protobuf.Timestamp`. All times are **UTC**; prefer the `*Utc` fields where a reply exposes both.

---

## 🔑 Connection & auth

**gRPC endpoint (`Grpc`)** — the gateway URL, e.g. production `https://mt4.mrpc.pro:443` or staging `https://mt4-stg.mrpc.pro:443`. Set it explicitly per environment — see [Configuration](GETTING_STARTED.md#configuration-provide-your-connection-data).

**ServerName** — the broker server to attach to (e.g. `MetaQuotes-Demo`, `EGlobal-Demo`). Used by `ConnectByServerNameAsync`.

**Host / Port** — an optional direct connection path (`ConnectByHostPortAsync`) instead of a server name.

**ApiKey** — your MetaRPC token, sent in the `APIKey` header. `TRIAL` is the limited default (cannot open demo accounts).

**terminalInstanceGuid / session id** — a session GUID the gateway generates on connect and returns; the SDK attaches it to later calls automatically. You do **not** call `GetId` before connecting.

**Terminal "alive" / readiness** — after connecting, the terminal instance must be *alive* before it serves data. The connect helpers do a KICK (spin up) → WAIT (require alive) sequence with retries.

**Auto-provision (demo)** — if `User` is `0` or the password is blank, the SDK tries to open a demo account on the configured endpoint (needs a non-`TRIAL` key).

---

## 💰 Account

**Balance** — funds excluding floating P/L. **Equity** — balance + floating P/L of open orders.

**Margin / Free margin** — margin currently used / available for new orders. **Margin level** — `(Equity / Margin) × 100` percent.

**Leverage** — e.g. `100` for 1:100. **Currency** — the deposit currency (money fields are denominated in it). **Credit** — credit amount.

**Trade mode** — `EnumAccountTradeMode`: demo / contest / real. **Margin mode** — `EnumAccountMarginMode`: netting / exchange / **hedging** (the usual MT4 model).

---

## 📈 Symbols & prices

**Symbol** — an instrument name (e.g. `EURUSD`). Brokers may add suffixes (`EURUSD.r`) — use the exact name from `SymbolsAsync()`.

**Bid / Ask** — best sell / buy price. **Spread** — `Ask − Bid`; the SDK's `GetSpreadAsync` returns it in **points** (computed from a quote).

**Digits** — decimal places in the price. **Point** — the smallest price step, `10^-digits` (e.g. `0.00001` for a 5-digit symbol).

**Point vs pip** — on 3/5-digit symbols **1 pip = 10 points**. `PointsToPipsAsync` converts.

**Volume (lots)** — order size. **VolumeMin / VolumeMax / VolumeStep** — broker limits; snap your size to them (`NormalizeVolumeAsync`).

**Tick value / tick size / contract size** — money per tick, minimal price increment, units per lot — inputs for P/L and risk sizing (`TickValueWithSizeAsync`).

**Timeframe** — bar period. `QuoteHistoryAsync` uses `ENUM_QUOTE_HISTORY_TIMEFRAME` (`Qh*`); `BarsAsync` uses `QUOTE_HISTORY_TIMEFRAME` (`Qhb*`) — don't mix the two.

---

## 📦 Orders & trading

**Order** — MT4 works with **orders** (not netted positions). Each has a **ticket** (unique `int` id).

**Market order** — fills now at Bid/Ask. **Pending order** — waits at a target price: buy/sell **limit** or **stop**.

**Order type (open)** — `OpenedOrderType`: `OoOpBuy`, `OoOpSell`, `OoOpBuylimit`, `OoOpSelllimit`, `OoOpBuystop`, `OoOpSellstop` (+ `OoOpBalance`/`OoOpCredit` ledger entries).

**Operation type (send)** — `OrderSendOperationType`: `OcOpBuy`/`OcOpSell` (market) or `OcOpBuylimit`/`OcOpBuystop`/`OcOpSelllimit`/`OcOpSellstop` (pending).

**SL / TP** — stop-loss / take-profit, as **absolute prices** (points-based helpers exist in Sugar).

**Slippage** — max allowed price deviation on a market order, in **points**.

**Magic number** — an `int` tag to identify an order's strategy/EA.

**Close vs delete** — closing a *market* order vs deleting a *pending* one. `OrderCloseDeleteData.Mode` (`OrderCloseDeleteMode`) says which happened. **Close-by** — close one order with an opposite one (hedged accounts).

---

## 📡 Streaming

**Subscription** — a server stream of events, returned as `IAsyncEnumerable<T>`. Always pass a `CancellationToken`; cancelling completes it gracefully (a `Cancelled` status is normal).

**Event group** — `MT4_SUB_ENUM_EVENT_GROUP_TYPE` (`OrderProfit` periodic update / `OrderUpdate` create-modify-close) on trade/profit streams.

**Snapshot semantics** — profit/ticket streams emit the **current** set each interval; compute add/remove diffs on the client.

Guide: [gRPC Streaming](GRPC_STREAM_MANAGEMENT.md).

---

## ⚠️ Errors & resilience

**ApiExceptionMT4** — thrown when the gateway returns an error (carries the broker/terminal detail). Not a numeric return code.

**Reconnection** — unary calls retry on transient gRPC statuses (`Unavailable`, `DeadlineExceeded`, `Internal`) and reconnectable terminal errors, with exponential backoff; streams restart with backoff.

**Deadline** — an absolute per-call UTC cut-off (`deadline`) that converts to a timeout. **CancellationToken** — cooperative cancel for a call or a stream.

---

## 📝 Conventions in these docs

**`*Data`** — a protobuf reply payload (e.g. `AccountSummaryData`). **`*Async`** — the async method; most have a synchronous twin without the suffix. **Getter** (`GetBalanceAsync`, …) — a `MT4Service` shortcut returning a single value.
