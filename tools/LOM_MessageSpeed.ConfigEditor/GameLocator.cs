using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal static class GameLocator
    {
        internal const string ConfigRelativePath = @"BepInEx\config\lom-messagespeed.cfg";

        internal static IReadOnlyList<string> FindCandidates()
        {
            HashSet<string> candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddNearbyCandidates(candidates);
            AddSteamCandidates(candidates);
            List<string> result = new List<string>(candidates);
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        internal static bool TryValidateRoot(string selectedPath, out string root, out string error)
        {
            root = string.Empty;
            error = string.Empty;
            try
            {
                string candidate = selectedPath.Trim();
                if (!Path.IsPathFullyQualified(candidate))
                {
                    error = "相対パスは使用できません。絶対パスを指定してください。";
                    return false;
                }

                root = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!File.Exists(Path.Combine(root, "Mortal.exe")))
                {
                    error = "Mortal.exeが見つかりません。";
                    return false;
                }

                string configPath = Path.GetFullPath(Path.Combine(root, ConfigRelativePath));
                string rootPrefix = root + Path.DirectorySeparatorChar;
                if (!configPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    error = "設定パスがゲームルート外を指しています。";
                    return false;
                }

                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                error = "フォルダを確認できません: " + ex.Message;
                return false;
            }
        }

        internal static string GetConfigPath(string root)
        {
            return Path.GetFullPath(Path.Combine(root, ConfigRelativePath));
        }

        internal static string GetPluginPath(string root)
        {
            return Path.GetFullPath(Path.Combine(root, PluginManifest.InstallRelativePath));
        }

        private static void AddNearbyCandidates(HashSet<string> candidates)
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 7 && directory != null; i++, directory = directory.Parent)
            {
                AddIfValid(candidates, directory.FullName);
            }
        }

        private static void AddSteamCandidates(HashSet<string> candidates)
        {
            List<string> steamRoots = new List<string>();
            string? programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrWhiteSpace(programFilesX86))
            {
                steamRoots.Add(Path.Combine(programFilesX86, "Steam"));
            }

            foreach (string steamRoot in steamRoots)
            {
                AddIfValid(candidates, Path.Combine(steamRoot, "steamapps", "common", "LegendOfMortal"));
                string libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(libraryFile))
                {
                    continue;
                }

                try
                {
                    string vdf = File.ReadAllText(libraryFile);
                    foreach (Match match in Regex.Matches(vdf, "\\\"path\\\"\\s*\\\"(?<path>[^\\\"]+)\\\"", RegexOptions.CultureInvariant))
                    {
                        string library = match.Groups["path"].Value.Replace("\\\\", "\\");
                        AddIfValid(candidates, Path.Combine(library, "steamapps", "common", "LegendOfMortal"));
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // Steam情報は任意候補。読めない場合は手動選択へ進む。
                }
            }
        }

        private static void AddIfValid(HashSet<string> candidates, string path)
        {
            string root;
            string error;
            if (TryValidateRoot(path, out root, out error))
            {
                candidates.Add(root);
            }
        }
    }

    internal readonly struct GameRunningStatus
    {
        internal GameRunningStatus(bool blocksSave, string message)
        {
            BlocksSave = blocksSave;
            Message = message;
        }

        internal bool BlocksSave { get; }
        internal string Message { get; }
    }

    internal static class GameProcessGuard
    {
        internal static GameRunningStatus Check(string? selectedRoot)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName("Mortal");
            }
            catch (Exception ex)
            {
                return new GameRunningStatus(true, "判定不能（安全のため保存禁止）: " + ex.Message);
            }

            if (processes.Length == 0)
            {
                return new GameRunningStatus(false, "起動していません");
            }

            foreach (Process process in processes)
            {
                using (process)
                {
                    try
                    {
                        string? executable = process.MainModule?.FileName;
                        if (string.IsNullOrEmpty(executable))
                        {
                            return new GameRunningStatus(true, "Mortalプロセスの実行場所を確認できません（安全のため保存禁止）");
                        }

                        if (!string.IsNullOrEmpty(selectedRoot))
                        {
                            string expected = Path.GetFullPath(Path.Combine(selectedRoot, "Mortal.exe"));
                            if (string.Equals(Path.GetFullPath(executable), expected, StringComparison.OrdinalIgnoreCase))
                            {
                                return new GameRunningStatus(true, "起動中（保存できません）");
                            }
                        }
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception || ex is UnauthorizedAccessException)
                    {
                        return new GameRunningStatus(true, "Mortalプロセスの実行場所を確認できません（安全のため保存禁止）: " + ex.Message);
                    }
                }
            }

            return new GameRunningStatus(false, "別の場所のMortalプロセスのみ検出");
        }
    }
}
