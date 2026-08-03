using System;
using System.Collections.Generic;
using System.IO;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal static class GameLocationOptions
    {
        internal const string SteamRelativePath = @"SteamLibrary\steamapps\common\LegendOfMortal";
        internal const string SystemSteamRelativePath = @"Program Files (x86)\Steam\steamapps\common\LegendOfMortal";

        internal static IReadOnlyList<string> GetReadyFixedDrives()
        {
            List<string> result = new List<string>();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    {
                        result.Add(drive.Name.TrimEnd(Path.DirectorySeparatorChar));
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        internal static string GetDriveTemplate(string drive)
        {
            string root = Path.GetPathRoot(Path.GetFullPath(drive + Path.DirectorySeparatorChar))
                ?? throw new ArgumentException("ドライブを正規化できません。", nameof(drive));
            return Path.Combine(root, SteamRelativePath);
        }

        internal static IReadOnlyList<string> GetDriveCandidates(string drive)
        {
            List<string> result = new List<string> { GetDriveTemplate(drive) };
            string root = Path.GetPathRoot(Path.GetFullPath(drive + Path.DirectorySeparatorChar)) ?? string.Empty;
            if (string.Equals(root, @"C:\", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(Path.Combine(root, SystemSteamRelativePath));
            }

            return result;
        }
    }
}
