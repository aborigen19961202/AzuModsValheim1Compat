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
1. Scans installed mods in your `BepInEx/plugins` folder.
2. Detects Azumatt mods (`AzuCraftyBoxes`, `AzuAutoStore`, `AzuExtendedPlayerInventory`, `AzuAntiArthriticCrafting`, `AzuClock`, `PetPantry`, `Recycle_N_Reclaim`, etc.).
3. Safely replaces the broken `ldsfld` instruction with `ldc.i8 0L` directly.
4. Preserves an automatic backup (`<ModName>.dll.orig.bak`).
5. Allows your mods to load cleanly without crashing!

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
