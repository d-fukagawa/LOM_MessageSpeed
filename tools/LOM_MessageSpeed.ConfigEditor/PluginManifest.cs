using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal sealed class PluginManifest
    {
        private readonly byte[]? suppliedPayload;
        internal const string Guid = "lom-messagespeed";
        internal const string DisplayName = "LOM_MessageSpeed";
        internal const string DllFileName = "LOM_MessageSpeed.dll";
        internal const string InstallRelativePath = @"BepInEx\plugins\LOM_MessageSpeed\LOM_MessageSpeed.dll";
        internal const string FlatRelativePath = @"BepInEx\plugins\LOM_MessageSpeed.dll";
        internal const string ResourceName = "LOM.MessageSpeed.ConfigEditor.Payload.LOM_MessageSpeed.dll";

        internal static readonly PluginManifest Current = new PluginManifest(
            version: new Version(0, 2, 0),
            expectedSha256: "0D23512EB5E59C8AAEC4545D344F8D4948C99E22E163D6DB2E5AB0D6D98B40A3",
            schemaVersion: ConfigSchema.SchemaVersion,
            expectedLength: 19968,
            assemblyVersion: new Version(1, 0, 0, 0),
            fileVersion: new Version(0, 2, 0, 0),
            productVersionPrefix: "0.2.0",
            sourceZipSha256: "7B4195E7AD4EB2247445F44C7E9C54DC1060ECE050287FA41C39F6E3BE20A689",
            knownOlderSha256: new[]
            {
                "FE44FE8796E57C52FC7E6E60A8C2277B499953E775BCAC9A0A9CCAC251F720E1"
            });

        internal PluginManifest(
            Version? version,
            string? expectedSha256,
            int schemaVersion,
            byte[]? suppliedPayload = null,
            long? expectedLength = null,
            Version? assemblyVersion = null,
            Version? fileVersion = null,
            string? productVersionPrefix = null,
            string? sourceZipSha256 = null,
            IEnumerable<string>? knownOlderSha256 = null)
        {
            Version = version;
            ExpectedSha256 = expectedSha256;
            SchemaVersion = schemaVersion;
            this.suppliedPayload = suppliedPayload;
            ExpectedLength = expectedLength;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
            ProductVersionPrefix = productVersionPrefix;
            SourceZipSha256 = sourceZipSha256;
            KnownOlderSha256 = new HashSet<string>(
                knownOlderSha256 ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
        }

        internal Version? Version { get; }
        internal string? ExpectedSha256 { get; }
        internal long? ExpectedLength { get; }
        internal Version? AssemblyVersion { get; }
        internal Version? FileVersion { get; }
        internal string? ProductVersionPrefix { get; }
        internal string? SourceZipSha256 { get; }
        internal int SchemaVersion { get; }
        internal IReadOnlySet<string> KnownOlderSha256 { get; }
        internal bool HasApprovedPayload =>
            Version != null && !string.IsNullOrWhiteSpace(ExpectedSha256);
        internal bool HasEmbeddedPayload
        {
            get
            {
                if (!HasApprovedPayload)
                {
                    return false;
                }
                if (suppliedPayload != null)
                {
                    return true;
                }
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
                return stream != null;
            }
        }
        internal string VersionDisplay => HasApprovedPayload ? Version!.ToString() : "未承認（同梱なし）";

        internal byte[]? ReadPayload()
        {
            if (!HasEmbeddedPayload)
            {
                return null;
            }

            if (suppliedPayload != null)
            {
                return (byte[])suppliedPayload.Clone();
            }

            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                return null;
            }

            using MemoryStream memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
