# Adaptive Mode Preset

> A preset is the layer above orchestrators: it **reads the market and picks one**. Adaptive Mode checks a floating-loss safety cap, then uses spread as a volatility proxy to dispatch to **Scalp** (calm) or **Trailing** (volatile) with tuned parameters.

* **Class:** `AdaptiveModePreset` (`examples/Presets/10_AdaptiveModePreset.cs`)
* **Menu:** `10. PRESET: adaptive mode` — `dotnet run -- --example=10`
* **Builds on:** the [Scalp](../Orchestrators/Scalp.md) and [Trailing](../Orchestrators/Trailing.md) orchestrators + [MT4Sugar](../../MT4Sugar/MT4Sugar.Overview.md) (`GetTotalProfitLossAsync`, `GetSymbolSnapshotAsync`)

> 🛡️ **Described by default** (`Live = false`): the chosen orchestrator only describes its plan. `Live` flows down to it — set `Live = true` to trade. See [Strategies overview](../Strategies.Master.Overview.md#described-by-default-how-to-go-live). Live places **real** orders.

---

## ⚙️ Parameters

| Property | Default | Meaning |
| -------- | ------- | ------- |
| `Symbol` | `EURUSD` | Trading symbol. |
| `CalmSpreadPoints` | `20` | Spread at/below which the market counts as **calm** → Scalp. |
| `RiskMoney` | `20` | Base money risk per trade. |
| `MaxFloatingLoss` | `-100` | Abort before trading if current floating P/L is worse than this. |
| `Live` | `false` | Flows down to the chosen orchestrator. |

## 🔄 Flow

1. **Safety cap.** Read floating P/L (`GetTotalProfitLossAsync`). If it's below `MaxFloatingLoss`, **stand down** — don't add risk on a bleeding book.
2. **Analyze.** Read the symbol snapshot; compare `SpreadPoints` to `CalmSpreadPoints`.
3. **Dispatch:**
   * **Calm** (spread ≤ cap) → `ScalpOrchestrator` (150pt stop, 300pt TP, 30s hold).
   * **Volatile** (spread > cap) → `TrailingStopOrchestrator` with **reduced risk** (`RiskMoney × 0.7`), wider stop and a trailing exit.
4. The chosen orchestrator runs (described or live per `Live`).

## ▶️ Example (described)

```
+== ADAPTIVE MODE PRESET =======================================+
  floating P/L = 10,58 (cap -100)
  EURUSD spread=1,0pt -> CALM
  mode: SCALP (tight spread)
+== SCALP ORCHESTRATOR =========================================+
  EURUSD BUY  risk=$20 over 150pt -> 0,13 lot(s)
  ...
```

## 🧩 Notes

* **Spread is a cheap proxy.** It's a stand-in for volatility; a production preset would use historical bar ranges.
* **Safety first.** The floating-loss cap runs **before** any analysis — it can veto the whole cycle.
* **Compose your own.** A preset is just "read state → choose orchestrator → set parameters". Copy this one and change the rules.
