using BashGPT.Configuration;

var configService = new ConfigurationService();

// config-Subkommando: bashgpt config <set|get|list> [key] [value]
if (args.Length >= 1 && args[0].Equals("config", StringComparison.OrdinalIgnoreCase))
{
    return await HandleConfigCommand(args[1..], configService);
}

if (args.Length == 0)
{
    Console.Error.WriteLine("Verwendung: bashgpt \"<prompt>\"");
    Console.Error.WriteLine("           bashgpt config set <schlüssel> <wert>");
    Console.Error.WriteLine("           bashgpt config get <schlüssel>");
    Console.Error.WriteLine("           bashgpt config list");
    return 1;
}

var prompt = string.Join(" ", args);
var config = await configService.LoadAsync();

Console.WriteLine($"[bashGPT] Prompt:   {prompt}");
Console.WriteLine($"[bashGPT] Provider: {config.DefaultProvider}");
Console.WriteLine("[bashGPT] (Noch nicht implementiert – Entwicklung läuft)");
return 0;

static async Task<int> HandleConfigCommand(string[] args, ConfigurationService svc)
{
    if (args.Length == 0)
    {
        Console.Error.WriteLine("Verwendung: bashgpt config <set|get|list> [schlüssel] [wert]");
        return 1;
    }

    var subCommand = args[0].ToLowerInvariant();

    try
    {
        switch (subCommand)
        {
            case "list":
                Console.WriteLine(await svc.ListAsync());
                return 0;

            case "get" when args.Length >= 2:
                Console.WriteLine(await svc.GetAsync(args[1]));
                return 0;

            case "set" when args.Length >= 3:
                await svc.SetAsync(args[1], args[2]);
                Console.WriteLine($"✓ {args[1]} = {args[2]}");
                return 0;

            case "get":
            case "set":
                Console.Error.WriteLine($"Verwendung: bashgpt config {subCommand} <schlüssel>{(subCommand == "set" ? " <wert>" : "")}");
                return 1;

            default:
                Console.Error.WriteLine($"Unbekanntes config-Subkommando '{subCommand}'. Erlaubt: set, get, list");
                return 1;
        }
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine($"Fehler: {ex.Message}");
        return 1;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine($"Fehler: {ex.Message}");
        return 1;
    }
}
