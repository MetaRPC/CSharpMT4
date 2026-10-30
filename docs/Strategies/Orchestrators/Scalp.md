# Scalp Orchestrator

> Risk-sized market entry with a protective stop and a fixed take-profit, held for a short window, then force-closed. A quick in-and-out scaffold.

* **Class:** `ScalpOrchestrator` (`examples/Orchestrators/6_ScalpOrchestrator.cs`)
* **Menu:** `6. ORCHESTRATOR: scalp` — `dotnet run -- --example=6`
* **Builds on:** [MT4Sugar](../../MT4Sugar/MT4Sugar.Overview.md) (`BuyMarketByRiskAsync`, `ModifySlTpAsync`, `CloseAllMarketAsync`)

> 🛡️ **Described by default** (`Live = false`): prints the plan, sends nothing. Set `Live = true` to trade — see [Strategies overview](../Strategies.Master.Overview.md#described-by-default-how-to-go-live). Live places **real** orders.

---

## ⚙️ Parameters

| Property | Default | Meaning |
| -------- | ------- | ------- |
| `Symbol` | `EURUSD` | Trading symbol. |
| `IsBuy` | `true` | Direction (true = BUY, false = SELL). |
| `RiskMoney` | `20` | Money risked over the stop distance (deposit currency). |
| `StopPoints` | `150` | Stop distance in points (also drives risk sizing). |
| `TakeProfitPoints` | `300` | Take-profit distance in points. |
| `MaxHoldSeconds` | `30` | Force-close after this long if SL/TP hasn't triggered. |
| `Live` | `false` | When true, actually trades. |

## 🔄 Flow

1. Snapshot the symbol; size the volume from `RiskMoney` over `StopPoints` (`CalcVolumeForRisk`).
2. **Live:** `BuyMarketByRiskAsync` / `SellMarketByRiskAsync` — opens with a protective SL `StopPoints` away.
3. Attach the take-profit (`ModifySlTp`).
4. Hold up to `MaxHoldSeconds`, checking whether it closed on its own (SL/TP hit).
5. Flatten any remainder (`CloseAllMarket`).

## ▶️ Example (described)

```
+== SCALP ORCHESTRATOR =========================================+
  EURUSD BUY  risk=$20 over 150pt -> 0,13 lot(s)
  SL=150pt  TP=300pt  maxHold=30s  spread=1,0pt
  [described] would: open-by-risk -> attach TP -> hold -> force-close. Set Live=true to run.
```

## 🧩 Notes

* **Risk-first sizing.** Volume comes from your money risk and the stop distance, not a fixed lot.
* **Tune for volatility.** Tight stops/short holds for calm markets; widen both when it's choppy.
* **Guarded.** Keep `Live = false` while you tune parameters.
