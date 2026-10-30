# MT4Sugar · `EnsureSelectedAsync`

> Ensure the symbol is in Market Watch (`SymbolSelect` with select=true) before quoting/trading.

→ extension method on **[`MT4Service`](../../MT4Service/Symbol_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Makes sure the symbol is present in Market Watch (calls `SymbolSelect` with `select: true`). A symbol that isn't selected may return empty quotes and can't be streamed, so call this once before working with an unfamiliar symbol.

## Signature

```csharp
Task EnsureSelectedAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying call(s). |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`Task` — completes once the symbol is selected.

## Example

```csharp
await svc.EnsureSelectedAsync("EURUSD");
```

## Scenario

A symbol not in Market Watch returns no quotes — select it first.

```csharp
await svc.EnsureSelectedAsync("GBPJPY");
var q = await svc.QuoteAsync("GBPJPY");   // now returns a live quote
Console.WriteLine($"GBPJPY {q.Bid}/{q.Ask}");
```

## Notes

* **Idempotent.** Selecting an already-selected symbol is harmless.
* **Do it first.** Quotes/streams for an unselected symbol can come back empty.

## See also

* [`GetSymbolSnapshotAsync`](../1.%20Snapshots/GetSymbolSnapshot.md) — read the symbol after selecting it.
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Symbol](../../MT4Service/Symbol_Methods.md)
