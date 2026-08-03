using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal sealed class ConfigDocument
    {
        internal const decimal MinimumMultiplier = ConfigSchema.MinimumMultiplier;
        internal const decimal MaximumMultiplier = ConfigSchema.MaximumMultiplier;
        internal const decimal DefaultMultiplier = ConfigSchema.DefaultMessageMultiplier;

        private static readonly Regex AssignmentPattern = new Regex(
            @"^(?<indent>[ \t]*)(?<key>[A-Za-z0-9_.-]+)(?<before>[ \t]*)=(?<after>[ \t]*)(?<value>.*?)(?<tail>[ \t]*)$",
            RegexOptions.CultureInvariant);

        private readonly string path;
        private readonly string originalText;
        private readonly bool hasBom;
        private readonly byte[]? originalHash;
        private readonly long originalLength;
        private readonly DateTime originalWriteTimeUtc;
        private readonly ValueLocation? enabledLocation;
        private readonly ValueLocation? multiplierLocation;

        private ConfigDocument(
            string path,
            string originalText,
            bool hasBom,
            byte[]? originalHash,
            long originalLength,
            DateTime originalWriteTimeUtc,
            ValueLocation? enabledLocation,
            ValueLocation? multiplierLocation,
            bool enabled,
            decimal multiplier,
            bool exists)
        {
            this.path = path;
            this.originalText = originalText;
            this.hasBom = hasBom;
            this.originalHash = originalHash;
            this.originalLength = originalLength;
            this.originalWriteTimeUtc = originalWriteTimeUtc;
            this.enabledLocation = enabledLocation;
            this.multiplierLocation = multiplierLocation;
            Enabled = enabled;
            SpeedMultiplier = multiplier;
            Exists = exists;
        }

        internal bool Enabled { get; }
        internal decimal SpeedMultiplier { get; }
        internal bool Exists { get; }

        internal static ConfigDocument Load(string configPath)
        {
            string fullPath = Path.GetFullPath(configPath);
            FileInfo info = new FileInfo(fullPath);
            if (!info.Exists)
            {
                return CreateMissing(fullPath);
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(fullPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new ConfigException("設定ファイルを読み込めません: " + ex.Message, ex);
            }

            bool bom = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf;
            string text;
            try
            {
                UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);
                text = strictUtf8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
            }
            catch (DecoderFallbackException ex)
            {
                throw new ConfigException("設定ファイルは有効なUTF-8ではありません。元ファイルは変更しません。", ex);
            }

            ParsedValues parsed = Parse(text);
            return new ConfigDocument(
                fullPath,
                text,
                bom,
                SHA256.HashData(bytes),
                info.Length,
                info.LastWriteTimeUtc,
                parsed.EnabledLocation,
                parsed.MultiplierLocation,
                parsed.Enabled,
                parsed.Multiplier,
                true);
        }

        internal static ConfigDocument CreateMissing(string configPath)
        {
            string fullPath = Path.GetFullPath(configPath);
            string initial = ConfigTemplate.Load();
            ParsedValues parsed = Parse(initial);
            return new ConfigDocument(
                fullPath,
                initial,
                false,
                null,
                0,
                DateTime.MinValue,
                parsed.EnabledLocation,
                parsed.MultiplierLocation,
                parsed.Enabled,
                parsed.Multiplier,
                false);
        }

        internal void Save(bool enabled, decimal multiplier)
        {
            ValidateMultiplier(multiplier, "SpeedMultiplier");

            if (enabledLocation == null || multiplierLocation == null)
            {
                throw new ConfigException("対象設定の位置を確認できないため保存できません。");
            }

            EnsureUnchanged();
            if (Exists && (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
            {
                throw new ConfigException("設定ファイルは読み取り専用です。属性を確認してください。");
            }

            string enabledText = enabled ? "true" : "false";
            string multiplierText = multiplier.ToString("0.0############################", CultureInfo.InvariantCulture);
            string updated = ReplaceValues(
                originalText,
                new[]
                {
                    new Replacement(enabledLocation, enabledText),
                    new Replacement(multiplierLocation, multiplierText)
                });
            WriteAtomically(updated);
        }

        private static ParsedValues Parse(string text)
        {
            string? section = null;
            List<ValueLocation> enabled = new List<ValueLocation>();
            List<ValueLocation> multipliers = new List<ValueLocation>();

            int position = 0;
            while (position <= text.Length)
            {
                int newline = text.IndexOf('\n', position);
                int lineEnd = newline >= 0 ? newline : text.Length;
                int contentEnd = lineEnd > position && text[lineEnd - 1] == '\r' ? lineEnd - 1 : lineEnd;
                string line = text.Substring(position, contentEnd - position);
                string trimmed = line.Trim();

                if (trimmed.Length != 0 && !trimmed.StartsWith("#", StringComparison.Ordinal) && !trimmed.StartsWith(";", StringComparison.Ordinal))
                {
                    if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
                    {
                        section = trimmed.Substring(1, trimmed.Length - 2);
                    }
                    else
                    {
                        Match match = AssignmentPattern.Match(line);
                        if (match.Success)
                        {
                            string key = match.Groups["key"].Value;
                            Group value = match.Groups["value"];
                            ValueLocation location = new ValueLocation(position + value.Index, value.Length, value.Value.Trim());
                            if (string.Equals(section, "General", StringComparison.Ordinal) && string.Equals(key, "Enabled", StringComparison.Ordinal))
                            {
                                enabled.Add(location);
                            }
                            else if (string.Equals(section, "Message", StringComparison.Ordinal) && string.Equals(key, "SpeedMultiplier", StringComparison.Ordinal))
                            {
                                multipliers.Add(location);
                            }
                        }
                    }
                }

                if (newline < 0)
                {
                    break;
                }

                position = newline + 1;
            }

            if (enabled.Count != 1)
            {
                throw new ConfigException("[General] Enabledが" + enabled.Count.ToString(CultureInfo.InvariantCulture) + "個あります。正確に1個必要です。");
            }

            if (multipliers.Count != 1)
            {
                throw new ConfigException("[Message] SpeedMultiplierが" + multipliers.Count.ToString(CultureInfo.InvariantCulture) + "個あります。正確に1個必要です。");
            }

            bool enabledValue;
            if (string.Equals(enabled[0].Value, "true", StringComparison.OrdinalIgnoreCase))
            {
                enabledValue = true;
            }
            else if (string.Equals(enabled[0].Value, "false", StringComparison.OrdinalIgnoreCase))
            {
                enabledValue = false;
            }
            else
            {
                throw new ConfigException("Enabledの値がtrueまたはfalseではありません。");
            }

            decimal multiplier = ParseMultiplier(multipliers[0], "SpeedMultiplier");
            return new ParsedValues(enabled[0], multipliers[0], enabledValue, multiplier);
        }

        private void EnsureUnchanged()
        {
            if (!Exists)
            {
                if (File.Exists(path))
                {
                    throw new ConfigChangedException("読み込み後に設定ファイルが作成されました。再読み込みしてください。");
                }

                return;
            }

            FileInfo current = new FileInfo(path);
            if (!current.Exists)
            {
                throw new ConfigChangedException("読み込み後に設定ファイルが削除されました。再読み込みしてください。");
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new ConfigException("保存前の再確認に失敗しました: " + ex.Message, ex);
            }

            bool metadataChanged = current.Length != originalLength || current.LastWriteTimeUtc != originalWriteTimeUtc;
            if (metadataChanged || originalHash == null || !CryptographicOperations.FixedTimeEquals(originalHash, SHA256.HashData(bytes)))
            {
                throw new ConfigChangedException("読み込み後に設定ファイルが外部で変更されました。再読み込みしてください。");
            }
        }

        private void WriteAtomically(string text)
        {
            string? directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                throw new ConfigException("BepInExのconfigフォルダが存在しません。");
            }

            string tempPath = Path.Combine(directory, ".lom-messagespeed." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp");
            try
            {
                byte[] body = new UTF8Encoding(false, true).GetBytes(text);
                using (FileStream stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    if (hasBom)
                    {
                        byte[] preamble = Encoding.UTF8.GetPreamble();
                        stream.Write(preamble, 0, preamble.Length);
                    }

                    stream.Write(body, 0, body.Length);
                    stream.Flush(true);
                }

                if (Exists)
                {
                    File.Replace(tempPath, path, path + ".bak", true);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                throw new ConfigException("設定の安全な保存に失敗しました。元ファイルは保持されています: " + ex.Message, ex);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
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

        private static decimal ParseMultiplier(ValueLocation location, string name)
        {
            if (!decimal.TryParse(location.Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value))
            {
                throw new ConfigException(name + "は小数点に '.' を使った有限の数値で指定してください。");
            }

            ValidateMultiplier(value, name);
            return value;
        }

        private static void ValidateMultiplier(decimal value, string name)
        {
            if (value < MinimumMultiplier || value > MaximumMultiplier)
            {
                throw new ConfigException(name + "が保存可能範囲0.1～10.0の外です。元ファイルは変更しません。");
            }
        }

        private static string ReplaceValues(string text, Replacement[] replacements)
        {
            Array.Sort(replacements, delegate (Replacement left, Replacement right)
            {
                return right.Location.Start.CompareTo(left.Location.Start);
            });
            string updated = text;
            foreach (Replacement replacement in replacements)
            {
                updated = updated.Remove(replacement.Location.Start, replacement.Location.Length)
                    .Insert(replacement.Location.Start, replacement.Value);
            }

            return updated;
        }

        private sealed class Replacement
        {
            internal Replacement(ValueLocation location, string value)
            {
                Location = location;
                Value = value;
            }

            internal ValueLocation Location { get; }
            internal string Value { get; }
        }

        private sealed class ValueLocation
        {
            internal ValueLocation(int start, int length, string value)
            {
                Start = start;
                Length = length;
                Value = value;
            }

            internal int Start { get; }
            internal int Length { get; }
            internal string Value { get; }
        }

        private sealed class ParsedValues
        {
            internal ParsedValues(
                ValueLocation enabledLocation,
                ValueLocation multiplierLocation,
                bool enabled,
                decimal multiplier)
            {
                EnabledLocation = enabledLocation;
                MultiplierLocation = multiplierLocation;
                Enabled = enabled;
                Multiplier = multiplier;
            }

            internal ValueLocation EnabledLocation { get; }
            internal ValueLocation MultiplierLocation { get; }
            internal bool Enabled { get; }
            internal decimal Multiplier { get; }
        }
    }

    internal class ConfigException : Exception
    {
        internal ConfigException(string message) : base(message) { }
        internal ConfigException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal sealed class ConfigChangedException : ConfigException
    {
        internal ConfigChangedException(string message) : base(message) { }
    }
}
