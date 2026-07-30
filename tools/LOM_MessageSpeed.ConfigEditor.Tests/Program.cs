using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using LOM.MessageSpeed.ConfigEditor;

namespace LOM.MessageSpeed.ConfigEditor.Tests
{
    internal static class Program
    {
        private static int passed;
        private static string root = string.Empty;

        private static int Main()
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
                Run("ゲームルートと設定パス検証", TestGameRootValidation);
                Run("対象ゲームプロセス起動中の保存禁止判定", TestGameProcessGuard);
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
            const string original = "# header\r\n[General]\r\nEnabled = true\r\nUnknown = keep\r\n\r\n[Other]\r\nEnabled = untouched\r\n\r\n[Message]\r\n; SpeedMultiplier = 9\r\nSpeedMultiplier = 1.5\r\nTail = keep\r\n";
            string path = Write("preserve.cfg", original, false);
            ConfigDocument.Load(path).Save(false, 2.0m);
            const string expected = "# header\r\n[General]\r\nEnabled = false\r\nUnknown = keep\r\n\r\n[Other]\r\nEnabled = untouched\r\n\r\n[Message]\r\n; SpeedMultiplier = 9\r\nSpeedMultiplier = 2.0\r\nTail = keep\r\n";
            Equal(expected, ReadText(path), "2キー以外が変わりました");
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

        private static string Minimal(string enabled, string multiplier, string newline)
        {
            return "[General]" + newline + "Enabled = " + enabled + newline + newline + "[Message]" + newline + "SpeedMultiplier = " + multiplier + newline;
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
