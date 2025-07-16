# Sending a Market or Pending Order

> **Request:** send a trade order (market or pending)
> Sends a new order using the specified parameters and receives back execution details.

---

### Code Example

```csharp
// Using service wrapper
await _service.ShowOrderSendExample();

// Or directly using MT4Account
var request = new OrderSendRequest
{
    Symbol     = Constants.DefaultSymbol,
    Volume     = Constants.DefaultVolume,
    OrderType  = ENUM_ORDER_TYPE_TF.OrderTypeTfBuy
};

var result = await _mt4.OrderSendAsync(request);
_logger.LogInformation(
    "OrderSendAsync: Ticket={Ticket}, Volume={Volume}, Price={Price}, OpenTime={OpenTime}",
    result.Ticket, result.Volume, result.Price, result.OpenTime.ToDateTime().ToLocalTime()
);
```

---

### Method Signature

```csharp
Task<OrderSendData> OrderSendAsync(OrderSendRequest request)
```

---

## 📃 Input

**OrderSendRequest** — object with fields:

| Field        | Type                 | Description                        |
| ------------ | -------------------- | ---------------------------------- |
| `Symbol`     | `string`             | Trading symbol (e.g., "EURUSD")    |
| `Volume`     | `double`             | Order volume in lots (e.g., `0.1`) |
| `OrderType`  | `ENUM_ORDER_TYPE_TF` | Type of order (market/pending)     |
| `Price`      | `double?` (optional) | Order price for pending orders     |
| `Slippage`   | `int?` (optional)    | Max slippage allowed (in points)   |
| `StopLoss`   | `double?` (optional) | Stop Loss price                    |
| `TakeProfit` | `double?` (optional) | Take Profit price                  |
| `Comment`    | `string?` (optional) | Optional order comment             |
| `Magic`      | `int?` (optional)    | Magic number to tag the order      |

---

## ⬆️ Output

**OrderSendData** — object with properties:

| Field      | Type             | Description                     |
| ---------- | ---------------- | ------------------------------- |
| `Ticket`   | `int`            | Unique order ID assigned by MT4 |
| `Volume`   | `double`         | Confirmed order volume          |
| `Price`    | `double`         | Actual execution price          |
| `OpenTime` | `DateTime` (UTC) | Time when order was executed    |

---

## 🎯 Purpose

Use this method to place a **new trade** — either market or pending — with full control over volume, price, and risk parameters. The result includes the **assigned ticket number**, price, and open time for confirmation or logging.

---

### ❓ Notes

This method is currently **commented in main test code** because it requires an order with:

* Valid symbol, connection, and
* Terminal in trading state.

Once integration is stable, it can be safely **uncommented for production use.**
