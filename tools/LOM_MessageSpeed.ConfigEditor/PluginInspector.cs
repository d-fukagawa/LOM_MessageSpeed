using System;
using System.Collections.Generic;
using System.IO;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal enum PluginState
    {
        PayloadUnavailable,
        NotInstalled,
        Approved,
        KnownOlder,
        SameVersionDifferentHash,
        NewerVersion,
        Unknown,
        CorruptOrUnreadable,
        DuplicatePlacement
    }

    internal sealed class PluginInspection
    {
        internal PluginInspection(
            PluginState state,
            string path,
            string? hash,
            Version? version,
            DateTime? lastWriteTime,
            IReadOnlyList<string>? conflictingPaths = null)
        {
            State = state;
            Path = path;
            Sha256 = hash;
            Version = version;
            LastWriteTime = lastWriteTime;
            ConflictingPaths = conflictingPaths ?? Array.Empty<string>();
        }

        internal PluginState State { get; }
        internal string Path { get; }
        internal string? Sha256 { get; }
        internal Version? Version { get; }
        internal DateTime? LastWriteTime { get; }
        internal IReadOnlyList<string> ConflictingPaths { get; }
        internal bool AllowsInstall => State == PluginState.NotInstalled;
        internal bool AllowsUpdate => State == PluginState.KnownOlder;
    }

    internal static class PluginInspector
    {
        internal static PluginInspection Inspect(string gameRoot, PluginManifest manifest)
        {
            string target = Path.GetFullPath(Path.Combine(gameRoot, PluginManifest.InstallRelativePath));
            string flat = Path.GetFullPath(Path.Combine(gameRoot, PluginManifest.FlatRelativePath));
            bool targetExists = File.Exists(target);
            bool flatExists = File.Exists(flat);

            if (targetExists && flatExists)
            {
                return new PluginInspection(
                    PluginState.DuplicatePlacement,
                    target,
                    null,
                    null,
                    null,
                    new[] { target, flat });
            }

            if (!targetExists && !flatExists)
            {
                return new PluginInspection(
                    manifest.HasEmbeddedPayload ? PluginState.NotInstalled : PluginState.PayloadUnavailable,
                    target,
                    null,
                    null,
                    null);
            }

            string inspectedPath = targetExists ? target : flat;
            try
            {
                PluginBinaryIdentity identity = PluginBinaryValidator.Read(inspectedPath);
                DateTime timestamp = File.GetLastWriteTimeUtc(inspectedPath);

                if (!targetExists)
                {
                    return new PluginInspection(
                        PluginState.Unknown,
                        inspectedPath,
                        identity.Sha256,
                        identity.FileVersion,
                        timestamp,
                        new[] { inspectedPath });
                }

                if (!manifest.HasEmbeddedPayload)
                {
                    return new PluginInspection(
                        PluginState.PayloadUnavailable,
                        inspectedPath,
                        identity.Sha256,
                        identity.FileVersion,
                        timestamp);
                }

                if (string.Equals(identity.Sha256, manifest.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    if (PluginBinaryValidator.MatchesApproved(identity, manifest, out string _))
                    {
                        return new PluginInspection(
                            PluginState.Approved,
                            inspectedPath,
                            identity.Sha256,
                            identity.FileVersion ?? manifest.Version,
                            timestamp);
                    }

                    return new PluginInspection(
                        PluginState.CorruptOrUnreadable,
                        inspectedPath,
                        identity.Sha256,
                        identity.FileVersion,
                        timestamp);
                }

                if (manifest.KnownOlderSha256.Contains(identity.Sha256))
                {
                    return new PluginInspection(
                        PluginState.KnownOlder,
                        inspectedPath,
                        identity.Sha256,
                        identity.FileVersion,
                        timestamp);
                }

                PluginState state;
                if (identity.FileVersion == null)
                {
                    state = PluginState.CorruptOrUnreadable;
                }
                else if (identity.FileVersion == manifest.FileVersion || identity.FileVersion == manifest.Version)
                {
                    state = PluginState.SameVersionDifferentHash;
                }
                else if (identity.FileVersion > manifest.Version)
                {
                    state = PluginState.NewerVersion;
                }
                else
                {
                    state = PluginState.Unknown;
                }

                return new PluginInspection(
                    state,
                    inspectedPath,
                    identity.Sha256,
                    identity.FileVersion,
                    timestamp);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is ArgumentException ||
                ex is BadImageFormatException ||
                ex is NotSupportedException)
            {
                return new PluginInspection(
                    PluginState.CorruptOrUnreadable,
                    inspectedPath,
                    null,
                    null,
                    null);
            }
        }
    }
}
