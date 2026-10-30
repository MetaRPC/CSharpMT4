# Closing an Order By Opposite

> **Request:** close one open order using another opposite-position order
> Sends a request to match and close two opposite-direction orders by ticket.

**API Information:**

* **SDK wrapper:** `MT4Account.OrderCloseByAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.TradingHelper`
* **Proto definition:** `OrderCloseBy` (in the TradingHelper service)

### RPC

* **Service:** `mt4_term_api.TradingHelper`
* **Method:** `OrderCloseBy(OrderCloseByRequest) → OrderCloseByReply`
* **Low‑level client (generated):** `TradingHelper.TradingHelperClient.OrderCloseBy(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<OrderCloseByData> OrderCloseByAsync(
            OrderCloseByRequest request,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`OrderCloseByRequest { TicketToClose: int, OppositeTicketClosingBy: int }`

**Reply message:**

`OrderCloseByReply { data: OrderCloseByData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
// Build the request (see below) and call: await _service.OrderCloseByAsync(request);

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.
// Both tickets must be OPEN and of OPPOSITE types (Buy vs Sell) on the SAME symbol.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

var request = new OrderCloseByRequest
{
    TicketToClose = 123456,
    OppositeTicketClosingBy = 654321
};

var result = await _mt4.OrderCloseByAsync(
    request: request,
    deadline: null,
    cancellationToken: cts.Token);

Console.WriteLine($"Closed by opposite: Profit={result.Profit}, Price={result.ClosePrice}, Time={result.CloseTime}");
```

---

## 🔽 Input

* **`request`** (`OrderCloseByRequest`) — structure with:

  | Field                     | Type  | Description                                                  |
  | ------------------------- | ----- | ------------------------------------------------------------ |
  | `TicketToClose`           | `int` | Ticket ID of the primary order to close.                     |
  | `OppositeTicketClosingBy` | `int` | Ticket ID of the opposite order used to close the first one. |

Optional:

* **`deadline`** (`DateTime?`) — optional per-call deadline.
* **`cancellationToken`** (`CancellationToken`) — to cancel the request.

> ⚠️ Both tickets must be valid **open** orders, on the **same symbol**, and with **opposite directions**.

---

## ⬆️ Output

Returns **`OrderCloseByData`** with:

| Field        | Type     | Description                              |
| ------------ | -------- | ---------------------------------------- |
| `Profit`     | `double` | Profit/loss from the close-by operation. |
| `ClosePrice` | `double` | Execution price of the operation.        |
| `CloseTime`  | `Timestamp` | Timestamp of the operation.           |

---

## 🎯 Purpose

Close matched hedge positions — one Buy and one Sell — via the **Close By** mechanism. This can reduce commission/slippage compared to closing each order separately.

---

## 🧩 Notes & Tips

* **Same symbol & opposite side.** Close By works only when both orders are for the **same instrument** and have **opposite** directions.
* **Volumes.** Brokers commonly require **equal volumes** for a single Close By operation. If volumes differ, the request may be rejected; use partial close(s) or multiple operations if supported by your workflow.
* **Ticket range.** This API uses `int` tickets on the wire. If you store tickets as `long`, ensure they fit into `Int32` before sending.
* **Hedging mode.** Close By implies hedged positions. If the account/broker enforces netting, this operation may not be allowed.

---

## ⚠️ Pitfalls

* **Already closed/invalid ticket.** Expect an API error if either ticket is not currently open.
* **Different symbols or same side.** Any mismatch leads to a clear server error.
* **Symbol suffixes.** `EURUSD.m` and `EURUSD` are different instruments — match exactly.

---

## 🧪 Testing Suggestions

* **Happy path.** Open a Buy and a Sell with the **same symbol** and **same lot**; call `OrderCloseByAsync` and verify `Profit` aligns with price difference and contract settings.
* **Edge cases.** Try mismatched symbols or same-side tickets and confirm a proper `ApiExceptionMT4` is thrown and handled.
* **Timeouts.** Use a short cancellation window (e.g., 3–5s) and verify graceful cancellation without leaving partial states.
