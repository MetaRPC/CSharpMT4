# MT4Sugar · `GetSymbolSnapshotAsync`

> Top-of-book price **plus** the contract facts you need to trade a symbol — digits, point, spread and volume limits — in one object.

→ extension method on **[`MT4Service`](../../MT4Service/Symbol_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Reads the symbol once and bundles everything a sizing/pricing decision needs into a single [`SymbolSnapshot`](#returns): the live quote (Bid/Ask), the price precision (Digits/Point), the current SpreadPoints, and the tradable volume limits. It saves you from calling `QuoteAsync` plus several `Get*` getters and stitching the results together yourself. Use it as the **first step** of almost any trade: read the snapshot, then size and price from its fields.

## Signature

```csharp
Task<SymbolSnapshot> GetSymbolSnapshotAsync(string symbol, DateTime? deadline = null, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument to read, e.g. `"EURUSD"`. |
| `deadline` | `DateTime?` | `null` | Optional hard deadline for the underlying calls. |
| `ct` | `CancellationToken` | `default` | Cancellation token. |

## Returns

`SymbolSnapshot` — a compact value object:

| Field | Type | Meaning |
| ----- | ---- | ------- |
| `Symbol` | `string` | The symbol name. |
| `Bid` / `Ask` | `double` | Current top-of-book prices. |
| `Digits` | `int` | Price decimal places. |
| `Point` | `double` | Point size, `10^-Digits` (e.g. `0.00001` on 5 digits). |
| `SpreadPoints` | `double` | Current spread in points (`(Ask − Bid) / Point`). |
| `VolumeMin` / `VolumeMax` | `double` | Smallest / largest tradable volume (lots). |
| `VolumeStep` | `double` | Volume increment (lots). |

## Example

```csharp
var s = await svc.GetSymbolSnapshotAsync("EURUSD");
Console.WriteLine($"{s.Symbol} {s.Bid}/{s.Ask} digits={s.Digits} spread={s.SpreadPoints}pt vol[{s.VolumeMin}..{s.VolumeMax}/{s.VolumeStep}]");
```

## Scenario

Read the symbol once, then use its fields to filter and price an entry.

```csharp
var s = await svc.GetSymbolSnapshotAsync("EURUSD");

// 1) skip trading when the spread is abnormally wide
if (s.SpreadPoints > 25) { Console.WriteLine($"spread {s.SpreadPoints}pt too wide — skip"); return; }

// 2) price a 150-point stop off the snapshot, then enter at the minimum lot
double sl = s.Ask - 150 * s.Point;
await svc.BuyMarketAsync(s.Symbol, s.VolumeMin, sl: sl);
```

## Notes

* **Select first.** If the symbol isn't in Market Watch the quote may be empty — call [`EnsureSelectedAsync`](../2.%20Normalization_Utils/EnsureSelected.md) once beforehand.
* **Point vs pip.** `Point` is the raw price step; on 3/5-digit symbols 1 pip = 10 points (see [`PointsToPipsAsync`](../2.%20Normalization_Utils/PointsToPips.md)).
* **Snapshot, not a stream.** It's a one-shot read; for live updates use [`ReadTicks`](../7.%20Streams_Helpers/ReadTicks.md).

## See also

* [`GetAccountSnapshotAsync`](GetAccountSnapshot.md) — the account-side counterpart.
* [`NormalizeVolumeAsync`](../2.%20Normalization_Utils/NormalizeVolume.md) · [`PriceFromOffsetPointsAsync`](../2.%20Normalization_Utils/PriceFromOffsetPoints.md) — size and price from these fields.
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Symbol](../../MT4Service/Symbol_Methods.md)
