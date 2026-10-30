# MT4Sugar · `ReadTrades`

> Live trade-event stream (alias over `OnTradeAsync`).

→ extension method on **[`MT4Service`](../../MT4Service/Streaming_Methods.md)** — Sugar is just a readable shortcut over the service.

---

## What it does

Live trade-event stream (order create/modify/close) as an `IAsyncEnumerable`. Same consumption pattern as [`ReadTicks`](ReadTicks.md).

## Signature

```csharp
IAsyncEnumerable<OnTradeData> ReadTrades(CancellationToken ct = default)
```

## Parameters

| Parameter | Type | Default | Meaning |
| --------- | ---- | ------- | ------- |
| `ct` | `CancellationToken` | `default` | Cancellation token — your off-switch for the stream. |

## Returns

`IAsyncEnumerable<OnTradeData>` — one item per trade event on the account.

## Example

```csharp
await foreach (var e in svc.ReadTrades(ct))
    Console.WriteLine($"trade event: {e.Type}");
```

## Scenario

React to order events (fills/closes) as they happen.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try { await foreach (var ev in svc.ReadTrades(cts.Token)) Console.WriteLine($"trade event: {ev}"); }
catch (OperationCanceledException) { }
```

## Notes

* **Cancel to stop**, and catch `OperationCanceledException`.
* Full lifecycle: [gRPC Streaming](../../All_Guides/GRPC_STREAM_MANAGEMENT.md).

## See also

* [`ReadTicks`](ReadTicks.md)
* [MT4Sugar overview](../MT4Sugar.Overview.md) · [MT4Service · Subscriptions](../../MT4Service/Streaming_Methods.md)
