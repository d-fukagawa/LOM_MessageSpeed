using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal sealed class PluginBinaryIdentity
    {
        internal PluginBinaryIdentity(string sha256, long length, Version? assemblyVersion, Version? fileVersion, string? productVersion)
        {
            Sha256 = sha256;
            Length = length;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
            ProductVersion = productVersion;
        }

        internal string Sha256 { get; }
        internal long Length { get; }
        internal Version? AssemblyVersion { get; }
        internal Version? FileVersion { get; }
        internal string? ProductVersion { get; }
    }

    internal static class PluginBinaryValidator
    {
        internal static PluginBinaryIdentity Read(string path)
        {
            using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            string hash = Convert.ToHexString(SHA256.HashData(stream));
            long length = stream.Length;
            AssemblyName assembly = AssemblyName.GetAssemblyName(path);
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
            return new PluginBinaryIdentity(
                hash,
                length,
                assembly.Version,
                ParseVersion(info.FileVersion),
                info.ProductVersion);
        }

        internal static bool MatchesApproved(PluginBinaryIdentity identity, PluginManifest manifest, out string error)
        {
            if (!manifest.HasApprovedPayload ||
                !string.Equals(identity.Sha256, manifest.ExpectedSha256, StringComparison.OrdinalIgnoreCase) ||
                (manifest.ExpectedLength.HasValue && identity.Length != manifest.ExpectedLength.Value) ||
                (manifest.AssemblyVersion != null && identity.AssemblyVersion != manifest.AssemblyVersion) ||
                (manifest.FileVersion != null && identity.FileVersion != manifest.FileVersion) ||
                (manifest.ProductVersionPrefix != null &&
                    (identity.ProductVersion == null ||
                     !identity.ProductVersion.StartsWith(manifest.ProductVersionPrefix, StringComparison.Ordinal))))
            {
                error = "DLLのSHA-256、サイズ、または版情報が承認済みmanifestと一致しません。";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal static Version? ParseVersion(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            int suffix = value.IndexOfAny(new[] { '-', '+', ' ', '\t', '\r', '\n' });
            string normalized = suffix >= 0 ? value.Substring(0, suffix) : value;
            return Version.TryParse(normalized, out Version? version) ? version : null;
        }
    }
}
