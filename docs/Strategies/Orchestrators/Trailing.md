# Trailing-Stop Orchestrator

> Risk-sized market entry protected by a **trailing stop**: once price has moved far enough in favor, the stop ratchets to lock in gains, staying a fixed distance behind price. Exits on stop-out or timeout.

* **Class:** `TrailingStopOrchestrator` (`examples/Orchestrators/7_TrailingStopOrchestrator.cs`)
* **Menu:** `7. ORCHESTRATOR: trailing stop` — `dotnet run -- --example=7`
* **Builds on:** [MT4Sugar](../../MT4Sugar/MT4Sugar.Overview.md) (`BuyMarketByRiskAsync`, `ModifySlTpAsync`) + [MT4Service](../../MT4Service/MT4Service.Overview.md) (`QuoteAsync`, `OpenedOrdersTicketsAsync`)

> 🛡️ **Described by default** (`Live = false`): prints the plan, sends nothing. Set `Live = true` to trade — see [Strategies overview](../Strategies.Master.Overview.md#described-by-default-how-to-go-live). Live places **real** orders.

---

## ⚙️ Parameters

| Property | Default | Meaning |
| -------- | ------- | ------- |
| `Symbol` | `EURUSD` | Trading symbol. |
| `IsBuy` | `true` | Direction (BUY trails the stop up, SELL trails it down). |
| `RiskMoney` | `20` | Money risked over the initial stop. |
| `StopPoints` | `200` | Initial stop distance in points (and risk-sizing basis). |
| `TrailStartPoints` | `150` | Profit (points) required before the stop starts trailing. |
| `TrailDistancePoints` | `100` | Distance the trailing stop keeps behind price. |
| `MaxHoldSeconds` | `60` | Force-close after this long. |
| `PollSeconds` | `3` | Seconds between trailing checks. |
| `Live` | `false` | When true, actually trades. |

## 🔄 Flow

1. Risk-size and open a market order with an initial SL (`BuyMarketByRiskAsync` / `SellMarketByRiskAsync`).
2. Poll the quote every `PollSeconds`. Once profit ≥ `TrailStartPoints`, move the SL to `TrailDistancePoints` behind price **only if it improves** (`ModifySlTp`).
3. If the market takes out the trailing stop (ticket gone) → done.
4. On `MaxHoldSeconds` timeout → flatten (`CloseAllMarket`).

## ▶️ Example (described)

```
+== TRAILING-STOP ORCHESTRATOR =================================+
  EURUSD BUY  risk=$20 over 200pt -> 0,1 lot(s)
  trail: start after 150pt, keep 100pt behind; maxHold=60s
  [described] would: open-by-risk -> poll quote -> ratchet SL via ModifySlTp -> exit. Set Live=true to run.
```

## 🧩 Notes

* **Only tightens.** The stop never loosens — it moves to lock profit, never widens risk.
* **`TrailStartPoints` ≥ broker min stop.** Don't start trailing closer than the broker's minimum stop distance.
* **Poll cadence.** Faster `PollSeconds` reacts sooner but adds load; 2–5 s is typical.
