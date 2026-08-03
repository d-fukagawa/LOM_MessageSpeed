using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using Fungus;
using HarmonyLib;
using LOM.MessageSpeed.Patches;
using Mortal.Story;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LOM.MessageSpeed
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "lom-messagespeed";
        public const string PluginName = "LOM_MessageSpeed";
        public const string PluginVersion = "0.2.0";
        // Preserve the 0.1.0 Harmony owner ID for compatibility with existing
        // diagnostics and any external ordering rule that refers to the plugin GUID.
        internal const string MessageHarmonyId = PluginGuid;

        private const BindingFlags DeclaredMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static Plugin instance;
        private Harmony messageHarmony;
        private Contract contract;
        private MessageSpeedConfig messageConfig;
        private StoryManager observedStoryManager;
        private int observedSceneHandle = int.MinValue;
        private bool messagePatchesApplied;

        internal static DialogueSpeedState State { get; private set; }

        private sealed class Contract
        {
            internal MethodInfo ProcessTokens;
            internal MethodInfo NotifyEnd;
            internal MethodInfo SkipDialog;
            internal MethodInfo SayDialogOnDestroy;
            internal MethodInfo Say;
            internal MethodInfo GetWriter;
            internal MethodInfo GetSayDialog;
            internal MethodInfo GetStoryManagerInstance;
            internal MethodInfo GetIsWriting;
            internal MethodInfo GetIsWaitingForInput;
            internal FieldInfo WritingSpeed;
            internal FieldInfo CurrentWritingSpeed;
            internal FieldInfo SkipDialogField;
            internal MethodInfo[] PlaceholderGetters;
        }

        private void Awake()
        {
            instance = this;
            messageConfig = new MessageSpeedConfig(Config);

            Logger.LogInfo("LOM_MessageSpeed loaded");
            Logger.LogInfo("Enabled: " + messageConfig.Enabled.Value);
            Logger.LogInfo("SpeedMultiplier: " + messageConfig.EffectiveMultiplier);
            LogVersions();

            InitializeMessageCategory();
        }

        private void InitializeMessageCategory()
        {
            List<string> missing = new List<string>();
            contract = ValidateContract(missing);
            if (missing.Count != 0)
            {
                Logger.LogError(
                    "Message patch target not found. The current game version may not be supported.");
                for (int i = 0; i < missing.Count; i++)
                {
                    Logger.LogError("Missing or incompatible Message contract: " + missing[i]);
                }

                return;
            }

            State = new DialogueSpeedState(
                messageConfig,
                Logger,
                contract.WritingSpeed,
                contract.CurrentWritingSpeed,
                contract.SkipDialogField,
                contract.GetSayDialog,
                contract.GetIsWriting,
                contract.GetIsWaitingForInput);

            try
            {
                ApplyMessagePatches();
                messagePatchesApplied = true;
                LogPatchTarget(contract.ProcessTokens);
                LogPatchTarget(contract.NotifyEnd);
                LogPatchTarget(contract.SkipDialog);
                LogPatchTarget(contract.SayDialogOnDestroy);
            }
            catch (Exception ex)
            {
                if (messageHarmony != null)
                {
                    messageHarmony.UnpatchSelf();
                }

                State.RestoreAll(true);
                messagePatchesApplied = false;
                Logger.LogError(
                    "Message Harmony patch application failed; only Message patches were removed: " + ex);
            }
        }

        private void Update()
        {
            if (!messagePatchesApplied || State == null)
            {
                return;
            }

            try
            {
                Scene activeScene = SceneManager.GetActiveScene();
                int sceneHandle = activeScene.handle;
                StoryManager currentStoryManager = StoryManager.Instance;

                if (sceneHandle != observedSceneHandle || !ReferenceEquals(currentStoryManager, observedStoryManager))
                {
                    State.ClearScene();
                    observedSceneHandle = sceneHandle;
                    observedStoryManager = currentStoryManager;

                    if (currentStoryManager != null)
                    {
                        ValidateScene(activeScene);
                    }
                }

                if (!messageConfig.Enabled.Value)
                {
                    State.RestoreAll(true);
                }
                else if (State.HasAppliedStates)
                {
                    State.RestoreIdleWriters();
                }
            }
            catch (Exception ex)
            {
                State.RestoreAll(true);
                Logger.LogError("Dialogue scene validation or speed restoration failed; this scene remains fail-closed: " + ex);
                State.ClearScene();
            }
        }

        private void OnDestroy()
        {
            if (State != null)
            {
                State.RestoreAll(true);
                State.ClearScene();
            }

            if (messageHarmony != null)
            {
                messageHarmony.UnpatchSelf();
            }

            messagePatchesApplied = false;
            if (ReferenceEquals(instance, this))
            {
                instance = null;
            }
        }

        internal static void HandlePatchException(string patchName, Exception exception, Writer writer)
        {
            try
            {
                if (State != null)
                {
                    if (writer != null)
                    {
                        State.RestoreWriter(writer, true);
                    }
                    else
                    {
                        State.RestoreAll(true);
                    }
                }
            }
            catch
            {
                // Preserve the original game method even if best-effort restoration also fails.
            }

            ManualLogSource logger = instance == null ? null : instance.Logger;
            if (logger != null)
            {
                logger.LogError(patchName + " failed; the original game method will continue: " + exception);
            }
        }

        private void ValidateScene(Scene activeScene)
        {
            UnityEngine.Object[] all = UnityEngine.Object.FindObjectsOfType(typeof(SayDialogPlaceholder));
            List<SayDialogPlaceholder> placeholders = new List<SayDialogPlaceholder>();
            for (int i = 0; i < all.Length; i++)
            {
                SayDialogPlaceholder placeholder = all[i] as SayDialogPlaceholder;
                if (placeholder != null && placeholder.gameObject.scene.handle == activeScene.handle)
                {
                    placeholders.Add(placeholder);
                }
            }

            if (placeholders.Count != 1)
            {
                LogSceneFailure("expected exactly one SayDialogPlaceholder, found " + placeholders.Count);
                return;
            }

            List<SayDialog> dialogs = new List<SayDialog>(4);
            for (int i = 0; i < contract.PlaceholderGetters.Length; i++)
            {
                SayDialog dialog = (SayDialog)contract.PlaceholderGetters[i].Invoke(placeholders[0], null);
                if (dialog == null)
                {
                    LogSceneFailure("a SayDialogPlaceholder dialogue reference is null");
                    return;
                }

                for (int j = 0; j < dialogs.Count; j++)
                {
                    if (ReferenceEquals(dialogs[j], dialog))
                    {
                        LogSceneFailure("SayDialogPlaceholder contains duplicate SayDialog references");
                        return;
                    }
                }

                dialogs.Add(dialog);
            }

            List<Writer> writers = new List<Writer>(4);
            for (int i = 0; i < dialogs.Count; i++)
            {
                Writer writer = (Writer)contract.GetWriter.Invoke(dialogs[i], null);
                if (writer == null)
                {
                    LogSceneFailure("a verified SayDialog returned a null Writer");
                    return;
                }

                for (int j = 0; j < writers.Count; j++)
                {
                    if (ReferenceEquals(writers[j], writer))
                    {
                        LogSceneFailure("verified SayDialogs contain duplicate Writer references");
                        return;
                    }
                }

                writers.Add(writer);
            }

            SayDialog active = (SayDialog)contract.GetSayDialog.Invoke(null, null);
            if (active != null)
            {
                bool activeKnown = false;
                for (int i = 0; i < dialogs.Count; i++)
                {
                    if (ReferenceEquals(dialogs[i], active))
                    {
                        activeKnown = true;
                        break;
                    }
                }

                if (!activeKnown)
                {
                    LogSceneFailure("the active SayDialog is not one of the four verified dialogue instances");
                    return;
                }
            }

            State.SetSceneDialogs(dialogs, contract.GetWriter);
        }

        private void LogSceneFailure(string detail)
        {
            State.ClearScene();
            Logger.LogWarning("Message speed adjustment disabled for the current scene: " + detail);
        }

        private Contract ValidateContract(List<string> missing)
        {
            Contract result = new Contract();
            result.ProcessTokens = FindUniqueMethod(
                typeof(Writer), "ProcessTokens", typeof(IEnumerator), false,
                new Type[] { typeof(List<TextTagToken>), typeof(bool), typeof(Action) }, missing);
            result.NotifyEnd = FindUniqueMethod(
                typeof(Writer), "NotifyEnd", typeof(void), false,
                new Type[] { typeof(bool) }, missing);
            result.SkipDialog = FindUniqueMethod(
                typeof(StoryManager), "SkipDialog", typeof(void), false,
                new Type[] { typeof(bool) }, missing);
            result.SayDialogOnDestroy = FindUniqueMethod(
                typeof(SayDialog), "OnDestroy", typeof(void), false,
                Type.EmptyTypes, missing);
            result.Say = FindUniqueMethod(
                typeof(SayDialog), "Say", typeof(void), false,
                new Type[]
                {
                    typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                    typeof(bool), typeof(AudioClip), typeof(Action)
                }, missing);
            result.GetWriter = FindUniqueMethod(
                typeof(SayDialog), "GetWriter", typeof(Writer), false, Type.EmptyTypes, missing);
            result.GetSayDialog = FindUniqueMethod(
                typeof(SayDialog), "GetSayDialog", typeof(SayDialog), true, Type.EmptyTypes, missing);
            result.GetStoryManagerInstance = FindUniqueMethod(
                typeof(StoryManager), "get_Instance", typeof(StoryManager), true, Type.EmptyTypes, missing);
            result.GetIsWriting = FindUniqueMethod(
                typeof(Writer), "get_IsWriting", typeof(bool), false, Type.EmptyTypes, missing);
            result.GetIsWaitingForInput = FindUniqueMethod(
                typeof(Writer), "get_IsWaitingForInput", typeof(bool), false, Type.EmptyTypes, missing);

            result.WritingSpeed = FindUniqueField(typeof(Writer), "writingSpeed", typeof(float), false, missing);
            result.CurrentWritingSpeed = FindUniqueField(typeof(Writer), "currentWritingSpeed", typeof(float), false, missing);
            result.SkipDialogField = FindUniqueField(typeof(StoryManager), "_skipDialog", typeof(bool), false, missing);

            string[] names = { "character", "narrative", "center", "think" };
            result.PlaceholderGetters = new MethodInfo[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                result.PlaceholderGetters[i] = FindUniqueMethod(
                    typeof(SayDialogPlaceholder), "get_" + names[i], typeof(SayDialog), false,
                    Type.EmptyTypes, missing);
            }

            return result;
        }

        private static MethodInfo FindUniqueMethod(
            Type type,
            string name,
            Type returnType,
            bool isStatic,
            Type[] parameterTypes,
            List<string> missing)
        {
            List<MethodInfo> matches = new List<MethodInfo>();
            MethodInfo[] methods = type.GetMethods(DeclaredMembers);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != name || method.ReturnType != returnType || method.IsStatic != isStatic)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                {
                    continue;
                }

                bool exact = true;
                for (int j = 0; j < parameters.Length; j++)
                {
                    if (parameters[j].ParameterType != parameterTypes[j])
                    {
                        exact = false;
                        break;
                    }
                }

                if (exact)
                {
                    matches.Add(method);
                }
            }

            if (matches.Count != 1)
            {
                missing.Add(type.FullName + "." + name + " (exact matches: " + matches.Count + ")");
                return null;
            }

            return matches[0];
        }

        private static FieldInfo FindUniqueField(
            Type type,
            string name,
            Type fieldType,
            bool isStatic,
            List<string> missing)
        {
            List<FieldInfo> matches = new List<FieldInfo>();
            FieldInfo[] fields = type.GetFields(DeclaredMembers);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.Name == name && field.FieldType == fieldType && field.IsStatic == isStatic)
                {
                    matches.Add(field);
                }
            }

            if (matches.Count != 1)
            {
                missing.Add(type.FullName + "." + name + " (exact matches: " + matches.Count + ")");
                return null;
            }

            return matches[0];
        }

        private void ApplyMessagePatches()
        {
            messageHarmony = new Harmony(MessageHarmonyId);
            messageHarmony.Patch(
                contract.ProcessTokens,
                prefix: new HarmonyMethod(typeof(MessageTypingPatch), "ProcessTokensPrefix"));
            messageHarmony.Patch(
                contract.NotifyEnd,
                postfix: new HarmonyMethod(typeof(MessageTypingPatch), "NotifyEndPostfix"));
            messageHarmony.Patch(
                contract.SkipDialog,
                prefix: new HarmonyMethod(typeof(MessageTypingPatch), "SkipDialogPrefix"));
            messageHarmony.Patch(
                contract.SayDialogOnDestroy,
                prefix: new HarmonyMethod(typeof(MessageTypingPatch), "SayDialogOnDestroyPrefix"));
        }

        private void LogPatchTarget(MethodInfo method)
        {
            Logger.LogInfo("Patch target: " + method.DeclaringType.FullName + "." + method);
        }

        private void LogVersions()
        {
            Logger.LogInfo("Game version: " + Application.version);
            Logger.LogInfo("Plugin version: " + PluginVersion);
            Logger.LogInfo("BepInEx version: " + typeof(BaseUnityPlugin).Assembly.GetName().Version);
            Logger.LogInfo("Fungus.dll version: " + typeof(Writer).Assembly.GetName().Version);
            Logger.LogInfo("Mortal.Story.dll version: " + typeof(StoryManager).Assembly.GetName().Version);
        }
    }
}
