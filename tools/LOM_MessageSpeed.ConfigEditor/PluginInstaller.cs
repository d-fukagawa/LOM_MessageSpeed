using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal sealed class PluginInstallResult
    {
        internal PluginInstallResult(bool success, bool changed, string message)
        {
            Success = success;
            Changed = changed;
            Message = message;
        }

        internal bool Success { get; }
        internal bool Changed { get; }
        internal string Message { get; }
    }

    internal sealed class PluginInstaller
    {
        private readonly Func<string?, GameRunningStatus> processCheck;
        private readonly Action<string>? afterReplaceForTest;

        internal PluginInstaller(
            Func<string?, GameRunningStatus>? processCheck = null,
            Action<string>? afterReplaceForTest = null)
        {
            this.processCheck = processCheck ?? GameProcessGuard.Check;
            this.afterReplaceForTest = afterReplaceForTest;
        }

        internal PluginInstallResult Install(
            string gameRoot,
            PluginManifest manifest,
            PluginInspection? expectedInspection = null)
        {
            if (!GameLocator.TryValidateRoot(gameRoot, out string validated, out string error))
            {
                return Fail("ゲームルートの再検証に失敗しました: " + error);
            }

            BepInExInspection bepinex = BepInExInspector.Inspect(validated);
            if (!bepinex.AllowsPluginUse || bepinex.HasReparsePoint)
            {
                return Fail("BepInExの前提を確認できないため導入しません: " + bepinex.Message);
            }

            GameRunningStatus running = processCheck(validated);
            if (running.BlocksSave)
            {
                return Fail("ゲーム起動中または状態不明のため導入しません。" + running.Message);
            }

            byte[]? payload = manifest.ReadPayload();
            if (payload == null || !manifest.HasApprovedPayload)
            {
                return Fail("承認済みプラグインがツールへ同梱されていません。");
            }

            string payloadHash = Convert.ToHexString(SHA256.HashData(payload));
            if (!string.Equals(payloadHash, manifest.ExpectedSha256, StringComparison.OrdinalIgnoreCase) ||
                (manifest.ExpectedLength.HasValue && payload.LongLength != manifest.ExpectedLength.Value))
            {
                return Fail("同梱プラグインの検証に失敗しました。ファイルは変更していません。");
            }

            PluginInspection inspection = PluginInspector.Inspect(validated, manifest);
            if (expectedInspection != null &&
                (expectedInspection.State != inspection.State ||
                 !string.Equals(expectedInspection.Sha256, inspection.Sha256, StringComparison.OrdinalIgnoreCase)))
            {
                return Fail("確認後に既存DLLの状態が変わったため導入しません。");
            }

            if (inspection.State == PluginState.Approved)
            {
                return new PluginInstallResult(true, false, "承認済みの同一版が導入済みです。ファイルは変更していません。");
            }

            if (!inspection.AllowsInstall && !inspection.AllowsUpdate)
            {
                return Fail("既存DLLは確認済みの版ではないため、自動で上書きしません。");
            }

            string destination = Path.GetFullPath(Path.Combine(validated, PluginManifest.InstallRelativePath));
            string pluginsRoot = Path.GetFullPath(Path.Combine(validated, "BepInEx", "plugins"));
            string prefix = pluginsRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return Fail("導入先がBepInEx/pluginsの外を指しています。");
            }

            if (!TryRejectReparsePoints(validated, destination, out error))
            {
                return Fail(error);
            }

            if (File.Exists(destination))
            {
                if ((File.GetAttributes(destination) & FileAttributes.ReadOnly) != 0)
                {
                    return Fail("既存DLLは読み取り専用のため変更しません: " + destination);
                }

                try
                {
                    using FileStream locked = new FileStream(
                        destination,
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.None);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    return Fail("既存DLLを安全に排他確認できないため変更しません: " + destination);
                }
            }

            string directory = Path.GetDirectoryName(destination)!;
            string temp = Path.Combine(
                directory,
                "." + PluginManifest.DllFileName + "." +
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp");
            string backup = destination + ".bak";
            bool replaced = false;
            try
            {
                Directory.CreateDirectory(directory);
                if (!TryRejectReparsePoints(validated, destination, out error))
                {
                    return Fail(error);
                }

                using (FileStream stream = new FileStream(
                    temp,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.WriteThrough))
                {
                    stream.Write(payload, 0, payload.Length);
                    stream.Flush(true);
                }

                PluginBinaryIdentity staged = PluginBinaryValidator.Read(temp);
                if (!PluginBinaryValidator.MatchesApproved(staged, manifest, out error))
                {
                    return Fail("一時DLLの検証に失敗しました: " + error);
                }

                PluginInspection current = PluginInspector.Inspect(validated, manifest);
                if (current.State != inspection.State ||
                    !string.Equals(current.Sha256, inspection.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    return Fail("検査後に既存DLLが変更されたため導入しません。");
                }

                running = processCheck(validated);
                if (running.BlocksSave)
                {
                    return Fail("導入直前にゲームの起動を検出したため変更しません。" + running.Message);
                }

                if (inspection.AllowsUpdate)
                {
                    File.Replace(temp, destination, backup, true);
                }
                else
                {
                    File.Move(temp, destination);
                }
                replaced = true;
                afterReplaceForTest?.Invoke(destination);

                PluginBinaryIdentity installed = PluginBinaryValidator.Read(destination);
                if (!PluginBinaryValidator.MatchesApproved(installed, manifest, out error))
                {
                    bool rolledBack = TryRollback(destination, backup, inspection.AllowsUpdate);
                    return Fail(
                        "導入後の検証に失敗しました。" +
                        (rolledBack
                            ? "元のDLLへ復元しました。"
                            : "復元にも失敗しました。現在のDLL: " + destination + " / backup: " + backup));
                }

                PluginInspection verified = PluginInspector.Inspect(validated, manifest);
                if (verified.State != PluginState.Approved)
                {
                    bool rolledBack = TryRollback(destination, backup, inspection.AllowsUpdate);
                    return Fail(rolledBack
                        ? "導入後の再検査に失敗したため元のDLLへ復元しました。"
                        : "導入後の再検査と復元に失敗しました: " + destination);
                }

                return new PluginInstallResult(
                    true,
                    true,
                    inspection.AllowsInstall
                        ? "プラグインを新規インストールしました。cfgは変更していません。"
                        : "プラグインを更新し、直前のDLLを.bakへ保存しました。cfgは変更していません。");
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is NotSupportedException ||
                ex is ArgumentException ||
                ex is BadImageFormatException)
            {
                bool rolledBack = replaced &&
                    TryRollback(destination, backup, inspection.AllowsUpdate);
                return Fail(
                    "プラグイン導入に失敗しました。" +
                    (replaced
                        ? (rolledBack
                            ? "元のDLLへ復元しました。"
                            : "復元にも失敗しました。現在のDLLとbackupを確認してください: " + destination + " / " + backup)
                        : "既存DLLは変更していません。") +
                    " 詳細: " + ex.Message);
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
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        internal static bool TryRejectReparsePoints(string gameRoot, string destination, out string error)
        {
            string root = Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar);
            string target = Path.GetFullPath(destination);
            string prefix = root + Path.DirectorySeparatorChar;
            if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                error = "導入先がゲームルート外を指しています。";
                return false;
            }

            string current = root;
            if (ExistsAndIsReparsePoint(current))
            {
                error = "ゲームルートがreparse pointのため導入しません: " + current;
                return false;
            }

            string relative = Path.GetRelativePath(root, target);
            foreach (string part in relative.Split(Path.DirectorySeparatorChar))
            {
                current = Path.Combine(current, part);
                if (ExistsAndIsReparsePoint(current))
                {
                    error = "導入経路にjunctionまたはsymbolic linkがあります: " + current;
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool ExistsAndIsReparsePoint(string path)
        {
            return (File.Exists(path) || Directory.Exists(path)) &&
                (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }

        private static bool TryRollback(string destination, string backup, bool wasUpdate)
        {
            try
            {
                if (wasUpdate && File.Exists(backup))
                {
                    File.Replace(backup, destination, null, true);
                }
                else if (!wasUpdate && File.Exists(destination))
                {
                    File.Delete(destination);
                }
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private static PluginInstallResult Fail(string message)
        {
            return new PluginInstallResult(false, false, message);
        }
    }
}
