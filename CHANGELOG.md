# Changelog

All notable changes to **ItemEnhancements** will be documented in this file.

## [1.0.0] - 2026-09-25

### Added
- **Full Equipment Enhancement (+1 to +20)**: Upgrade weapons, armor, shields, and utility accessories.
- **100% Vanilla Save-Safe**: All refinement data and crafter names are saved directly into native `ItemDrop.ItemData.m_customData`.
- **4 Tiers of Custom Enhancement Scrolls**:
  - **Tier 1 (+1 to +5)**: Dropped by Meadows & Black Forest monsters.
  - **Tier 2 (+6 to +10)**: Dropped by Swamp & Mountain monsters.
  - **Tier 3 (+11 to +15)**: Dropped by Plains & Mistlands monsters.
  - **Tier 4 (+16 to +20)**: Dropped by Ashlands, Deep North monsters, and guaranteed from World Bosses.
- **Workbench Scroll Fusion**: Combine 3 lower-tier scrolls into 1 higher-tier scroll at any standard workbench.
- **Interactive Refinement GUI**:
  - Toggle with `F8` or click the `[⚡ Enhance]` button beside the Repair button at any Crafting Station.
  - Live stat comparison preview (+diff view), success rates, risk level warnings, and anvil strike audio/particle feedback.
  - Select items easily by clicking directly in your inventory while the enhancement window is open.
- **Tier-Colored Inventory Badges**: Dynamic colored `+X` rank badges rendered on item quality icons in inventory grids.
- **Modular YAML Configurations & Safe Overrides**:
  - Granular control over abilities, success rates, penalties, and drop rates across 5 modular YAML files in `BepInEx/config/ckforgame.ItemEnhancements/`.
  - Non-destructive `*.override.yml` support protects custom balance settings from being overwritten during mod updates.
  - Background live hot-reload automatically recalculates equipment stats upon saving files without restarting the game.