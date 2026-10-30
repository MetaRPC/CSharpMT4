# MT4Sugar · `CloseAllAsync`

> Close/delete **all** open orders (optionally one symbol); returns how many were closed.

→ extension method on **[`MT4Service`](../../MT4Service/Trading_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Closes/deletes **every** open order (optionally just one symbol) and returns how many it acted on. The "flatten everything" button.

## Signature

```csharp
Task<int> CloseAllAsync(string? symbol = null, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string?` | `null` | Restrict to one symbol, or `null` for the whole account. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`int` — how many orders were closed/deleted.

## Example

```csharp
int closed = await svc.CloseAllAsync();          // everything
int e = await svc.CloseAllAsync("EURUSD");       // one symbol
```

## Scenario

Panic button: flatten everything on the account.

```csharp
int closed = await svc.CloseAllAsync();
Console.WriteLine($"flattened {closed} order(s)");
```

## Notes

* ⚠️ Acts on **all** orders — use deliberately.
* For one side only: [`CloseAllMarketAsync`](CloseAllMarket.md) (positions) or [`CancelAllPendingAsync`](CancelAllPending.md) (pendings).

## See also

* [`CloseAllMarketAsync`](CloseAllMarket.md) · [`CancelAllPendingAsync`](CancelAllPending.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Trading](../../MT4Service/Trading_Methods.md)
