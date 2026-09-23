# Valheim Item Enhancements (MMORPG Refinement Mod)

A comprehensive MMORPG-style item enhancement system (**+1 to +20**) for **Valheim**. Upgrade your weapons, shields, armor, and utility accessories with custom progression, particle effects, anvil soundscapes, and modular YAML configurations.

---

## 🌟 Key Features

1. **Full Equipment Enhancement System (+1 to +20)**:
   - **Supports all equipment in the game**: Swords, axes, maces, clubs, knives, spears, polearms (atgeirs), bows, crossbows, magic staves, all shields, all armor pieces (helmets, chest pieces, leg greaves, cloaks), and utility accessories (e.g. Megingjord, Wishbone).
   - **100% Vanilla Save-Safe**: Enhancement levels and data are stored directly inside Valheim's native `m_customData` dictionary. Seamlessly works in single-player, dedicated servers, and multiplayer environments without corrupting character or world saves.

2. **Enhancement Scrolls & Free Refinement by Default**:
   - **No Coin Fees**: Free of coin costs by default (`RequireCoins = false`).
   - **4 Tiers of Enhancement Scrolls**: Slay monsters across different biomes to acquire enhancement scrolls tailored to each refinement tier.
   - **Scroll Fusion at Workbench**: Combine 3 lower-tier scrolls at any standard workbench to craft 1 higher-tier scroll.

3. **Deep Integration with Vanilla Stats & Abilities**:
   - **All Damage Types**: Scales all native weapon damage components (Slash, Pierce, Blunt, Chop, Pickaxe, Fire, Frost, Lightning, Poison, Spirit) by +5% per level (up to +50% at +20).
   - **Armor Plating**: Provides both flat armor (+1.5 per level) and percentage armor bonuses (+2% per level), up to +10 flat armor and +8% at +20.
   - **Shield Mastery**: Boosts Shield Block Power, Deflection Force, and Timed Block (Parry Multiplier).
   - **Stamina & Eitr Conservation**:
     - Reduces weapon attack stamina cost (up to -10%).
     - Reduces magic staff eitr spellcasting cost (up to -4%).
     - Reduces shield blocking stamina cost (up to -4%).
     - Reduces dodge roll stamina, sprinting stamina, and jump stamina costs.
   - **Movement Speed Penalty Relief**: Gradually negates the heavy movement speed penalties (-5% to -20%) of heavy armor and tower shields, granting positive movement speed bonuses at high levels.
   - **Durability & Weight Reduction**: Increases maximum durability (+5% per tier up to +30%) and reduces equipment carry weight (up to -12%).
   - **Specialized Perks**:
     - Knives: Increases backstab / sneak attack damage multiplier (up to +0.20x).
     - Mage Sets: Increases Eitr regeneration rate (up to +20%).
     - Utility Belts (e.g., Megingjord): Increases maximum carry weight limit (up to +100).

4. **Modular YAML Configuration System & Safe Overrides**:
   - Configuration files are neatly organized inside `BepInEx/config/ckforgame.ItemEnhancements/`:
     - `AbilityReference.txt`: Complete documentation for all 25 supported Ability IDs with syntax examples.
     - `Item.yml`: Weapons, durability, weight reduction, accessories, and cumulative level rewards (`Levels: 1-20`).
     - `Armor.yml`: Armor, shields, mobility stamina, movement speed, and eitr regen.
     - `Cheat.yml`: Optional overpowered abilities (MaxHealth, MaxStamina, MaxEitr, HealthRegen, StaminaRegen, HealingMultiplier).
     - `Success.yml`: Success rates per level (+1 to +20), safe levels, downgrade/break rules, coin costs, and crafting station requirements.
     - `Drops.yml`: Granular drop rates per biome and scroll tier, elite monster multipliers, and world boss guarantees.
   - **Safe Overrides (`*.override.yml`)**: Prevent mod updates from overwriting your personal adjustments. Simply create an override file (e.g., `Item.override.yml`, `Drops.override.yml`) to define only the values you want to modify.
   - **Live Hot-Reload**: Automatically detects when `.yml` or `.override.yml` files are saved and immediately updates in-game stats without restarting the game.

5. **MMORPG Refinement GUI with Audio & Visual Feedback**:
   - Open via configurable hotkey **`F8`** or click the **`[⚡ Enhance]`** button located right beside the Repair button on any Crafting Station.
   - Select equipment from the dropdown menu or simply right-click/left-click any item in your inventory while the window is open.
   - Real-time stat progression comparison (+diff preview).
   - Clear success rate gauge and risk warnings.
   - Dynamic anvil striking sound effects, sparks particles, forging animation delay, and HUD center announcement banners.

6. **Tier-Colored Inventory Badges**:
   - `+1 to +3`: **Common** (`#4ade80`, Vibrant Green)
   - `+4 to +6`: **Refined** (`#22d3ee`, Cyan)
   - `+7 to +9`: **Rare** (`#3b82f6`, Royal Blue)
   - `+10 to +12`: **Epic** (`#a855f7`, Purple)
   - `+13 to +15`: **Legendary** (`#f59e0b`, Amber Gold)
   - `+16 to +19`: **Mythic** (`#ef4444`, Crimson Red)
   - `+20`: **Divine** (`#ffd700`, Divine Gold)

---

## 📜 Enhancement Scrolls by Tier

| Scroll Name | Target Levels | Primary Biomes & Monsters | Default Drop Rate |
|---|:---:|---|:---:|
| **Enhancement Scroll Tier 1 (Common)** | **+1 to +5** | Meadows & Black Forest (Boars, Greydwarfs, Skeletons) | **15.0%** |
| **Enhancement Scroll Tier 2 (Refined)** | **+6 to +10** | Swamp & Mountain (Draugr, Blobs, Wolves, Drakes, Trolls) | **10.0%** |
| **Enhancement Scroll Tier 3 (Rare)** | **+11 to +15** | Plains & Mistlands (Fulings, Lox, Seekers, Stone Golems) | **6.0%** |
| **Enhancement Scroll Tier 4 (Divine)** | **+16 to +20** | Ashlands, Gjall, and all World Bosses | **3.0%** (Bosses: 100%) |

> **Starred Monster Multiplier**: 1-star and 2-star creatures receive enhanced drop multipliers (+50% per star level).  
> **World Bosses**: Guarantee a 100% scroll drop (1 to 3 scrolls depending on the boss).  
> **Scroll Fusion**: Craft 1 higher-tier scroll from 3 lower-tier scrolls at a Workbench.

---

## ⚙️ Default Success Rates (+1 to +20)

| Target Level | Success Rate | Required Scroll | Coin Cost | Result on Failure |
|:---:|:---:|:---:|:---:|:---:|
| **+1** | 100% | Tier 1 Scroll x 1 | **Free** | Safe (No loss) |
| **+2** | 100% | Tier 1 Scroll x 1 | **Free** | Safe (No loss) |
| **+3** | 95% | Tier 1 Scroll x 1 | **Free** | Safe (No loss) |
| **+4** | 90% | Tier 1 Scroll x 1 | **Free** | Downgrades to +2 |
| **+5** | 80% | Tier 1 Scroll x 1 | **Free** | Downgrades to +3 |
| **+6** | 70% | Tier 2 Scroll x 1 | **Free** | Downgrades to +4 |
| **+7** | 60% | Tier 2 Scroll x 1 | **Free** | Downgrades to +5 |
| **+8** | 50% | Tier 2 Scroll x 1 | **Free** | Downgrades to +6 |
| **+9** | 40% | Tier 2 Scroll x 1 | **Free** | Downgrades to +7 |
| **+10** | 35% | Tier 2 Scroll x 1 | **Free** | Downgrades to +8 |
| **+11** | 30% | Tier 3 Scroll x 1 | **Free** | Downgrades to +9 |
| **+12** | 25% | Tier 3 Scroll x 1 | **Free** | Downgrades to +10 |
| **+13** | 20% | Tier 3 Scroll x 1 | **Free** | Downgrades to +11 |
| **+14** | 15% | Tier 3 Scroll x 1 | **Free** | Downgrades to +12 |
| **+15** | 12% | Tier 3 Scroll x 1 | **Free** | Downgrades to +13 |
| **+16** | 10% | Tier 4 Scroll x 1 | **Free** | Downgrades to +14 |
| **+17** | 8% | Tier 4 Scroll x 1 | **Free** | Downgrades to +15 |
| **+18** | 5% | Tier 4 Scroll x 1 | **Free** | Downgrades to +16 |
| **+19** | 3% | Tier 4 Scroll x 1 | **Free** | Downgrades to +17 |
| **+20** | 1% | Tier 4 Scroll x 1 | **Free** | Downgrades to +18 |

*(All rates, penalties, and safe levels can be customized in `Success.yml`)*

---

## ⚡ Optional Cheat Abilities

For players seeking a power-fantasy experience, the mod includes a dedicated set of **Cheat Abilities** that can be toggled ON or OFF in `Cheat.yml` (`EnableCheatAbilities: true / false`):

| Cheat Ability | Ability ID | Applicable Equipment | Example at +5 | Example at +20 |
|---|---|---|:---:|:---:|
| **Max Health Capacity** | `MaxHealth` | Armor, Shields, Accessories | **+10 HP** | **+100 HP** per piece |
| **Max Stamina Capacity** | `MaxStamina` | Armor, Weapons, Accessories | **+10 Stamina** | **+100 Stamina** per piece |
| **Max Eitr Capacity** | `MaxEitr` | Armor, Weapons, Accessories | **+10 Eitr** | **+100 Eitr** per piece |
| **Health Regen Multiplier** | `HealthRegen` | Armor, Shields, Accessories | **+10%** | **+100%** (+2x health regen per piece) |
| **Stamina Regen Multiplier** | `StaminaRegen` | Armor, Shields, Weapons, Accessories | **+10%** | **+100%** (+2x stamina regen per piece) |
| **Healing Received Multiplier** | `HealingMultiplier` | Armor, Shields, Accessories | **+15%** | **+100%** (+2x healing received) |

> **Note**:
> 1. Eitr regeneration (`EitrRegen`) is a standard ability already included in `Armor.yml` and does not require cheat mode.
> 2. `Cheat.yml` abilities are completely ignored when `EnableCheatAbilities: false`.

---

## 🎮 How to Play

1. Hunt monsters and bosses across Valheim to farm **Enhancement Scrolls**.
2. Stand near a Workbench or Forge (within 5.0 meters).
3. Press **`F8`** or click the **`[⚡ Enhance]`** button in the crafting station window.
4. Click an equipment from the dropdown menu, or right-click / left-click an item in your inventory.
5. Click **`⚡ ENHANCE ITEM ⚡`** to begin refinement!

---

## 📁 Configuration & Custom Overrides

YAML configuration files are stored at:  
`BepInEx/config/ckforgame.ItemEnhancements/`

```
BepInEx/config/ckforgame.ItemEnhancements/
├── AbilityReference.txt # Reference guide for all 25 supported Ability IDs
├── Success.yml          # Success rates +1 to +20, safe level, penalties, coin fees
├── Drops.yml            # Monster & boss scroll drop rates across all biomes
├── Item.yml             # Weapons, durability, weight reduction, and accessories
├── Armor.yml            # Armor, shields, stamina reduction, and eitr regen
└── Cheat.yml            # Optional cheat abilities (Health/Stamina/Eitr capacity and regens)
```

### 💡 How to Create Custom Overrides (`*.override.yml`)
1. Open the folder `BepInEx/config/ckforgame.ItemEnhancements/`.
2. Create a new file with `.override.` inserted before `.yml`:
   - `Item.override.yml`
   - `Armor.override.yml`
   - `Drops.override.yml`
   - `Success.override.yml`
   - `Cheat.override.yml`
3. Specify only the settings or levels you wish to modify:
   ```yaml
   Levels:
     1:
       - Ability: WeaponDamage
         Value: 10.0
     5:
       - Ability: CarryWeightBonus
         Value: 50.0
   ```
4. Save the file: The mod automatically hot-reloads the changes immediately without restarting the game.
5. **Future-Proof**: Whenever the mod is updated, base `.yml` files may be refreshed, but your `*.override.yml` files are never replaced!

---

## 🇹🇭 สรุปสำหรับผู้เล่นชาวไทย (Thai Summary)

ม็อดระบบตีบวกอุปกรณ์สไตล์ MMORPG (+1 ถึง +20) สำหรับเกม Valheim:
- รองรับอาวุธ ชุดเกราะ โล่ และเครื่องประดับทุกชนิด ปลอดภัยต่อไฟล์เซฟ 100%
- ปรับภาษาหลักของ UI, ข้อความแจ้งเตือน, ชื่อคัมภีร์ และ Config ทั้งหมดเป็น **ภาษาอังกฤษ** โดยหากเปิดเกมด้วยภาษาไทย ตัวเกมจะแสดงคำแปลภาษาไทยให้อัตโนมัติผ่านระบบ Localization ของ Valheim
- ฟาร์มม้วนคัมภีร์ตีบวก 4 ระดับ (Tier 1-4) ได้จากการกำจัดมอนสเตอร์และบอสในแต่ละไบโอม
- รวมคัมภีร์ระดับต่ำ 3 ใบเป็นระดับถัดไป 1 ใบได้ที่โต๊ะ Workbench
- เปิดหน้าต่างตีบวกได้ด้วยปุ่ม **`F8`** หรือกดปุ่ม **`[⚡ Enhance]`** ที่โต๊ะคราฟต์
- สามารถปรับแต่งอัตราสำเร็จ ค่าสเตตัส และการดรอปได้อย่างอิสระผ่านไฟล์ YAML ในโฟลเดอร์ `BepInEx/config/ckforgame.ItemEnhancements/`
