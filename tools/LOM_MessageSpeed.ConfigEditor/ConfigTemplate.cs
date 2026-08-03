using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal static class ConfigTemplate
    {
        internal const string ResourceName =
            "LOM.MessageSpeed.ConfigEditor.Templates.lom-messagespeed.cfg";

        internal static string Load()
        {
            using Stream? stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                throw new ConfigException("内蔵設定テンプレートを読み込めません。");
            }

            using StreamReader reader = new StreamReader(
                stream,
                new UTF8Encoding(false, true),
                true);
            string text = reader.ReadToEnd();
            string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal)
                .Replace("\n", "\r\n", StringComparison.Ordinal);
            return normalized.EndsWith("\r\n", StringComparison.Ordinal)
                ? normalized
                : normalized + "\r\n";
        }
    }
}
