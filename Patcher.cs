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

            // 0. ZRoutedRpc.Everybody: convert const/literal to public static long
            // This fixes ServerSync and all Azumatt mods entirely in memory, without touching plugin DLLs on disk!
            var zrpc = mainModule.GetType("ZRoutedRpc");
            var everybodyField = zrpc?.Fields.FirstOrDefault(f => f.Name == "Everybody");
            if (everybodyField != null && everybodyField.IsLiteral)
            {
                everybodyField.Attributes &= ~FieldAttributes.Literal;
                everybodyField.Attributes &= ~FieldAttributes.HasDefault;
                everybodyField.Attributes |= FieldAttributes.Static | FieldAttributes.Public;
                everybodyField.Constant = null;

                var cctor = zrpc.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
                if (cctor == null)
                {
                    cctor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, mainModule.TypeSystem.Void);
                    var il = cctor.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldc_I8, 0L);
                    il.Emit(OpCodes.Stsfld, everybodyField);
                    il.Emit(OpCodes.Ret);
                    zrpc.Methods.Add(cctor);
                }
                else
                {
                    var il = cctor.Body.GetILProcessor();
                    var first = cctor.Body.Instructions[0];
                    il.InsertBefore(first, il.Create(OpCodes.Ldc_I8, 0L));
                    il.InsertBefore(first, il.Create(OpCodes.Stsfld, everybodyField));
                }

                Log.LogInfo(" - Converted ZRoutedRpc.Everybody from const to runtime static field [Fixes ServerSync / Azumatt mods in memory]");
            }

            // 1. Character.Message(MessageType type, string msg, int amount, Sprite icon) -> calls Message(type, msg, amount, icon, false)
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

            // 2. SEMan.AddStatusEffect(StatusEffect, bool, int, float) -> calls (StatusEffect, bool, int, float, (short)0)
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

            // 3. Inventory.IsTeleportable() -> calls IsTeleportable(false)
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

                // 3b. Inventory.AddItem(ItemData, int, int, int) -> calls AddItem(item, amount, x, y, false)
                var targetAdd4 = inventory.Methods.FirstOrDefault(m => m.Name == "AddItem" && m.Parameters.Count == 5 && m.Parameters[0].ParameterType.Name == "ItemData");
                var existingAdd4 = inventory.Methods.FirstOrDefault(m => m.Name == "AddItem" && m.Parameters.Count == 4 && m.Parameters[0].ParameterType.Name == "ItemData");
                if (targetAdd4 != null && existingAdd4 == null)
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

                // 3c. Inventory.AddItem(string, int, int, int, long, string, Vector2i, bool) -> calls 10-param overload
                // Fixes Smoothbrain's ItemDataManager shared library (Cooking, Blacksmithing, ExtraSlots, EpicLoot)
                var targetAdd10 = inventory.Methods.FirstOrDefault(m => m.Name == "AddItem" && m.Parameters.Count == 10 && m.Parameters[0].ParameterType.Name == "String");
                var existingAdd8 = inventory.Methods.FirstOrDefault(m => m.Name == "AddItem" && m.Parameters.Count == 8 && m.Parameters[0].ParameterType.Name == "String" && m.Parameters[6].ParameterType.Name == "Vector2i");
                if (targetAdd10 != null && existingAdd8 == null)
                {
                    var bridge = new MethodDefinition("AddItem", MethodAttributes.Public | MethodAttributes.HideBySig, targetAdd10.ReturnType);
                    for (int i = 0; i < 7; i++)
                        bridge.Parameters.Add(new ParameterDefinition(targetAdd10.Parameters[i].Name, ParameterAttributes.None, targetAdd10.Parameters[i].ParameterType));
                    bridge.Parameters.Add(new ParameterDefinition("worldGen", ParameterAttributes.None, mainModule.TypeSystem.Boolean));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0); // this
                    for (int i = 0; i < 7; i++)
                        il.Emit(OpCodes.Ldarg, bridge.Parameters[i]);
                    il.Emit(OpCodes.Ldarg, bridge.Parameters[7]); // cheated = worldGen
                    il.Emit(OpCodes.Ldc_I4_0); // pickedUp = false
                    il.Emit(OpCodes.Ldc_I4_0); // dropIfFullInv = false
                    il.Emit(OpCodes.Callvirt, targetAdd10);
                    il.Emit(OpCodes.Ret);

                    inventory.Methods.Add(bridge);
                    Log.LogInfo(" - Injected Inventory.AddItem(8 params) [Fixes ItemDataManager / Cooking / EpicLoot / ExtraSlots]");
                }
            }

            // 4. EffectList.Create(Vector3, Quaternion, Transform, float, int) -> calls Create(..., ZDOID.None)
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

            // 5. Terminal.ConsoleCommand..ctor (12 params) -> calls 13-param constructor with hideBehindDevCommands = false
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

            // 6. ItemDrop.ItemData.GetTooltip (5 params) -> calls 6-param overload with appending = false
            // Fixes Blacksmithing & Cooking mods which patch GetTooltip(ItemData, int, bool, float, int)
            var itemDataType = mainModule.GetType("ItemDrop/ItemData");
            if (itemDataType != null)
            {
                var target6 = itemDataType.Methods.FirstOrDefault(m => m.Name == "GetTooltip" && m.Parameters.Count == 6 && m.IsStatic);
                var existing5 = itemDataType.Methods.FirstOrDefault(m => m.Name == "GetTooltip" && m.Parameters.Count == 5 && m.IsStatic);
                if (target6 != null && existing5 == null)
                {
                    var bridge = new MethodDefinition("GetTooltip", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, target6.ReturnType);
                    for (int i = 0; i < 5; i++)
                        bridge.Parameters.Add(new ParameterDefinition(target6.Parameters[i].Name, ParameterAttributes.None, target6.Parameters[i].ParameterType));

                    var il = bridge.Body.GetILProcessor();
                    for (int i = 0; i < 5; i++)
                        il.Emit(OpCodes.Ldarg, bridge.Parameters[i]);
                    il.Emit(OpCodes.Ldc_I4_0); // appending = false
                    il.Emit(OpCodes.Call, target6);
                    il.Emit(OpCodes.Ret);

                    itemDataType.Methods.Add(bridge);
                    Log.LogInfo(" - Injected ItemDrop.ItemData.GetTooltip(5 params) [Fixes Blacksmithing & Cooking]");
                }
            }

            // 7. InventoryGrid.Element: create nested subclass inheriting from InventoryElement
            // In Valheim 1.0.7, InventoryGrid.Element was refactored to top-level InventoryElement.
            // This bridge restores the nested type so mods referencing InventoryGrid/Element don't fail reflection.
            var inventoryGrid = mainModule.GetType("InventoryGrid");
            var inventoryElement = mainModule.GetType("InventoryElement");
            if (inventoryGrid != null && inventoryElement != null)
            {
                var existingElement = inventoryGrid.NestedTypes.FirstOrDefault(t => t.Name == "Element");
                if (existingElement == null)
                {
                    var nestedElement = new TypeDefinition("", "Element",
                        TypeAttributes.NestedPublic | TypeAttributes.Class | TypeAttributes.BeforeFieldInit,
                        inventoryElement);

                    var vector2iType = mainModule.GetType("Vector2i");
                    if (vector2iType != null)
                    {
                        nestedElement.Fields.Add(new FieldDefinition("m_pos", FieldAttributes.Public, vector2iType));
                    }

                    var baseCtor = inventoryElement.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0);
                    if (baseCtor != null)
                    {
                        var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, mainModule.TypeSystem.Void);
                        var il = ctor.Body.GetILProcessor();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, baseCtor);
                        il.Emit(OpCodes.Ret);
                        nestedElement.Methods.Add(ctor);
                    }

                    inventoryGrid.NestedTypes.Add(nestedElement);
                    Log.LogInfo(" - Injected InventoryGrid.Element subclassing InventoryElement");
                }

                // 8. InventoryGrid.OnRightClick(UIInputHandler element) -> calls OnRightDown
                // In Valheim 1.0.7, OnRightClick was renamed to OnRightDown.
                // Fixes AzuAutoStore's InventoryGridButtonHandlingPatches
                var uiInputHandlerType = mainModule.GetType("UIInputHandler");
                var existingRight = inventoryGrid.Methods.FirstOrDefault(m => m.Name == "OnRightClick" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "UIInputHandler");
                var targetDown = inventoryGrid.Methods.FirstOrDefault(m => m.Name == "OnRightDown" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "UIInputHandler");
                if (existingRight == null && targetDown != null && uiInputHandlerType != null)
                {
                    var bridge = new MethodDefinition("OnRightClick", MethodAttributes.Public | MethodAttributes.HideBySig, mainModule.TypeSystem.Void);
                    bridge.Parameters.Add(new ParameterDefinition("element", ParameterAttributes.None, uiInputHandlerType));

                    var il = bridge.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldarg_1); // element
                    il.Emit(OpCodes.Call, targetDown);
                    il.Emit(OpCodes.Ret);

                    inventoryGrid.Methods.Add(bridge);

                    // Redirect UpdateGui's ldftn OnRightDown to OnRightClick so Harmony patches intercept the right-click
                    var updateGui = inventoryGrid.Methods.FirstOrDefault(m => m.Name == "UpdateGui");
                    if (updateGui != null && updateGui.HasBody)
                    {
                        foreach (var inst in updateGui.Body.Instructions)
                        {
                            if (inst.OpCode == OpCodes.Ldftn && inst.Operand is MethodReference mr && mr.Name == "OnRightDown")
                            {
                                inst.Operand = bridge;
                            }
                        }
                    }

                    Log.LogInfo(" - Injected InventoryGrid.OnRightClick(UIInputHandler) [Fixes AzuAutoStore Favoriting]");
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

                string[] allPluginDlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);

                // 1. Clean up / restore previously rewritten Azumatt DLLs if backups exist.
                // Since ZRoutedRpc.Everybody is now solved completely in memory in assembly_valheim.dll,
                // we restore original files so there is zero risk of metadata corruption.
                foreach (string dllPath in allPluginDlls)
                {
                    string backupPath = dllPath + ".orig.bak";
                    if (File.Exists(backupPath))
                    {
                        string fileName = Path.GetFileName(dllPath);
                        if (fileName.StartsWith("Azu", StringComparison.OrdinalIgnoreCase) ||
                            fileName.Equals("MistBeGone.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                File.Copy(backupPath, dllPath, true);
                                Log.LogInfo($"Restored original {fileName} from backup (memory patch handles ZRoutedRpc.Everybody).");
                            }
                            catch (Exception ex)
                            {
                                Log.LogWarning($"Could not restore backup for {fileName}: {ex.Message}");
                            }
                        }
                    }
                }

                // 2. Scan for plugins that call the old 2-argument VisEquipment.AttachArmor (e.g. MagicPlugin).
                // We patch their call sites to pass quality = 0 to the 3-arg method,
                // preventing AmbiguousMatchException for mods like EpicLoot.
                foreach (string dllPath in allPluginDlls)
                {
                    string fileName = Path.GetFileName(dllPath);
                    if (fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("AzuModsValheim1Compat.dll", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("Backpacks.dll", StringComparison.OrdinalIgnoreCase)) // Backpacks is handled by ADARC patcher
                    {
                        continue;
                    }

                    try
                    {
                        PatchAttachArmorCalls(dllPath);
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Error checking AttachArmor calls in {fileName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Critical error during plugin compatibility patch execution: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void PatchAttachArmorCalls(string dllPath)
        {
            string fileName = Path.GetFileName(dllPath);

            // Fast check: scan file bytes for "AttachArmor"
            byte[] dllBytes = File.ReadAllBytes(dllPath);
            string asText = System.Text.Encoding.ASCII.GetString(dllBytes);
            if (!asText.Contains("AttachArmor"))
                return;

            using (var stream = new MemoryStream(dllBytes))
            using (var assembly = AssemblyDefinition.ReadAssembly(stream))
            {
                int patchedCalls = 0;

                foreach (var type in assembly.MainModule.Types)
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody || method.Body.Instructions == null)
                            continue;

                        var il = method.Body.GetILProcessor();
                        var instructions = method.Body.Instructions.ToArray();

                        foreach (var inst in instructions)
                        {
                            if (inst.OpCode == OpCodes.Callvirt &&
                                inst.Operand is MethodReference methodRef &&
                                methodRef.DeclaringType != null &&
                                methodRef.DeclaringType.Name == "VisEquipment" &&
                                methodRef.Name == "AttachArmor" &&
                                methodRef.Parameters.Count == 2)
                            {
                                // Construct new 3-parameter reference: AttachArmor(int, int, int)
                                var newRef = new MethodReference("AttachArmor", methodRef.ReturnType, methodRef.DeclaringType)
                                {
                                    HasThis = methodRef.HasThis,
                                    ExplicitThis = methodRef.ExplicitThis,
                                    CallingConvention = methodRef.CallingConvention
                                };
                                foreach (var p in methodRef.Parameters)
                                    newRef.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
                                newRef.Parameters.Add(new ParameterDefinition("quality", ParameterAttributes.None, assembly.MainModule.TypeSystem.Int32));

                                il.InsertBefore(inst, il.Create(OpCodes.Ldc_I4_0));
                                inst.Operand = newRef;
                                patchedCalls++;
                            }
                        }
                    }
                }

                if (patchedCalls > 0)
                {
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

                    Log.LogInfo($"[Compatibility Fix] Patched {fileName}: updated {patchedCalls} VisEquipment.AttachArmor call(s) to 3-parameter version with quality=0.");
                }
            }
        }
    }
}
