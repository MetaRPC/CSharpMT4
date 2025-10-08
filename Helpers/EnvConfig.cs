using Microsoft.Extensions.Configuration;

namespace MetaRPC.CSharpMT4.Helpers;

/// <summary>
/// Loads configuration for MT4 client.
/// Merge order (later overrides earlier):
///   1) Environment variables   (lower priority)
///   2) appsettings.json        (higher)
///   3) appsettings.Development.json (highest)
///
/// Binds the "MT4Options" section to <see cref="Mt4Options"/> and applies sane defaults.
/// Returns both the bound options and the full configuration root.
/// </summary>
public static class EnvConfig
{
    /*
    =============================================================================
    MT4Options – keys, env names and defaults
    -----------------------------------------------------------------------------
    | JSON key (path)             | ENV variable              | Type   | Default
    |-----------------------------|---------------------------|--------|---------------------------|
    | MT4Options:User             | MT4Options__User          | ulong  | — (required by caller)    |
    | MT4Options:Password         | MT4Options__Password      | string | — (required by caller)    |
    | MT4Options:ServerName       | MT4Options__ServerName    | string | — (e.g., "MetaQuotes-Demo")|
    | MT4Options:Grpc             | MT4Options__Grpc          | string | https://mt4.mrpc.pro:443  |
    | MT4Options:Host             | MT4Options__Host          | string | null (optional)           |
    | MT4Options:Port             | MT4Options__Port          | int    | 0    (optional)           |
    | MT4Options:Symbol           | MT4Options__Symbol        | string | EURUSD (if you set it)    |
    | MT4Options:TimeoutSeconds   | MT4Options__TimeoutSeconds| int    | 60                         |
    | MT4Options:ConnectRetries   | MT4Options__ConnectRetries| int    | 3                          |
    | MT4Options:ForceServerNameOnly| MT4Options__ForceServerNameOnly| bool | false (by type default)  |
    -----------------------------------------------------------------------------
    Notes:
      • Provider order matters. Because we add env FIRST and json AFTER,
        values from JSON override env values with the same keys.
      • Nested keys in env use double underscores: MT4Options__Grpc, etc.
      • If Grpc is not set inside MT4Options, we also check a flat key "Grpc"
        at the root (useful for very simple configs).
      • This loader does NOT validate credentials; callers should check required fields.

    Quick examples
      # PowerShell
      $env:MT4Options__User="168418518"
      $env:MT4Options__Password="rlmj5or"
      $env:MT4Options__ServerName="MetaQuotes-Demo"
      $env:MT4Options__Grpc="http://localhost:5000"

      # bash/zsh
      export MT4Options__User=168418518
      export MT4Options__Password=rlmj5or
      export MT4Options__ServerName="MetaQuotes-Demo"
      export MT4Options__Grpc="http://localhost:5000"
    =============================================================================
    */

    public static (Mt4Options Opts, IConfigurationRoot Cfg) Load()
    {
        // Order is important: later providers override earlier ones.
        // Here JSON overrides ENV (by design for this app).
        var cfg = new ConfigurationBuilder()
            .AddEnvironmentVariables() // lower priority
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // higher priority
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // highest
            .Build();

        // Bind MT4Options section (missing values keep type defaults).
        var mt4 = cfg.GetSection("MT4Options").Get<Mt4Options>() ?? new Mt4Options();

        // Fallback for Grpc: allow a flat "Grpc" key at root if section value is empty.
        mt4.Grpc ??= cfg["Grpc"] ?? "https://mt4.mrpc.pro:443";

        // Apply sane defaults where needed.
        if (mt4.TimeoutSeconds <= 0) mt4.TimeoutSeconds = 60;
        if (mt4.ConnectRetries <= 0) mt4.ConnectRetries = 3;

        return (mt4, cfg);
    }
}



