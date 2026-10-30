# MT4Sugar · `GetTotalProfitLossAsync`

> Total floating profit of open orders (optionally one symbol).

→ extension method on **[`MT4Service`](../../MT4Service/Orders_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Total **floating** P/L across open orders (optionally one symbol). The usual input to a floating-loss safety cap.

## Signature

```csharp
Task<double> GetTotalProfitLossAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string?` | `null` | Restrict to one symbol, or `null` for the whole account. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`double` — summed floating P/L in deposit currency.

## Example

```csharp
double pl = await svc.GetTotalProfitLossAsync();
```

## Scenario

Floating-loss safety cap before adding risk (the pattern the Adaptive preset uses).

```csharp
double floating = await svc.GetTotalProfitLossAsync();
if (floating < -100) { Console.WriteLine($"floating {floating} below cap — stand down"); return; }
```

## Notes

* **Floating, not realized.** For closed trades use [`OrdersHistoryLastAsync`](OrdersHistoryLast.md).
* Negative values are losses — compare against a cap before adding risk.

## See also

* [`GetProfitableOrdersAsync`](GetProfitableOrders.md) · [`GetLosingOrdersAsync`](GetLosingOrders.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Positions & Orders](../../MT4Service/Orders_Methods.md)
