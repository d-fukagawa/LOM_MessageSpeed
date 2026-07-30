using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using Fungus;
using Mortal.Story;

namespace LOM.MessageSpeed
{
    internal sealed class DialogueSpeedState
    {
        private sealed class AppliedState
        {
            internal Writer Writer;
            internal float OriginalWritingSpeed;
            internal float AppliedWritingSpeed;
            internal SayDialog Owner;
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        private readonly MessageSpeedConfig config;
        private readonly ManualLogSource log;
        private readonly FieldInfo writingSpeedField;
        private readonly FieldInfo currentWritingSpeedField;
        private readonly FieldInfo skipDialogField;
        private readonly MethodInfo activeSayDialogGetter;
        private readonly MethodInfo writerIsWritingGetter;
        private readonly MethodInfo writerIsWaitingGetter;
        private readonly Dictionary<Writer, SayDialog> allowedWriters;
        private readonly Dictionary<Writer, AppliedState> appliedStates;
        private readonly HashSet<Writer> duplicateWarnings;
        private readonly HashSet<Writer> ownershipWarnings;

        internal DialogueSpeedState(
            MessageSpeedConfig config,
            ManualLogSource log,
            FieldInfo writingSpeedField,
            FieldInfo currentWritingSpeedField,
            FieldInfo skipDialogField,
            MethodInfo activeSayDialogGetter,
            MethodInfo writerIsWritingGetter,
            MethodInfo writerIsWaitingGetter)
        {
            this.config = config;
            this.log = log;
            this.writingSpeedField = writingSpeedField;
            this.currentWritingSpeedField = currentWritingSpeedField;
            this.skipDialogField = skipDialogField;
            this.activeSayDialogGetter = activeSayDialogGetter;
            this.writerIsWritingGetter = writerIsWritingGetter;
            this.writerIsWaitingGetter = writerIsWaitingGetter;

            allowedWriters = new Dictionary<Writer, SayDialog>(ReferenceComparer<Writer>.Instance);
            appliedStates = new Dictionary<Writer, AppliedState>(ReferenceComparer<Writer>.Instance);
            duplicateWarnings = new HashSet<Writer>(ReferenceComparer<Writer>.Instance);
            ownershipWarnings = new HashSet<Writer>(ReferenceComparer<Writer>.Instance);
        }

        internal bool HasAppliedStates
        {
            get { return appliedStates.Count != 0; }
        }

        internal void SetSceneDialogs(IList<SayDialog> dialogs, MethodInfo getWriterMethod)
        {
            RestoreAll(true);
            allowedWriters.Clear();
            duplicateWarnings.Clear();
            ownershipWarnings.Clear();

            for (int i = 0; i < dialogs.Count; i++)
            {
                SayDialog dialog = dialogs[i];
                Writer writer = (Writer)getWriterMethod.Invoke(dialog, null);
                allowedWriters.Add(writer, dialog);
            }
        }

        internal bool IsKnownDialog(SayDialog dialog)
        {
            if (dialog == null)
            {
                return false;
            }

            foreach (SayDialog known in allowedWriters.Values)
            {
                if (ReferenceEquals(known, dialog))
                {
                    return true;
                }
            }

            return false;
        }

        internal void ClearScene()
        {
            RestoreAll(true);
            allowedWriters.Clear();
            duplicateWarnings.Clear();
            ownershipWarnings.Clear();
        }

        internal void TryApply(Writer writer, StoryManager storyManager)
        {
            if (!config.Enabled.Value)
            {
                RestoreAll(true);
                return;
            }

            if (writer == null || storyManager == null)
            {
                return;
            }

            SayDialog owner;
            if (!allowedWriters.TryGetValue(writer, out owner))
            {
                return;
            }

            SayDialog activeDialog = (SayDialog)activeSayDialogGetter.Invoke(null, null);
            if (!ReferenceEquals(activeDialog, owner))
            {
                return;
            }

            if ((bool)skipDialogField.GetValue(storyManager))
            {
                return;
            }

            AppliedState existing;
            if (appliedStates.TryGetValue(writer, out existing))
            {
                Restore(existing, true);
                appliedStates.Remove(writer);
                if (duplicateWarnings.Add(writer))
                {
                    log.LogWarning("Duplicate Writer.ProcessTokens start detected; speed adjustment was skipped for this interval.");
                }

                return;
            }

            float multiplier = config.EffectiveMultiplier;
            if (multiplier == 1.0f)
            {
                return;
            }

            float original = (float)writingSpeedField.GetValue(writer);
            if (!IsPositiveFinite(original))
            {
                return;
            }

            float adjusted = original * multiplier;
            if (!IsPositiveFinite(adjusted))
            {
                return;
            }

            AppliedState state = new AppliedState
            {
                Writer = writer,
                OriginalWritingSpeed = original,
                AppliedWritingSpeed = adjusted,
                Owner = owner
            };

            appliedStates.Add(writer, state);
            writingSpeedField.SetValue(writer, adjusted);
        }

        internal void RestoreWriter(Writer writer, bool early)
        {
            if (writer == null)
            {
                return;
            }

            AppliedState state;
            if (!appliedStates.TryGetValue(writer, out state))
            {
                return;
            }

            Restore(state, early);
            appliedStates.Remove(writer);
        }

        internal void RestoreDialog(SayDialog dialog)
        {
            if (dialog == null || appliedStates.Count == 0)
            {
                return;
            }

            List<Writer> matches = new List<Writer>();
            foreach (KeyValuePair<Writer, AppliedState> pair in appliedStates)
            {
                if (ReferenceEquals(pair.Value.Owner, dialog))
                {
                    matches.Add(pair.Key);
                }
            }

            for (int i = 0; i < matches.Count; i++)
            {
                RestoreWriter(matches[i], true);
            }
        }

        internal void RestoreAll(bool early)
        {
            if (appliedStates.Count == 0)
            {
                return;
            }

            List<AppliedState> states = new List<AppliedState>(appliedStates.Values);
            for (int i = 0; i < states.Count; i++)
            {
                Restore(states[i], early);
            }

            appliedStates.Clear();
        }

        internal void RestoreIdleWriters()
        {
            if (appliedStates.Count == 0)
            {
                return;
            }

            List<Writer> idle = new List<Writer>();
            foreach (KeyValuePair<Writer, AppliedState> pair in appliedStates)
            {
                Writer writer = pair.Key;
                bool isWriting = (bool)writerIsWritingGetter.Invoke(writer, null);
                bool isWaiting = (bool)writerIsWaitingGetter.Invoke(writer, null);
                if (!isWriting && !isWaiting)
                {
                    idle.Add(writer);
                }
            }

            for (int i = 0; i < idle.Count; i++)
            {
                RestoreWriter(idle[i], true);
            }
        }

        private void Restore(AppliedState state, bool early)
        {
            try
            {
                float currentWritingSpeed = (float)writingSpeedField.GetValue(state.Writer);
                if (currentWritingSpeed == state.AppliedWritingSpeed)
                {
                    writingSpeedField.SetValue(state.Writer, state.OriginalWritingSpeed);
                }
                else if (ownershipWarnings.Add(state.Writer))
                {
                    log.LogWarning("Writer.writingSpeed changed after LOM_MessageSpeed applied it; the other value was preserved.");
                }

                if (early)
                {
                    float current = (float)currentWritingSpeedField.GetValue(state.Writer);
                    if (current == state.AppliedWritingSpeed)
                    {
                        currentWritingSpeedField.SetValue(state.Writer, state.OriginalWritingSpeed);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError("Failed to restore Writer speed safely: " + ex);
            }
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0.0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
