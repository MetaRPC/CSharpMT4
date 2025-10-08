namespace MetaRPC.CSharpMT4.Helpers;

/// <summary>
/// MT4 connection options (pure config DTO).
/// Filled by configuration binding (ENV + appsettings.json) and validated before use.
/// 
/// Why this exists (and not inside MT4Account):
/// - Separation of concerns: parsing/validation/defaults live here; network logic lives in MT4Account.
/// - Testability & profiles: easy to swap/load different configs (dev/qa/prod) without touching code.
/// - Clear binding: matches "MT4Options" section and ENV keys (double underscores).
/// 
/// Fields (JSON key  | ENV name                 | Required | Meaning / Default)
/// -----------------------------------------------------------------------------
/// User              | MT4Options__User          | yes      | MT4 login (account number)
/// Password          | MT4Options__Password      | yes      | MT4 password (investor or master)
/// ServerName        | MT4Options__ServerName    | yes      | Broker server name, e.g. "MetaQuotes-Demo"
/// Grpc              | MT4Options__Grpc          | yes*     | gRPC endpoint to Terminal Manager
/// Symbol            | MT4Options__Symbol        | no       | Base chart symbol; default "EURUSD"
/// Host              | MT4Options__Host          | no       | Optional direct host of trade server (rarely used)
/// Port              | MT4Options__Port          | no       | Optional port for Host
/// TimeoutSeconds    | MT4Options__TimeoutSeconds| no       | 60 by default (raised by caller if needed)
/// ConnectRetries    | MT4Options__ConnectRetries| no       | 3 by default
/// ForceServerNameOnly| MT4Options__ForceServerNameOnly | no | true: skip Host/Port branch
/// 
/// Notes:
/// - JSON overrides ENV in this app (see EnvConfig.Load order). If a key is in both, JSON wins.
/// - ValidateOrError() checks only presence/basic sanity; deeper checks are done at connect time.
/// 
/// Typical usage:
///   var (opt, cfg) = EnvConfig.Load();
///   var err = opt.ValidateOrError();
///   if (!string.IsNullOrEmpty(err)) { /* print and exit */ }
///   await using var account = new MT4Account(opt.User, opt.Password, opt.Grpc!);
///   // then ConnectByServerNameAsync(...) etc., using opt.ServerName / opt.Symbol / timeouts.
/// </summary>
public sealed class Mt4Options
{
    public ulong  User { get; set; }
    public string Password { get; set; } = "";
    public string ServerName { get; set; } = "";

    public string? Grpc { get; set; }
    public string? Symbol { get; set; } = "EURUSD";

    // Optional fallback path (not recommended by default)
    public string? Host { get; set; }
    public int?    Port { get; set; }

    // Dial knobs
    public int  TimeoutSeconds   { get; set; } = 60;
    public int  ConnectRetries   { get; set; } = 3;
    public bool ForceServerNameOnly { get; set; } = true;

    public string ValidateOrError()
    {
        if (User == 0) return "Invalid MT4Options.User";
        if (string.IsNullOrWhiteSpace(Password)) return "Invalid MT4Options.Password";
        if (string.IsNullOrWhiteSpace(ServerName)) return "Invalid MT4Options.ServerName";
        if (string.IsNullOrWhiteSpace(Symbol)) return "Invalid MT4Options.Symbol";
        return "";
    }
}

