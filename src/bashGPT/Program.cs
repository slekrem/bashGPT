if (args.Length == 0)
{
    Console.Error.WriteLine("Verwendung: bashgpt \"<prompt>\"");
    Console.Error.WriteLine("Beispiel:   bashgpt \"zeige alle .cs Dateien\"");
    return 1;
}

var prompt = string.Join(" ", args);
Console.WriteLine($"[bashGPT] Prompt empfangen: {prompt}");
Console.WriteLine("[bashGPT] (Noch nicht implementiert – Entwicklung läuft)");
return 0;
