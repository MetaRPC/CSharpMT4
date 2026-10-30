# Sending a Market or Pending Order

> **Request:** send a trade order (market or pending)
> Sends a new order using the specified parameters and returns execution details.

**API Information:**

* **SDK wrapper:** `MT4Account.OrderSendAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.TradingHelper`
* **Proto definition:** `OrderSend` (in the TradingHelper service)

### RPC

* **Service:** `mt4_term_api.TradingHelper`
* **Method:** `OrderSend(OrderSendRequest) → OrderSendReply`
* **Low‑level client (generated):** `TradingHelper.TradingHelperClient.OrderSend(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<OrderSendData> OrderSendAsync(
            OrderSendRequest request,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`OrderSendRequest { Symbol, OperationType, Volume, Price, Slippage, Stoploss, Takeprofit, MagicNumber, Comment }`

**Reply message:**

`OrderSendReply { data: OrderSendData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
// Same OrderSendRequest shape as low-level; the service forwards and returns OrderSendData:
//   var sent = await _service.OrderSendAsync(request);   // request built below

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

var request = new OrderSendRequest
{
    Symbol       = "EURUSD",
    OperationType= OrderSendOperationType.OcOpBuy, // market BUY
    Volume       = 0.10,
    Price        = 0,   // market order -> 0 (server will use current price)
    Slippage     = 5,   // in points
    MagicNumber  = 123456,
    Comment      = "Test order"
};

var result = await _mt4.OrderSendAsync(
    request: request,
    deadline: null,
    cancellationToken: cts.Token);

Console.WriteLine($"The order was successfully opened. Ticket: {result.Ticket}, Price: {result.Price}");
// If your proto returns open time:
// Console.WriteLine($"OpenTime (UTC): {result.OpenTime}");
```

---

## 🔽 Input

**`OrderSendRequest`** — fields:

| Field           | Type                     | Required | Description                                                  |
| --------------- | ------------------------ | :------: | ------------------------------------------------------------ |
| `Symbol`        | `string`                 |    ✔️    | Trading symbol (e.g., `"EURUSD"`).                           |
| `OperationType` | `OrderSendOperationType` |    ✔️    | Order operation (market/pending).                            |
| `Volume`        | `double`                 |    ✔️    | Order volume in lots (> 0).                                  |
| `Price`         | `double`                 |     ➖    | For **pending** orders — target price. For **market** — `0`. |
| `Slippage`      | `int`                    |     ➖    | Max slippage allowed (in **points**).                        |
| `MagicNumber`   | `int`                    |     ➖    | Optional tag used to identify EA/strategy.                   |
| `Comment`       | `string`                 |     ➖    | Optional order comment (broker formats may vary).            |

#### ENUM: `OrderSendOperationType` — field `OperationType`

| Value               | Description            |
| ------------------- | ---------------------- |
| `OcOpBuy = 0`       | Market **Buy**         |
| `OcOpSell = 1`      | Market **Sell**        |
| `OcOpBuylimit = 2`  | Pending **Buy Limit**  |
| `OcOpSelllimit = 3` | Pending **Sell Limit** |
| `OcOpBuystop = 4`   | Pending **Buy Stop**   |
| `OcOpSellstop = 5`  | Pending **Sell Stop**  |

> Note the casing: pending members are `OcOpBuylimit` / `OcOpSelllimit` / `OcOpBuystop` / `OcOpSellstop` (lowercase after `Op`).

---

## ⬆️ Output

**`OrderSendData`** — typical fields:

| Field      | Type        | Description                                |
| ---------- | ----------- | ------------------------------------------ |
| `Ticket`   | `int`       | Unique order ID assigned by MT4.           |
| `Volume`   | `double`    | Filled volume (lots).                      |
| `Price`    | `double`    | Actual execution price (or pending price). |
| `OpenTime` | `Timestamp` | Time when the order was opened.            |

> The exact set of returned fields depends on your proto. The core fields are `Ticket` and `Price` (used in examples).

---

## 🎯 Purpose

Place a **new trade** — market or pending — with control over volume, price, slippage, and attribution (magic/comment). The response includes the assigned **ticket** and execution **price** for confirmation and subsequent management.

---

## 🧩 Notes & Tips

* **Slippage units = points.** Not pips. For 5‑digit FX, 1 pip = 10 points.
* **Validate volume & price.** Use `SymbolParams` to read `VolumeMin/Max/Step` and `Digits` before sending.
* **Market vs Pending.** For market orders set `Price = 0`; for pending you must set a valid `Price` relative to the current market.
* **MagicNumber hygiene.** Use a stable magic per strategy to simplify ticket attribution and reconciliation.

---

## ⚠️ Pitfalls

* **Trade mode disabled.** If `TradeMode` for the symbol forbids new orders, server will reject.
* **Wrong side/price for pending.** e.g., BuyLimit above market or BuyStop below market → error.
* **Volume step.** Non‑aligned `Volume` (not a multiple of `VolumeStep`) is rejected by many brokers.

---

## 🧪 Testing Suggestions

* **Happy path:** Send a tiny market order on a demo (e.g., `0.01` lot); expect non‑zero `Ticket` and a reasonable `Price`.
* **Pending order:** Place a `BuyLimit` well below current Bid; verify order appears in open/pending list with the requested price.
* **Failure path:** Try `Volume = 0` or invalid pending price and confirm you receive an `ApiExceptionMT4` with a clear code without crashing the app.
