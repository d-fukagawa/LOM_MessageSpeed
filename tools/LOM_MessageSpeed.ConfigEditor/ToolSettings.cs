using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal enum GameLocationMode
    {
        Drive,
        Manual
    }

    internal sealed class ToolSettings
    {
        public GameLocationMode LocationMode { get; set; } = GameLocationMode.Drive;
        public string LastDrive { get; set; } = "C:";
        public string LastValidatedManualPath { get; set; } = string.Empty;
        public string LastValidatedGameRoot { get; set; } = string.Empty;
        public int LastSelectedTab { get; set; }

        internal static string DefaultPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LOM_MessageSpeed",
                    "ConfigEditor",
                    "settings.json");
            }
        }

        internal static ToolSettings Load(string path, out string? warning)
        {
            warning = null;
            if (!File.Exists(path))
            {
                return new ToolSettings();
            }

            try
            {
                ToolSettings? settings = JsonSerializer.Deserialize<ToolSettings>(
                    File.ReadAllText(path, Encoding.UTF8),
                    JsonOptions);
                if (settings == null || !Enum.IsDefined(settings.LocationMode) ||
                    settings.LastSelectedTab < 0 || settings.LastSelectedTab > 2)
                {
                    throw new JsonException("設定値が不正です。");
                }

                settings.LastDrive = string.IsNullOrWhiteSpace(settings.LastDrive) ? "C:" : settings.LastDrive;
                settings.LastValidatedManualPath ??= string.Empty;
                settings.LastValidatedGameRoot ??= string.Empty;
                return settings;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                warning = "ツール設定が破損しているため既定値へ戻しました。ゲームとcfgは変更していません: " + ex.Message;
                return new ToolSettings();
            }
        }

        internal void Save(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string? directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new IOException("ツール設定の保存先を確認できません。");
            }

            Directory.CreateDirectory(directory);
            string temp = Path.Combine(directory, ".settings." + Guid.NewGuid().ToString("N") + ".tmp");
            byte[] bytes = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(this, JsonOptions));
            try
            {
                using (FileStream stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                {
                    File.Replace(temp, fullPath, null, true);
                }
                else
                {
                    File.Move(temp, fullPath);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(temp))
                    {
                        File.Delete(temp);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
