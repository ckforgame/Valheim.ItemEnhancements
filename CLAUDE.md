# CLAUDE.md - Valheim.ItemEnhancements Assistant Guide

This repository contains the source code for **Valheim Item Enhancements**, an MMORPG refinement mod (+1 to +20) for Valheim.

## Essential Reference
- **Master Technical Specification**: [AGENTS.md](file:///D:/GameCustoms/ValheimCustoms/Valheim.ItemEnhancements/AGENTS.md) contains the full architecture, lifecycle sequence diagrams, Harmony patch inventory, Ability ID catalog, and historical bug documentation.
- **End-User Documentation**: [README.md](file:///D:/GameCustoms/ValheimCustoms/Valheim.ItemEnhancements/README.md) contains gameplay and configuration guides in English and Thai.

## Common Build & Verification Commands
- **Build Release**: `dotnet build -c Release`
- **Build Debug**: `dotnet build -c Debug`
- Target Framework: `.NETFramework,Version=v4.8` (`net48`), C# 10.0
- Build outputs automatically deploy to Steam and r2modman BepInEx plugin folders via MSBuild post-build targets.

## Critical Architectural Invariants
1. **Save Safety**: Data is stored exclusively in `m_customData["ValheimEnhancement_Level"]` and `m_customData["ValheimEnhancement_Crafter"]`. Never alter vanilla file formats.
2. **Template Prefab Guard**: Any prefab cloning must set `ScrollItemManager.IsCloningCustomPrefab = true` to skip `Awake()` on `ZNetView` and `ItemDrop`.
3. **Allocation-Free Hot Loops**: Query `PlayerBonusCache` and `YamlConfigManager.GetCumulativeAbilityValue()`; never allocate memory in per-frame hooks.
4. **Safe Overrides**: Support user balance tweaks via `*.override.yml` files which are merged over base YAML files at startup and during live hot-reload (`FileSystemWatcher`).
