# Modifying an Order (SL/TP/Price)

> **Request:** change an existing order's stop-loss, take-profit, price or expiration via `OrderModify`.
> Returns whether the modification was accepted.

**API Information:**

* **SDK wrapper:** `MT4Account.OrderModifyAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.TradingHelper`
* **Proto definition:** `OrderModify` (in the TradingHelper service)

### RPC

* **Service:** `mt4_term_api.TradingHelper`
* **Method:** `OrderModify(OrderModifyRequest) → OrderModifyReply`
* **Low‑level client (generated):** `TradingHelper.TradingHelperClient.OrderModifyAsync(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<OrderModifyData> OrderModifyAsync(
            OrderModifyRequest request,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`OrderModifyRequest { OrderTicket: int, NewPrice?, NewStopLoss?, NewTakeProfit?, NewExpiration? }`

**Reply message:**

`OrderModifyReply { data: OrderModifyData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
// Build the request (see below) and call: await _service.OrderModifyAsync(request);
// Or via sugar (SL/TP only): await _service.ModifySlTpAsync(ticket, sl: 1.0950, tp: 1.1050);

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

var request = new OrderModifyRequest
{
    OrderTicket   = 123456,
    NewStopLoss   = 1.09500,   // set only the fields you want to change
    NewTakeProfit = 1.10500,
};

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var result = await _mt4.OrderModifyAsync(
    request: request,
    deadline: null,
    cancellationToken: cts.Token);

Console.WriteLine($"Modified: {result.OrderWasModified}");
```

---

## 🔽 Input

**`OrderModifyRequest`** — fields:

| Field           | Type        | Required | Description                                             |
| --------------- | ----------- | :------: | ------------------------------------------------------- |
| `OrderTicket`   | `int`       |    ✔️    | Ticket of the order to modify.                          |
| `NewPrice`      | `double`    |     ➖    | New price (for a pending order).                        |
| `NewStopLoss`   | `double`    |     ➖    | New stop-loss price.                                    |
| `NewTakeProfit` | `double`    |     ➖    | New take-profit price.                                  |
| `NewExpiration` | `Timestamp` |     ➖    | New expiration (for a pending order).                   |

Optional call parameters:

* **`deadline`** (`DateTime?`) — optional absolute **UTC** deadline.
* **`cancellationToken`** (`CancellationToken`) — cooperative cancel.

> Only set the fields you intend to change; the `Has*` flags on the proto track which were supplied.

---

## ⬆️ Output — `OrderModifyData`

| Field             | Type   | Description                                   |
| ----------------- | ------ | --------------------------------------------- |
| `OrderWasModified`| `bool` | `true` if the broker accepted the change.     |

---

## 🎯 Purpose

Adjust protective stops or a pending order's price/expiration without closing it:

* Move SL to breakeven / trail a stop.
* Attach or retarget a take-profit.
* Re-price or re-time a pending order.

---

## 🧩 Notes & Tips

* **Prices are absolute.** Pass broker-valid, normalized prices (see `SymbolParams` `Digits`), not offsets.
* **Stops distance.** Brokers enforce a minimum stop distance; too-tight SL/TP is rejected.
* **Sugar shortcut.** `MT4Sugar.ModifySlTpAsync(ticket, sl, tp)` builds the request for the common SL/TP case.

---

## ⚠️ Pitfalls

* **Unchanged values.** Submitting the current SL/TP unchanged may be rejected as a no-op by some brokers.
* **Wrong ticket/state.** Modifying a closed/invalid ticket throws `ApiExceptionMT4`.

---

## 🧪 Testing Suggestions

* **Happy path.** Open a market order, then set SL/TP a safe distance away → `OrderWasModified = true`, visible in `OpenedOrders`.
* **Failure path.** Try a stop closer than the broker minimum and confirm a clear handled error.
