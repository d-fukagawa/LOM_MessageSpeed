using System;
using Fungus;
using Mortal.Story;

namespace LOM.MessageSpeed.Patches
{
    internal static class MessageTypingPatch
    {
        internal static void ProcessTokensPrefix(Writer __instance)
        {
            try
            {
                Plugin.State.TryApply(__instance, StoryManager.Instance);
            }
            catch (Exception ex)
            {
                Plugin.HandlePatchException("Writer.ProcessTokens Prefix", ex, __instance);
            }
        }

        internal static void NotifyEndPostfix(Writer __instance)
        {
            try
            {
                Plugin.State.RestoreWriter(__instance, false);
            }
            catch (Exception ex)
            {
                Plugin.HandlePatchException("Writer.NotifyEnd Postfix", ex, __instance);
            }
        }

        internal static void SkipDialogPrefix(bool enabled)
        {
            if (!enabled)
            {
                return;
            }

            try
            {
                Plugin.State.RestoreAll(true);
            }
            catch (Exception ex)
            {
                Plugin.HandlePatchException("StoryManager.SkipDialog Prefix", ex, null);
            }
        }

        internal static void SayDialogOnDestroyPrefix(SayDialog __instance)
        {
            try
            {
                Plugin.State.RestoreDialog(__instance);
            }
            catch (Exception ex)
            {
                Plugin.HandlePatchException("SayDialog.OnDestroy Prefix", ex, null);
            }
        }
    }
}
