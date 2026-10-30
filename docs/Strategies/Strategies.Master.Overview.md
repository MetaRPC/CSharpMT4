# Strategies — Overview

> Ready-made **orchestrators** (single strategies) and a **preset** (an adaptive wrapper that picks one), all built on top of [MT4Service](../MT4Service/MT4Service.Overview.md) and [MT4Sugar](../MT4Sugar/MT4Sugar.Overview.md). Use them as runnable examples and as a starting point for your own logic.

---

## 🛡️ Described by default — how to go live

**Every orchestrator and the preset ship guarded.** With `Live = false` (the default) they read the market, compute the plan and **print** it — **no orders are sent**. This is safe to run anywhere.

To actually trade, flip the flag:

* **In the menu** — set `Live = true` on the entry in `Program.cs` (lines ~107–111), e.g.
  `new GridOrchestrator(svc) { Symbol = sym, Live = true }`.
* **Programmatically** — set the property when you construct it: `new ScalpOrchestrator(svc) { Live = true, RiskMoney = 20 }`.

> ⚠️ **Live places real orders** on the configured account. Test on a demo you control — not on a shared account.

Run any of them from the examples menu (`dotnet run`) or directly: `dotnet run -- --example=<N>`.

---

## 🤖 Orchestrators (menu 6–9)

| # | Orchestrator | Idea | Instruments it uses |
| - | ------------ | ---- | ------------------- |
| 6 | **[Scalp](Orchestrators/Scalp.md)** | Risk-sized market entry + fixed TP, force-close after a hold | market order, `BuyMarketByRisk` |
| 7 | **[Trailing Stop](Orchestrators/Trailing.md)** | Risk-sized entry, ratchet the stop behind price | market order + `ModifySlTp` |
| 8 | **[Grid](Orchestrators/Grid.md)** | Symmetric pending-limit grid around price | pending limits (`OcOpBuylimit`/`OcOpSelllimit`) |
| 9 | **[Breakout](Orchestrators/Breakout.md)** | Buy-stop + sell-stop straddle, keep the side that fills | pending stops (`OcOpBuystop`/`OcOpSellstop`) |

Each is a small, parametrized class: construct it, set properties, `await ExecuteAsync(ct)`.

---

## 🧩 Presets (menu 10)

| # | Preset | Idea |
| - | ------ | ---- |
| 10 | **[Adaptive Mode](Presets/AdaptiveMode.md)** | Reads the market (spread as a volatility proxy) + a floating-loss safety cap, then dispatches to **Scalp** (calm) or **Trailing** (volatile) with tuned parameters. |

A preset is the next layer up: it **chooses** an orchestrator and its parameters for the current conditions.

---

## 🧱 Where they sit

```
MT4Service / MT4Sugar   ← the API the strategies call
      ↓
Orchestrators           ← one strategy each (Scalp, Trailing, Grid, Breakout)
      ↓
Presets                 ← pick + configure an orchestrator per market state
```

> MT4 note: these are intentionally bounded to what MT4 offers (orders, pending orders, no DOM). They favor **readable, guarded** flows over completeness — copy one and adapt it.
