# ✅ Getting an Account Summary

> **Request:** full account summary (`AccountSummaryData`) from **MT4**. Fetch all core account metrics in a single call.

**API Information:**

* **SDK wrapper:** `MT4Account.AccountSummaryAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.AccountHelper`
* **Proto definition:** `AccountSummary` (account-helper proto)

### RPC

* **Service:** `mt4_term_api.AccountHelper`
* **Method:** `AccountSummary(AccountSummaryRequest) → AccountSummaryReply`
* **Low‑level client (generated):** `AccountHelper.AccountHelperClient.AccountSummaryAsync(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<AccountSummaryData> AccountSummaryAsync(
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`AccountSummaryRequest {}`  *(no fields)*

**Reply message:**

`AccountSummaryReply { data: AccountSummaryData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
// MT4Service returns the data; you decide how to present it.
var s = await _service.AccountSummaryAsync();
_logger.LogInformation("Balance={Balance}, Equity={Equity}, Currency={Currency}",
    s.AccountBalance, s.AccountEquity, s.AccountCurrency);

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // keep short; bump if your terminal is slow
var summary = await _mt4.AccountSummaryAsync(
    deadline: null,
    cancellationToken: cts.Token);

_logger.LogInformation("Account Summary: Balance={Balance}, Equity={Equity}, Currency={Currency}",
    summary.AccountBalance,
    summary.AccountEquity,
    summary.AccountCurrency);
```

---

## 🔽Input

No required input parameters.

Optional:

* **`deadline`** (`DateTime?`) — optional UTC deadline for request timeout.
* **`cancellationToken`** (`CancellationToken`) — token to cancel the operation.

---

## ⬆️Output

Returns an **`AccountSummaryData`** structure with the following fields:

| Field                | Type                    | Description                                       |
| -------------------- | ----------------------- | ------------------------------------------------- |
| `AccountLogin`       | `long`                  | Account number (login ID).                        |
| `AccountBalance`     | `double`                | Account balance excluding open positions.         |
| `AccountEquity`      | `double`                | Equity — balance including floating P/L.          |
| `AccountMargin`      | `double`                | Currently used margin.                            |
| `AccountMarginFree`  | `double`                | Free margin available for opening new trades.     |
| `AccountMarginLevel` | `double`                | Margin level, percent = (Equity / Margin) × 100.  |
| `AccountCredit`      | `double`                | Credit amount.                                    |
| `AccountCurrency`    | `string`                | Account deposit currency (e.g. `"USD"`, `"EUR"`). |
| `AccountLeverage`    | `long`                  | Leverage applied to the account.                  |
| `AccountUserName`    | `string`                | Account holder's name.                            |
| `AccountCompanyName` | `string`                | Broker's name or company.                         |
| `AccountTradeMode`   | `EnumAccountTradeMode`  | Demo / contest / real — see enum below.           |
| `AccountMarginMode`  | `EnumAccountMarginMode` | Netting / exchange / hedging — see enum below.    |

---

## 🧱 Related enums (from proto)

### `EnumAccountTradeMode` — field `AccountTradeMode`

* `AccountTradeModeDemo = 0` — demo / practice account
* `AccountTradeModeContest = 1` — contest account
* `AccountTradeModeReal = 2` — real (live) trading account

### `EnumAccountMarginMode` — field `AccountMarginMode`

* `AccountMarginModeRetailNetting = 0` — retail, netting (one net position per symbol)
* `AccountMarginModeExchange = 1` — exchange margin model
* `AccountMarginModeRetailHedging = 2` — retail, hedging (the usual MT4 model)

---

## 🎯Purpose

This method is used to retrieve real-time account information and is typically used for:

* Displaying account status in dashboards or UI
* Validating available margin and balance before trading
* Monitoring overall account exposure and risk

It's a foundational API call for any MT4 integration dealing with trading or reporting.

---

## 🧩 Notes & Tips

* **Connection required.** The call throws a clear error if not connected (guarded by `EnsureConnected()`). Connect via `ConnectByServerNameAsync` / `ConnectByHostPortAsync` first.
* **Timeouts.** Example uses a 3s `CancellationTokenSource` for snappy UX. If you don't pass a `deadline`, the library's default per-RPC timeout is **8s** (`DefaultRpcTimeout`).
* **Reconnect behavior.** Under the hood, unary calls use `ExecuteWithReconnect`: retries on transient gRPC statuses (`Unavailable`, `DeadlineExceeded`, `Internal`) and on reconnectable terminal errors (`TERMINAL_INSTANCE_NOT_FOUND`, `TERMINAL_REGISTRY_TERMINAL_NOT_FOUND`). Exponential backoff starts \~250ms with jitter (±150ms), capped at 5s, max attempts 8.
* **Snapshot semantics.** `AccountEquity`/`AccountBalance` are snapshots. Re-query right before risk checks or order placement.
* **Currencies.** `AccountCurrency` is your deposit currency; instrument P/L may be in the quote currency—don't mix them in math without converting.
* **Formatting.** Round for display only; keep raw doubles for calculations.

---

## ⚠️ Pitfalls

* **Stale terminal.** With a disconnected/paused terminal, values might be outdated without a hard server error. Log connection state alongside numbers.
* **Roll-over effects.** Swaps/commissions around roll-over can cause brief equity/balance divergences.
* **Type matching.** Use the exact generated C# types from the proto for arithmetic/serialization to avoid subtle overflow/rounding issues.

---

## 🧪 Testing Suggestions

* **Happy path.** Values are non-negative; currency is non-empty; equity ≈ balance on a flat account.
* **Edge cases.** With open positions, `equity != balance` and margins/free margins look reasonable.
* **Failure path.** Simulate terminal down/unavailable: expect a clear exception (no crash), and bounded retries/backoff visible in logs.
