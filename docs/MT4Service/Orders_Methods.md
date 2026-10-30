# MT4Service — Orders Methods

> Read the current order book and history. (Placing/closing orders lives in **[Trading Methods](Trading_Methods.md)**.)

→ builds on low-level **[Orders — Overview](../MT5Account/Orders/Orders_Overview.md)**

---

## 📋 Methods

| Method | Returns | Low-level spec |
| ------ | ------- | -------------- |
| `OpenedOrdersAsync(sortType, deadline, ct)` | `OpenedOrdersData` | [OpenedOrders](../MT5Account/Orders/OpenedOrders.md) |
| `OpenedOrdersTicketsAsync(deadline, ct)` | `OpenedOrdersTicketsData` | [OpenedOrderTickets](../MT5Account/Orders/OpenedOrderTickets.md) |
| `OrdersHistoryAsync(from, to, sortType, page, itemsPerPage, …)` | `OrdersHistoryData` | [OrdersHistory](../MT5Account/Orders/OrdersHistory.md) |

* `sortType` on `OpenedOrdersAsync` is `EnumOpenedOrderSortType` (default `SortByOpenTimeAsc`).
* `sortType` on `OrdersHistoryAsync` is `EnumOrderHistorySortType` (default `HistorySortByCloseTimeDesc`).
* Each open order's `.OrderType` is an `OpenedOrderType` (`OoOpBuy`/`OoOpSell`/`OoOpBuylimit`/…).

---

## 💡 Usage examples

### 1) Monitor the open book

```csharp
var opened = await service.OpenedOrdersAsync();
Console.WriteLine($"{opened.OrderInfos.Count} open order(s)");
foreach (var o in opened.OrderInfos)
    Console.WriteLine($"[{o.OrderType}] #{o.Ticket} {o.Symbol} {o.Lots}@{o.OpenPrice} P/L={o.Profit}");
```

### 2) Lightweight ticket list

```csharp
var t = await service.OpenedOrdersTicketsAsync();
Console.WriteLine($"{t.Tickets.Count}: [{string.Join(", ", t.Tickets)}]");
```

### 3) Last week's closed orders (paged)

```csharp
var hist = await service.OrdersHistoryAsync(
    DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
    itemsPerPage: 100);
Console.WriteLine($"{hist.OrdersInfo.Count} closed order(s)");
```

---

## 🧩 Notes & Tips

* **Fetch before you act.** Re-query right before modify/close to avoid stale-ticket errors.
* **Page long histories.** Use `page` / `itemsPerPage` for wide ranges.
* **Tickets are `int`.** Store as `int` (or validate a `long` fits) before passing to close/modify.
