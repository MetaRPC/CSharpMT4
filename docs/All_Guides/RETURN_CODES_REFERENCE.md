# Return Codes & Error Handling

> How CSharpMT4 reports success and failure. **MT4 does not use MT5's numeric `returned_code` / `10009`.** Instead, calls either return data (success) or **throw `ApiExceptionMT4`** (failure), carrying classic MT4/MQL error codes.

---

## ✅ How success is signalled

* **Data methods** (`AccountSummaryAsync`, `QuoteAsync`, `OpenedOrdersAsync`, …) — return their typed `*Data` object. No exception = success.
* **`OrderSendAsync`** — success is a **positive `Ticket`**:

  ```csharp
  var r = await service.OrderSendAsync(req);
  if (r.Ticket > 0) Console.WriteLine($"opened #{r.Ticket} @ {r.Price}");
  ```
* **`OrderModifyAsync`** — `OrderModifyData.OrderWasModified == true`.
* **`OrderCloseDeleteAsync`** — succeeds (no throw); `Mode` says whether a market order was *closed* or a pending one *deleted* (`OrderCloseDeleteMode`).

> There is **no** "check a numeric retcode" step like in MT5. If the gateway/broker rejects the operation, the call **throws** — wrap trading in `try/catch`.

---

## ⚠️ Failures — `ApiExceptionMT4`

Any gateway/terminal/broker error is raised as **`ApiExceptionMT4`**. It exposes:

| Property | Type | Meaning |
| -------- | ---- | ------- |
| `Error` | `Error` | The raw proto error object. |
| `ErrorType` | `ErrorType` | Where it came from (see below). |
| `ErrorCode` | `string?` | Gateway error code (string). |
| `ErrorMessageText` | `string?` | Human-readable message. |
| `MqlErrorCode` | `MqlErrorCode` | Classic MT4/MQL error (enum, see below). |
| `MqlErrorIntCode` | `int` | Same error as a raw number. |
| `MqlErrorDescription` | `string?` | MT4 error description. |

```csharp
try
{
    var r = await service.OrderSendAsync(req);
    Console.WriteLine($"opened #{r.Ticket}");
}
catch (ApiExceptionMT4 ex)
{
    Console.WriteLine($"rejected: {ex.MqlErrorCode} ({ex.MqlErrorIntCode}) — {ex.MqlErrorDescription ?? ex.ErrorMessageText}");
}
```

### `ErrorType` — error category

* `Undefined = 0`
* `Mrpc = 1` · `MrpcValidation = 2` · `MrpcTimeout = 3` — gateway-side (request shape, validation, timeout)
* `TerminalApiTimeout = 4` · `TerminalApiGetParams = 5` · `TerminalApiSetResult = 6` · `TerminalApiUnexpected = 7` — terminal bridge
* `MqlExecution = 8` · `MqlCustomExecution = 9` — the MQL/trade server rejected it (read `MqlErrorCode`)

---

## 📋 Common MT4 trade errors (`MqlErrorCode`)

These are the classic MT4 server/trade codes you'll actually hit. The full enum has 100+ members; the trade-relevant ones:

| Code | Member | Meaning / typical cause |
| ---- | ------ | ----------------------- |
| 0 | `ErrNoError` | success |
| 2 | `ErrCommonError` | generic error |
| 3 | `ErrInvalidTradeParameters` | bad request fields |
| 4 | `ErrServerBusy` | server busy — retry later |
| 6 | `ErrNoConnection` | no connection to trade server |
| 8 | `ErrTooFrequentRequests` | throttled — slow down |
| 64 | `ErrAccountDisabled` | account disabled |
| 65 | `ErrInvalidAccount` | invalid account |
| 128 | `ErrTradeTimeout` | trade request timed out |
| 129 | `ErrInvalidPrice` | price not valid / stale |
| 130 | `ErrInvalidStops` | SL/TP too close or wrong side |
| 131 | `ErrInvalidTradeVolume` | volume not a valid lot step |
| 132 | `ErrMarketClosed` | market closed |
| 133 | `ErrTradeDisabled` | trading disabled for symbol/account |
| 134 | `ErrNotEnoughMoney` | insufficient free margin |
| 135 | `ErrPriceChanged` | price moved — re-send |
| 136 | `ErrOffQuotes` | no quotes available |
| 137 | `ErrBrokerBusy` | broker busy |
| 138 | `ErrRequote` | requote — price changed between quote and send |
| 139 | `ErrOrderLocked` | order is locked/being processed |
| 141 | `ErrTooManyRequests` | too many requests |
| 145 | `ErrTradeModifyDenied` | modification denied (e.g. too close to market) |
| 146 | `ErrTradeContextBusy` | trade context busy — retry |
| 147 | `ErrTradeExpirationDenied` | pending expiration not allowed |
| 148 | `ErrTradeTooManyOrders` | order limit reached |

> Most of these are **broker/market conditions**, not SDK bugs. Validate inputs up front: round price to `GetDigitsAsync`, snap volume to `GetVolumeStepAsync`, respect `GetVolumeMinAsync`, and keep SL/TP beyond the broker's minimum stop distance.

---

## 🔁 Transient errors the SDK handles for you

You generally **don't** catch these — `MT4Account` retries them automatically:

* **gRPC transients:** `Unavailable`, `DeadlineExceeded`, `Internal` — retried with exponential backoff.
* **Reconnectable terminal errors:** e.g. `TERMINAL_INSTANCE_NOT_FOUND`, `TERMINAL_REGISTRY_TERMINAL_NOT_FOUND`, dead-terminal / readiness-probe conditions — the client reconnects and retries.
* **Stream `Cancelled`:** when *you* cancel a subscription's token, the `Cancelled` status is treated as a clean end, not an error.

See **[gRPC Streaming](GRPC_STREAM_MANAGEMENT.md)** for stream lifecycle details.

---

## 🧩 Handling pattern

```csharp
try
{
    var r = await service.BuyMarketByRiskAsync("EURUSD", stopPoints: 200, riskMoney: 50);
    Console.WriteLine($"opened #{r.Ticket}");
}
catch (ApiExceptionMT4 ex) when (ex.MqlErrorCode == MqlErrorCode.ErrRequote
                              || ex.MqlErrorCode == MqlErrorCode.ErrPriceChanged)
{
    // price moved — re-read the quote and try again
}
catch (ApiExceptionMT4 ex)
{
    logger.LogWarning("trade rejected: {Code} — {Msg}", ex.MqlErrorCode, ex.MqlErrorDescription);
}
```

* **Fail fast on logic errors** (invalid volume/stops) — fix the request, don't retry.
* **Retry briefly on transient trade errors** (`ErrRequote`, `ErrPriceChanged`, `ErrTradeContextBusy`, `ErrServerBusy`) with a fresh quote.
* **Surface account/market errors** (`ErrNotEnoughMoney`, `ErrMarketClosed`, `ErrTradeDisabled`) to the user — retrying won't help.
