using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using LOM.MessageSpeed.ConfigEditor;

namespace LOM.MessageSpeed.ConfigEditor.Tests
{
    internal static class Program
    {
        private static int passed;
        private static string root = string.Empty;

        [STAThread]
        private static int Main(string[] args)
        {
            root = Path.Combine(Path.GetTempPath(), "LOM_ConfigEditor_Tests_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(root);
            try
            {
                Run("正常cfg・コメント・未知項目・順序を保持", TestPreservation);
                Run("UTF-8 BOMを保持", TestBom);
                Run("LF改行を保持", TestLf);
                Run("bool true/false", TestBooleans);
                Run("有効な倍率境界と代表値", TestValidValues);
                Run("不正値を拒否", TestInvalidValues);
                Run("重複キーを拒否", TestDuplicates);
                Run("対象キーの大文字小文字を厳密照合", TestExactNames);
                Run("日本語カルチャでも小数点はInvariant", TestJapaneseCulture);
                Run("外部変更を検出", TestExternalChange);
                Run("読み取り専用を拒否", TestReadOnly);
                Run("ロック中cfgの保存失敗で元ファイルを保持", TestLockedConfig);
                Run("置換失敗で元ファイルを保持し一時ファイルを除去", TestReplaceFailure);
                Run("原子的置換と1世代バックアップ", TestBackup);
                Run("未生成cfgの明示的新規作成経路", TestMissingCreation);
                Run("内蔵cfgテンプレート", TestEmbeddedTemplate);
                Run("ゲーム終了後の外部更新へ画面値を再適用", TestRebasedSave);
                Run("ゲームルートと設定パス検証", TestGameRootValidation);
                Run("BepInExなしでもLevel 1ゲームルートを受理", TestLevelOneGameRoot);
                Run("BepInEx状態モデル", TestBepInExStates);
                Run("BepInEx案内定数", TestBepInExSupportInfo);
                Run("BepInEx簡素表示とアクセシビリティ", TestBepInExPresentation);
                Run("BepInEx前提なしのプラグイン導入を拒否", TestPluginRequiresBepInEx);
                Run("対象ゲームプロセス起動中の保存禁止判定", TestGameProcessGuard);
                Run("Portrait未知設定を変更せず保持", TestPortraitPreservation);
                Run("ドライブテンプレート", TestDriveTemplate);
                Run("保存済みゲームルートの起動時自動選択", TestSavedStartupRoot);
                Run("起動時候補の一意選択と複数候補拒否", TestStartupCandidates);
                Run("ツール設定の保存・破損復旧", TestToolSettings);
                Run("未承認payloadをfail closed", TestUnapprovedPayload);
                Run("プラグイン未導入判定", TestPluginInspection);
                Run("プラグイン新規導入・同一版無変更", TestPluginInstall);
                Run("プラグイン更新・1世代backup・cfg保持", TestPluginUpdate);
                Run("ゲーム起動中のプラグイン導入拒否", TestPluginRunningGuard);
                Run("未知版とflat配置を自動上書きしない", TestUnknownAndFlatRefusal);
                Run("重複配置を検出", TestDuplicateInspection);
                Run("確認後のDLL変更を拒否", TestInspectionRace);
                Run("ゲームルート外パスを拒否", TestOutsidePathRefusal);
                Run("inspectorの同版別hash・新版・破損状態", TestInspectorConflictStates);
                Run("readonlyとlock中DLLを拒否", TestPluginFileGuards);
                Run("reparse point導入経路を拒否", TestReparsePointRefusal);
                Run("導入後検証失敗でrollback", TestPostInstallRollback);
                if (Array.IndexOf(args, "--require-embedded-payload") >= 0)
                {
                    Run("承認済み内蔵resourceのidentityと新規導入", TestApprovedEmbeddedPayload);
                }
                int bepinexRootIndex = Array.IndexOf(args, "--verify-bepinex-root");
                if (bepinexRootIndex >= 0)
                {
                    if (bepinexRootIndex + 1 >= args.Length)
                    {
                        throw new ArgumentException("--verify-bepinex-rootにはpathが必要です");
                    }
                    string bepinexRoot = args[bepinexRootIndex + 1];
                    Run("公式BepInEx be.692の構成・version・SHA-256", delegate { TestOfficialBepInEx(bepinexRoot); });
                }
                int renderIndex = Array.IndexOf(args, "--render-bepinex-ui");
                if (renderIndex >= 0)
                {
                    if (renderIndex + 1 >= args.Length)
                    {
                        throw new ArgumentException("--render-bepinex-uiには出力directoryが必要です");
                    }
                    RenderBepInExUi(args[renderIndex + 1]);
                }
                Console.WriteLine("PASS: " + passed.ToString(CultureInfo.InvariantCulture) + " tests");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL after " + passed.ToString(CultureInfo.InvariantCulture) + " tests: " + ex);
                return 1;
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestPreservation()
        {
            const string original = "# header\r\n[General]\r\nEnabled = true\r\nUnknown = keep\r\n\r\n[Other]\r\nEnabled = untouched\r\n\r\n[Message]\r\n; SpeedMultiplier = 9\r\nSpeedMultiplier = 1.5\r\nTail = keep\r\n\r\n[Portrait]\r\nMotionSpeedMultiplier = 1.0\r\nTransitionSpeedMultiplier = 1.0\r\n";
            string path = Write("preserve.cfg", original, false);
            ConfigDocument.Load(path).Save(false, 2.0m);
            const string expected = "# header\r\n[General]\r\nEnabled = false\r\nUnknown = keep\r\n\r\n[Other]\r\nEnabled = untouched\r\n\r\n[Message]\r\n; SpeedMultiplier = 9\r\nSpeedMultiplier = 2.0\r\nTail = keep\r\n\r\n[Portrait]\r\nMotionSpeedMultiplier = 1.0\r\nTransitionSpeedMultiplier = 1.0\r\n";
            Equal(expected, ReadText(path), "文字送り2キー以外が変わりました");
        }

        private static void TestBom()
        {
            string path = Write("bom.cfg", Minimal("true", "1.5", "\r\n"), true);
            ConfigDocument.Load(path).Save(true, 0.5m);
            byte[] bytes = File.ReadAllBytes(path);
            True(bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf, "BOMが失われました");
        }

        private static void TestLf()
        {
            string path = Write("lf.cfg", Minimal("true", "1.5", "\n"), false);
            ConfigDocument.Load(path).Save(false, 1.0m);
            True(!ReadText(path).Contains('\r'), "LFがCRLFへ変わりました");
        }

        private static void TestBooleans()
        {
            foreach (string value in new[] { "true", "false", "TRUE", "False" })
            {
                string path = Write("bool-" + value + ".cfg", Minimal(value, "1.5", "\r\n"), false);
                ConfigDocument.Load(path);
            }
            ExpectConfigError(delegate { ConfigDocument.Load(Write("bad-bool.cfg", Minimal("yes", "1.5", "\r\n"), false)); });
        }

        private static void TestValidValues()
        {
            foreach (string value in new[] { "0.1", "0.2", "0.5", "1.0", "1.5", "2.0", "10.0" })
            {
                ConfigDocument doc = ConfigDocument.Load(Write("valid-" + value.Replace('.', '-') + ".cfg", Minimal("true", value, "\r\n"), false));
                Equal(decimal.Parse(value, CultureInfo.InvariantCulture), doc.SpeedMultiplier, "倍率解析不一致");
            }
        }

        private static void TestInvalidValues()
        {
            foreach (string value in new[] { "0", "-1", "NaN", "Infinity", "0.09", "10.1", "", "abc", "1,5" })
            {
                string local = value;
                ExpectConfigError(delegate { ConfigDocument.Load(Write("invalid-" + Guid.NewGuid().ToString("N") + ".cfg", Minimal("true", local, "\r\n"), false)); });
            }
        }

        private static void TestDuplicates()
        {
            string duplicateEnabled = Minimal("true", "1.5", "\r\n").Replace("Enabled = true", "Enabled = true\r\nEnabled = false", StringComparison.Ordinal);
            string duplicateMultiplier = Minimal("true", "1.5", "\r\n") + "[Message]\r\nSpeedMultiplier = 2.0\r\n";
            ExpectConfigError(delegate { ConfigDocument.Load(Write("dup-e.cfg", duplicateEnabled, false)); });
            ExpectConfigError(delegate { ConfigDocument.Load(Write("dup-m.cfg", duplicateMultiplier, false)); });
        }

        private static void TestExactNames()
        {
            ExpectConfigError(delegate { ConfigDocument.Load(Write("case.cfg", Minimal("true", "1.5", "\r\n").Replace("Enabled", "enabled", StringComparison.Ordinal), false)); });
        }

        private static void TestJapaneseCulture()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
                string path = Write("culture.cfg", Minimal("true", "1.5", "\r\n"), false);
                ConfigDocument.Load(path).Save(true, 1.5m);
                True(ReadText(path).Contains("SpeedMultiplier = 1.5", StringComparison.Ordinal), "小数点がInvariantではありません");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        private static void TestExternalChange()
        {
            string path = Write("external.cfg", Minimal("true", "1.5", "\r\n"), false);
            ConfigDocument doc = ConfigDocument.Load(path);
            File.AppendAllText(path, "# external\r\n", Encoding.UTF8);
            bool caught = false;
            try { doc.Save(false, 2.0m); } catch (ConfigChangedException) { caught = true; }
            True(caught, "外部変更が拒否されませんでした");
            True(ReadText(path).EndsWith("# external\r\n", StringComparison.Ordinal), "外部変更が上書きされました");
        }

        private static void TestReadOnly()
        {
            string path = Write("readonly.cfg", Minimal("true", "1.5", "\r\n"), false);
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
            try
            {
                ExpectConfigError(delegate { ConfigDocument.Load(path).Save(false, 2.0m); });
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        private static void TestLockedConfig()
        {
            string original = Minimal("true", "1.5", "\r\n");
            string path = Write("locked.cfg", original, false);
            ConfigDocument doc = ConfigDocument.Load(path);
            using (FileStream locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                ExpectConfigError(delegate { doc.Save(false, 2.0m); });
            }
            Equal(original, ReadText(path), "ロック中の元cfgが変わりました");
        }

        private static void TestReplaceFailure()
        {
            string original = Minimal("true", "1.5", "\r\n");
            string path = Write("replace-failure.cfg", original, false);
            string backup = Write("replace-failure.cfg.bak", "previous backup", false);
            ConfigDocument doc = ConfigDocument.Load(path);
            using (FileStream locked = new FileStream(backup, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                ExpectConfigError(delegate { doc.Save(false, 2.0m); });
            }
            Equal(original, ReadText(path), "置換失敗時の元cfgが変わりました");
            True(Directory.GetFiles(root, ".lom-messagespeed.*.tmp").Length == 0, "一時ファイルが残りました");
        }

        private static void TestBackup()
        {
            string first = Minimal("true", "1.5", "\r\n");
            string path = Write("backup.cfg", first, false);
            ConfigDocument.Load(path).Save(false, 2.0m);
            Equal(first, ReadText(path + ".bak"), "初回バックアップ不一致");
            string second = ReadText(path);
            ConfigDocument.Load(path).Save(true, 5.0m);
            Equal(second, ReadText(path + ".bak"), "バックアップが1世代で更新されません");
            True(Directory.GetFiles(Path.GetDirectoryName(path)!, "backup.cfg.bak*").Length == 1, "複数世代のバックアップがあります");
        }

        private static void TestMissingCreation()
        {
            string path = Path.Combine(root, "created.cfg");
            ConfigDocument doc = ConfigDocument.Load(path);
            True(!doc.Exists, "未生成判定が誤っています");
            doc.Save(true, 1.5m);
            Equal(Minimal("true", "1.5", "\r\n"), ReadText(path), "最小設定の内容が不正です");
            True(!File.Exists(path + ".bak"), "新規作成時に不要なbakがあります");
        }

        private static void TestEmbeddedTemplate()
        {
            Equal(Minimal("true", "1.5", "\r\n"), ConfigTemplate.Load(), "内蔵テンプレート不一致");
        }

        private static void TestRebasedSave()
        {
            string path = Write("rebase.cfg", Minimal("true", "1.5", "\r\n"), false);
            ConfigDocument stale = ConfigDocument.Load(path);
            string latest = Minimal("true", "3.0", "\r\n") + "\r\n[GameAdded]\r\nValue = keep\r\n";
            File.WriteAllText(path, latest, new UTF8Encoding(false));

            ConfigSaveResult result = ConfigSaveCoordinator.Save(
                stale,
                path,
                false,
                2.0m,
                delegate { return true; });

            True(result.Rebased, "外部更新へ再適用した判定になりません");
            string saved = ReadText(path);
            True(saved.Contains("Enabled = false", StringComparison.Ordinal), "画面のEnabledが保存されません");
            True(saved.Contains("SpeedMultiplier = 2.0", StringComparison.Ordinal), "画面の倍率が保存されません");
            True(saved.Contains("[GameAdded]\r\nValue = keep", StringComparison.Ordinal), "ゲーム追加項目が失われました");
            Equal(latest, ReadText(path + ".bak"), "最新cfgがbackupされません");

            string blockedPath = Write("rebase-blocked.cfg", Minimal("true", "1.5", "\r\n"), false);
            ConfigDocument blocked = ConfigDocument.Load(blockedPath);
            File.WriteAllText(blockedPath, latest, new UTF8Encoding(false));
            ExpectConfigError(delegate
            {
                ConfigSaveCoordinator.Save(
                    blocked,
                    blockedPath,
                    false,
                    2.0m,
                    delegate { return false; });
            });
            Equal(latest, ReadText(blockedPath), "ゲーム状態を再確認できないのに保存されました");
        }

        private static void TestGameRootValidation()
        {
            string game = Path.Combine(root, "game");
            Directory.CreateDirectory(Path.Combine(game, "BepInEx", "config"));
            File.WriteAllBytes(Path.Combine(game, "Mortal.exe"), Array.Empty<byte>());
            string validated;
            string error;
            True(GameLocator.TryValidateRoot(game, out validated, out error), error);
            Equal(Path.Combine(game, "BepInEx", "config", "lom-messagespeed.cfg"), GameLocator.GetConfigPath(validated), "設定パス不一致");
            True(!GameLocator.TryValidateRoot(root, out validated, out error), "不正ルートを受理しました");
            True(!GameLocator.TryValidateRoot(".", out validated, out error), "相対パスを受理しました");
        }

        private static void TestLevelOneGameRoot()
        {
            string game = CreateLevelOneRoot("level-one-game");
            True(GameLocator.TryValidateRoot(game, out string validated, out string error), error);
            Equal(Path.GetFullPath(game), validated, "Level 1ルート不一致");
            Equal(BepInExState.NotInstalled, BepInExInspector.Inspect(game).State, "BepInExなしを誤認しました");
            StartupGameSelection saved = StartupGameLocator.Select(game, Array.Empty<string>());
            Equal(StartupGameSelectionState.Found, saved.State, "保存済みLevel 1ルートを再確認できません");

            string japanese = CreateLevelOneRoot("長い日本語パス-活俠傳-ゲームフォルダ確認");
            Equal(BepInExState.NotInstalled, BepInExInspector.Inspect(japanese).State, "日本語pathを確認できません");
        }

        private static void TestBepInExStates()
        {
            Equal(BepInExState.GameNotSelected, BepInExInspector.Inspect(null).State, "未選択状態不一致");

            string partial = CreateLevelOneRoot("bepinex-partial");
            Directory.CreateDirectory(Path.Combine(partial, "BepInEx"));
            BepInExInspection partialInspection = BepInExInspector.Inspect(partial);
            Equal(BepInExState.Partial, partialInspection.State, "空フォルダを部分導入にしませんでした");
            Equal(BepInExStatusTone.Information, partialInspection.Tone, "準備途中が赤色以外の案内になりません");
            Equal("BepInExは見つかりませんでした", partialInspection.CurrentDisplay, "準備途中の通常表示が複雑です");

            string unsupported = CreateLevelOneRoot("bepinex-v5");
            Directory.CreateDirectory(Path.Combine(unsupported, "BepInEx", "core"));
            File.WriteAllBytes(Path.Combine(unsupported, "BepInEx", "core", "BepInEx.dll"), Array.Empty<byte>());
            Equal(BepInExState.IncompatibleBepInEx5, BepInExInspector.Inspect(unsupported).State, "BepInEx 5候補を対応済みにしました");

            string il2cpp = CreateLevelOneRoot("bepinex-il2cpp");
            Directory.CreateDirectory(Path.Combine(il2cpp, "BepInEx", "core"));
            File.WriteAllBytes(Path.Combine(il2cpp, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll"), Array.Empty<byte>());
            Equal(BepInExState.IncompatibleIl2Cpp, BepInExInspector.Inspect(il2cpp).State, "IL2CPPを個別判定できません");

            string mixed = CreateGameRoot("bepinex-mixed");
            File.WriteAllBytes(Path.Combine(mixed, "BepInEx", "core", "BepInEx.dll"), Array.Empty<byte>());
            Equal(BepInExState.MixedInstallation, BepInExInspector.Inspect(mixed).State, "混在構成を個別判定できません");

            string installed = CreateGameRoot("bepinex-installed");
            BepInExInspection before = BepInExInspector.Inspect(installed);
            Equal(BepInExState.InstalledNotInitialized, before.State, "初回起動前状態不一致");
            True(before.AllowsPluginUse, "構造が揃った未確認versionでPlugin操作が許可されません");
            Equal(BepInExStatusTone.Warning, before.Tone, "未確認versionが互換性注意になりません");

            BepInExInspection otherVersion = BepInExInspector.InspectCompleteInstallation(
                installed, false, "6.0.0-be.785+test", false);
            True(otherVersion.AllowsPluginUse, "別のBepInEx 6 buildでPlugin操作が禁止されました");
            Equal(BepInExStatusTone.Warning, otherVersion.Tone, "別buildが互換性注意になりません");
            True(otherVersion.CurrentDisplay.Contains("BepInEx 6.0.0-be.785", StringComparison.Ordinal), "別buildの現在versionが表示されません");
            True(otherVersion.Message.Contains("そのまま使用できます", StringComparison.Ordinal), "正常動作時の継続案内がありません");

            BepInExInspection integrity = BepInExInspector.InspectCompleteInstallation(
                installed, false, BepInExInspector.VerifiedProductVersionPrefix, false);
            Equal(BepInExState.IntegrityMismatch, integrity.State, "既知versionのSHA不一致を検出できません");
            Equal(BepInExStatusTone.Error, integrity.Tone, "SHA不一致が完全性エラーになりません");
            True(!integrity.AllowsPluginUse, "SHA不一致でPlugin操作が許可されました");
            True(integrity.Message.Contains("一致しません", StringComparison.Ordinal), "SHA不一致の平易な説明がありません");
            True(!integrity.Message.Contains("マルウェア", StringComparison.Ordinal), "SHA不一致をマルウェアと断定しています");

            BepInExInspection unknownVersion = BepInExInspector.InspectCompleteInstallation(
                installed, false, null, false);
            True(unknownVersion.CurrentDisplay.Contains("versionを確認できません", StringComparison.Ordinal), "version取得不能の通常表示が不一致です");
            True(unknownVersion.AllowsPluginUse, "version取得不能だけでPlugin操作が禁止されました");

            Directory.CreateDirectory(Path.Combine(installed, "BepInEx", "config"));
            File.WriteAllText(Path.Combine(installed, "BepInEx", "config", "BepInEx.cfg"), string.Empty);
            File.WriteAllText(Path.Combine(installed, "BepInEx", "LogOutput.log"), string.Empty);
            Equal(BepInExState.Ready, BepInExInspector.Inspect(installed).State, "初期化済み状態不一致");

            string similar = CreateLevelOneRoot("bepinex-similar-name");
            Directory.CreateDirectory(Path.Combine(similar, "BepInEx-old"));
            File.WriteAllText(Path.Combine(similar, "winhttp.dll.old"), string.Empty);
            Equal(BepInExState.NotInstalled, BepInExInspector.Inspect(similar).State, "類似名をBepInExとして誤認しました");

            string allMessages = string.Join(" ", new[]
            {
                partialInspection.Message,
                BepInExInspector.Inspect(unsupported).Message,
                BepInExInspector.Inspect(il2cpp).Message
            });
            True(!allMessages.Contains("自動操作は行いません", StringComparison.Ordinal), "不安を招く旧文言が残っています");
        }

        private static void TestBepInExSupportInfo()
        {
            True(BepInExSupportInfo.WhyRequired.Length >= 100 && BepInExSupportInfo.WhyRequired.Length <= 200, "必要理由が100～200文字ではありません");
            True(BepInExSupportInfo.OfficialGuideUrl.StartsWith("https://docs.bepinex.dev/", StringComparison.Ordinal), "公式HTTPS URLではありません");
            True(BepInExSupportInfo.OfficialBuildsUrl.StartsWith("https://builds.bepinex.dev/", StringComparison.Ordinal), "公式build URLではありません");
            True(BepInExSupportInfo.RequiredPackage.Contains("Unity.Mono-win-x64", StringComparison.Ordinal), "対象package表記が不一致です");
            Equal("6.0.0-be.692", BepInExSupportInfo.VerifiedVersion, "動作確認versionが不一致です");
            Equal("Unity Mono / Windows x64", BepInExSupportInfo.VerifiedRuntime, "動作確認runtimeが不一致です");
            Equal(64, BepInExSupportInfo.VerifiedPackageSha256.Length, "ZIP SHA-256の長さが不一致です");
            Equal(
                "https://gist.github.com/d-fukagawa/7557dd9f2128d2ac59fec677a31541f1",
                BepInExSupportInfo.ReinstallGuideUrl,
                "公開Gist URLが不一致です");
            True(BepInExSupportInfo.ReinstallGuideUrl.StartsWith("https://gist.github.com/d-fukagawa/", StringComparison.Ordinal),
                "Gist URLのschemeまたはownerが不一致です");
        }

        private static void TestBepInExPresentation()
        {
            using MainForm form = new MainForm(Path.Combine(root, "presentation.settings"));
            TabControl tabs = GetField<TabControl>(form, "tabs");
            TabPage tab = GetField<TabPage>(form, "bepinexTab");
            tabs.SelectedTab = tab;
            form.CreateControl();
            form.PerformLayout();

            Label verified = GetField<Label>(form, "supportVerifiedValue");
            Label current = GetField<Label>(form, "supportStateValue");
            Label message = GetField<Label>(form, "supportMessageValue");
            Label next = GetField<Label>(form, "supportNextValue");
            Label details = GetField<Label>(form, "supportDetailsValue");
            Button guide = GetField<Button>(form, "openReinstallGuideButton");
            Button toggle = GetField<Button>(form, "toggleBepInExDetailsButton");

            True(verified.Text.Contains(BepInExSupportInfo.VerifiedVersion, StringComparison.Ordinal), "想定versionが表示されません");
            True(verified.Text.Contains(BepInExSupportInfo.VerifiedRuntime, StringComparison.Ordinal), "想定runtimeが表示されません");
            True(!string.IsNullOrWhiteSpace(current.Text), "現在入っているものが表示されません");
            True(!string.IsNullOrWhiteSpace(message.Text), "継続条件が表示されません");
            True(!string.IsNullOrWhiteSpace(next.Text), "問題時の次操作が表示されません");
            Equal("導入・入れ直し手順を開く", guide.Text, "Gistボタンの文言が不一致です");
            True(guide.Enabled, "公開済みGistボタンが有効になりません");
            True(guide.TabStop, "Gistボタンへキーボードで移動できません");
            Equal("問題報告用の詳細情報", toggle.Text, "詳細ボタンの名称が不一致です");
            True(!details.Visible, "問題報告用の詳細情報が既定で開いています");
            True(current.AccessibleName != verified.AccessibleName, "想定と現在をスクリーンリーダーで区別できません");
        }

        private static void RenderBepInExUi(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            RenderBepInExUiAtScale(outputDirectory, 1.0f, "bepinex-support-100.png");
            RenderBepInExUiAtScale(outputDirectory, 1.5f, "bepinex-support-150.png");
        }

        private static void RenderBepInExUiAtScale(string outputDirectory, float scale, string fileName)
        {
            using MainForm form = new MainForm(Path.Combine(root, "render-" + fileName + ".settings"));
            TabControl tabs = GetField<TabControl>(form, "tabs");
            tabs.SelectedTab = GetField<TabPage>(form, "bepinexTab");
            if (scale != 1.0f)
            {
                form.Scale(new SizeF(scale, scale));
            }
            form.Show();
            Application.DoEvents();
            form.PerformLayout();
            using Bitmap bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(Path.Combine(outputDirectory, fileName));
            form.Hide();
        }

        private static T GetField<T>(object target, string name) where T : class
        {
            FieldInfo? field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return (T?)field?.GetValue(target) ?? throw new InvalidOperationException("field not found: " + name);
        }

        private static void TestOfficialBepInEx(string gameRoot)
        {
            BepInExInspection inspection = BepInExInspector.Inspect(gameRoot);
            Equal(BepInExState.InstalledNotInitialized, inspection.State, "公式be.692を初回起動前と判定できません");
            Equal(BepInExStatusTone.Success, inspection.Tone, "公式be.692が確認済みになりません");
            True(inspection.IsVerifiedBuild, "公式be.692のversionとSHA-256を確認できません");
            True(inspection.AllowsPluginUse, "公式be.692でPlugin操作が許可されません");
        }

        private static void TestPluginRequiresBepInEx()
        {
            string game = CreateLevelOneRoot("plugin-without-bepinex");
            byte[] payload = TestPayload();
            PluginInstallResult result = new PluginInstaller(delegate { return new GameRunningStatus(false, "停止中"); })
                .Install(game, TestManifest(payload));
            True(!result.Success && !result.Changed, "BepInExなしでPluginを書き込みました");
            True(!File.Exists(GameLocator.GetPluginPath(game)), "BepInExなしでDLLが作成されました");
        }

        private static void TestGameProcessGuard()
        {
            string game = Path.Combine(root, "running-game");
            Directory.CreateDirectory(game);
            string mortal = Path.Combine(game, "Mortal.exe");
            File.Copy(Path.Combine(Environment.SystemDirectory, "timeout.exe"), mortal);
            using Process process = Process.Start(new ProcessStartInfo
            {
                FileName = mortal,
                Arguments = "/T 20 /NOBREAK",
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("テスト用プロセスを開始できません");
            try
            {
                GameRunningStatus status = GameProcessGuard.Check(game);
                True(status.BlocksSave, "対象ゲームプロセス起動中に保存可能と判定されました");
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        private static void TestPortraitPreservation()
        {
            string portrait = "\r\n[Portrait]\r\nMotionSpeedMultiplier = 2.0\r\nTransitionSpeedMultiplier = 0.5\r\n";
            string path = Write("portrait.cfg", Minimal("true", "1.5", "\r\n") + portrait, false);
            ConfigDocument.Load(path).Save(false, 2.0m);
            True(ReadText(path).EndsWith(portrait, StringComparison.Ordinal), "Portrait設定が変更されました");
        }

        private static void TestDriveTemplate()
        {
            Equal(@"C:\SteamLibrary\steamapps\common\LegendOfMortal", GameLocationOptions.GetDriveTemplate("C:"), "Cドライブテンプレート不一致");
            True(GameLocationOptions.GetDriveCandidates("C:").Count == 2, "Cドライブ補助候補がありません");
            Equal(@"D:\SteamLibrary\steamapps\common\LegendOfMortal", GameLocationOptions.GetDriveTemplate("D:"), "Dドライブテンプレート不一致");
        }

        private static void TestSavedStartupRoot()
        {
            string saved = CreateGameRoot("saved-startup-game");
            string fallback = CreateGameRoot("unused-fallback-game");
            StartupGameSelection selection = StartupGameLocator.Select(saved, new[] { fallback });
            Equal(StartupGameSelectionState.Found, selection.State, "保存済みルートを自動選択できません");
            Equal(Path.GetFullPath(saved), selection.Root, "保存済みルート不一致");
            True(selection.UsedSavedRoot, "保存済みルート使用判定がありません");
        }

        private static void TestStartupCandidates()
        {
            string first = CreateGameRoot("startup-first-game");
            string second = CreateGameRoot("startup-second-game");
            StartupGameSelection one = StartupGameLocator.Select(
                Path.Combine(root, "missing-saved-game"),
                new[] { first, first, Path.Combine(root, "invalid") });
            Equal(StartupGameSelectionState.Found, one.State, "一意候補を自動選択できません");
            Equal(Path.GetFullPath(first), one.Root, "一意候補不一致");
            True(!one.UsedSavedRoot, "fallbackが保存済み扱いになりました");

            StartupGameSelection multiple = StartupGameLocator.Select(
                string.Empty,
                new[] { first, second });
            Equal(StartupGameSelectionState.Multiple, multiple.State, "複数候補を自動選択しました");

            StartupGameSelection none = StartupGameLocator.Select(
                string.Empty,
                new[] { Path.Combine(root, "invalid") });
            Equal(StartupGameSelectionState.NotFound, none.State, "不正候補を受理しました");
        }

        private static void TestToolSettings()
        {
            string path = Path.Combine(root, "settings", "settings.json");
            ToolSettings source = new ToolSettings
            {
                LocationMode = GameLocationMode.Manual,
                LastDrive = "D:",
                LastValidatedManualPath = @"D:\Games\LegendOfMortal",
                LastValidatedGameRoot = @"D:\Games\LegendOfMortal",
                LastSelectedTab = 1
            };
            source.Save(path);
            ToolSettings loaded = ToolSettings.Load(path, out string? warning);
            True(warning == null, "正常settingsで警告されました");
            Equal(GameLocationMode.Manual, loaded.LocationMode, "選択方式不一致");
            Equal("D:", loaded.LastDrive, "ドライブ不一致");
            Equal(@"D:\Games\LegendOfMortal", loaded.LastValidatedGameRoot, "確定ゲームルート不一致");
            File.WriteAllText(path, "{broken", Encoding.UTF8);
            ToolSettings fallback = ToolSettings.Load(path, out warning);
            True(warning != null, "破損settingsで警告がありません");
            Equal(GameLocationMode.Drive, fallback.LocationMode, "破損時に既定値へ戻りません");
        }

        private static void TestUnapprovedPayload()
        {
            string game = CreateGameRoot("no-payload-game");
            PluginManifest noPayload = new PluginManifest(
                new Version(1, 0),
                new string('A', 64),
                ConfigSchema.SchemaVersion);
            PluginInstallResult result = new PluginInstaller(delegate { return new GameRunningStatus(false, "停止中"); })
                .Install(game, noPayload);
            True(!result.Success && !result.Changed, "未承認payloadで導入されました");
            True(!File.Exists(GameLocator.GetPluginPath(game)), "未承認payloadが書き込まれました");
        }

        private static void TestPluginInspection()
        {
            string game = CreateGameRoot("inspect-game");
            byte[] payload = TestPayload();
            PluginInspection inspection = PluginInspector.Inspect(game, TestManifest(payload));
            Equal(PluginState.NotInstalled, inspection.State, "未導入判定不一致");
        }

        private static void TestPluginInstall()
        {
            string game = CreateGameRoot("install-game");
            byte[] payload = TestPayload();
            PluginManifest manifest = TestManifest(payload);
            PluginInstaller installer = new PluginInstaller(delegate { return new GameRunningStatus(false, "停止中"); });
            PluginInstallResult first = installer.Install(game, manifest);
            True(first.Success && first.Changed, "新規導入に失敗しました: " + first.Message);
            string destination = GameLocator.GetPluginPath(game);
            Equal(Convert.ToHexString(SHA256.HashData(payload)), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(destination))), "導入payload不一致");
            DateTime writeTime = File.GetLastWriteTimeUtc(destination);
            PluginInstallResult second = installer.Install(game, manifest);
            True(second.Success && !second.Changed, "同一版で無変更になりません");
            Equal(writeTime, File.GetLastWriteTimeUtc(destination), "同一版のDLLが書き換わりました");
        }

        private static void TestPluginUpdate()
        {
            string game = CreateGameRoot("update-game");
            string configDirectory = Path.Combine(game, "BepInEx", "config");
            Directory.CreateDirectory(configDirectory);
            string config = Path.Combine(configDirectory, "lom-messagespeed.cfg");
            File.WriteAllText(config, "sentinel", Encoding.UTF8);
            string destination = GameLocator.GetPluginPath(game);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            byte[] old = File.ReadAllBytes(typeof(Program).Assembly.Location);
            File.WriteAllBytes(destination, old);
            byte[] payload = TestPayload();
            PluginInstallResult result = new PluginInstaller(delegate { return new GameRunningStatus(false, "停止中"); })
                .Install(game, TestManifest(payload, Convert.ToHexString(SHA256.HashData(old))));
            True(result.Success && result.Changed, "更新に失敗しました: " + result.Message);
            Equal(Convert.ToHexString(SHA256.HashData(old)), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(destination + ".bak"))), "backup不一致");
            Equal("sentinel", File.ReadAllText(config, Encoding.UTF8), "cfgが変更されました");
        }

        private static void TestPluginRunningGuard()
        {
            string game = CreateGameRoot("guard-game");
            byte[] payload = TestPayload();
            PluginInstallResult result = new PluginInstaller(delegate { return new GameRunningStatus(true, "起動中"); })
                .Install(game, TestManifest(payload));
            True(!result.Success && !File.Exists(GameLocator.GetPluginPath(game)), "ゲーム起動中に導入されました");
        }

        private static void TestUnknownAndFlatRefusal()
        {
            byte[] payload = TestPayload();
            PluginManifest manifest = TestManifest(payload);
            PluginInstaller installer = new PluginInstaller(delegate { return new GameRunningStatus(false, "停止中"); });

            string unknownGame = CreateGameRoot("unknown-game");
            string target = GameLocator.GetPluginPath(unknownGame);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, File.ReadAllBytes(typeof(Program).Assembly.Location));
            PluginInstallResult unknown = installer.Install(unknownGame, manifest);
            True(!unknown.Success && !unknown.Changed, "未知版を上書きしました");

            string flatGame = CreateGameRoot("flat-game");
            string flat = Path.Combine(flatGame, PluginManifest.FlatRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(flat)!);
            File.WriteAllBytes(flat, payload);
            PluginInstallResult flatResult = installer.Install(flatGame, manifest);
            True(!flatResult.Success && !File.Exists(GameLocator.GetPluginPath(flatGame)), "flat配置と重複する導入を行いました");
        }

        private static void TestDuplicateInspection()
        {
            string game = CreateGameRoot("duplicate-game");
            byte[] payload = TestPayload();
            string target = GameLocator.GetPluginPath(game);
            string flat = Path.Combine(game, PluginManifest.FlatRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, payload);
            File.WriteAllBytes(flat, payload);
            PluginInspection inspection = PluginInspector.Inspect(game, TestManifest(payload));
            Equal(PluginState.DuplicatePlacement, inspection.State, "重複配置判定不一致");
            Equal(2, inspection.ConflictingPaths.Count, "重複パス数不一致");
        }

        private static void TestInspectionRace()
        {
            string game = CreateGameRoot("race-game");
            byte[] payload = TestPayload();
            PluginManifest manifest = TestManifest(payload);
            PluginInspection before = PluginInspector.Inspect(game, manifest);
            string target = GameLocator.GetPluginPath(game);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, File.ReadAllBytes(typeof(Program).Assembly.Location));
            PluginInstallResult result = new PluginInstaller(delegate { return new GameRunningStatus(false, "停止中"); })
                .Install(game, manifest, before);
            True(!result.Success && !result.Changed, "確認後に追加されたDLLを上書きしました");
        }

        private static void TestOutsidePathRefusal()
        {
            string game = CreateGameRoot("outside-game");
            string outside = Path.Combine(root, "outside.dll");
            True(
                !PluginInstaller.TryRejectReparsePoints(game, outside, out string _),
                "ゲームルート外パスを受理しました");
        }

        private static void TestApprovedEmbeddedPayload()
        {
            byte[] payload = PluginManifest.Current.ReadPayload()
                ?? throw new InvalidOperationException("承認済みresourceがありません");
            Equal(
                PluginManifest.Current.ExpectedSha256,
                Convert.ToHexString(SHA256.HashData(payload)),
                "内蔵resource hash不一致");
            Equal(PluginManifest.Current.ExpectedLength!.Value, payload.LongLength, "内蔵resource size不一致");

            string game = CreateGameRoot("approved-resource-game");
            PluginInstallResult result = new PluginInstaller(delegate { return new GameRunningStatus(false, "停止中"); })
                .Install(game, PluginManifest.Current);
            True(result.Success && result.Changed, "内蔵resourceから新規導入できません: " + result.Message);
            PluginInspection inspection = PluginInspector.Inspect(game, PluginManifest.Current);
            Equal(PluginState.Approved, inspection.State, "導入後に承認版と判定されません");
        }

        private static void TestInspectorConflictStates()
        {
            byte[] approved = TestPayload();
            byte[] other = File.ReadAllBytes(typeof(Program).Assembly.Location);

            string sameGame = CreateGameRoot("same-version-other-hash");
            string sameTarget = GameLocator.GetPluginPath(sameGame);
            Directory.CreateDirectory(Path.GetDirectoryName(sameTarget)!);
            File.WriteAllBytes(sameTarget, other);
            Version otherVersion = PluginBinaryValidator.Read(sameTarget).FileVersion
                ?? throw new InvalidOperationException("テストassemblyにFileVersionがありません");
            PluginInspection same = PluginInspector.Inspect(
                sameGame,
                new PluginManifest(otherVersion, Convert.ToHexString(SHA256.HashData(approved)), 1, approved));
            Equal(PluginState.SameVersionDifferentHash, same.State, "同版別hash判定不一致");

            string newerGame = CreateGameRoot("newer-version");
            string newerTarget = GameLocator.GetPluginPath(newerGame);
            Directory.CreateDirectory(Path.GetDirectoryName(newerTarget)!);
            File.WriteAllBytes(newerTarget, approved);
            PluginInspection newer = PluginInspector.Inspect(
                newerGame,
                new PluginManifest(new Version(0, 1), Convert.ToHexString(SHA256.HashData(other)), 1, other));
            Equal(PluginState.NewerVersion, newer.State, "新版判定不一致");

            string corruptGame = CreateGameRoot("corrupt-plugin");
            string corruptTarget = GameLocator.GetPluginPath(corruptGame);
            Directory.CreateDirectory(Path.GetDirectoryName(corruptTarget)!);
            File.WriteAllText(corruptTarget, "not a managed DLL", Encoding.UTF8);
            PluginInspection corrupt = PluginInspector.Inspect(corruptGame, TestManifest(approved));
            Equal(PluginState.CorruptOrUnreadable, corrupt.State, "破損判定不一致");
        }

        private static void TestPluginFileGuards()
        {
            byte[] approved = TestPayload();
            byte[] old = File.ReadAllBytes(typeof(Program).Assembly.Location);
            string oldHash = Convert.ToHexString(SHA256.HashData(old));
            PluginManifest manifest = TestManifest(approved, oldHash);
            PluginInstaller installer = new PluginInstaller(delegate { return new GameRunningStatus(false, "停止中"); });

            string readOnlyGame = CreateGameRoot("readonly-plugin");
            string readOnlyTarget = GameLocator.GetPluginPath(readOnlyGame);
            Directory.CreateDirectory(Path.GetDirectoryName(readOnlyTarget)!);
            File.WriteAllBytes(readOnlyTarget, old);
            File.SetAttributes(readOnlyTarget, File.GetAttributes(readOnlyTarget) | FileAttributes.ReadOnly);
            try
            {
                PluginInstallResult result = installer.Install(readOnlyGame, manifest);
                True(!result.Success && !result.Changed, "readonly DLLを更新しました");
            }
            finally { File.SetAttributes(readOnlyTarget, FileAttributes.Normal); }

            string lockedGame = CreateGameRoot("locked-plugin");
            string lockedTarget = GameLocator.GetPluginPath(lockedGame);
            Directory.CreateDirectory(Path.GetDirectoryName(lockedTarget)!);
            File.WriteAllBytes(lockedTarget, old);
            using FileStream locked = new FileStream(lockedTarget, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            PluginInstallResult lockedResult = installer.Install(lockedGame, manifest);
            True(!lockedResult.Success && !lockedResult.Changed, "lock中DLLを更新しました");
        }

        private static void TestReparsePointRefusal()
        {
            string game = CreateGameRoot("reparse-game");
            string plugins = Path.Combine(game, "BepInEx", "plugins");
            string external = Path.Combine(root, "reparse-external");
            Directory.CreateDirectory(external);
            Directory.Delete(plugins);
            try
            {
                Directory.CreateSymbolicLink(plugins, external);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                return;
            }

            BepInExInspection bepinex = BepInExInspector.Inspect(game);
            True(bepinex.HasReparsePoint, "BepInEx診断がreparse pointを明示しません");

            string destination = GameLocator.GetPluginPath(game);
            True(
                !PluginInstaller.TryRejectReparsePoints(game, destination, out string _),
                "reparse point経路を受理しました");
        }

        private static void TestPostInstallRollback()
        {
            string game = CreateGameRoot("rollback-game");
            byte[] approved = TestPayload();
            byte[] old = File.ReadAllBytes(typeof(Program).Assembly.Location);
            string target = GameLocator.GetPluginPath(game);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, old);
            PluginManifest manifest = TestManifest(
                approved,
                Convert.ToHexString(SHA256.HashData(old)));
            PluginInstaller installer = new PluginInstaller(
                delegate { return new GameRunningStatus(false, "停止中"); },
                delegate(string installed) { File.WriteAllText(installed, "corrupted after replace", Encoding.UTF8); });
            PluginInstallResult result = installer.Install(game, manifest);
            True(!result.Success && !result.Changed, "導入後破損を成功扱いしました");
            Equal(
                Convert.ToHexString(SHA256.HashData(old)),
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target))),
                "rollbackで元DLLが復元されません");
        }

        private static byte[] TestPayload()
        {
            string path = Path.Combine(
                Environment.CurrentDirectory,
                "src",
                "bin",
                "Release",
                "netstandard2.1",
                "LOM_MessageSpeed.dll");
            return File.ReadAllBytes(path);
        }

        private static PluginManifest TestManifest(byte[] payload, params string[] knownOlderSha256)
        {
            return new PluginManifest(
                new Version(99, 0),
                Convert.ToHexString(SHA256.HashData(payload)),
                ConfigSchema.SchemaVersion,
                payload,
                knownOlderSha256: knownOlderSha256);
        }

        private static string CreateGameRoot(string name)
        {
            string game = CreateLevelOneRoot(name);
            Directory.CreateDirectory(Path.Combine(game, "BepInEx", "core"));
            Directory.CreateDirectory(Path.Combine(game, "BepInEx", "plugins"));
            foreach (string relative in new[]
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
            })
            {
                File.WriteAllBytes(Path.Combine(game, relative), Array.Empty<byte>());
            }
            return game;
        }

        private static string CreateLevelOneRoot(string name)
        {
            string game = Path.Combine(root, name);
            Directory.CreateDirectory(game);
            File.WriteAllBytes(Path.Combine(game, "Mortal.exe"), Array.Empty<byte>());
            return game;
        }

        private static string Minimal(string enabled, string multiplier, string newline)
        {
            return "[General]" + newline + "Enabled = " + enabled + newline + newline +
                "[Message]" + newline + "SpeedMultiplier = " + multiplier + newline;
        }

        private static string Write(string name, string text, bool bom)
        {
            string path = Path.Combine(root, name);
            using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            if (bom)
            {
                byte[] preamble = Encoding.UTF8.GetPreamble();
                stream.Write(preamble, 0, preamble.Length);
            }
            byte[] body = new UTF8Encoding(false).GetBytes(text);
            stream.Write(body, 0, body.Length);
            return path;
        }

        private static string ReadText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
            return Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);
        }

        private static void ExpectConfigError(Action action)
        {
            try { action(); } catch (ConfigException) { return; }
            throw new InvalidOperationException("ConfigExceptionが発生しませんでした");
        }

        private static void Run(string name, Action test)
        {
            test();
            passed++;
            Console.WriteLine("ok " + passed.ToString(CultureInfo.InvariantCulture) + " - " + name);
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(message + " expected=" + expected + " actual=" + actual);
            }
        }
    }
}
