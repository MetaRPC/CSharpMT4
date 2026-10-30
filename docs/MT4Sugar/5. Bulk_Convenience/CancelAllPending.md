# MT4Sugar · `CancelAllPendingAsync`

> Delete only **pending** (limit/stop) orders; returns how many.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Deletes only **pending** (limit/stop) orders, leaving open positions alone. Returns the count. Used to tear down a grid/straddle that didn't trigger.

## Signature

```csharp
Task<int> CancelAllPendingAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string?` | `null` | Restrict to one symbol, or `null` for the whole account. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`int` — how many pending orders were deleted.

## Example

```csharp
int n = await svc.CancelAllPendingAsync();
```

## Scenario

Tear down a pending grid/straddle that didn't trigger.

```csharp
int deleted = await svc.CancelAllPendingAsync("EURUSD");
Console.WriteLine($"cancelled {deleted} pending order(s)");
```

## Notes

* The pendings half of [`CloseAllAsync`](CloseAll.md); positions via [`CloseAllMarketAsync`](CloseAllMarket.md).

## See also

* [`CloseAllMarketAsync`](CloseAllMarket.md) · [`CloseAllAsync`](CloseAll.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
