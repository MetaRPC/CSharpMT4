# Getting the Server Timezone

> **Request:** the broker server's timezone/UTC offset (`ServerTimezoneData`) via `ServerTimezone`.
> Use it to convert server-local times to/from UTC and to label timestamps correctly.

**API Information:**

* **SDK wrapper:** `MT4Account.ServerTimezoneAsync(...)` (from NuGet package `MetaRPC.MT4`)
* **gRPC service:** `mt4_term_api.ServerTimezone`
* **Proto definition:** `ServerTimezone` (in the ServerTimezone service)

### RPC

* **Service:** `mt4_term_api.ServerTimezone`
* **Method:** `ServerTimezone(ServerTimezoneRequest) → ServerTimezoneReply`
* **Low‑level client (generated):** `ServerTimezone.ServerTimezoneClient.ServerTimezone(request, headers, deadline, cancellationToken)`
* **SDK wrapper (your class):**

```csharp
namespace mt4_term_api
{
    public class MT4Account
    {
        public async Task<ServerTimezoneData> ServerTimezoneAsync(
            DateTime? deadline = null,
            CancellationToken cancellationToken = default);
    }
}
```

**Request message:**

`ServerTimezoneRequest {}`

**Reply message:**

`ServerTimezoneReply { data: ServerTimezoneData }`

---

### Code Example

```csharp
// --- Service wrapper (typed, data-returning) ---
var tz = await _service.ServerTimezoneAsync();
Console.WriteLine($"Server={tz.Server} utcOffset={tz.UtcOffsetSeconds}s source={tz.Source}");

// --- Low-level (direct account call) ---
// Preconditions: account is connected via ConnectByServerName/ConnectByHostPort.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var data = await _mt4.ServerTimezoneAsync(
    deadline: null,
    cancellationToken: cts.Token);

var offset = TimeSpan.FromSeconds(data.UtcOffsetSeconds);
Console.WriteLine($"{data.Server}: UTC{(offset < TimeSpan.Zero ? "" : "+")}{offset} (source: {data.Source})");
```

---

## 🔽 Input

No required parameters.

| Parameter           | Type                | Description                                        |
| ------------------- | ------------------- | -------------------------------------------------- |
| `deadline`          | `DateTime?`         | Optional absolute **UTC** deadline for the call.   |
| `cancellationToken` | `CancellationToken` | Cooperative cancel for the call/retry loop.        |

---

## ⬆️ Output — `ServerTimezoneData`

| Field             | Type                     | Description                                      |
| ----------------- | ------------------------ | ------------------------------------------------ |
| `Server`          | `string`                 | Server name (e.g. `"EGlobal-Demo"`).             |
| `UtcOffsetSeconds`| `int`                    | Server offset from UTC, in seconds.              |
| `Source`          | `SERVER_TIMEZONE_SOURCE` | How the offset was determined — see enum below.  |
| `ValidatedUtc`    | `Timestamp`              | When the offset was last validated (UTC).        |

---

## 🧱 Related enums (from proto)

### `SERVER_TIMEZONE_SOURCE` — field `Source`

* `Unknown = 0` — source not determined
* `Live = 1` — derived from a live server response (most reliable)
* `LastValidated = 2` — last known validated value (cached/fallback)

---

## 🎯 Purpose

Correctly align times between your app (UTC) and the broker server:

* Convert bar/quote/order timestamps to server-local for display.
* Validate the server clock as part of a connection health check.

---

## 🧩 Notes & Tips

* **Prefer `*Utc` fields elsewhere.** Most replies already expose a `*Utc` timestamp; use this offset only when you must show server-local time.
* **Source matters.** `Live` is authoritative; `LastValidated` is a cached fallback — refresh if precision matters.

---

## ⚠️ Pitfalls

* **DST drift.** Some brokers shift with daylight saving; re-read the offset periodically rather than caching forever.
* **Sign confusion.** `UtcOffsetSeconds` is server-minus-UTC; add it to UTC to get server-local time.

---

## 🧪 Testing Suggestions

* **Happy path.** `Server` is non-empty; `Source` is `Live`; `UtcOffsetSeconds` matches the broker's stated timezone.
* **Edge cases.** On reconnect the first read may report `LastValidated` before a `Live` value arrives.
