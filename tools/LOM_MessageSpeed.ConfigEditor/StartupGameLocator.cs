using System;
using System.Collections.Generic;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal enum StartupGameSelectionState
    {
        Found,
        Multiple,
        NotFound
    }

    internal sealed class StartupGameSelection
    {
        internal StartupGameSelection(
            StartupGameSelectionState state,
            string? root,
            bool usedSavedRoot,
            string detail)
        {
            State = state;
            Root = root;
            UsedSavedRoot = usedSavedRoot;
            Detail = detail;
        }

        internal StartupGameSelectionState State { get; }
        internal string? Root { get; }
        internal bool UsedSavedRoot { get; }
        internal string Detail { get; }
    }

    internal static class StartupGameLocator
    {
        internal static StartupGameSelection Select(
            string? savedRoot,
            IEnumerable<string> fallbackCandidates)
        {
            string detail = "有効なゲームフォルダが見つかりません。";
            if (!string.IsNullOrWhiteSpace(savedRoot))
            {
                if (GameLocator.TryValidateRoot(savedRoot, out string validatedSaved, out string savedError))
                {
                    return new StartupGameSelection(
                        StartupGameSelectionState.Found,
                        validatedSaved,
                        true,
                        string.Empty);
                }

                detail = savedError;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> validSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> valid = new List<string>();
            foreach (string candidate in fallbackCandidates)
            {
                if (string.IsNullOrWhiteSpace(candidate) || !seen.Add(candidate))
                {
                    continue;
                }

                if (GameLocator.TryValidateRoot(candidate, out string validated, out string error))
                {
                    if (validSeen.Add(validated))
                    {
                        valid.Add(validated);
                    }
                }
                else
                {
                    detail = error;
                }
            }

            if (valid.Count == 1)
            {
                return new StartupGameSelection(
                    StartupGameSelectionState.Found,
                    valid[0],
                    false,
                    string.Empty);
            }

            if (valid.Count > 1)
            {
                return new StartupGameSelection(
                    StartupGameSelectionState.Multiple,
                    null,
                    false,
                    valid.Count.ToString() + "個のゲームフォルダ候補が見つかりました。");
            }

            return new StartupGameSelection(
                StartupGameSelectionState.NotFound,
                null,
                false,
                detail);
        }
    }
}
