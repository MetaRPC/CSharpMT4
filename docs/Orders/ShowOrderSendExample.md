# Sending a Market or Pending Order

> **Request:** send a trade order (market or pending)
> Sends a new order using the specified parameters and returns execution details.

---

### Code Example

```csharp
// --- Quick use (service wrapper) ---
// Opens a small BUY on the provided symbol. Uses fixed demo params inside the method
// (volume=0.1, slippage=5 points, magic=123456, comment="Test order").
await _service.ShowOrderSendExample("EURUSD");

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

### Method Signatures

```csharp
// Service wrapper (example implementation)
Task ShowOrderSendExample(string symbol);
```

```csharp
// Low-level account call
Task<OrderSendData> OrderSendAsync(
    OrderSendRequest request,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
);
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

#### ENUM: `OrderSendOperationType`

| Value           | Description           |
| --------------- | --------------------- |
| `OcOpBuy`       | Market **Buy**        |
| `OcOpSell`      | Market **Sell**       |
| `OcOpBuyLimit`  | Pending **BuyLimit**  |
| `OcOpSellLimit` | Pending **SellLimit** |
| `OcOpBuyStop`   | Pending **BuyStop**   |
| `OcOpSellStop`  | Pending **SellStop**  |

*(Names follow the proto used in your project; see `mrpc-proto` repo for authoritative list.)*

---

## ⬆️ Output

**`OrderSendData`** — typical fields:

| Field      | Type     | Description                                |
| ---------- | -------- | ------------------------------------------ |
| `Ticket`   | `int`    | Unique order ID assigned by MT4.           |
| `Price`    | `double` | Actual execution price (or pending price). |
| `OpenTime` | `string` | (If provided) UTC time when order opened.  |

> The exact set of returned fields depends on your proto. The core fields are `Ticket` and `Price` (used in examples).

---

## 🎯 Purpose

Place a **new trade** — market or pending — with control over volume, price, slippage, and attribution (magic/comment). The response includes the assigned **ticket** and execution **price** for confirmation and subsequent management.

---

## 🧩 Notes & Tips

* **Slippage units = points.** Not pips. For 5‑digit FX, 1 pip = 10 points.
* **Validate volume & price.** Use `ShowSymbolParams` to read `VolumeMin/Max/Step` and `Digits` before sending.
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
