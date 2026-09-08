# MT4Sugar API Reference

> Complete API reference for `MT4Sugar` in MetaTrader 4 automation.

---

## 📌 Layer Overview

`MT4Sugar` is a core component of the MetaRPC architecture.

- **`MT4Account`**: Layer 1 — low-level gRPC client communicating directly with the MT4 terminal gateway.
- **`MT4Service`**: Layer 2 — typed wrapper methods providing clean signatures and native data structures.
- **`MT4Sugar`**: Layer 3 — high-level convenience layer featuring risk-based volume sizing, auto-normalization, and batch order actions.

---

## 📑 Method Groups

### 1. Connection & Session
- `connect_by_server_name` / `ConnectByServerNameAsync`: Connect via MT4 broker server name.
- `connect_by_host_port` / `ConnectByHostPortAsync`: Direct connection to terminal IP and port.
- `is_connected` / `IsConnected`: Check active channel health.
- `disconnect` / `Disconnect`: Gracefully release gRPC connection.

### 2. Account Information
- `account_summary` / `AccountSummary`: Full balance, equity, margin, free margin, leverage, currency, trade mode.
- `account_info_double`: Balance, equity, margin free, profit.
- `account_info_integer`: Login, leverage, trade allowed, margin mode.
- `account_info_string`: Currency, broker company, server name.

### 3. Market & Symbol Info
- `symbol_info_tick`: Latest bid, ask, last, volume.
- `symbol_params_many`: Batch query symbol specifications (point, digits, contract size, stops level).
- `symbols_total`: Total available market symbols count.
- `symbol_select`: Add or remove symbol from MarketWatch.

### 4. Trading Operations
- `order_send` / `OrderSend`: Send market orders, pending orders, modifications.
- `order_close` / `OrderClose`: Close open orders or positions.
- `order_modify` / `OrderModify`: Update Stop Loss and Take Profit levels.
- `order_check` / `OrderCheck`: Pre-flight margin and parameter validation before execution.

### 5. Real-Time Streaming
- `on_symbol_tick` / `SubscribeToTicks`: Real-time streaming price tick feed.
- `on_trade` / `SubscribeToTrades`: Execution reports and order state events.
- `on_position_profit` / `SubscribeToPositionProfit`: Real-time P&L changes across open positions.
- `on_book_event` / `SubscribeToMarketBook`: Level II market depth (DOM) updates.
