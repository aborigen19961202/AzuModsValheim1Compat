using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AzuModsValheim1Compat
{
    public static class Patcher
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource("AzuModsValheim1Compat");
        private static bool _hasExecuted = false;

        private static readonly HashSet<string> TargetFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        // BepInEx Preloader discovery hook
        public static IEnumerable<string> TargetDLLs
        {
            get
            {
                ExecuteCompatibilityPatch();
                return Array.Empty<string>();
            }
        }

        // BepInEx Preloader initializer hook
        public static void Initialize()
        {
            ExecuteCompatibilityPatch();
        }

        // BepInEx Preloader patch hook (unused since we patch plugin assemblies directly)
        public static void Patch(AssemblyDefinition assembly)
        {
        }

        public static void ExecuteCompatibilityPatch()
        {
            if (_hasExecuted)
                return;

            _hasExecuted = true;

            try
            {
                string pluginsPath = Paths.PluginPath;
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

                    // Skip backup, temporary, or patcher files
                    if (fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("AzuModsValheim1Compat.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Check if DLL is in our known target list or has 'Azu' in its name
                    bool isTarget = TargetFileNames.Contains(fileName) ||
                                    fileName.StartsWith("Azu", StringComparison.OrdinalIgnoreCase);

                    if (!isTarget)
                        continue;

                    try
                    {
                        int replacements = PatchAssemblyFile(dllPath);
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
                    Log.LogInfo($"Valheim 1.0.7 compatibility patch applied successfully: {patchedAssembliesCount} assembly(ies) updated ({totalReplacementsCount} ZRoutedRpc.Everybody instructions fixed).");
                }
                else
                {
                    Log.LogInfo("All Azumatt mods are already compatible or no unpatched mods found.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Critical error during compatibility patch execution: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static int PatchAssemblyFile(string dllPath)
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
                            // Detect: ldsfld int64 ZRoutedRpc::Everybody
                            if (instruction.OpCode == OpCodes.Ldsfld &&
                                instruction.Operand is FieldReference fieldRef &&
                                fieldRef.DeclaringType != null &&
                                fieldRef.DeclaringType.Name == "ZRoutedRpc" &&
                                fieldRef.Name == "Everybody")
                            {
                                // In Valheim 1.0.7, ZRoutedRpc.Everybody became a const (literal) 0L.
                                // Calling ldsfld against a literal throws MissingFieldException.
                                // We replace the instruction with ldc.i8 0L.
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

                // Create backup if not already present
                string backupPath = dllPath + ".orig.bak";
                if (!File.Exists(backupPath))
                {
                    File.Copy(dllPath, backupPath, false);
                }

                // Write modified assembly to a temporary file
                string tempPath = dllPath + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                assembly.Write(tempPath);

                // Safely overwrite target DLL
                File.Copy(tempPath, dllPath, true);
                File.Delete(tempPath);

                Log.LogInfo($"[Compatibility Fix] Patched {fileName}: replaced {replacedInstructions} ZRoutedRpc.Everybody instruction(s). Backup: {Path.GetFileName(backupPath)}");
                return replacedInstructions;
            }
        }
    }
}
