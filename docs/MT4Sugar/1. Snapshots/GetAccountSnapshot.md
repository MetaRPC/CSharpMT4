# MT4Sugar · `GetAccountSnapshotAsync`

> A compact account snapshot from a single `AccountSummary` round-trip.

→ extension method on **[`MT4Service`](../../MT4Service/Account_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Pulls the whole money side of the account in **one** `AccountSummary` round trip and packs it into an `AccountSnapshot`, so you get balance, equity, margin and currency together instead of several getter calls. A natural first step before deciding whether to trade.

## Signature

```csharp
Task<AccountSnapshot> GetAccountSnapshotAsync(DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`AccountSnapshot` — a compact value object:

| Field | Type | Meaning |
| ----- | ---- | ------- |
| `Login` | `long` | Account login. |
| `Balance` | `double` | Balance. |
| `Equity` | `double` | Equity (balance + floating P/L). |
| `Margin` | `double` | Used margin. |
| `FreeMargin` | `double` | Free margin available for new trades. |
| `MarginLevel` | `double` | Margin level, percent. |
| `Currency` | `string` | Deposit currency. |

## Example

```csharp
var a = await svc.GetAccountSnapshotAsync();
Console.WriteLine($"#{a.Login} {a.Currency} Bal={a.Balance} Eq={a.Equity} Free={a.FreeMargin}");
```

## Scenario

Gate trading on account health before sending any order.

```csharp
var a = await svc.GetAccountSnapshotAsync();
if (a.FreeMargin < 500 || a.MarginLevel < 200)
{
    Console.WriteLine($"Low margin (free={a.FreeMargin}, level={a.MarginLevel}%) — skip entry.");
    return;
}
Console.WriteLine($"OK to trade: equity {a.Equity} {a.Currency}");
```

## Notes

* **One round trip.** Cheaper than calling `GetBalance`/`GetEquity`/… separately.
* **Gate risk with it.** `FreeMargin` / `MarginLevel` make a good pre-trade safety check.

## See also

* [`GetSymbolSnapshotAsync`](GetSymbolSnapshot.md) — the symbol-side counterpart.
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Account](../../MT4Service/Account_Methods.md)
