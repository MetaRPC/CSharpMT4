# Getting Started with CSharpMT4

> **Quick Setup & Overview** - Start automating your trading with CSharpMT4 (C# for MetaTrader 4) in minutes.

---

## 🎯 What is CSharpMT4?

**CSharpMT4** is an industrial-grade, type-safe C# client library for interacting with **MetaTrader 4** terminals via high-performance **gRPC**. It eliminates complex C++ DLL wrappers and provides direct, reliable programmatic trading.

### 🌟 Key Advantages
-  **High Throughput**: Native gRPC streaming for sub-millisecond price ticks and trade execution.
-  **Three-Layer Architecture**: Low-level gRPC (`MT4Account`), typed wrapper methods (`MT4Service`), and high-level convenience (`MT4Sugar`).
-  **Resilient Connection**: Auto-reconnect, exponential backoff, and transparent channel healing.
-  **Production Ready**: Fully verified across institutional accounts, hedge fund systems, and automated retail bots.

---

## 📦 Installation

Install CSharpMT4 via your standard C# package manager:

```bash
dotnet add package MetaRPC.MT4
```

This pulls the **published NuGet package** `MetaRPC.MT4` from nuget.org — the generated gRPC clients plus the `MT4Account` low-level wrapper. That is all your own app needs.

> ℹ️ **This repository is the examples/demo project, not the package itself.** It references the SDK from the local `package/` folder (`<ProjectReference ... package\MetaRPC.MT4.csproj />`), so it compiles against that source. Any local edit here lives in this repo only — it is **not** in what `dotnet add package` downloads unless it is published as a new NuGet version. Your own project should just `dotnet add package MetaRPC.MT4`.

---

## 🔑 API Key & Authentication

Connecting to MetaRPC production endpoints (`mt4.mrpc.pro:443`) requires an API key:

1. **Sign Up**: Create an account for free at [https://mrpc.pro/signup](https://mrpc.pro/signup).
2. **Generate API Key**: In your MetaRPC Portal dashboard at [https://mrpc.pro/my](https://mrpc.pro/my), go to **API Keys** to generate and copy your personal API token.
3. **Configure Connection**: Pass your API key / token along with the server address (`mt4.mrpc.pro:443`) in your connection settings.

---

## ⚙️ Configuration — provide your connection data

The SDK **requires** your connection settings. It reads them in this order and falls back to the built-in defaults **only if none are set**:

**1) Command line / environment variables (highest priority)**

* `--api-key <key>` (or the first positional argument) → API key.
* `MRPC_API_KEY` → API key.
* `MT4Options__User`, `MT4Options__Password`, `MT4Options__ServerName`, `MT4Options__Grpc`, `MT4Options__Symbol`, `MT4Options__ApiKey` → any setting (the double underscore is the nested-key separator).

**2) `appsettings.json` → section `MT4Options`**

```json
{
  "MT4Options": {
    "User": 0,
    "Password": "<your password>",
    "ServerName": "<broker server, e.g. MetaQuotes-Demo>",
    "Grpc": "https://mt4.mrpc.pro:443",
    "ApiKey": "<your API key>",
    "Symbol": "EURUSD"
  }
}
```

**3) Built-in defaults (last resort)** — `Grpc` → **production** `https://mt4.mrpc.pro:443`, `ApiKey` → `TRIAL`.

> ⚠️ **Set `Grpc` explicitly for any non-production environment.** If you test against a different MetaRPC endpoint (for example staging `https://mt4-stg.mrpc.pro:443`) and do **not** set `Grpc`, the SDK silently falls back to **production** and the terminal won't connect where you expect. Always provide `User`, `Password`, `ServerName`, `Grpc` and `ApiKey` — via environment or `appsettings.json` — **before** connecting.

> If `User` is `0` or the password is blank, the SDK tries to **auto-provision a demo** on the configured endpoint; that requires a non-`TRIAL` API key.

---

## ▶️ How to Run

There are **two different scenarios**, and it matters which one you're in:

| | **A. Run this repository** | **B. Use the SDK in your own project** |
| --- | --- | --- |
| **Goal** | Explore the SDK — see every call run, try the strategies | Build your own bot |
| **You get it by** | `git clone` this repo | `dotnet add package MetaRPC.MT4` |
| **Entry point** | the example menu (12 ready-made demos) | your own `Program.cs` |
| **Config** | `appsettings.json` / env (see above) | however you like — constructor args, env, your own config |
| **The `--example` / menu flags** | ✅ live here | ❌ not part of the package |

### A. Run this repository (the examples / demo project)

Clone, then run. On start it loads your [configuration](#configuration-provide-your-connection-data), connects, and shows a menu of 12 demos (the three API tiers, the orchestrators, the preset, the sandbox, and the protobuf inspector).

```bash
git clone https://github.com/MetaRPC/CSharpMT4.git
cd CSharpMT4
dotnet run                      # interactive menu (pick 1–12, a = all, q = quit)
```

The menu that appears:

| # | Entry | What it runs |
| :-: | ----- | ------------ |
| 1 | LOW-LEVEL: info | `MT4Account` read calls — account, symbols, orders |
| 2 | LOW-LEVEL: trading | `MT4Account` send/modify/close flow (guarded — described unless enabled) |
| 3 | LOW-LEVEL: streaming | All five `MT4Account` streams, sampled a few seconds each |
| 4 | SERVICE suite (all) | Every `MT4Service` group, read-only |
| 5 | SUGAR scenario | `MT4Sugar` assess → plan → execute lifecycle (guarded) |
| 6 | ORCHESTRATOR: scalp | Risk-sized entry + TP + timed exit (described) |
| 7 | ORCHESTRATOR: trailing stop | Entry protected by a trailing stop (described) |
| 8 | ORCHESTRATOR: grid | Symmetric pending-order grid (described) |
| 9 | ORCHESTRATOR: breakout | Buy/sell-stop straddle (described) |
| 10 | PRESET: adaptive mode | Picks scalp or trailing from live conditions (described) |
| 11 | USER CODE sandbox | Your own scratch example, pre-connected |
| 12 | PROTOBUF inspector | Explore the generated proto types (no connection needed) |
| **a** | Run ALL | Run every entry in order |
| **q** | Quit | Exit |

> The orchestrators, preset and sugar/trading demos are **described (dry-run) by default** — they print their plan and send nothing until you turn trading on. See [Strategies](../Strategies/Strategies.Master.Overview.md#described-by-default-how-to-go-live).

Pick demos non-interactively instead of using the menu — everything after `--` is passed to the program, not to `dotnet run`:

```bash
dotnet run -- --example=1       # one demo by number (1–12)
dotnet run -- --example=sugar   # ...or by a substring of its name
dotnet run -- --all             # run the whole suite in order
dotnet run -- --inspect         # protobuf inspector only — no connection needed
dotnet run -- --api-key <KEY>   # supply the API key on the command line
```

> ℹ️ Piped / redirected stdin (CI) runs the whole suite automatically, so nothing hangs waiting for menu input.

### B. Use the SDK in your own project

You do **not** clone this repo. Add the package, write your own entry point (the [Minimal Connection Example](#minimal-connection-example) below), and run it:

```bash
dotnet add package MetaRPC.MT4
dotnet run
```

There is **no menu** and `appsettings.json` is **not required** — you pass `user`, `password`, `serverName`, `grpcServer` and `apiKey` into `new MT4Account(...)` however your app prefers. The example menu and the `--example` flags belong to this repository only.

### What the package actually gives you

`dotnet add package MetaRPC.MT4` pulls three layers stacked on the raw protocol:

```
MT4Sugar    ← high-level convenience (risk sizing, normalization, one-liners)
MT4Service  ← typed, data-returning wrappers
MT4Account  ← low-level gRPC calls (connect, session id, retries, streams)
────────────────────────────────────────────────────────────────────────
generated gRPC / protobuf stubs   ← the raw proto contract
```

You normally call `MT4Account` / `MT4Service` / `MT4Sugar`. You **can** drop down to the generated protobuf clients directly, but the wrappers exist precisely so you don't have to hand-build requests, attach the `APIKey`/session-`id` headers, or write reconnect logic yourself. Cloning this repo gives you those layers **plus** the runnable examples; adding the package gives you the layers only.

---

## 🆔 Automatic Account ID & Authentication

MetaRPC endpoints require authentication and session management:

1. **`APIKey`**: Your personal authentication token from [https://mrpc.pro/my](https://mrpc.pro/my) (obtained by registering at [https://mrpc.pro/signup](https://mrpc.pro/signup)). Sent in the `APIKey` header.

2. **`id`**: A terminal session GUID returned by `Connect` / `ConnectEx` (`terminalInstanceGuid`).

> 💡 **Seamless Automation**: You do not need to call `GetId` or provide an `id` header when connecting. The server automatically generates a session GUID upon connection and returns it to the caller. The SDK automatically captures this session ID and attaches it alongside your `APIKey` to all subsequent requests and streaming subscriptions.

## 🔌 Minimal Connection Example

Here is how easy it is to initialize `MT4Account`, connect to your MetaTrader terminal, and retrieve your account balance:

```csharp
var account = new MT4Account(user, password, grpcServer, apiKey);
// defaults: waitForTerminalIsAlive = true, timeoutSeconds = 30
await account.ConnectByServerNameAsync(serverName, "EURUSD");
var summary = await account.AccountSummaryAsync();
Console.WriteLine($"Balance: {summary.AccountBalance}, Equity: {summary.AccountEquity}");
```

---

## 🗺️ Documentation Road Map

To get the most out of CSharpMT4, follow this suggested reading order:

1.  **[Your First Project](Your_First_Project.md)** - Build and run a working project in 10 minutes.
2.  **[Project Map](PROJECT_MAP.md)** - Understand the 3 architectural layers and interaction flow.
3.  **[Glossary](GLOSSARY.md)** - Essential MetaTrader 4 and algorithmic trading terminology.
4.  **[MT4 for Beginners](MT4_For_Beginners.md)** - Step-by-step terminal setup and demo account guide.
5.  **[gRPC Streaming](GRPC_STREAM_MANAGEMENT.md)** - Subscribe to ticks, trades, DOM, and position updates.
6.  **[Return Codes](RETURN_CODES_REFERENCE.md)** - Complete reference of broker and gateway return codes.
