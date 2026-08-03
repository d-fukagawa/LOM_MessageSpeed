using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal enum BepInExState
    {
        GameNotSelected,
        NotInstalled,
        Partial,
        IncompatibleBepInEx5,
        IncompatibleIl2Cpp,
        IncompatibleVersion,
        MixedInstallation,
        InstalledNotInitialized,
        Ready,
        IntegrityMismatch,
        InspectionFailed
    }

    internal enum BepInExStatusTone
    {
        Neutral,
        Information,
        Success,
        Warning,
        Error
    }

    internal sealed class BepInExInspection
    {
        internal BepInExInspection(
            BepInExState state,
            BepInExStatusTone tone,
            string title,
            string message,
            string nextAction,
            string details,
            bool hasReparsePoint = false,
            string? detectedVersion = null,
            bool isVerifiedBuild = false,
            string? currentDisplay = null)
        {
            State = state;
            Tone = tone;
            Title = title;
            Message = message;
            NextAction = nextAction;
            Details = details;
            HasReparsePoint = hasReparsePoint;
            DetectedVersion = detectedVersion;
            IsVerifiedBuild = isVerifiedBuild;
            CurrentDisplay = currentDisplay ?? title;
        }

        internal BepInExState State { get; }
        internal BepInExStatusTone Tone { get; }
        internal string Title { get; }
        internal string Message { get; }
        internal string NextAction { get; }
        internal string Details { get; }
        internal bool HasReparsePoint { get; }
        internal string? DetectedVersion { get; }
        internal bool IsVerifiedBuild { get; }
        internal string CurrentDisplay { get; }
        internal bool AllowsPluginUse =>
            !HasReparsePoint &&
            (State == BepInExState.InstalledNotInitialized || State == BepInExState.Ready);
    }

    internal static class BepInExInspector
    {
        internal const string VerifiedProductVersionPrefix = "6.0.0-be.692+851521c";

        private static readonly IReadOnlyDictionary<string, string> VerifiedHashes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["winhttp.dll"] = "D3B62F4C9C3E2196EF82603C52D6B98C043A0D6C125C081FD33D7CA3798B41B8",
                [@"BepInEx\core\BepInEx.Core.dll"] = "0C3AE15668F9AEFF35FEB05D29A65A249C025C7B9418D7149404EE8F234B866E",
                [@"BepInEx\core\BepInEx.Preloader.Core.dll"] = "5D0D3C4AC7B8B92776F212EE4BD096564BBF5609ECC8FFC38C034B206176EBB4",
                [@"BepInEx\core\BepInEx.Unity.Common.dll"] = "541F377BA0445D0560664E86ECD0778A2CAC7DC862C32B8989E52AFBCF7DDDB9",
                [@"BepInEx\core\BepInEx.Unity.Mono.dll"] = "F1F0CDC821A6F7D824A4A027950FAE95E12F7DE12219B5D0F57F3AE875A935C8",
                [@"BepInEx\core\BepInEx.Unity.Mono.Preloader.dll"] = "0FCFA54344626F8F791D768538C85E115C6ED890239B039AB08BD60CF176BCA1",
                [@"BepInEx\core\0Harmony.dll"] = "93C0A24085BABF49D8478B28B8271217A76DD02A5DB68794E222BAAA3CE6F2FB",
                [@"BepInEx\core\AssetRipper.Primitives.dll"] = "677B7513CD9EAC47385B66365CADA31F8679C37BDD7E132C48E2A072DAA31C04",
                [@"BepInEx\core\Mono.Cecil.dll"] = "7AE470288FFF4A402899C254D0A76CEFEF55877F5C54F96E83C797CC5BB6E2F6",
                [@"BepInEx\core\Mono.Cecil.Mdb.dll"] = "5896D1898F616701FFF18F3B2C71E6B844D2390EF9F41E1C5FCCCE8CB27C698E",
                [@"BepInEx\core\Mono.Cecil.Pdb.dll"] = "174DB44A067F58561510AF746F3CAEB032037762C57A31C8D9EE32DB25174984",
                [@"BepInEx\core\Mono.Cecil.Rocks.dll"] = "54AC539FB5DDC8B44C0E9ACD0FCB7324F89D1A072EDF8EBC1B06DD691E3D3927",
                [@"BepInEx\core\MonoMod.RuntimeDetour.dll"] = "BD349394835AF325854045AEA90EC58117D029D2303EBB4748B177B73186AFD0",
                [@"BepInEx\core\MonoMod.Utils.dll"] = "0C6CBEDE5A816FD00F8158548F0F4011D33D1AB3B61DBEE56037604AC36D58A0",
                [@"BepInEx\core\SemanticVersioning.dll"] = "8603F22E2D552A37A9B59474E952FAD4320B53B20A902AB639E7A2ADDDF4FA3E"
            };

        private static readonly string[] RequiredFiles =
        {
            "doorstop_config.ini",
            "winhttp.dll",
            @"BepInEx\core\BepInEx.Core.dll",
            @"BepInEx\core\BepInEx.Preloader.Core.dll",
            @"BepInEx\core\BepInEx.Unity.Common.dll",
            @"BepInEx\core\BepInEx.Unity.Mono.dll",
            @"BepInEx\core\BepInEx.Unity.Mono.Preloader.dll",
            @"BepInEx\core\0Harmony.dll",
            @"BepInEx\core\AssetRipper.Primitives.dll",
            @"BepInEx\core\Mono.Cecil.dll",
            @"BepInEx\core\Mono.Cecil.Mdb.dll",
            @"BepInEx\core\Mono.Cecil.Pdb.dll",
            @"BepInEx\core\Mono.Cecil.Rocks.dll",
            @"BepInEx\core\MonoMod.RuntimeDetour.dll",
            @"BepInEx\core\MonoMod.Utils.dll",
            @"BepInEx\core\SemanticVersioning.dll"
        };

        internal static BepInExInspection Inspect(string? gameRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return Result(BepInExState.GameNotSelected, BepInExStatusTone.Neutral,
                    "ゲームフォルダを選択してください",
                    "まだBepInExの確認は行っていません。",
                    "先にゲームフォルダを選択してください。",
                    "ゲームフォルダ未選択",
                    currentDisplay: "未確認");
            }

            try
            {
                if (!GameLocator.TryValidateRoot(gameRoot, out string root, out string error))
                {
                    return Result(BepInExState.InspectionFailed, BepInExStatusTone.Error,
                        "ゲームフォルダを確認できません",
                        "フォルダを読み取れないため、BepInExの状態を確認できませんでした。ファイルは変更していません。",
                        "ゲームフォルダを確認して、もう一度お試しください。",
                        error);
                }

                bool any = Directory.Exists(Path.Combine(root, "BepInEx")) ||
                    File.Exists(Path.Combine(root, "doorstop_config.ini")) ||
                    File.Exists(Path.Combine(root, "winhttp.dll"));
                if (!any)
                {
                    return Result(BepInExState.NotInstalled, BepInExStatusTone.Information,
                        "BepInExは見つかりませんでした",
                        "LOM_MessageSpeedを使用するにはBepInExが必要です。",
                        "「導入・入れ直し手順を開く」から導入し、状態を再確認してください。",
                        "BepInExフォルダ、doorstop_config.ini、winhttp.dllはいずれも未検出",
                        currentDisplay: "BepInExは見つかりませんでした");
                }

                bool reparse = HasReparsePoint(root, RequiredFiles);
                bool mono6 = File.Exists(Path.Combine(root, "BepInEx", "core", "BepInEx.Unity.Mono.dll"));
                bool il2cpp = File.Exists(Path.Combine(root, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll")) ||
                    Directory.Exists(Path.Combine(root, "BepInEx", "interop"));
                bool bepinex5 = File.Exists(Path.Combine(root, "BepInEx", "core", "BepInEx.dll"));

                if (mono6 && (il2cpp || bepinex5))
                {
                    return Result(BepInExState.MixedInstallation, BepInExStatusTone.Warning,
                        "BepInExが見つかりました",
                        "LOM_MessageSpeedが読み込まれない可能性があります。",
                        "正常に動作しない場合は、BepInExの入れ直しをお試しください。",
                        "Unity Mono 6構成に加えて、" + DetectionNames(bepinex5, il2cpp) + "を検出",
                        reparse,
                        currentDisplay: "BepInEx（複数の構成を検出）");
                }

                if (il2cpp)
                {
                    return Result(BepInExState.IncompatibleIl2Cpp, BepInExStatusTone.Warning,
                        "BepInExが見つかりました",
                        "LOM_MessageSpeedで使用する種類とは異なります。",
                        "BepInExの入れ直しをお試しください。",
                        "BepInEx.Unity.IL2CPP.dllまたはBepInEx\\interopを検出",
                        reparse,
                        currentDisplay: "BepInEx（versionを確認できません）\r\nUnity IL2CPP / Windows");
                }

                if (bepinex5)
                {
                    return Result(BepInExState.IncompatibleBepInEx5, BepInExStatusTone.Warning,
                        "BepInExが見つかりました",
                        "LOM_MessageSpeedで使用するversionとは異なります。",
                        "BepInExの入れ直しをお試しください。",
                        @"BepInEx\core\BepInEx.dllを検出",
                        reparse,
                        currentDisplay: "BepInEx 5\r\nUnity Mono / Windows");
                }

                string corePath = Path.Combine(root, "BepInEx", "core", "BepInEx.Core.dll");
                string? version = null;
                if (File.Exists(corePath))
                {
                    FileVersionInfo info = FileVersionInfo.GetVersionInfo(corePath);
                    version = info.ProductVersion;
                    if (info.FileMajorPart > 0 && info.FileMajorPart != 6)
                    {
                        return Result(BepInExState.IncompatibleVersion, BepInExStatusTone.Warning,
                            "BepInExが見つかりました",
                            "LOM_MessageSpeedで使用するversionとは異なります。",
                            "正常に動作しない場合は、BepInExの入れ直しをお試しください。",
                            "検出version: " + (version ?? "不明"),
                            reparse,
                            version,
                            currentDisplay: FormatCurrent(version, "Unity Mono / Windows"));
                    }
                }

                List<string> missing = new List<string>();
                foreach (string relative in RequiredFiles)
                {
                    if (!File.Exists(Path.Combine(root, relative)))
                    {
                        missing.Add(relative);
                    }
                }
                if (!Directory.Exists(Path.Combine(root, "BepInEx", "plugins")))
                {
                    missing.Add(@"BepInEx\plugins");
                }

                if (missing.Count > 0)
                {
                    return Result(BepInExState.Partial, BepInExStatusTone.Information,
                        "BepInExは見つかりませんでした",
                        "BepInExの導入が途中の可能性があります。",
                        "導入・入れ直し手順を確認し、状態を再確認してください。",
                        "不足: " + string.Join(", ", missing),
                        reparse,
                        version,
                        currentDisplay: "BepInExは見つかりませんでした");
                }

                bool initialized = File.Exists(Path.Combine(root, "BepInEx", "config", "BepInEx.cfg")) &&
                    (File.Exists(Path.Combine(root, "BepInEx", "LogOutput.log")) ||
                     File.Exists(Path.Combine(root, "BepInEx", "LogOutput.txt")));
                return InspectCompleteInstallation(root, reparse, version, initialized);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            {
                return Result(BepInExState.InspectionFailed, BepInExStatusTone.Error,
                    "BepInExを確認できません",
                    "ファイルを読み取れなかったため、状態を確認できませんでした。既存ファイルは変更していません。",
                    "ゲームを終了し、フォルダを読み取れることを確認してから再試行してください。",
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal static BepInExInspection InspectCompleteInstallation(
            string root,
            bool reparse,
            string? version,
            bool initialized)
        {
            bool verifiedBuild = version != null &&
                version.StartsWith(VerifiedProductVersionPrefix, StringComparison.OrdinalIgnoreCase);
            try
            {
                if (reparse)
                {
                    return Result(initialized ? BepInExState.Ready : BepInExState.InstalledNotInitialized,
                        BepInExStatusTone.Warning,
                        "BepInExは見つかりましたが確認が必要です",
                        "リンクされたフォルダまたはファイルが含まれるため、プラグインのインストールを一時停止しています。",
                        "既存ファイルを変更せず、「問題報告用の詳細情報」を確認してください。",
                        "シンボリックリンクまたはジャンクションを検出。リンク先のSHA-256は読み取っていません",
                        true,
                        version,
                        false,
                        FormatCurrent(version, "Unity Mono / Windows x64"));
                }

                if (verifiedBuild)
                {
                    List<string> mismatches = FindHashMismatches(root);
                    if (mismatches.Count > 0)
                    {
                        return Result(BepInExState.IntegrityMismatch, BepInExStatusTone.Error,
                            "BepInExのファイルを確認できません",
                            "動作確認済みの内容と一致しません。",
                            "公式配布元からの入れ直しをおすすめします。",
                            "SHA-256不一致: " + string.Join(", ", mismatches),
                            reparse,
                            version,
                            currentDisplay: FormatCurrent(version, "Unity Mono / Windows x64"));
                    }
                }

                BepInExStatusTone tone = verifiedBuild ? BepInExStatusTone.Success : BepInExStatusTone.Warning;
                string verification = "BepInExのバージョンが異なっていても、LOM_MessageSpeedが正常に動作する場合はそのまま使用できます。";
                return initialized
                    ? Result(BepInExState.Ready, tone,
                        "BepInExが見つかりました",
                        verification,
                        "正常に動作しない場合は、BepInExの入れ直しをお試しください。",
                        "検出version: " + (version ?? "不明") + "。検出runtime: Unity Mono / Windows x64。Plugin操作: 有効",
                        false,
                        version,
                        verifiedBuild,
                        FormatCurrent(version, "Unity Mono / Windows x64"))
                    : Result(BepInExState.InstalledNotInitialized, tone,
                        "BepInExが見つかりました",
                        verification,
                        "ゲームを起動してタイトル画面まで進み、終了後に「状態を再確認」を押してください。",
                        "検出version: " + (version ?? "不明") + "。検出runtime: Unity Mono / Windows x64。BepInEx.cfgと初期logは未検出。Plugin操作: 有効",
                        false,
                        version,
                        verifiedBuild,
                        FormatCurrent(version, "Unity Mono / Windows x64"));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return Result(BepInExState.InspectionFailed, BepInExStatusTone.Error,
                    "BepInExを確認できません",
                    "ファイルを読み取れなかったため、状態を確認できませんでした。既存ファイルは変更していません。",
                    "ゲームを終了し、フォルダを読み取れることを確認してから再試行してください。",
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static BepInExInspection Result(
            BepInExState state,
            BepInExStatusTone tone,
            string title,
            string message,
            string nextAction,
            string details,
            bool hasReparsePoint = false,
            string? detectedVersion = null,
            bool isVerifiedBuild = false,
            string? currentDisplay = null)
        {
            bool allowsPluginUse = !hasReparsePoint &&
                (state == BepInExState.InstalledNotInitialized || state == BepInExState.Ready);
            string reportDetails = details +
                "。検出version: " + (detectedVersion ?? "取得不能") +
                "。Plugin操作: " + (allowsPluginUse ? "有効" : "無効") +
                (hasReparsePoint ? "（reparse pointを検出）" : string.Empty);
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(profile))
            {
                reportDetails = reportDetails.Replace(profile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
            }
            return new BepInExInspection(state, tone, title, message, nextAction, reportDetails,
                hasReparsePoint, detectedVersion, isVerifiedBuild, currentDisplay);
        }

        private static string FormatCurrent(string? version, string runtime)
        {
            string displayVersion = string.IsNullOrWhiteSpace(version)
                ? "BepInEx（versionを確認できません）"
                : "BepInEx " + version.Split('+')[0];
            return displayVersion + "\r\n" + runtime;
        }

        private static List<string> FindHashMismatches(string root)
        {
            List<string> mismatches = new List<string>();
            foreach (KeyValuePair<string, string> expected in VerifiedHashes)
            {
                string path = Path.Combine(root, expected.Key);
                using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                string actual = Convert.ToHexString(SHA256.HashData(stream));
                if (!string.Equals(actual, expected.Value, StringComparison.OrdinalIgnoreCase))
                {
                    mismatches.Add(expected.Key);
                }
            }
            return mismatches;
        }

        private static string DetectionNames(bool bepinex5, bool il2cpp)
        {
            if (bepinex5 && il2cpp) return "BepInEx 5とUnity IL2CPP版の特徴ファイル";
            return bepinex5 ? "BepInEx 5の特徴ファイル" : "Unity IL2CPP版の特徴ファイル";
        }

        private static bool HasReparsePoint(string root, IEnumerable<string> files)
        {
            foreach (string directory in new[]
            {
                Path.Combine(root, "BepInEx"),
                Path.Combine(root, "BepInEx", "core"),
                Path.Combine(root, "BepInEx", "plugins"),
                Path.Combine(root, "BepInEx", "config")
            })
            {
                if (Directory.Exists(directory) && (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }
            }

            foreach (string relative in files)
            {
                string path = Path.Combine(root, relative);
                if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
