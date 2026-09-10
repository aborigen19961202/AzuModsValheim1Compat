using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AzuModsValheim1Compat
{
    public static class Patcher
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource("AzuModsValheim1Compat");
        private static bool _pluginsPatched = false;

        private static readonly HashSet<string> TargetPluginNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AzuCraftyBoxes.dll",
            "AzuAutoStore.dll",
            "AzuExtendedPlayerInventory.dll",
            "AzuAntiArthriticCrafting.dll",
            "AzuClock.dll",
            "FactionAssigner.dll",
            "MistBeGone.dll",
            "PetPantry.dll",
            "Recycle_N_Reclaim.dll",
            "CurrencyPocket.dll",
            "SaveCrossbowState.dll",
            "TrueInstantLootDrop.dll"
        };

        // Tell BepInEx Preloader to pass assembly_valheim.dll to Patch()
        public static IEnumerable<string> TargetDLLs
        {
            get
            {
                ExecutePluginPatch();
                return new[] { "assembly_valheim.dll" };
            }
        }

        public static void Initialize()
        {
            ExecutePluginPatch();
        }

        // BepInEx Preloader hook for assembly_valheim.dll
        public static void Patch(AssemblyDefinition assembly)
        {
            if (assembly == null || assembly.Name.Name != "assembly_valheim")
                return;

            try
            {
                Log.LogInfo("Applying Valheim 1.0.7 API bridge methods to assembly_valheim...");
                InjectBridges(assembly);
                Log.LogInfo("Valheim 1.0.7 API bridges injected successfully!");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to inject API bridges into assembly_valheim: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void InjectBridges(AssemblyDefinition assembly)
        {
            var mainModule = assembly.MainModule;

            // 1. VisEquipment.AttachArmor(int itemHash, int variant) -> calls AttachArmor(itemHash, variant, 0)
            var visEquip = mainModule.GetType("VisEquipment");
            if (visEquip != null)
            {
                var target = visEquip.Methods.FirstOrDefault(m => m.Name == "AttachArmor" && m.Parameters.Count == 3);
                var existing = visEquip.Methods.FirstOrDefault(m => m.Name == "AttachArmor" && m.Parameters.Count == 2);
                if (target != null && existing == null)
                {
                    var bridge = new MethodDefinition("AttachArmor", MethodAttributes.Public | MethodAttributes.HideBySig, target.ReturnType);
                    bridge.Parameters.Add(new ParameterDefinition("itemHash", ParameterAttributes.None, target.Parameters[0].ParameterType));
                    bridge.Parameters.Add(new ParameterDefinition("variant", ParameterAttributes.None, target.Parameters[1].ParameterType));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldarg_1); // itemHash
                    il.Emit(OpCodes.Ldarg_2); // variant
                    il.Emit(OpCodes.Ldc_I4_0); // quality = 0
                    il.Emit(OpCodes.Callvirt, target);
                    il.Emit(OpCodes.Ret);

                    visEquip.Methods.Add(bridge);
                    Log.LogInfo(" - Injected VisEquipment.AttachArmor(int, int) [Fixes MagicPlugin / ExtraSlots error]");
                }
            }

            // 2. Character.Message(MessageType type, string msg, int amount, Sprite icon) -> calls Message(type, msg, amount, icon, false)
            var character = mainModule.GetType("Character");
            if (character != null)
            {
                var target = character.Methods.FirstOrDefault(m => m.Name == "Message" && m.Parameters.Count == 5);
                var existing = character.Methods.FirstOrDefault(m => m.Name == "Message" && m.Parameters.Count == 4);
                if (target != null && existing == null)
                {
                    var bridge = new MethodDefinition("Message", MethodAttributes.Public | MethodAttributes.HideBySig, target.ReturnType);
                    for (int i = 0; i < 4; i++)
                        bridge.Parameters.Add(new ParameterDefinition(target.Parameters[i].Name, ParameterAttributes.None, target.Parameters[i].ParameterType));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldarg, bridge.Parameters[3]);
                    il.Emit(OpCodes.Ldc_I4_0); // always = false
                    il.Emit(OpCodes.Callvirt, target);
                    il.Emit(OpCodes.Ret);

                    character.Methods.Add(bridge);
                    Log.LogInfo(" - Injected Character.Message(MessageType, string, int, Sprite)");
                }
            }

            // 3. SEMan.AddStatusEffect(StatusEffect, bool, int, float) -> calls (StatusEffect, bool, int, float, (short)0)
            var seMan = mainModule.GetType("SEMan");
            if (seMan != null)
            {
                var target1 = seMan.Methods.FirstOrDefault(m => m.Name == "AddStatusEffect" && m.Parameters.Count == 5 && m.Parameters[0].ParameterType.Name == "StatusEffect");
                var existing1 = seMan.Methods.FirstOrDefault(m => m.Name == "AddStatusEffect" && m.Parameters.Count == 4 && m.Parameters[0].ParameterType.Name == "StatusEffect");
                if (target1 != null && existing1 == null)
                {
                    var bridge = new MethodDefinition("AddStatusEffect", MethodAttributes.Public | MethodAttributes.HideBySig, target1.ReturnType);
                    for (int i = 0; i < 4; i++)
                        bridge.Parameters.Add(new ParameterDefinition(target1.Parameters[i].Name, ParameterAttributes.None, target1.Parameters[i].ParameterType));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldarg, bridge.Parameters[3]);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_I2); // short 0
                    il.Emit(OpCodes.Callvirt, target1);
                    il.Emit(OpCodes.Ret);

                    seMan.Methods.Add(bridge);
                    Log.LogInfo(" - Injected SEMan.AddStatusEffect(StatusEffect, bool, int, float)");
                }

                // Overload 2: (int nameHash, bool, int, float) -> calls (int, bool, int, float, (short)0)
                var target2 = seMan.Methods.FirstOrDefault(m => m.Name == "AddStatusEffect" && m.Parameters.Count == 5 && m.Parameters[0].ParameterType.Name == "Int32");
                var existing2 = seMan.Methods.FirstOrDefault(m => m.Name == "AddStatusEffect" && m.Parameters.Count == 4 && m.Parameters[0].ParameterType.Name == "Int32");
                if (target2 != null && existing2 == null)
                {
                    var bridge = new MethodDefinition("AddStatusEffect", MethodAttributes.Public | MethodAttributes.HideBySig, target2.ReturnType);
                    for (int i = 0; i < 4; i++)
                        bridge.Parameters.Add(new ParameterDefinition(target2.Parameters[i].Name, ParameterAttributes.None, target2.Parameters[i].ParameterType));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldarg, bridge.Parameters[3]);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_I2); // short 0
                    il.Emit(OpCodes.Callvirt, target2);
                    il.Emit(OpCodes.Ret);

                    seMan.Methods.Add(bridge);
                    Log.LogInfo(" - Injected SEMan.AddStatusEffect(int, bool, int, float)");
                }
            }

            // 4. Inventory.IsTeleportable() -> calls IsTeleportable(false)
            var inventory = mainModule.GetType("Inventory");
            if (inventory != null)
            {
                var targetTele = inventory.Methods.FirstOrDefault(m => m.Name == "IsTeleportable" && m.Parameters.Count == 1);
                var existingTele = inventory.Methods.FirstOrDefault(m => m.Name == "IsTeleportable" && m.Parameters.Count == 0);
                if (targetTele != null && existingTele == null)
                {
                    var bridge = new MethodDefinition("IsTeleportable", MethodAttributes.Public | MethodAttributes.HideBySig, targetTele.ReturnType);
                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4_0); // ignoreExtraSlots = false
                    il.Emit(OpCodes.Callvirt, targetTele);
                    il.Emit(OpCodes.Ret);

                    inventory.Methods.Add(bridge);
                    Log.LogInfo(" - Injected Inventory.IsTeleportable()");
                }

                // 4b. Inventory.AddItem(ItemData, int, int, int) -> calls AddItem(item, amount, x, y, false)
                var targetAdd4 = inventory.Methods.FirstOrDefault(m => m.Name == "AddItem" && m.Parameters.Count == 5 && m.Parameters[0].ParameterType.Name == "ItemData");
                var existingAdd3 = inventory.Methods.FirstOrDefault(m => m.Name == "AddItem" && m.Parameters.Count == 4 && m.Parameters[0].ParameterType.Name == "ItemData");
                if (targetAdd4 != null && existingAdd3 == null)
                {
                    var bridge = new MethodDefinition("AddItem", MethodAttributes.Public | MethodAttributes.HideBySig, targetAdd4.ReturnType);
                    for (int i = 0; i < 4; i++)
                        bridge.Parameters.Add(new ParameterDefinition(targetAdd4.Parameters[i].Name, ParameterAttributes.None, targetAdd4.Parameters[i].ParameterType));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldarg, bridge.Parameters[3]);
                    il.Emit(OpCodes.Ldc_I4_0); // worldGen = false
                    il.Emit(OpCodes.Callvirt, targetAdd4);
                    il.Emit(OpCodes.Ret);

                    inventory.Methods.Add(bridge);
                    Log.LogInfo(" - Injected Inventory.AddItem(ItemData, int, int, int)");
                }
            }

            // 5. EffectList.Create(Vector3, Quaternion, Transform, float, int) -> calls Create(..., ZDOID.None)
            var effectList = mainModule.GetType("EffectList");
            if (effectList != null)
            {
                var targetCreate = effectList.Methods.FirstOrDefault(m => m.Name == "Create" && m.Parameters.Count == 6);
                var existingCreate = effectList.Methods.FirstOrDefault(m => m.Name == "Create" && m.Parameters.Count == 5);
                var zdoidType = mainModule.GetType("ZDOID");
                var noneField = zdoidType?.Fields.FirstOrDefault(f => f.Name == "None");
                if (targetCreate != null && existingCreate == null && noneField != null)
                {
                    var bridge = new MethodDefinition("Create", MethodAttributes.Public | MethodAttributes.HideBySig, targetCreate.ReturnType);
                    for (int i = 0; i < 5; i++)
                        bridge.Parameters.Add(new ParameterDefinition(targetCreate.Parameters[i].Name, ParameterAttributes.None, targetCreate.Parameters[i].ParameterType));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldarg, bridge.Parameters[3]);
                    il.Emit(OpCodes.Ldarg, bridge.Parameters[4]);
                    il.Emit(OpCodes.Ldsfld, noneField); // ZDOID.None
                    il.Emit(OpCodes.Callvirt, targetCreate);
                    il.Emit(OpCodes.Ret);

                    effectList.Methods.Add(bridge);
                    Log.LogInfo(" - Injected EffectList.Create(Vector3, Quaternion, Transform, float, int)");
                }
            }

            // 6. Terminal.ConsoleCommand..ctor (12 params) -> calls 13-param constructor with hideBehindDevCommands = false
            var terminal = mainModule.GetType("Terminal");
            var consoleCmd = terminal?.NestedTypes.FirstOrDefault(t => t.Name == "ConsoleCommand");
            if (consoleCmd != null)
            {
                var targetCtor = consoleCmd.Methods.FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == 13 && m.Parameters[2].ParameterType.Name == "ConsoleEvent");
                var existing12 = consoleCmd.Methods.FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == 12 && m.Parameters[2].ParameterType.Name == "ConsoleEvent");
                if (targetCtor != null && existing12 == null)
                {
                    var bridge = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, mainModule.TypeSystem.Void);
                    for (int i = 0; i < 8; i++)
                        bridge.Parameters.Add(new ParameterDefinition(targetCtor.Parameters[i].Name, ParameterAttributes.None, targetCtor.Parameters[i].ParameterType));
                    for (int i = 9; i < 13; i++)
                        bridge.Parameters.Add(new ParameterDefinition(targetCtor.Parameters[i].Name, ParameterAttributes.None, targetCtor.Parameters[i].ParameterType));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0); // this
                    for (int i = 0; i < 8; i++)
                        il.Emit(OpCodes.Ldarg, bridge.Parameters[i]);
                    il.Emit(OpCodes.Ldc_I4_0); // hideBehindDevCommands = false
                    for (int i = 8; i < 12; i++)
                        il.Emit(OpCodes.Ldarg, bridge.Parameters[i]);
                    il.Emit(OpCodes.Call, targetCtor);
                    il.Emit(OpCodes.Ret);

                    consoleCmd.Methods.Add(bridge);
                    Log.LogInfo(" - Injected Terminal.ConsoleCommand..ctor (12 params)");
                }
            }
        }

        public static void ExecutePluginPatch()
        {
            if (_pluginsPatched)
                return;

            _pluginsPatched = true;

            try
            {
                string pluginsPath = Paths.PluginPath;
                if (string.IsNullOrEmpty(pluginsPath) || !Directory.Exists(pluginsPath))
                {
                    try
                    {
                        pluginsPath = Path.Combine(Paths.BepInExRootPath, "plugins");
                    }
                    catch
                    {
                    }
                }

                if (string.IsNullOrEmpty(pluginsPath) || !Directory.Exists(pluginsPath))
                {
                    Log.LogWarning($"Plugins directory not found at: {pluginsPath}");
                    return;
                }

                Log.LogInfo("Scanning plugins directory for Azumatt / ServerSync mods requiring Valheim 1.0.7 compatibility...");

                string[] allPluginDlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
                int patchedAssembliesCount = 0;
                int totalReplacementsCount = 0;

                foreach (string dllPath in allPluginDlls)
                {
                    string fileName = Path.GetFileName(dllPath);

                    if (fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("AzuModsValheim1Compat.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    bool isTarget = TargetPluginNames.Contains(fileName) ||
                                    fileName.StartsWith("Azu", StringComparison.OrdinalIgnoreCase);

                    if (!isTarget)
                        continue;

                    try
                    {
                        int replacements = PatchPluginFile(dllPath);
                        if (replacements > 0)
                        {
                            patchedAssembliesCount++;
                            totalReplacementsCount += replacements;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Error patching {fileName}: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                if (patchedAssembliesCount > 0)
                {
                    Log.LogInfo($"Plugin compatibility patch applied: {patchedAssembliesCount} assembly(ies) updated ({totalReplacementsCount} ZRoutedRpc.Everybody instructions fixed).");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Critical error during plugin compatibility patch execution: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static int PatchPluginFile(string dllPath)
        {
            string fileName = Path.GetFileName(dllPath);

            byte[] dllBytes = File.ReadAllBytes(dllPath);
            using (var stream = new MemoryStream(dllBytes))
            using (var assembly = AssemblyDefinition.ReadAssembly(stream))
            {
                int replacedInstructions = 0;

                foreach (var type in assembly.MainModule.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody || method.Body.Instructions == null)
                            continue;

                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.OpCode == OpCodes.Ldsfld &&
                                instruction.Operand is FieldReference fieldRef &&
                                fieldRef.DeclaringType != null &&
                                fieldRef.DeclaringType.Name == "ZRoutedRpc" &&
                                fieldRef.Name == "Everybody")
                            {
                                instruction.OpCode = OpCodes.Ldc_I8;
                                instruction.Operand = 0L;
                                replacedInstructions++;
                            }
                        }
                    }
                }

                if (replacedInstructions == 0)
                {
                    return 0;
                }

                string backupPath = dllPath + ".orig.bak";
                if (!File.Exists(backupPath))
                {
                    File.Copy(dllPath, backupPath, false);
                }

                string tempPath = dllPath + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                assembly.Write(tempPath);
                File.Copy(tempPath, dllPath, true);
                File.Delete(tempPath);

                Log.LogInfo($"[Compatibility Fix] Patched {fileName}: replaced {replacedInstructions} ZRoutedRpc.Everybody instruction(s). Backup: {Path.GetFileName(backupPath)}");
                return replacedInstructions;
            }
        }
    }
}
