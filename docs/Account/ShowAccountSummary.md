# Getting an Account Summary

> **Request:** full account summary (`AccountSummaryData`) from MT4
> Fetch all core account metrics in a single call.

---

### Code Example

```csharp
var summary = await _mt4.AccountSummaryAsync();
_logger.LogInformation("Account Summary: Balance={Balance}, Equity={Equity}, Currency={Currency}",
    summary.AccountBalance,
    summary.AccountEquity,
    summary.AccountCurrency);
```

---

###  Method Signature

```csharp
Task<AccountSummaryData> AccountSummaryAsync(
    DateTime? deadline = null,
    CancellationToken cancellationToken = default
)
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

| Field               | Type     | Description                                       |
| ------------------- | -------- | ------------------------------------------------- |
| `AccountBalance`    | `double` | Account balance excluding open positions.         |
| `AccountEquity`     | `double` | Equity — balance including floating P/L.          |
| `AccountMargin`     | `double` | Currently used margin.                            |
| `AccountFreeMargin` | `double` | Free margin available for opening new trades.     |
| `AccountCurrency`   | `string` | Account deposit currency (e.g. `"USD"`, `"EUR"`). |
| `AccountLeverage`   | `int`    | Leverage applied to the account.                  |
| `AccountName`       | `string` | Account holder's name.                            |
| `AccountNumber`     | `int`    | Account number (login ID).                        |
| `Company`           | `string` | Broker's name or company.                         |

> *Note: Fields may vary slightly depending on MT4 implementation.*

---

## 🎯Purpose

This method is used to retrieve real-time account information and is typically used for:

* Displaying account status in dashboards or UI
* Validating available margin and balance before trading
* Monitoring overall account exposure and risk

It's a foundational API call for any MT4 integration dealing with trading or reporting.
