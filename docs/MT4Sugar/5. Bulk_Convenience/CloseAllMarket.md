# MT4Sugar · `CloseAllMarketAsync`

> Close only **market** (buy/sell) orders; returns how many.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Closes only **market** (buy/sell) orders, leaving pending orders working. Returns the count.

## Signature

```csharp
Task<int> CloseAllMarketAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string?` | `null` | Restrict to one symbol, or `null` for the whole account. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`int` — how many market orders were closed.

## Example

```csharp
int n = await svc.CloseAllMarketAsync("EURUSD");
```

## Scenario

Before news: close open positions but keep working pendings.

```csharp
int positions = await svc.CloseAllMarketAsync("EURUSD");
Console.WriteLine($"closed {positions} position(s); pendings untouched");
```

## Notes

* The positions half of [`CloseAllAsync`](CloseAll.md); pendings via [`CancelAllPendingAsync`](CancelAllPending.md).

## See also

* [`CancelAllPendingAsync`](CancelAllPending.md) · [`CloseAllAsync`](CloseAll.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
