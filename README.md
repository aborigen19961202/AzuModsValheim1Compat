# AzuModsValheim1Compat

**AzuModsValheim1Compat** is a lightweight BepInEx Preloader compatibility patch for **Valheim 1.0.7** (Unity 6 update). It resolves the game-crashing `MissingFieldException: Field not found: .ZRoutedRpc.Everybody` in mods created by **Azumatt** (including `AzuCraftyBoxes`, `AzuAutoStore`, and others).

> [!IMPORTANT]
> **Temporary Compatibility Patch**: This is an unofficial community fix intended to keep your favorite mods functional until the original author, **Azumatt**, releases official updates. Once official updates are released on Thunderstore, you can simply uninstall this patch.

---

## The Problem Explained

With the release of **Valheim 1.0.7** (Unity 6 engine update):
- The game's internal `ZRoutedRpc.Everybody` field was changed from a `public static readonly long` field to a `public const long` (compile-time literal).
- Older versions of Azumatt's `ServerSync` library embedded inside his mods attempt to read this via the IL instruction `ldsfld ZRoutedRpc::Everybody`.
- Under .NET / Mono CLR runtime, calling `ldsfld` on a compile-time literal throws `MissingFieldException: Using static instructions with literal field` inside the static constructor (`.cctor()`), completely preventing the mods from loading.

## What This Patch Does

This patch runs in the **BepInEx Preloader** phase before the game engine starts plugins:
1. **In-Memory `ZRoutedRpc.Everybody` Restoration**: Converts `ZRoutedRpc.Everybody` from a compile-time `const` back into an active runtime static field in memory. **No mod DLLs need to be modified on disk!** ServerSync and all Azumatt mods run cleanly without metadata corruption.
2. **API Compatibility Bridges**: Injects backwards-compatibility bridge methods into `assembly_valheim.dll` for breaking changes introduced in Valheim 1.0.7:
    - `PlayerProfile.m_itemCraftStats`: Injects missing `m_itemCraftStats` dictionary and auto-initializes it in constructor (fixes **Blacksmithing** character select & save/load crash).
    - `Inventory.Load`: Renames redundant 2-parameter overload to eliminate `AmbiguousMatchException` in Harmony patches (fixes **AzuAutoStore** `InventorySelectSameItemAfterLoad`).
    - `InventoryGrid.OnRightClick`: Restores `OnRightClick(UIInputHandler)` method and redirects `UpdateGui` (fixes **AzuAutoStore** Favoriting and button handling).
    - `Inventory.AddItem`: Restores 8-parameter overload (fixes Smoothbrain's `ItemDataManager` shared library in **Cooking**, **Blacksmithing**, **ExtraSlots**, and **EpicLoot**).
    - `ItemDrop.ItemData.GetTooltip`: Restores 5-parameter static overload (fixes **Blacksmithing** & **Cooking** crashes).
    - `Character.Message`: Restores 4-parameter overload.
    - `SEMan.AddStatusEffect`: Restores 4-parameter overloads (both StatusEffect and int hash).
    - `Inventory.IsTeleportable`: Restores 0-parameter overload.
    - `Inventory.AddItem`: Restores 4-parameter overload.
    - `EffectList.Create`: Restores 5-parameter overload.
    - `Terminal.ConsoleCommand`: Restores 12-parameter constructor.
    - `InventoryGrid.Element`: Restores nested type definition referencing new `InventoryElement`.
3. **AzuCraftyBoxes EpicLoot Compatibility**: Automatically updates `EpicLootEnchantingUI` patches in `AzuCraftyBoxes.dll` to reference the merged `EpicLoot` assembly, resolving `TypeLoadException: Could not load type 'EpicLoot_UnityLib.InventoryManagement' from assembly 'EpicLoot-UnityLib'`.
4. **VisEquipment.AttachArmor Call-Site Upgrading**: Automatically upgrades legacy 2-parameter `AttachArmor` calls in plugins (such as `MagicPlugin`) to the 1.0.7 3-parameter signature without colliding with `EpicLoot` Harmony patches.
5. **Blacksmithing Transpiler Compatibility**: Automatically bypasses foreign hook scanning in `Blacksmithing.ApplyTranspilerToAll`, eliminating `InvalidOperationException: Sequence contains no matching element` in Unity 6 when playing with `AzuCraftyBoxes`.
6. **In-Memory `Inventory.AddItem` Transpiler Compatibility**: Provides the expected `ZLog.Log` placeholder inside the 4-parameter `Inventory.AddItem` bridge so outdated transpilers (like `AzuAutoStore` / MUC `RemoveLogging`) succeed in memory without throwing `ArgumentOutOfRangeException`. This completely unblocks `ItemDataManager` (**Cooking**, **Blacksmithing**, **ExtraSlots**) and ensures character creation and character saving (`Inventory.Save` / `OnNewCharacterDone`) work seamlessly without any exceptions.
7. Allows your modpack to load smoothly and cleanly!


---

## Supported Mods

- **AzuCraftyBoxes** (Craft from nearby chests)
- **AzuAutoStore** (Auto-deposit items into containers)
- **AzuExtendedPlayerInventory**
- **AzuAntiArthriticCrafting**
- **AzuClock**
- **FactionAssigner**
- **MistBeGone**
- **PetPantry**
- **Recycle_N_Reclaim**
- **CurrencyPocket**
- **SaveCrossbowState**
- **TrueInstantLootDrop**

---

## Installation

### Via r2modman / Thunderstore Mod Manager (Recommended)
1. Install this mod into your profile.
2. Start the game through the mod manager as usual.

### Manual Installation
1. Extract `AzuModsValheim1Compat.dll` into your `BepInEx/patchers/AzuModsValheim1Compat/` folder.
2. Launch Valheim via Steam or BepInEx.

---

## Українська (Опис українською)

**AzuModsValheim1Compat** — це прелоадер-патч сумісності BepInEx для **Valheim 1.0.7** (Unity 6). Він усуває критичну помилку:
`MissingFieldException: Field not found: .ZRoutedRpc.Everybody Due to: Using static instructions with literal field`
у модах від **Azumatt** (`AzuCraftyBoxes`, `AzuAutoStore`, `AzuExtendedPlayerInventory` та інших).

### Що робить патч:
- Під час запуску гри (Preloader) перевіряє плагіни в папці `BepInEx/plugins`.
- Знаходить збірки модів Azumatt та замінює застарілу IL-інструкцію звернення до `ZRoutedRpc.Everybody` на коректну константу `0L`.
- Створює резервну копію оригіналу (`.orig.bak`).
- Дозволяє користуватися модами без вильотів та зависань!

> [!NOTE]
> Це тимчасове рішення для спільноти. Як тільки Azumatt випустить офіційне оновлення своїх модів на Thunderstore, цей патч можна буде просто видалити.

---

## Credits & License
- Developed for the Valheim modding community.
- MIT License.
