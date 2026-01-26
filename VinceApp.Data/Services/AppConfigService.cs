using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VinceApp.Services
{
    public static class AppConfigService
    {
        private static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VinceApp");

        public static readonly string UserConfigPath =
            Path.Combine(AppDataDir, "appsettings.user.json");

        public static JsonObject ReadUserConfig()
        {
            Directory.CreateDirectory(AppDataDir);

            if (!File.Exists(UserConfigPath))
                return new JsonObject();

            var text = File.ReadAllText(UserConfigPath);
            if (string.IsNullOrWhiteSpace(text)) return new JsonObject();

            return JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
        }

        public static void WriteUserConfig(JsonObject root)
        {
            Directory.CreateDirectory(AppDataDir);

            var json = root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(UserConfigPath, json);
        }

        public static bool GetActivatedFlag()
        {
            var root = ReadUserConfig();
            return root?["App"]?["Activated"]?.GetValue<bool>() == true;
        }

        public static void SetActivatedFlag(bool value)
        {
            var root = ReadUserConfig();
            root["App"] ??= new JsonObject();
            root["App"]!["Activated"] = value;
            WriteUserConfig(root);
        }

        public static string? GetConnectionString()
        {
            var root = ReadUserConfig();
            return root?["ConnectionStrings"]?["DefaultConnection"]?.GetValue<string>();
        }

        public static void SetConnectionString(string connStr)
        {
            var root = ReadUserConfig();
            root["ConnectionStrings"] ??= new JsonObject();
            root["ConnectionStrings"]!["DefaultConnection"] = connStr;
            WriteUserConfig(root);
        }
        public static string GetClient()
        {
            var root = ReadUserConfig();
            return root?["App"]?["Client"]?.GetValue<string>() ?? "POS";
        }

        public static void SetClient(string client)
        {
            client = string.IsNullOrWhiteSpace(client) ? "POS" : client.Trim().ToUpperInvariant();

            var root = ReadUserConfig();
            root["App"] ??= new JsonObject();
            root["App"]!["Client"] = client;
            WriteUserConfig(root);
        }

    }
}
