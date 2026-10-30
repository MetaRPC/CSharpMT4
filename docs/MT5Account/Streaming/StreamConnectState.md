# Streaming Terminal Connection State

> **Request:** subscribe to terminal connection-state events via `OnConnectState`.
> Emits a snapshot whenever the terminal's connectivity/liveness changes — a heartbeat for your UI.

**API Information:**

* **SDK wrapper:** `MT4Account.OnConnectStateAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.Connection`
* **Proto definition:** `OnConnectState` (in the Connection service)

### RPC

* **Service:** `mt4_term_api.Connection`
* **Method:** `OnConnectState(OnConnectStateRequest) → stream OnConnectStateReply`
* **Low‑level client (generated):** `Connection.ConnectionClient.OnConnectState(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async IAsyncEnumerable<ConnectStateData> OnConnectStateAsync(
            [EnumeratorCancellation] CancellationToken ct = default);
    }
}
```

**Request message:**

`OnConnectStateRequest {}`

**Reply message:**

`OnConnectStateReply { data: ConnectStateData }` (server stream)

---

### Code Example

```csharp
// --- Service wrapper (streaming; same shape as low-level) ---
// await foreach (var s in _service.OnConnectStateAsync(ct)) { ... }

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await foreach (var state in _mt4.OnConnectStateAsync(cts.Token))
{
    Console.WriteLine($"[{state.State}] connected={state.IsConnected} alive={state.IsAlive} api={state.ApiIsAlive} server={state.Server}");
    break; // demo: take the first snapshot
}
```

---

## 🔽 Input

No required parameters.

| Parameter | Type                | Description                               |
| --------- | ------------------- | ----------------------------------------- |
| `ct`      | `CancellationToken` | Cancels the (open-ended) stream.          |

---

## ⬆️ Output — `ConnectStateData`

| Field          | Type        | Description                                        |
| -------------- | ----------- | -------------------------------------------------- |
| `Id`           | `string`    | Terminal instance id.                              |
| `State`        | `string`    | Human-readable state (e.g. `"Connected"`).         |
| `IsConnected`  | `bool`      | Transport is connected.                            |
| `IsAlive`      | `bool`      | Terminal process is alive.                         |
| `ApiIsAlive`   | `bool`      | Terminal API is responsive.                        |
| `Server`       | `string`    | Server name (e.g. `"EGlobal-Demo"`).               |
| `Account`      | `ulong`     | Account login.                                     |
| `ErrorMessage` | `string`    | Error detail when state is not healthy (if any).   |
| `Timestamp`    | `Timestamp` | Time of the snapshot.                              |

---

## 🎯 Purpose

Watch terminal connectivity in real time:

* Drive a connection indicator in dashboards/CLIs.
* Detect drops/reconnects and pause trading logic while not `IsAlive`/`ApiIsAlive`.
* Log connectivity transitions for diagnostics.

---

## 🧩 Notes & Tips

* **Snapshot semantics.** Each item is the current state; compute transitions on the client if you need edge events.
* **Healthy = all true.** Treat `IsConnected && IsAlive && ApiIsAlive` as "ready to trade".
* **Endless stream.** Always bound it with a `CancellationToken`; cancellation completes it gracefully.

---

## ⚠️ Pitfalls

* **Acting while not alive.** Gate order placement on the healthy flags; sending during a drop yields transient errors.
* **Chatty logging.** Don't log every identical snapshot — log on change.

---

## 🧪 Testing Suggestions

* **Happy path.** On a live connection the first snapshot has `State="Connected"` and all health flags `true`.
* **Transition.** Briefly interrupt connectivity and verify the stream reports the unhealthy state, then recovery.
