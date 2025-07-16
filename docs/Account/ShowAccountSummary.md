# Account Summary

> **Request:** retrieve general account information

Fetch a full snapshot of the trading account's current status, including balance, equity, margin, and account currency.

---

### Code Example

```csharp
await _service.ShowAccountSummary();

// Output:
// Balance: 10000, Equity: 9980, Currency: USD
```

---

### Method Signature

```csharp
Task<AccountSummaryData> AccountSummaryAsync(
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
```

---

## Input

This method requires no specific parameters, other than:

* **`deadline`** (`DateTime?`, optional): An optional UTC deadline for request timeout.
* **`cancellationToken`** (`CancellationToken`, optional): Token to cancel the operation if needed.

---

## Output

Returns an **`AccountSummaryData`** structure containing:

| Field               | Type     | Description                                      |
| ------------------- | -------- | ------------------------------------------------ |
| `AccountBalance`    | `double` | Account balance (excluding open positions).      |
| `AccountEquity`     | `double` | Equity — current balance including floating P/L. |
| `AccountMargin`     | `double` | Currently used margin.                           |
| `AccountFreeMargin` | `double` | Margin available for opening new positions.      |
| `AccountCurrency`   | `string` | Account currency (e.g. `"USD"`, `"EUR"`).        |
| `AccountLeverage`   | `int`    | Account leverage (e.g. 100, 500).                |
| `AccountName`       | `string` | Trader's name (if provided).                     |
| `AccountNumber`     | `int`    | Trading account number.                          |
| `Company`           | `string` | Broker's name or company managing the account.   |

> *Note: Some fields may vary depending on MT4 API implementation.*

---

## Purpose

This method allows retrieval of current account information to be used for:

* Displaying key data in dashboards or UIs
* Verifying balance and margin status before placing trades
* Monitoring risk and exposure in real time

It provides a foundational step for any trade- or account-related logic. ✨
