# 🧭 MT4 for Beginners: Your First Steps

Welcome! This guide is for complete beginners — never touched MetaTrader 4 or trading before. It explains what MT4 is, how to create a free **demo account** in the terminal, and where to go next to drive it from **C#** with CSharpMT4.

---

## 🖥️ What is MetaTrader 4 (MT4)?

**MetaTrader 4** is a trading platform used by millions of traders worldwide. With it you can:

- Trade forex, metals, commodities and more
- Analyze markets with charts and indicators
- Run automated trading strategies
- Monitor your positions in real time

Think of MT4 as the **cockpit** for the markets. **CSharpMT4** lets you drive that cockpit programmatically from C# over gRPC — reading prices, placing orders, streaming ticks — without clicking around the terminal by hand.

---

## 🛡️ Demo vs Live: start safely

### Why start with a demo account?

A **demo account** is a risk-free practice account funded with virtual money. Start here — always:

- ✅ **Zero risk** — virtual funds (typically $10,000–$100,000)
- ✅ **Real market conditions** — live prices and behavior
- ✅ **Learn the platform** — before any real money is involved
- ✅ **Perfect for testing** — develop and run your CSharpMT4 code against it

### Live account

A **live account** uses real money. Only move to live once you understand MT4, have tested your strategy thoroughly on demo, and accept the risks.

---

## 🚀 Creating a demo account in MT4

You need an account (login, password, server) for the SDK to connect to. Here's the full walk-through in the terminal.

### Step 1️⃣ — Download & install MT4

Get the terminal from your broker, or the MetaTrader site: [https://www.metatrader4.com/en/download](https://www.metatrader4.com/en/download). Install it for your OS.

### Step 2️⃣ — Find the Navigator panel

On launch, MT4 often shows an "open an account" popup. If not, find the **Navigator** panel (left side, below **Market Watch**).

![Navigator Panel Location](../Guide_Images/1.%20NAV.png)

Inside Navigator you'll see **Accounts**, Indicators, Expert Advisors, and more.

### Step 3️⃣ — Open a new demo account

In **Navigator**, **right-click** on **Accounts** → **"Open an account"**. This opens the broker-server search.

### Step 4️⃣ — Select the demo server

Find **"MetaQuotes-Demo"** (the standard demo server) in the list — use the search box if needed — select it and click **Next**.

![MetaQuotes Server Selection](../Guide_Images/2.%20META.png)

### Step 5️⃣ — Choose "Open a new demo account"

![Open New Demo Account](../Guide_Images/3.%20OPEN.png)

### Step 6️⃣ — Fill in your details

Name, email, account type, deposit (e.g. $10,000), leverage (1:100 is fine), currency. Accept the terms and click **Next**.

![Personal Information Form](../Guide_Images/4.%20creator.png)

### Step 7️⃣ — Save your credentials

MT4 shows your **Login**, **Password**, **Investor Password** and **Server** (e.g. `MetaQuotes-Demo`).

> ⚠️ **Save these immediately** — you'll need the login, password and server name for the config below.

![Account Credentials](../Guide_Images/5.%20OKK.png)

---

## ⚙️ Connect CSharpMT4 to your demo account

Put the credentials MT4 gave you into the `MT4Options` section of `appsettings.json`:

```json
{
  "MT4Options": {
    "User": 591129415,
    "Password": "YourPasswordHere",
    "ServerName": "MetaQuotes-Demo",
    "Grpc": "https://mt4.mrpc.pro:443",
    "ApiKey": "<your API key>",
    "Symbol": "EURUSD"
  }
}
```

- **User** — your login number.
- **Password** — your master password.
- **ServerName** — the exact server name from MT4 (e.g. `MetaQuotes-Demo`).
- **Grpc** — the MetaRPC endpoint (leave as is for production).
- **ApiKey** — your key from [https://mrpc.pro/my](https://mrpc.pro/my).

Full precedence rules (env → appsettings → defaults) and API-key details are in **[Getting Started → Configuration](GETTING_STARTED.md#configuration-provide-your-connection-data)**.

Then run `dotnet run` — the console prints the resolved config and a `CONNECT` box. A successful connect means you're ready to go.

---

## 🧩 MT4 password types

MT4 gives you **two** passwords:

### Master password

- **Full access** — open/close trades, change settings.
- The one you created during registration.
- **This is the password CSharpMT4 uses** to trade.

### Investor password

- **View-only** — see trades, balance and history, but **cannot trade**.
- Useful for sharing performance or read-only monitoring tools.
- Set it in MT4 via **Tools → Options → Server → Change** (Investor Password).

---

## 🏦 Choosing a broker (optional)

The MetaQuotes demo server is perfect for practice. For live trading later you might pick a specific broker for better conditions or regional support.

> **Not financial advice.** Always do your own research.

Popular MT4 brokers: IC Markets, Pepperstone, OANDA, XM, RoboForex, FxPro.

What to look for:

- ✅ **Regulation** — FCA, ASIC, CySEC, etc.
- ✅ **MT4 support** — make sure they actually offer **MT4** (not only MT5).
- ✅ **Demo account** — free (note: demo accounts often expire after ~30 days of inactivity).
- ✅ **Good spreads** — lower spreads = lower cost.
- ✅ **Responsive support** — in your language.

---

## ➡️ What's next?

### 1. Connect CSharpMT4

Follow the main setup guide to install, configure and run:

👉 **[Getting Started](GETTING_STARTED.md)** · **[Your First Project (10 min)](Your_First_Project.md)**

### 2. Learn the three layers

CSharpMT4 is built in **three levels**, low to high — pick where to start:

- **Level 1 — [MT4Account](../API_Reference/MT4Account.md)**: direct low-level gRPC calls. Maximum control; everything else is built on it.
- **Level 2 — [MT4Service](../API_Reference/MT4Service.md)**: typed wrappers over MT4Account. Clean signatures, ready data. Best for most scenarios.
- **Level 3 — [MT4Sugar](../API_Reference/MT4Sugar.md)**: convenience helpers (risk sizing, normalization, batch actions). Most beginner-friendly.

---

## ⚠️ Important reminders

### 🛡️ Security
- **Never share your master password.** Use the investor password for monitoring tools.
- Keep credentials safe (a password manager helps). Don't commit real credentials to source control.

### 📊 Risk management
- Demo is risk-free, but build good habits: always use a stop-loss, don't risk more than 1–2% per trade, understand position sizing.

### 🧪 Testing
- Test all your code **on demo first**. Verify strategies over weeks, not days. Demo results don't guarantee live results.

---

**Ready to code?** Head to **[Getting Started](GETTING_STARTED.md)** and connect CSharpMT4 to your account. Good luck! 🚀
