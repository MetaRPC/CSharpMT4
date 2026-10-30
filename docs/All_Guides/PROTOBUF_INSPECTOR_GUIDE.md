# Protobuf Inspector

> An **interactive developer tool** that explores the MT4 gRPC protobuf types, fields and enums by reflection — no connection or docs needed to answer "what fields does this type have?" or "what are the valid enum values?".

Source: `examples/Tools/ProtobufInspector.cs` (class `ProtobufInspector`). It reflects over the generated `mt4_term_api` assembly, so it always matches the exact SDK version you have.

---

## 🚀 How to run

```bash
# reflection-only, no terminal connection (fastest)
dotnet run -- --inspect

# from the examples menu (connects first) → option 12
dotnet run
# or: dotnet run -- --example=inspect
```

You'll get a `>` prompt. Type a command (below), or `exit` to quit.

---

## ⌨️ Commands

| Command | What it does |
| ------- | ------------ |
| `list` / `ls` | List all `*Data` / `*Request` / `*Reply` types |
| `<TypeName>` | Inspect a type — all properties and their types (collections marked 📚) |
| `search <text>` / `find <text>` | Find types whose name contains text |
| `field <name>` | Find all types that contain a field (and its type) |
| `enum <name>` | Show an enum's members and numeric values |
| `help` / `?` | Full command reference |
| `exit` / `quit` / `q` | Leave the inspector |

---

## 💡 Examples

**What fields does `AccountSummaryData` have?**
```
> AccountSummaryData
  • AccountBalance      : double
  • AccountEquity       : double
  • AccountMarginFree   : double
  • AccountTradeMode    : EnumAccountTradeMode
  ...
```

**Which types have a `Ticket` field?**
```
> field Ticket
📦 OpenedOrderInfo:
   └─ Ticket: int
📦 HistoryOrderInfo:
   └─ Ticket: int
...
```

**What are the order operation values?**
```
> enum OrderSendOperationType
  OcOpBuy        = 0
  OcOpSell       = 1
  OcOpBuylimit   = 2
  OcOpSelllimit  = 3
  OcOpBuystop    = 4
  OcOpSellstop   = 5
```

**Find everything related to "Order":**
```
> search Order
  [Class]  OpenedOrderInfo
  [Class]  OrderSendRequest
  [Class]  OrderSendData
  ...
```

---

## 🎯 When it helps

* **"Field not found" / wrong name** → `field <name>` shows the correct property and which type has it.
* **Building a `Request`** → inspect the `*Request` type to see exactly which fields to set.
* **Reading a `Reply`** → inspect the `*Data` type to see what you can read back.
* **Enum values** → `enum <Name>` instead of guessing (see also the [ENUMs Reference](ENUMS_USAGE_REFERENCE.md)).

---

## 🧩 Notes

* **Reflection-based** — it reports the types in your actual `MetaRPC.MT4` assembly, so it can't drift from the code.
* **Type names are the C# generated names** (e.g. `OpenedOrderInfo`, `QuoteData`), matching what you use in code and in the [method docs](../MT5Account/MT4Account.Master.Overview.md).
* It's a **read-only explorer** — it never calls the gateway or places anything.
