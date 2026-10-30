# Getting Unrealized P/L Extrema

> **Request:** per-order unrealized P/L extrema (`TradeUnrealizedPLData`) from MT4
> For each currently open order, the best (max) and worst (min) floating P/L reached, with timestamps.

**API Information:**

* **SDK wrapper:** `MT4Account.TradeUnrealizedPLAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.TradeStats`
* **Proto definition:** `TradeUnrealizedPL` (trade-stats proto)

### RPC

* **Service:** `mt4_term_api.TradeStats`
* **Method:** `TradeUnrealizedPL(TradeUnrealizedPLRequest) → TradeUnrealizedPLReply`
* **Low‑level client (generated):** `TradeStats.TradeStatsClient.TradeUnrealizedPLAsync(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<TradeUnrealizedPLData> TradeUnrealizedPLAsync(
            string? from = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`TradeUnrealizedPLRequest { from?: string }`

**Reply message:**

`TradeUnrealizedPLReply { data: TradeUnrealizedPLData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var upl = await _service.TradeUnrealizedPLAsync();
Console.WriteLine($"{upl.Extrema.Count} open order(s) tracked");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var data = await _mt4.TradeUnrealizedPLAsync(
    from: null,
    deadline: null,
    cancellationToken: cts.Token);

foreach (var e in data.Extrema)
    Console.WriteLine($"#{e.Ticket} {e.Symbol}: max={e.MaxUnrealizedPl} min={e.MinUnrealizedPl}");
```

---

## 🔽 Input

| Parameter           | Type                | Description                                        |
| ------------------- | ------------------- | -------------------------------------------------- |
| `from`              | `string?`           | Optional ISO start date; null = all open orders    |
| `deadline`          | `DateTime?`         | Optional absolute **UTC** deadline for the call     |
| `cancellationToken` | `CancellationToken` | Cooperative cancel for the call/retry loop          |

---

## ⬆️ Output — `TradeUnrealizedPLData`

| Field     | Type                                     | Description                     |
| --------- | ---------------------------------------- | ------------------------------- |
| `Extrema` | `RepeatedField<TradeUnrealizedExtremaData>` | One entry per open order.    |

### `TradeUnrealizedExtremaData` (each item)

| Field                 | Type        | Description                                  |
| --------------------- | ----------- | -------------------------------------------- |
| `Ticket`              | `long`      | Order ticket.                                |
| `Symbol`              | `string`    | Symbol of the order.                         |
| `MaxUnrealizedPl`     | `double`    | Best floating P/L reached.                   |
| `MaxUnrealizedTime`   | `Timestamp` | Server time of the max (local).              |
| `MaxUnrealizedTimeUtc`| `Timestamp` | Server time of the max (UTC).                |
| `MinUnrealizedPl`     | `double`    | Worst floating P/L reached.                  |
| `MinUnrealizedTime`   | `Timestamp` | Server time of the min (local).              |
| `MinUnrealizedTimeUtc`| `Timestamp` | Server time of the min (UTC).                |

---

## 🎯 Purpose

Understand how far each open order swung in your favor and against you:

* Post-trade analysis of maximum adverse/favorable excursion (MAE/MFE).
* Tuning stop-loss / take-profit distances from real swings.
* Spotting orders that gave back large unrealized gains.

---

## 🧩 Notes & Tips

* **Open orders only.** Extrema are tracked for currently open orders; closed orders live in history (`OrdersHistory`).
* **Timestamps.** Prefer the `*Utc` fields for storage/comparison; convert local only for display.

---

## ⚠️ Pitfalls

* **Empty result.** With no open orders, `Extrema` is empty — not an error.
* **Fresh orders.** A just-opened order may show max ≈ min until the market moves.

---

## 🧪 Testing Suggestions

* **Happy path.** `Extrema.Count` equals the number of open orders; each `MaxUnrealizedPl >= MinUnrealizedPl`.
* **Edge cases.** Flat account → empty list; handle without dividing by zero in analytics.
