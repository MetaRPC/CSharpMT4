# Sending a Test Buy Order

> **Request:** place a market buy order with test parameters
> Sends a Buy order with default volume and comment to the trading server.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowOrderSendExample("EURUSD");

// Or directly from MT4Account
tick = await _mt4.QuoteAsync("EURUSD");

var request = new OrderSendRequest
{
    Symbol       = "EURUSD",
    OperationType = OrderSendOperationType.OcOpBuy,
    Volume       = 0.1,
    Price        = 0, // Market order; price optional
    Slippage     = 5,
    MagicNumber  = 123456,
    Comment      = "Test order"
};

var result = await _mt4.OrderSendAsync(request);
Console.WriteLine($"Order sent. Ticket: {result.Ticket}, Price: {result.Price}");
```

---

### Method Signature

```csharp
Task<OrderSendData> OrderSendAsync(
    OrderSendRequest request,
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## 🔽 Input

* **`request`** (`OrderSendRequest`) — contains all parameters for placing the order:

  * **`Symbol`** (`string`) — instrument (e.g., "EURUSD").
  * **`OperationType`** (`OrderSendOperationType`) — order type. Typically `OcOpBuy` or `OcOpSell`.
  * **`Volume`** (`double`) — trade volume in lots.
  * **`Price`** (`double`) — optional; set to `0` for market orders.
  * **`Slippage`** (`int`) — max price deviation allowed.
  * **`MagicNumber`** (`int`) — identifier for programmatic orders.
  * **`Comment`** (`string`) — custom comment attached to the order.

Optional:

* **`deadline`** (`DateTime?`) — optional deadline.
* **`cancellationToken`** (`CancellationToken`) — token for cancellation.

---

## ⬆️ Output

Returns an **`OrderSendData`** object with:

| Field    | Type     | Description                             |
| -------- | -------- | --------------------------------------- |
| `Ticket` | `int`    | Assigned order ticket number            |
| `Price`  | `double` | Final price at which order was executed |

---

## 🎯 Purpose

This method demonstrates how to place a simple Buy order via the API. It’s useful for:

* Integration tests
* Manual testing on demo accounts
* Validating order parameters and connectivity

---

### ❓ Why it's commented out in code:

This method creates a **real trade** (on demo or live account depending on context). It’s commented out by default to:

* ❌ Avoid accidental order execution while testing
* ✅ Ensure orders are sent intentionally with correct parameters

To use it, ensure symbol is valid and trading is enabled for the account. Recommended only in controlled test environments.
