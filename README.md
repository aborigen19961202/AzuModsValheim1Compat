# Valheim 1.0.7 Compatibility Patcher (Azumatt, Smoothbrain & Legacy Mods)
### *AzuModsValheim1Compat вЂ” Expanded Universal Compatibility Patch*

**AzuModsValheim1Compat** is a comprehensive BepInEx Preloader compatibility patch for **Valheim 1.0.7** (Unity 6 update).

> [!NOTE]
> **Expanded Beyond Original Scope**: What initially began as a simple fix for Azumatt's `ZRoutedRpc.Everybody` issue has expanded into a full-scale compatibility bridge for **Smoothbrain mods** (`Blacksmithing`, `Cooking`, `ExtraSlots`), **MagicPlugin**, **ItemDataManager**, and dozens of legacy Valheim 0.218 APIs. It resolves game-breaking crashes during startup, character selection, character creation, saving, and gameplay.

> [!IMPORTANT]
> **Temporary Compatibility Patch**: This is an unofficial community fix intended to keep your favorite mods functional until the original authors release official updates for Valheim 1.0.7. Once official updates are published on Thunderstore, you can safely uninstall this patch.

---

## What This Patch Does & What Was Fixed

### 1. Azumatt Mods & ServerSync
- **In-Memory `ZRoutedRpc.Everybody` Fix**: Converts the compile-time `const` back to an active runtime static field in memory. Fixes `MissingFieldException` across all Azumatt mods and `ServerSync` without altering DLL files on disk.
- **`Inventory.Load` Ambiguity**: Renames the unused Valheim 1.0.7 `Load(ZPackage, bool)` overload in memory, resolving `AmbiguousMatchException` in `AzuAutoStore`'s `InventorySelectSameItemAfterLoad`.
- **`InventoryGrid.OnRightClick`**: Bridges `OnRightClick(UIInputHandler)` to 1.0.7's `OnRightDown` and redirects `UpdateGui`, fixing `AzuAutoStore` Favoriting and button handling.
- **`AzuCraftyBoxes` EpicLoot Interop**: Fixes `TypeLoadException` referencing the old split `EpicLoot-UnityLib` assembly.

### 2. Smoothbrain Mods (Blacksmithing, Cooking, ExtraSlots)
- **Character Creation & Save/Load Crash Fix (`PlayerProfile.m_itemCraftStats`)**: Valheim 1.0.7 removed `m_itemCraftStats`. The patch injects this field into `PlayerProfile` in memory and auto-initializes it, completely fixing the `MissingFieldException` when creating, selecting, or saving characters.
- **Transpiler & Character Save Unblocking (`Inventory.AddItem` + `RemoveLogging`)**: Provides the expected `ZLog.Log` placeholder inside the 4-parameter `Inventory.AddItem` bridge. Outdated transpilers in `AzuAutoStore` and Smoothbrain's `ItemDataManager` now succeed cleanly without throwing `ArgumentOutOfRangeException` on `CodeMatcher.SetInstruction`, unblocking `ItemDataManager.ItemInfo` and allowing `Inventory.Save` / `OnNewCharacterDone` to succeed.
- **`ItemDrop.ItemData.GetTooltip`**: Restores the 5-parameter static tooltip method in memory, preventing crashes in `Blacksmithing` and `Cooking`.
- **`Inventory.AddItem` (8 parameters)**: Restores the legacy 8-parameter `AddItem` overload used by `ItemDataManager`.
- **`Blacksmithing.ApplyTranspilerToAll`**: Optimizes hook scanning to target vanilla `InventoryGui` methods cleanly under Unity 6.

### 3. MagicPlugin
- **`VisEquipment.AttachArmor` Call-Site Upgrading**: Automatically upgrades legacy 2-parameter `AttachArmor` calls in `MagicPlugin.dll` to the 1.0.7 3-parameter signature (`quality=0`) without colliding with `EpicLoot` Harmony patches.

### 4. General Valheim 1.0.7 In-Memory API Bridges
Restores breaking method signatures changed by Iron Gate so older mods continue to function seamlessly:
- `Character.Message(MessageType, string, int, Sprite)` (4-param overload)
- `SEMan.AddStatusEffect` (both StatusEffect and nameHash 4-param overloads)
- `Inventory.IsTeleportable()` (0-param overload)
- `EffectList.Create(Vector3, Quaternion, Transform, float, int)` (5-param overload)
- `Terminal.ConsoleCommand..ctor` (12-param constructor)
- `InventoryGrid.Element` (subclasses new `InventoryElement`)

---

## Supported & Verified Mods

| Mod | Author | Status | Notes |
| :--- | :--- | :--- | :--- |
| **AzuCraftyBoxes** | Azumatt | вњ… Working | Craft from nearby containers |
| **AzuAutoStore** | Azumatt | вњ… Working | Auto-deposit, favoriting & sorting |
| **AzuExtendedPlayerInventory** | Azumatt | вњ… Working | Hotbar & quick slots |
| **AzuAntiArthriticCrafting** | Azumatt | вњ… Working | Hold-to-craft |
| **AzuClock** | Azumatt | вњ… Working | In-game time display |
| **FactionAssigner** | Azumatt | вњ… Working | NPC faction configuration |
| **MistBeGone** | Azumatt | вњ… Working | Mistlands mist removal |
| **PetPantry** | Azumatt | вњ… Working | Pet feeding automation |
| **Recycle_N_Reclaim** | Azumatt | вњ… Working | Item recycling & reclamation |
| **CurrencyPocket** | Azumatt | вњ… Working | Dedicated coin storage |
| **SaveCrossbowState** | Azumatt | вњ… Working | Crossbow reload preservation |
| **TrueInstantLootDrop** | Azumatt | вњ… Working | Instant loot spawning |
| **Blacksmithing** | Smoothbrain | вњ… Working | Crafting stats, tooltips & leveling |
| **Cooking** | Smoothbrain | вњ… Working | Buffs, tooltips & cooking skill |
| **Extra Slots** | Smoothbrain | вњ… Working | Custom equipment inventory slots |
| **MagicPlugin** | Marlthon | вњ… Working | Magic weapons, armor & spells |
| **Epic Loot** | RandyKnapp | вњ… Verified | Interoperability verified |
| **Jotunn (JVL)** | ValheimModding | вњ… Verified | Interoperability verified |

---

## Installation

### Via r2modman / Thunderstore Mod Manager (Recommended)
1. Search for **AzuModsValheim1Compat** (or install via Thunderstore).
2. Start the game through the mod manager as usual (**"Start Modded"**).
3. The preloader will automatically apply all compatibility fixes.

### Manual Installation
1. Extract `AzuModsValheim1Compat.dll` into your `BepInEx/patchers/AzuModsValheim1Compat/` folder.
2. Launch Valheim via Steam or BepInEx.

---

## РЈРєСЂР°С—РЅСЃСЊРєР° (РћРїРёСЃ СѓРєСЂР°С—РЅСЃСЊРєРѕСЋ)

**Valheim 1.0.7 Compatibility Patcher (AzuModsValheim1Compat)** вЂ” С†Рµ СѓРЅС–РІРµСЂСЃР°Р»СЊРЅРёР№ РєРѕРјРїР»РµРєСЃРЅРёР№ РїСЂРµР»РѕР°РґРµСЂ-РїР°С‚С‡ СЃСѓРјС–СЃРЅРѕСЃС‚С– BepInEx РґР»СЏ **Valheim 1.0.7** (Unity 6).

> [!NOTE]
> **Р РѕР·С€РёСЂРµРЅРµ РѕРЅРѕРІР»РµРЅРЅСЏ:**  
> РЎРїРѕС‡Р°С‚РєСѓ РїСЂРѕС”РєС‚ СЃС‚РІРѕСЂСЋРІР°РІСЃСЏ СЏРє С€РІРёРґРєРµ РІРёРїСЂР°РІР»РµРЅРЅСЏ РєСЂРёС‚РёС‡РЅРѕС— РїРѕРјРёР»РєРё `ZRoutedRpc.Everybody` Сѓ РјРѕРґР°С… Azumatt. РџСЂРѕС‚Рµ РїС–Рґ С‡Р°СЃ С‚РµСЃС‚СѓРІР°РЅРЅСЏ Р·Р±С–СЂРѕРє РїР°С‚С‡ Р±СѓР»Рѕ **Р·РЅР°С‡РЅРѕ СЂРѕР·С€РёСЂРµРЅРѕ**, С– С‚РµРїРµСЂ РІС–РЅ РјС–СЃС‚РёС‚СЊ РїРѕРІРЅРёР№ СЃРїРµРєС‚СЂ РІРёРїСЂР°РІР»РµРЅСЊ РґР»СЏ РјРѕРґС–РІ РІС–Рґ **Smoothbrain** (`Blacksmithing`, `Cooking`, `ExtraSlots`), **MagicPlugin**, Р±С–Р±Р»С–РѕС‚РµРєРё `ItemDataManager`, Р° С‚Р°РєРѕР¶ РІРёРїСЂР°РІР»СЏС” Р·Р±РѕС— РїСЂРё СЃС‚РІРѕСЂРµРЅРЅС– С‚Р° Р·Р±РµСЂРµР¶РµРЅРЅС– РЅРѕРІРёС… РїРµСЂСЃРѕРЅР°Р¶С–РІ.

### Р©Рѕ РІРёРїСЂР°РІР»СЏС” РїР°С‚С‡:
1. **РњРѕРґРё Azumatt (`AzuCraftyBoxes`, `AzuAutoStore` С‚РѕС‰Рѕ):**
   - РЈСЃСѓРІР°С” `MissingFieldException: Field not found: .ZRoutedRpc.Everybody Due to: Using static instructions with literal field` Р±РµР·РїРѕСЃРµСЂРµРґРЅСЊРѕ РІ РѕРїРµСЂР°С‚РёРІРЅС–Р№ РїР°Рј'СЏС‚С– (Р±РµР· РјРѕРґРёС„С–РєР°С†С–С— С„Р°Р№Р»С–РІ РЅР° РґРёСЃРєСѓ).
   - Р’РёРїСЂР°РІР»СЏС” `AmbiguousMatchException` РґР»СЏ `Inventory.Load` С‚Р° РІС–РґРЅРѕРІР»СЋС” СЂРѕР±РѕС‚Сѓ РѕР±СЂР°РЅРѕРіРѕ (`Favoriting`) РІ `AzuAutoStore`.
   - Р’РёРїСЂР°РІР»СЏС” РєРѕРЅС„Р»С–РєС‚ `AzuCraftyBoxes` С–Р· СЃСѓС‡Р°СЃРЅРѕСЋ Р·Р±С–СЂРєРѕСЋ `EpicLoot`.
2. **РњРѕРґРё Smoothbrain (`Blacksmithing`, `Cooking`, `ExtraSlots`):**
   - **Р’РёРїСЂР°РІР»РµРЅРѕ СЃС‚РІРѕСЂРµРЅРЅСЏ С‚Р° Р·Р±РµСЂРµР¶РµРЅРЅСЏ РїРµСЂСЃРѕРЅР°Р¶С–РІ**: С–РЅР¶РµРєС‚СѓС” РІС–РґСЃСѓС‚РЅС” Сѓ 1.0.7 РїРѕР»Рµ `PlayerProfile.m_itemCraftStats`, РїСЂРёРїРёРЅСЏСЋС‡Рё РІРёР»СЊРѕС‚Рё РјРµРЅСЋ С‚Р° Р±Р»РѕРєСѓРІР°РЅРЅСЏ РєРЅРѕРїРєРё В«Р—Р°СЃС‚РѕСЃСѓРІР°С‚РёВ» РїСЂРё СЃС‚РІРѕСЂРµРЅРЅС– РїРµСЂСЃРѕРЅР°Р¶Р°.
   - **Р’РёРїСЂР°РІР»РµРЅРѕ СЂРѕР±РѕС‚Сѓ `ItemDataManager`**: Сѓ РјС–СЃС‚ `Inventory.AddItem` РґРѕРґР°РЅРѕ РЅРµРѕР±С…С–РґРЅС– С–РЅСЃС‚СЂСѓРєС†С–С—, Р·Р°РІРґСЏРєРё С‡РѕРјСѓ СЃС‚Р°СЂС– С‚СЂР°РЅСЃРїС–Р»РµСЂРё `RemoveLogging` РєРѕСЂРµРєС‚РЅРѕ СЃРїСЂР°С†СЊРѕРІСѓСЋС‚СЊ Р±РµР· РїРѕРјРёР»РєРё `ArgumentOutOfRangeException`.
   - Р’С–РґРЅРѕРІР»РµРЅРѕ СЃС‚Р°С‚РёС‡РЅРёР№ РјРµС‚РѕРґ РїС–РґРєР°Р·РѕРє `ItemDrop.ItemData.GetTooltip(5 РїР°СЂР°РјРµС‚СЂС–РІ)`.
3. **MagicPlugin:**
   - РћРЅРѕРІР»РµРЅРѕ РІРёРєР»РёРєРё `VisEquipment.AttachArmor` РїС–Рґ РЅРѕРІРёР№ С„РѕСЂРјР°С‚ РїР°СЂР°РјРµС‚СЂС–РІ 1.0.7.
4. **РЈРЅС–РІРµСЂСЃР°Р»СЊРЅС– API-РјРѕСЃС‚Рё РґР»СЏ СЃС‚Р°СЂРёС… РјРѕРґС–РІ:**
   - Р’С–РґРЅРѕРІР»РµРЅРѕ РІР°РЅС–Р»СЊРЅС– РјРµС‚РѕРґРё, Р·РјС–РЅРµРЅС– Iron Gate Сѓ РІРµСЂСЃС–С— 1.0.7 (`Character.Message`, `SEMan.AddStatusEffect`, `Inventory.IsTeleportable`, `EffectList.Create`, `Terminal.ConsoleCommand` С‚РѕС‰Рѕ).

---

## Credits & License
- Developed for the Valheim modding community.
- MIT License.