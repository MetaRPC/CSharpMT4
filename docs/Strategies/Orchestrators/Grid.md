# Grid Orchestrator

> A symmetric **pending-order grid** around the current price — buy-limits below Ask and sell-limits above Bid, several levels each at a fixed spacing. A range / mean-reversion scaffold.

* **Class:** `GridOrchestrator` (`examples/Orchestrators/8_GridOrchestrator.cs`)
* **Menu:** `8. ORCHESTRATOR: grid` — `dotnet run -- --example=8`
* **Builds on:** [MT4Service](../../MT4Service/MT4Service.Overview.md) (`OrderSendAsync` with pending types) + [MT4Sugar](../../MT4Sugar/MT4Sugar.Overview.md) (`PriceFromOffsetPoints`, `NormalizeVolume`, `CancelAllPending`)

> 🛡️ **Described by default** (`Live = false`): prints the grid, sends nothing. Set `Live = true` to place it — see [Strategies overview](../Strategies.Master.Overview.md#described-by-default-how-to-go-live). Live places **real** pending orders.

---

## ⚙️ Parameters

| Property | Default | Meaning |
| -------- | ------- | ------- |
| `Symbol` | `EURUSD` | Trading symbol. |
| `LevelsPerSide` | `3` | Number of pending levels on each side. |
| `SpacingPoints` | `150` | Spacing between levels, in points. |
| `Volume` | `0.01` | Volume per pending order (auto-normalized). |
| `Magic` | `4001` | Magic-number tag for the grid. |
| `Live` | `false` | When true, actually places orders. |

## 🔄 Flow

1. Snapshot the symbol; normalize the volume.
2. For each level `i = 1..LevelsPerSide`: compute `BUY_LIMIT` at `i × SpacingPoints` below Ask and `SELL_LIMIT` the same above Bid (`PriceFromOffsetPoints`).
3. **Live:** send each as `OrderSendAsync` with `OcOpBuylimit` / `OcOpSelllimit`.
4. Demo cleanup: `CancelAllPendingAsync(Symbol)` clears the grid it just set.

## ▶️ Example (described)

```
+== GRID ORCHESTRATOR =========================================+
  EURUSD bid=1,11736 ask=1,11776  3 level(s)/side @ 150pt, vol=0,01
  [described] L1: BUY_LIMIT @ 1,11626   SELL_LIMIT @ 1,11886
  [described] L2: BUY_LIMIT @ 1,11476   SELL_LIMIT @ 1,12036
  [described] L3: BUY_LIMIT @ 1,11326   SELL_LIMIT @ 1,12186
  Set Live=true to place the grid (OcOpBuylimit / OcOpSelllimit), then CancelAllPendingAsync to clear.
```

## 🧩 Notes

* **Uses pending orders directly.** Sugar has no pending helper, so the grid builds `OrderSendRequest` with pending `OperationType` — see [Enums](../../All_Guides/ENUMS_USAGE_REFERENCE.md#orders-trading).
* **Self-cleaning demo.** The example cancels its own pendings after placing them; a real grid would leave them working and manage fills.
* **Spacing vs min stop.** Keep `SpacingPoints` beyond the broker's minimum distance from market, or limits closest to price may be rejected.
