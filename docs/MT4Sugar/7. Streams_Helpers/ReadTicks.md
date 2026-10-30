# MT4Sugar · `ReadTicks`

> Live tick stream for a single symbol (thin alias over `OnSymbolTickAsync`).

→ extension method on **[`MT4Service`](../../MT4Service/Streaming_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Live tick stream for one symbol as an `IAsyncEnumerable` — consume with `await foreach`, stop via the `CancellationToken`. A thin reader over the service's tick subscription.

## Signature

```csharp
IAsyncEnumerable<OnSymbolTickData> ReadTicks(string symbol, CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `symbol` | `string` | — | Instrument, e.g. `"EURUSD"`. |
| `ct` | `CancellationToken` | `default` | Cancellation token — your off-switch for the stream. |

## Returns

`IAsyncEnumerable<OnSymbolTickData>` — each item's `.SymbolTick` has `Symbol`, `Bid`, `Ask`, `Time`.

## Example

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
await foreach (var t in svc.ReadTicks("EURUSD", cts.Token))
    Console.WriteLine($"{t.SymbolTick?.Bid}/{t.SymbolTick?.Ask}");
```

## Scenario

Watch price for a few seconds, acting on each tick, then stop via the token.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await foreach (var t in svc.ReadTicks("EURUSD", cts.Token))
        if (t.SymbolTick.Bid > 1.1000) { Console.WriteLine("level hit"); break; }
}
catch (OperationCanceledException) { }
```

## Notes

* **Cancel to stop.** Use a `CancellationTokenSource` with `CancelAfter`, and catch `OperationCanceledException`.
* Full lifecycle & reconnect behavior: [gRPC Streaming](../../All_Guides/GRPC_STREAM_MANAGEMENT.md).

## See also

* [`ReadTrades`](ReadTrades.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Subscriptions](../../MT4Service/Streaming_Methods.md)
