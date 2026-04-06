namespace GatewayPagamentos.Api.Configuration;

public static class EnvFileLoader
{
    public static void LoadIfExists(string fileName = ".env")
    {
        var envFilePath = FindFileInCurrentOrParentDirectories(fileName);
        if (envFilePath is null)
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(envFilePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line["export ".Length..].Trim();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            // Keep precedence for values already injected by OS/CI/container environment.
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindFileInCurrentOrParentDirectories(string fileName)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var fullPath = Path.Combine(directory.FullName, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
