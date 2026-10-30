# Breakout Orchestrator

> A **breakout straddle**: a buy-stop above Ask and a sell-stop below Bid, each with SL/TP. Whichever side the market breaks fills; the orchestrator cancels the opposite pending and lets the position run. Flattens on timeout if nothing breaks.

* **Class:** `BreakoutOrchestrator` (`examples/Orchestrators/9_BreakoutOrchestrator.cs`)
* **Menu:** `9. ORCHESTRATOR: breakout` — `dotnet run -- --example=9`
* **Builds on:** [MT4Service](../../MT4Service/MT4Service.Overview.md) (`OrderSendAsync` with pending stops, `OpenedOrdersAsync`) + [MT4Sugar](../../MT4Sugar/MT4Sugar.Overview.md) (`PriceFromOffsetPoints`, `NormalizePrice`, `CancelAllPending`)

> 🛡️ **Described by default** (`Live = false`): prints the plan, sends nothing. Set `Live = true` to trade — see [Strategies overview](../Strategies.Master.Overview.md#described-by-default-how-to-go-live). Live places **real** orders.

---

## ⚙️ Parameters

| Property | Default | Meaning |
| -------- | ------- | ------- |
| `Symbol` | `EURUSD` | Trading symbol. |
| `DistancePoints` | `200` | How far above/below the market to place each stop. |
| `StopLossPoints` | `200` | SL distance from the entry. |
| `TakeProfitPoints` | `400` | TP distance from the entry. |
| `Volume` | `0.01` | Volume per side (auto-normalized). |
| `MaxWaitSeconds` | `60` | Wait this long for a breakout before flattening. |
| `PollSeconds` | `3` | Seconds between trigger checks. |
| `Magic` | `4002` | Magic-number tag. |
| `Live` | `false` | When true, actually trades. |

## 🔄 Flow

1. Compute `BUY_STOP` at `DistancePoints` above Ask and `SELL_STOP` the same below Bid, each with SL/TP.
2. **Live:** send both as `OrderSendAsync` (`OcOpBuystop` / `OcOpSellstop`).
3. Poll `OpenedOrdersAsync` every `PollSeconds`. When a **market** order (`OoOpBuy`/`OoOpSell`) appears, one side filled → `CancelAllPendingAsync` drops the opposite stop; the position runs to its SL/TP.
4. If nothing breaks within `MaxWaitSeconds` → cancel both pendings.

## ▶️ Example (described)

```
+== BREAKOUT ORCHESTRATOR =====================================+
  EURUSD bid=1,11738 ask=1,118  BUY_STOP @ 1,12 / SELL_STOP @ 1,11539  vol=0,01  SL=200pt TP=400pt
  [described] would: place both stops (OcOpBuystop / OcOpSellstop) with SL/TP,
  then poll for a fill, cancel the opposite pending, and flatten on timeout. Set Live=true to run.
```

## 🧩 Notes

* **One side wins.** As soon as a stop fills (becomes a market order), the opposite pending is cancelled so you're not left with both sides.
* **SL/TP are on the pending.** They travel with the order into the position — no second call needed after the fill.
* **Distance vs min stop.** `DistancePoints` must exceed the broker's minimum stop distance from market.
