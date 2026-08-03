using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace LOM.MessageSpeed.StaticTests
{
    internal static class Program
    {
        private static readonly Dictionary<string, string> KnownHashes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Mortal.Story.dll", "ED5C318DB14868B03EEF3CC345B5FF9DD32C557CA9DB88CE7E07ACCFC5F47020" },
                { "Mortal.Core.dll", "9A76F508BC069A4F9BEB1A2E798A002A0D4B20D1210FE0CD2EA98C7B6FFC0FB1" },
                { "Mortal.Free.dll", "66A26E6EA047104D2BD5389DE65D4AF82680A6D0A5D0A84E77DD7F6E21414182" },
                { "Mortal.Battle.dll", "BE9025D12D75A6E6B70EE00FAFA0C3744F4E67A774427233CCA29CF0E79C3DAD" },
                { "Mortal.Combat.dll", "65D64D0BA936D90B8FF405A27477C1C3F5D65805AB900411C92481A638B103DD" },
                { "Fungus.dll", "BCB6D47EB31DEDC5D23D0DA281E0835AF0F939F30B64A39AAE7D5F6DAB96CAD2" },
                { "Assembly-CSharp.dll", "8A14A5D689B25D4EB09279CB85CBFA8092D33BDD86B22A4364E8CD8DED1B3CF3" },
                { "DOTween.dll", "A5D5F45D862B2FBC6F4597C2FD01D31F505918459005C461B7142CF898F3B861" }
            };

        private static int passed;
        private static string managedPath = string.Empty;
        private static string pluginPath = string.Empty;
        private static string? zipPath;

        private static int Main(string[] args)
        {
            try
            {
                ParseArguments(args);
                Run("Managed DLL SHA-256 baseline", TestKnownHashes);
                Run("Message managed contract regression", TestMessageManagedContract);
                Run("Plugin formal identity and message patch surface", TestPluginIdentity);
                Run("Plugin config contract", TestPluginConfigContract);
                Run("Portrait experiment exclusion", TestPortraitExperimentExclusion);
                Run("Plugin forbidden API and patch target scan", TestForbiddenReferences);
                Run("Plugin reference and game-DLL embedding scan", TestPluginReferences);
                if (!string.IsNullOrEmpty(zipPath))
                {
                    Run("Test ZIP content and DLL identity", TestZipArtifact);
                }
                Console.WriteLine(
                    "PASS: " + passed.ToString(CultureInfo.InvariantCulture) + " tests");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "FAIL after " + passed.ToString(CultureInfo.InvariantCulture) +
                    " tests: " + ex);
                return 1;
            }
        }

        private static void ParseArguments(string[] args)
        {
            string? gameRoot = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--game-root" && i + 1 < args.Length)
                {
                    gameRoot = args[++i];
                }
                else if (args[i] == "--plugin" && i + 1 < args.Length)
                {
                    pluginPath = Path.GetFullPath(args[++i]);
                }
                else if (args[i] == "--zip" && i + 1 < args.Length)
                {
                    zipPath = Path.GetFullPath(args[++i]);
                }
                else
                {
                    throw new ArgumentException("Unknown or incomplete argument: " + args[i]);
                }
            }

            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                gameRoot = Environment.GetEnvironmentVariable("LOM_GAME_ROOT");
            }

            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                throw new ArgumentException(
                    "Specify --game-root or the LOM_GAME_ROOT environment variable.");
            }

            managedPath = Path.Combine(
                Path.GetFullPath(gameRoot),
                "Mortal_Data",
                "Managed");
            True(Directory.Exists(managedPath), "Managed directory not found: " + managedPath);

            if (string.IsNullOrEmpty(pluginPath))
            {
                pluginPath = Path.GetFullPath(
                    Path.Combine(
                        Environment.CurrentDirectory,
                        "src",
                        "bin",
                        "Release",
                        "netstandard2.1",
                        "LOM_MessageSpeed.dll"));
            }

            True(File.Exists(pluginPath), "Plugin artifact not found: " + pluginPath);
        }

        private static void TestKnownHashes()
        {
            foreach (KeyValuePair<string, string> pair in KnownHashes)
            {
                string path = Path.Combine(managedPath, pair.Key);
                True(File.Exists(path), "Missing DLL: " + pair.Key);
                Equal(pair.Value, ComputeSha256(path), "SHA-256 mismatch for " + pair.Key);
            }
        }

        private static void TestMessageManagedContract()
        {
            using AssemblyDefinition fungus = ReadManaged("Fungus.dll");
            using AssemblyDefinition story = ReadManaged("Mortal.Story.dll");

            TypeDefinition writer = RequireType(fungus, "Fungus.Writer");
            RequireField(writer, "writingSpeed", "System.Single", false);
            RequireField(writer, "currentWritingSpeed", "System.Single", false);
            RequireMethod(
                writer,
                "ProcessTokens",
                "System.Collections.IEnumerator",
                false,
                "System.Collections.Generic.List`1<Fungus.TextTagToken>",
                "System.Boolean",
                "System.Action");
            RequireMethod(writer, "NotifyEnd", "System.Void", false, "System.Boolean");
            RequireMethod(writer, "get_IsWriting", "System.Boolean", false);
            RequireMethod(writer, "get_IsWaitingForInput", "System.Boolean", false);

            TypeDefinition sayDialog = RequireType(fungus, "Fungus.SayDialog");
            RequireMethod(sayDialog, "GetWriter", "Fungus.Writer", false);
            RequireMethod(sayDialog, "GetSayDialog", "Fungus.SayDialog", true);
            RequireMethod(sayDialog, "OnDestroy", "System.Void", false);

            TypeDefinition storyManager = RequireType(story, "Mortal.Story.StoryManager");
            RequireField(storyManager, "_skipDialog", "System.Boolean", false);
            MethodDefinition skip = RequireMethod(
                storyManager,
                "SkipDialog",
                "System.Void",
                false,
                "System.Boolean");
            RequireFieldWrite(skip, "Mortal.Story.StoryManager", "_skipDialog");
            RequireCall(skip, "Mortal.Story.StoryManager", "SpeedTimeScale", 1);
            RequireCall(skip, "Mortal.Story.StoryManager", "NormalTimeScale", 1);

            TypeDefinition placeholder =
                RequireType(story, "Mortal.Story.SayDialogPlaceholder");
            foreach (string name in new[] { "character", "narrative", "center", "think" })
            {
                RequireMethod(placeholder, "get_" + name, "Fungus.SayDialog", false);
            }
        }

#if PORTRAIT_EXPERIMENT_TESTS
        private static void TestMotionManagedContract()
        {
            using AssemblyDefinition story = ReadManaged("Mortal.Story.dll");
            TypeDefinition controller =
                RequireType(story, "Mortal.Story.StoryCharacterController");
            Equal("Fungus.Character", controller.BaseType.FullName, "Controller base mismatch");

            MethodDefinition shake = RequireMethod(
                controller,
                "Shake",
                "System.Void",
                false,
                "System.Single",
                "System.Single",
                "System.Int32");
            RequireCall(shake, "DG.Tweening.ShortcutExtensions", "DOShakeRotation", 1);

            MethodDefinition move = RequireMethod(
                controller,
                "MoveOffset",
                "System.Void",
                false,
                "System.Single",
                "System.Single",
                "System.Single");
            RequireCall(move, "DG.Tweening.DOTweenModuleUI", "DOAnchorPos", 1);

            MethodDefinition coroutine = RequireMethod(
                controller,
                "MoveOffsetCoroutine",
                "System.Collections.IEnumerator",
                false,
                "System.Single",
                "System.Single",
                "System.Single");
            CustomAttribute iterator = coroutine.CustomAttributes.SingleOrDefault(
                a => a.AttributeType.FullName ==
                    "System.Runtime.CompilerServices.IteratorStateMachineAttribute")
                ?? throw new InvalidOperationException("Motion iterator attribute missing");
            TypeReference stateReference =
                (TypeReference)iterator.ConstructorArguments[0].Value;
            TypeDefinition state = stateReference.Resolve();
            RequireField(state, "x", "System.Single", false);
            RequireField(state, "y", "System.Single", false);
            RequireField(state, "duraion", "System.Single", false);
            MethodDefinition moveNext =
                RequireMethod(state, "MoveNext", "System.Boolean", false);
            RequireCall(moveNext, "DG.Tweening.DOTweenModuleUI", "DOAnchorPos", 1);
            RequireCall(moveNext, "DG.Tweening.TweenExtensions", "WaitForCompletion", 1);

            MethodDefinition rotate = RequireMethod(
                controller,
                "Rotate",
                "System.Void",
                false,
                "System.Single",
                "System.Single");
            RequireCall(rotate, "DG.Tweening.ShortcutExtensions", "DORotateQuaternion", 1);

            TypeDefinition placeholder =
                RequireType(story, "Mortal.Story.CharacterPlaceholder");
            MethodDefinition wrapperCoroutine = RequireMethod(
                placeholder,
                "MoveOffsetCoroutine",
                "System.Collections.IEnumerator",
                false,
                "System.String",
                "System.Single",
                "System.Single",
                "System.Single");
            True(
                wrapperCoroutine.CustomAttributes.Any(
                    a => a.AttributeType.FullName ==
                        "System.Runtime.CompilerServices.IteratorStateMachineAttribute"),
                "Wrapper iterator attribute missing");
        }

        private static void TestTransitionManagedContract()
        {
            using AssemblyDefinition fungus = ReadManaged("Fungus.dll");
            using AssemblyDefinition story = ReadManaged("Mortal.Story.dll");

            TypeDefinition options = RequireType(fungus, "Fungus.PortraitOptions");
            FieldDefinition[] instanceFields = options.Fields.Where(f => !f.IsStatic).ToArray();
            Equal(16, instanceFields.Length, "PortraitOptions field count changed");
            Dictionary<string, string> expectedFields = new Dictionary<string, string>
            {
                { "character", "Fungus.Character" },
                { "replacedCharacter", "Fungus.Character" },
                { "portrait", "UnityEngine.Sprite" },
                { "display", "Fungus.DisplayType" },
                { "offset", "Fungus.PositionOffset" },
                { "fromPosition", "UnityEngine.RectTransform" },
                { "toPosition", "UnityEngine.RectTransform" },
                { "facing", "Fungus.FacingDirection" },
                { "useDefaultSettings", "System.Boolean" },
                { "fadeDuration", "System.Single" },
                { "moveDuration", "System.Single" },
                { "shiftOffset", "UnityEngine.Vector2" },
                { "move", "System.Boolean" },
                { "shiftIntoPlace", "System.Boolean" },
                { "waitUntilFinished", "System.Boolean" },
                { "onComplete", "System.Action" }
            };
            foreach (KeyValuePair<string, string> pair in expectedFields)
            {
                RequireField(options, pair.Key, pair.Value, false);
            }

            TypeDefinition stage = RequireType(fungus, "Fungus.Stage");
            Equal("Fungus.PortraitController", stage.BaseType.FullName, "Stage base mismatch");
            TypeDefinition storyStage =
                RequireType(story, "Mortal.Story.StoryStageController");
            Equal("Fungus.Stage", storyStage.BaseType.FullName, "StoryStage base mismatch");

            MethodDefinition show = RequireMethod(
                storyStage,
                "Show",
                "System.Void",
                false,
                "Fungus.PortraitOptions");
            MethodDefinition hide = RequireMethod(
                storyStage,
                "Hide",
                "System.Void",
                false,
                "Fungus.PortraitOptions");
            True(show.IsVirtual && !show.IsNewSlot, "Story Show is not an override");
            True(hide.IsVirtual && !hide.IsNewSlot, "Story Hide is not an override");

            TypeDefinition controller = RequireType(fungus, "Fungus.PortraitController");
            MethodDefinition clean = RequireMethod(
                controller,
                "CleanPortraitOptions",
                "Fungus.PortraitOptions",
                false,
                "Fungus.PortraitOptions");
            RequireFieldWrite(clean, "Fungus.PortraitOptions", "fadeDuration");
            RequireFieldWrite(clean, "Fungus.PortraitOptions", "moveDuration");

            MethodDefinition finish = RequireMethod(
                controller,
                "FinishCommand",
                "System.Void",
                false,
                "Fungus.PortraitOptions");
            RequireFieldRead(finish, "Fungus.PortraitOptions", "fadeDuration");
            RequireCall(finish, "Fungus.PortraitController", "WaitUntilFinished", 2);

            MethodDefinition doMove = RequireMethod(
                controller,
                "DoMoveTween",
                "System.Void",
                false,
                "Fungus.PortraitOptions");
            RequireFieldWrite(doMove, "Fungus.PortraitController", "waitTimer");

            MethodDefinition directShow = RequireMethod(
                controller,
                "Show",
                "System.Void",
                false,
                "Fungus.Character",
                "System.String");
            RequireCall(directShow, "Fungus.PortraitController", "Show", 1);
            True(
                directShow.Body.Instructions.Any(
                    i => i.OpCode == OpCodes.Newobj &&
                        i.Operand is MethodReference method &&
                        method.DeclaringType.FullName == "Fungus.PortraitOptions"),
                "Direct Show no longer creates PortraitOptions before virtual dispatch");
        }

#endif
        private static void TestPluginIdentity()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            Equal("LOM_MessageSpeed", plugin.Name.Name, "Assembly name mismatch");
            Equal(new Version(1, 0, 0, 0), plugin.Name.Version, "AssemblyVersion mismatch");
            CustomAttribute fileVersion = plugin.CustomAttributes.Single(
                a => a.AttributeType.FullName ==
                    "System.Reflection.AssemblyFileVersionAttribute");
            CustomAttribute informationalVersion = plugin.CustomAttributes.Single(
                a => a.AttributeType.FullName ==
                    "System.Reflection.AssemblyInformationalVersionAttribute");
            Equal("0.2.0.0", fileVersion.ConstructorArguments[0].Value, "FileVersion");
            string informational =
                (string)informationalVersion.ConstructorArguments[0].Value;
            True(
                informational == "0.2.0" ||
                informational.StartsWith("0.2.0+", StringComparison.Ordinal),
                "InformationalVersion mismatch: " + informational);

            TypeDefinition type = RequireType(plugin, "LOM.MessageSpeed.Plugin");
            CustomAttribute attribute = type.CustomAttributes.SingleOrDefault(
                a => a.AttributeType.FullName == "BepInEx.BepInPlugin")
                ?? throw new InvalidOperationException("BepInPlugin attribute missing");
            Equal("lom-messagespeed", attribute.ConstructorArguments[0].Value, "Plugin GUID");
            Equal("LOM_MessageSpeed", attribute.ConstructorArguments[1].Value, "Plugin name");
            Equal("0.2.0", attribute.ConstructorArguments[2].Value, "Plugin version");

            Equal(
                "lom-messagespeed",
                RequireConstant(type, "MessageHarmonyId"),
                "Message Harmony ID");

            RequireField(type, "messageHarmony", "HarmonyLib.Harmony", false);
            RequireField(type, "messagePatchesApplied", "System.Boolean", false);

            MethodDefinition applyMessage =
                RequireMethod(type, "ApplyMessagePatches", "System.Void", false);
            RequireCall(applyMessage, "HarmonyLib.Harmony", "Patch", 4);
            RequireString(applyMessage, "ProcessTokensPrefix");
            RequireString(applyMessage, "NotifyEndPostfix");
            RequireString(applyMessage, "SkipDialogPrefix");
            RequireString(applyMessage, "SayDialogOnDestroyPrefix");
        }

#if PORTRAIT_EXPERIMENT_TESTS
        private static void TestMotionPatchSignatures()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            TypeDefinition patch =
                RequireType(plugin, "LOM.MessageSpeed.Portrait.Patches.PortraitMotionPatch");
            MethodDefinition shake =
                RequireMethod(patch, "ShakePrefix", "System.Void", true, "System.Single&");
            MethodDefinition move =
                RequireMethod(patch, "MoveOffsetPrefix", "System.Void", true, "System.Single&");
            MethodDefinition coroutine = RequireMethod(
                patch,
                "MoveOffsetCoroutinePrefix",
                "System.Void",
                true,
                "System.Single&");
            MethodDefinition rotate =
                RequireMethod(patch, "RotatePrefix", "System.Void", true, "System.Single&");
            Equal("__0", shake.Parameters[0].Name, "Shake duration index");
            Equal("__2", move.Parameters[0].Name, "MoveOffset duration index");
            Equal("__2", coroutine.Parameters[0].Name, "Coroutine duration index");
            Equal("__0", rotate.Parameters[0].Name, "Rotate duration index");
        }

        private static void TestTransitionPatchSignatures()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            TypeDefinition patch = RequireType(
                plugin,
                "LOM.MessageSpeed.Portrait.Patches.PortraitTransitionPatch");
            MethodDefinition show = RequireMethod(
                patch,
                "ShowPrefix",
                "System.Void",
                true,
                "Mortal.Story.StoryStageController",
                "Fungus.PortraitOptions&");
            MethodDefinition hide = RequireMethod(
                patch,
                "HidePrefix",
                "System.Void",
                true,
                "Mortal.Story.StoryStageController",
                "Fungus.PortraitOptions&");
            Equal("__instance", show.Parameters[0].Name, "Show instance injection");
            Equal("__0", show.Parameters[1].Name, "Show options index");
            Equal("__instance", hide.Parameters[0].Name, "Hide instance injection");
            Equal("__0", hide.Parameters[1].Name, "Hide options index");
        }

        private static void TestCategoryFailureIsolation()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            TypeDefinition type = RequireType(plugin, "LOM.MessageSpeed.Plugin");
            MethodDefinition message =
                RequireMethod(type, "InitializeMessageCategory", "System.Void", false);
            MethodDefinition motion =
                RequireMethod(type, "InitializeMotionCategory", "System.Void", false);
            MethodDefinition transition =
                RequireMethod(type, "InitializeTransitionCategory", "System.Void", false);
            MethodDefinition destroy =
                RequireMethod(type, "OnDestroy", "System.Void", false);

            RequireFieldRead(message, "LOM.MessageSpeed.Plugin", "messageHarmony");
            RequireNoFieldReference(message, "LOM.MessageSpeed.Plugin", "motionHarmony");
            RequireFieldRead(motion, "LOM.MessageSpeed.Plugin", "motionHarmony");
            RequireNoFieldReference(motion, "LOM.MessageSpeed.Plugin", "messageHarmony");
            RequireFieldRead(transition, "LOM.MessageSpeed.Plugin", "transitionHarmony");
            RequireNoFieldReference(transition, "LOM.MessageSpeed.Plugin", "messageHarmony");
            RequireNoFieldReference(transition, "LOM.MessageSpeed.Plugin", "motionHarmony");
            RequireFieldRead(destroy, "LOM.MessageSpeed.Plugin", "messageHarmony");
            RequireFieldRead(destroy, "LOM.MessageSpeed.Plugin", "motionHarmony");
            RequireFieldRead(destroy, "LOM.MessageSpeed.Plugin", "transitionHarmony");

            RequireCall(message, "HarmonyLib.Harmony", "UnpatchSelf", 1);
            RequireCall(motion, "HarmonyLib.Harmony", "UnpatchSelf", 1);
            RequireCall(transition, "HarmonyLib.Harmony", "UnpatchSelf", 1);
            RequireCall(destroy, "HarmonyLib.Harmony", "UnpatchSelf", 3);

            MethodDefinition adjust =
                RequireMethod(
                    type,
                    "TryAdjustMotionDuration",
                    "System.Void",
                    true,
                    "System.Single&",
                    "System.String");
            RequireFieldRead(adjust, "LOM.MessageSpeed.Plugin", "portraitConfig");
            RequireNoFieldReference(adjust, "LOM.MessageSpeed.Plugin", "messageConfig");

            MethodDefinition prepare = RequireMethod(
                type,
                "TryPrepareTransitionOptions",
                "System.Void",
                true,
                "Mortal.Story.StoryStageController",
                "Fungus.PortraitOptions&",
                "System.String");
            RequireFieldRead(prepare, "LOM.MessageSpeed.Plugin", "portraitConfig");
            RequireNoFieldReference(prepare, "LOM.MessageSpeed.Plugin", "messageConfig");
            RequireCall(prepare, "LOM.MessageSpeed.Portrait.PortraitOptionsCopy", "Clone", 1);
            RequireCall(prepare, "LOM.MessageSpeed.Portrait.DurationScaler", "TryScaleDuration", 2);
        }

        private static void TestPortraitOptionsCopyArtifact()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            TypeDefinition copyType =
                RequireType(plugin, "LOM.MessageSpeed.Portrait.PortraitOptionsCopy");
            Equal(16, RequireConstant(copyType, "ExpectedInstanceFieldCount"), "Copy field count");
            MethodDefinition clone = RequireMethod(
                copyType,
                "Clone",
                "Fungus.PortraitOptions",
                true,
                "Fungus.PortraitOptions");

            string[] fields =
            {
                "character",
                "replacedCharacter",
                "portrait",
                "display",
                "offset",
                "fromPosition",
                "toPosition",
                "facing",
                "useDefaultSettings",
                "fadeDuration",
                "moveDuration",
                "shiftOffset",
                "move",
                "shiftIntoPlace",
                "waitUntilFinished",
                "onComplete"
            };
            foreach (string fieldName in fields)
            {
                int reads = clone.Body.Instructions.Count(
                    i => i.OpCode == OpCodes.Ldfld &&
                         i.Operand is FieldReference field &&
                         field.DeclaringType.FullName == "Fungus.PortraitOptions" &&
                         field.Name == fieldName);
                int writes = clone.Body.Instructions.Count(
                    i => i.OpCode == OpCodes.Stfld &&
                         i.Operand is FieldReference field &&
                         field.DeclaringType.FullName == "Fungus.PortraitOptions" &&
                         field.Name == fieldName);
                Equal(1, reads, "Copy read count: " + fieldName);
                Equal(1, writes, "Copy write count: " + fieldName);
            }
        }

#endif
        private static void TestPluginConfigContract()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            HashSet<string> strings = new HashSet<string>(
                AllMethods(plugin)
                    .Where(m => m.HasBody)
                    .SelectMany(m => m.Body.Instructions)
                    .Where(i => i.OpCode == OpCodes.Ldstr)
                    .Select(i => (string)i.Operand),
                StringComparer.Ordinal);

            foreach (string required in new[]
            {
                "General",
                "Enabled",
                "Message",
                "SpeedMultiplier"
            })
            {
                True(strings.Contains(required), "Missing config string: " + required);
            }

            TypeDefinition messageConfig =
                RequireType(plugin, "LOM.MessageSpeed.MessageSpeedConfig");
            MethodDefinition messageConstructor = RequireMethod(
                messageConfig,
                ".ctor",
                "System.Void",
                false,
                "BepInEx.Configuration.ConfigFile");
            RequireFloatConstant(messageConstructor, 1.5f, 1);
        }

        private static void TestPortraitExperimentExclusion()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            string[] forbiddenFragments =
            {
                ".Portrait",
                "MotionSpeedMultiplier",
                "TransitionSpeedMultiplier",
                "transition-test",
                "experimental 0.2.0"
            };
            foreach (TypeDefinition type in AllTypes(plugin.MainModule.Types))
            {
                True(
                    !type.FullName.Contains(".Portrait", StringComparison.Ordinal),
                    "Portrait type was compiled: " + type.FullName);
            }

            foreach (string value in AllMethods(plugin)
                .Where(m => m.HasBody)
                .SelectMany(m => m.Body.Instructions)
                .Where(i => i.OpCode == OpCodes.Ldstr)
                .Select(i => (string)i.Operand))
            {
                foreach (string fragment in forbiddenFragments)
                {
                    True(
                        !value.Contains(fragment, StringComparison.OrdinalIgnoreCase),
                        "Experimental string was compiled: " + value);
                }
            }
        }

        private static void TestForbiddenReferences()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            foreach (MethodDefinition method in AllMethods(plugin).Where(m => m.HasBody))
            {
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    if (!(instruction.Operand is MethodReference called))
                    {
                        continue;
                    }

                    True(
                        !(called.DeclaringType.FullName == "UnityEngine.Time" &&
                          called.Name == "set_timeScale"),
                        "Plugin writes Time.timeScale: " + method.FullName);
                    True(
                        called.DeclaringType.FullName != "System.Net.Http.HttpClient",
                        "Plugin references HttpClient");
                    True(
                        !called.DeclaringType.FullName.StartsWith(
                            "System.Net.Sockets.",
                            StringComparison.Ordinal),
                        "Plugin references sockets");
                    True(
                        !(called.DeclaringType.FullName.StartsWith(
                              "Microsoft.Win32.Registry",
                              StringComparison.Ordinal)),
                        "Plugin references registry");
                }
            }

        }

        private static void TestPluginReferences()
        {
            using AssemblyDefinition plugin = AssemblyDefinition.ReadAssembly(pluginPath);
            HashSet<string> references = new HashSet<string>(
                plugin.MainModule.AssemblyReferences.Select(r => r.Name),
                StringComparer.Ordinal);
            foreach (string expected in new[]
            {
                "BepInEx.Core",
                "BepInEx.Unity.Mono",
                "0Harmony",
                "Fungus",
                "Mortal.Story"
            })
            {
                True(references.Contains(expected), "Missing expected reference: " + expected);
            }

            foreach (string forbidden in new[]
            {
                "System.Net.Http",
                "Microsoft.Win32.Registry",
                "Mortal.Core",
                "Mortal.Combat",
                "Mortal.Battle"
            })
            {
                True(!references.Contains(forbidden), "Unexpected reference: " + forbidden);
            }
        }

        private static void TestZipArtifact()
        {
            True(zipPath != null && File.Exists(zipPath), "ZIP artifact not found");
            using ZipArchive archive = ZipFile.OpenRead(zipPath!);
            string[] names = archive.Entries.Select(e => e.FullName).ToArray();
            string[] expected =
            {
                "LOM_MessageSpeed.dll",
                "README.md",
                "LICENSE"
            };
            Equal(expected.Length, names.Length, "ZIP entry count");
            foreach (string name in expected)
            {
                Equal(1, names.Count(n => n == name), "ZIP entry mismatch: " + name);
            }

            foreach (string name in names)
            {
                True(
                    !name.Contains("..", StringComparison.Ordinal) &&
                    !Path.IsPathRooted(name) &&
                    !name.Contains('\\'),
                    "Unsafe ZIP entry: " + name);
            }

            ZipArchiveEntry dllEntry =
                archive.GetEntry("LOM_MessageSpeed.dll")
                ?? throw new InvalidOperationException("ZIP DLL missing");
            using Stream entryStream = dllEntry.Open();
            using SHA256 sha = SHA256.Create();
            string zipDllHash = Convert.ToHexString(sha.ComputeHash(entryStream));
            Equal(ComputeSha256(pluginPath), zipDllHash, "ZIP DLL differs from build artifact");
        }

        private static AssemblyDefinition ReadManaged(string name)
        {
            return AssemblyDefinition.ReadAssembly(Path.Combine(managedPath, name));
        }

        private static TypeDefinition RequireType(AssemblyDefinition assembly, string fullName)
        {
            TypeDefinition? type = AllTypes(assembly.MainModule.Types)
                .SingleOrDefault(t => t.FullName == fullName);
            return type ?? throw new InvalidOperationException("Missing type: " + fullName);
        }

        private static FieldDefinition RequireField(
            TypeDefinition type,
            string name,
            string fieldType,
            bool isStatic)
        {
            FieldDefinition[] matches = type.Fields.Where(
                f => f.Name == name &&
                     f.FieldType.FullName == fieldType &&
                     f.IsStatic == isStatic).ToArray();
            Equal(1, matches.Length, "Field contract mismatch: " + type.FullName + "." + name);
            return matches[0];
        }

        private static MethodDefinition RequireMethod(
            TypeDefinition type,
            string name,
            string returnType,
            bool isStatic,
            params string[] parameterTypes)
        {
            MethodDefinition[] matches = type.Methods.Where(
                m => m.Name == name &&
                     m.ReturnType.FullName == returnType &&
                     m.IsStatic == isStatic &&
                     m.Parameters.Select(p => p.ParameterType.FullName)
                         .SequenceEqual(parameterTypes)).ToArray();
            Equal(1, matches.Length, "Method contract mismatch: " + type.FullName + "." + name);
            return matches[0];
        }

        private static void RequireCall(
            MethodDefinition caller,
            string declaringType,
            string name,
            int expectedCount)
        {
            int count = caller.Body.Instructions.Count(
                i => i.Operand is MethodReference method &&
                     method.DeclaringType.FullName == declaringType &&
                     method.Name == name);
            Equal(
                expectedCount,
                count,
                "Call count mismatch: " + caller.FullName + " -> " + declaringType + "." + name);
        }

        private static void RequireFieldRead(
            MethodDefinition method,
            string declaringType,
            string name)
        {
            True(
                method.Body.Instructions.Any(
                    i => (i.OpCode == OpCodes.Ldfld || i.OpCode == OpCodes.Ldsfld) &&
                         i.Operand is FieldReference field &&
                         field.DeclaringType.FullName == declaringType &&
                         field.Name == name),
                "Field read missing: " + method.FullName + " -> " + declaringType + "." + name);
        }

        private static void RequireFieldWrite(
            MethodDefinition method,
            string declaringType,
            string name)
        {
            True(
                method.Body.Instructions.Any(
                    i => (i.OpCode == OpCodes.Stfld || i.OpCode == OpCodes.Stsfld) &&
                         i.Operand is FieldReference field &&
                         field.DeclaringType.FullName == declaringType &&
                         field.Name == name),
                "Field write missing: " + method.FullName + " -> " + declaringType + "." + name);
        }

        private static void RequireNoFieldReference(
            MethodDefinition method,
            string declaringType,
            string name)
        {
            True(
                !method.Body.Instructions.Any(
                    i => i.Operand is FieldReference field &&
                         field.DeclaringType.FullName == declaringType &&
                         field.Name == name),
                "Unexpected field reference: " +
                method.FullName + " -> " + declaringType + "." + name);
        }

        private static object RequireConstant(TypeDefinition type, string name)
        {
            FieldDefinition field = type.Fields.SingleOrDefault(f => f.Name == name)
                ?? throw new InvalidOperationException(
                    "Constant field missing: " + type.FullName + "." + name);
            True(field.HasConstant, "Field is not constant: " + field.FullName);
            return field.Constant;
        }

        private static void RequireString(MethodDefinition method, string value)
        {
            True(
                method.Body.Instructions.Any(
                    i => i.OpCode == OpCodes.Ldstr &&
                         string.Equals((string)i.Operand, value, StringComparison.Ordinal)),
                "String operand missing: " + method.FullName + " -> " + value);
        }

        private static void RequireFloatConstant(
            MethodDefinition method,
            float value,
            int expectedCount)
        {
            int count = method.Body.Instructions.Count(
                i => i.OpCode == OpCodes.Ldc_R4 &&
                     i.Operand is float actual &&
                     actual.Equals(value));
            Equal(
                expectedCount,
                count,
                "Float constant count: " + method.FullName + " -> " + value);
        }

        private static IEnumerable<TypeDefinition> AllTypes(
            IEnumerable<TypeDefinition> roots)
        {
            foreach (TypeDefinition type in roots)
            {
                yield return type;
                foreach (TypeDefinition nested in AllTypes(type.NestedTypes))
                {
                    yield return nested;
                }
            }
        }

        private static IEnumerable<MethodDefinition> AllMethods(AssemblyDefinition assembly)
        {
            return AllTypes(assembly.MainModule.Types).SelectMany(t => t.Methods);
        }

        private static string ComputeSha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(sha.ComputeHash(stream));
        }

        private static void Run(string name, Action test)
        {
            test();
            passed++;
            Console.WriteLine(
                "ok " + passed.ToString(CultureInfo.InvariantCulture) + " - " + name);
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message + " expected=" + expected + " actual=" + actual);
            }
        }

        private static void NearlyEqual(float expected, float actual, string message)
        {
            float tolerance = Math.Max(1e-6f, Math.Abs(expected) * 1e-5f);
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new InvalidOperationException(
                    message + " expected=" + expected + " actual=" + actual);
            }
        }

        private static void BitsEqual(float expected, float actual, string message)
        {
            int expectedBits = BitConverter.SingleToInt32Bits(expected);
            int actualBits = BitConverter.SingleToInt32Bits(actual);
            if (expectedBits != actualBits)
            {
                throw new InvalidOperationException(
                    message + " expectedBits=" + expectedBits + " actualBits=" + actualBits);
            }
        }
    }
}
