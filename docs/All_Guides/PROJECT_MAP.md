# 🗺️ CSharpMT4 Project Map

> The whole project as one annotated tree, with links. **Links point to section *overviews*, never to individual method pages** — open an overview to drill down into its methods.

---

## 🧱 The three layers

CSharpMT4 is built in three decoupled layers. Your code can enter at any level.

| Layer | Source | What it does | Overview |
| ----- | ------ | ------------ | -------- |
| **3 · MT4Sugar** | `MT4Sugar.cs` | Convenience on top of Service: risk-based sizing (`CalcVolumeForRiskAsync`, `BuyMarketByRiskAsync`), price/volume normalization, point-offset helpers, batch close/cancel. | [MT4Sugar overview](../MT4Sugar/MT4Sugar.Overview.md) · [index](../API_Reference/MT4Sugar.md) |
| **2 · MT4Service** | `MT4Service.cs` | Typed facade: clean signatures that return ready data structures (no raw proto). What most code uses. | [MT4Service overview](../MT4Service/MT4Service.Overview.md) · [index](../API_Reference/MT4Service.md) |
| **1 · MT4Account** | `package/Helpers/MT4Account.cs` | Low-level gRPC: proto request/reply, connect/session, auto-reconnect, streaming. Everything else builds on it. | [MT4Account overview](../MT5Account/MT4Account.Master.Overview.md) · [index](../API_Reference/MT4Account.md) |

```
Your code  →  MT4Sugar  →  MT4Service  →  MT4Account  →  gRPC  →  MetaRPC gateway → MT4 terminal
```

---

## 🌳 Repository tree

- **`CSharpMT4/`** — repo root (this is the examples/demo project; the SDK itself ships as the `MetaRPC.MT4` NuGet package)
  - `Program.cs` — entry point & example menu → [Getting Started → How to Run](GETTING_STARTED.md#how-to-run)
  - `appsettings.json` — connection config (`MT4Options`) → [Getting Started → Configuration](GETTING_STARTED.md#configuration-provide-your-connection-data)
  - `MT4Service.cs` — **Layer 2** source → [MT4Service overview](../MT4Service/MT4Service.Overview.md)
  - `MT4Sugar.cs` — **Layer 3** source → [MT4Sugar overview](../MT4Sugar/MT4Sugar.Overview.md)
  - `MetaRPC.CSharpMT4.csproj` / `.sln` — project & solution files
  - **`package/`** — the published SDK source (`MetaRPC.MT4`) this repo references
    - **`Helpers/`** — `MT4Account.cs` (**Layer 1**) → [MT4Account overview](../MT5Account/MT4Account.Master.Overview.md); `ApiExceptionMT4`, connection & reconnect helpers
    - `Mt4-term-api-*.cs`, `Mt4TermApi*.cs` — generated gRPC / protobuf stubs (the raw contract)
  - **`Helpers/`** — app-side helpers used by the examples
    - `ConnectionHelper.cs` — one-call connect/disconnect bootstrap
    - `EnvConfig.cs` — config loader (env → appsettings → defaults) → [Configuration](GETTING_STARTED.md#configuration-provide-your-connection-data)
    - `Mt4ConnectHelper.cs` — two-phase (KICK → WAIT) connect
    - `Mt4Options.cs` · `Retry.cs` · `RpcDiagnostics.cs` · `NetworkDiag.cs`
  - **`examples/`** — runnable demos (the numbered menu)
    - **`LowLevel/`** → [MT4Account overview](../MT5Account/MT4Account.Master.Overview.md)
      - `1_LowLevel.cs` · `2_Trading.cs` · `3_Streaming.cs`
    - **`Services/`** → [MT4Service overview](../MT4Service/MT4Service.Overview.md)
      - `4_ServiceExamples.cs`
    - **`Sugar/`** → [MT4Sugar overview](../MT4Sugar/MT4Sugar.Overview.md)
      - `5_SugarScenario.cs`
    - **`Orchestrators/`** → [Strategies overview](../Strategies/Strategies.Master.Overview.md)
      - `6_ScalpOrchestrator.cs` · `7_TrailingStopOrchestrator.cs` · `8_GridOrchestrator.cs` · `9_BreakoutOrchestrator.cs`
    - **`Presets/`** → [Strategies overview](../Strategies/Strategies.Master.Overview.md)
      - `10_AdaptiveModePreset.cs`
    - **`UserCode/`** → [UserCode Sandbox](USERCODE_SANDBOX_GUIDE.md)
      - `11_ProgramUserCode.cs`
    - **`Tools/`** → [Protobuf Inspector](PROTOBUF_INSPECTOR_GUIDE.md)
      - `12_ProtobufInspector.cs`
  - **`docs/`** — this documentation site (see the map below)
  - `mkdocs.yml` — docs site config · `.github/workflows/` — CI

---

## 📚 Documentation tree

- **`docs/`**
  - [Home](../index.md) — landing page
  - **`All_Guides/`** — guides
    - [Getting Started](GETTING_STARTED.md) — install, configure, run
    - [Your First Project](Your_First_Project.md) — working project in 10 minutes
    - [Project Map](PROJECT_MAP.md) — *you are here*
    - [MT4 for Beginners](MT4_For_Beginners.md) — demo account & terminal basics
    - [Glossary](GLOSSARY.md) — terms
    - [Sync vs Async](SYNC_VS_ASYNC.md) — the method model
    - [gRPC Streaming](GRPC_STREAM_MANAGEMENT.md) — subscriptions & cancellation
    - [ENUMs Reference](ENUMS_USAGE_REFERENCE.md) — input/output enums
    - [Return Codes](RETURN_CODES_REFERENCE.md) — error handling
    - [UserCode Sandbox](USERCODE_SANDBOX_GUIDE.md) — your own scratch example
    - [Protobuf Inspector](PROTOBUF_INSPECTOR_GUIDE.md) — explore the proto types
  - **`API_Reference/`** — generated method indexes (from source)
    - [MT4Account](../API_Reference/MT4Account.md) · [MT4Service](../API_Reference/MT4Service.md) · [MT4Sugar](../API_Reference/MT4Sugar.md)
  - **`MT5Account/`** — MT4Account docs → [overview](../MT5Account/MT4Account.Master.Overview.md) *(per-method pages inside)*
  - **`MT4Service/`** — MT4Service docs → [overview](../MT4Service/MT4Service.Overview.md) *(per-group pages inside)*
  - **`MT4Sugar/`** — MT4Sugar docs → [overview](../MT4Sugar/MT4Sugar.Overview.md) *(per-method pages inside)*
  - **`Strategies/`** — orchestrators & presets → [overview](../Strategies/Strategies.Master.Overview.md) *(per-strategy pages inside)*
  - `Guide_Images/` · `styles/` · `javascripts/` · `includes/` — assets & theme

> The `MT5Account/`, `MT4Service/`, `MT4Sugar/` and `Strategies/` folders hold per-method / per-strategy pages. By design this map links only their **overviews** — each overview is the index into its own detail pages.

---

## 🎯 Which layer should you use?

- **Most trading apps → [MT4Sugar](../MT4Sugar/MT4Sugar.Overview.md).** Risk sizing and auto-normalization protect you from off-quote and invalid-volume rejections.
- **Custom execution → [MT4Service](../MT4Service/MT4Service.Overview.md).** Typed calls when you manage your own sizing/normalization.
- **Protocol-level control → [MT4Account](../MT5Account/MT4Account.Master.Overview.md).** Raw proto structures and streams.
