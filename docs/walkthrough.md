# Walkthrough: ระบบม้วนคัมภีร์ตีบวก (Enhancement Scrolls) และยกเลิกค่าธรรมเนียม

เราได้พัฒนาและปรับปรุงระบบการตีบวกของ Mod **Valheim Item Enhancements** ตามคำขอ:
1. **ยกเลิกค่าธรรมเนียมเหรียญทองทั้งหมด (ฟรีค่าธรรมเนียม 100%)**
2. **สร้างระบบไอเทมม้วนคัมภีร์ตีบวก (Enhancement Scrolls) 4 ระดับ (Tiers)**
3. **ระบบดรอปจากมอนสเตอร์ (Monster Drops) ตาม Biomes, ระดับดาว และการันตีบอสโลก พร้อม Config ปรับแต่งได้อย่างละเอียด**
4. **ระบบหลอมรวมคัมภีร์ (Scroll Fusion Recipes) ที่โต๊ะคราฟต์**

---

## 1. รายละเอียดการพัฒนา (What Was Developed)

### 1.1 การยกเลิกค่าธรรมเนียมเหรียญทอง (Free Fees)
- ปรับค่าเริ่มต้นใน [ModConfig.cs](../Configuration/ModConfig.cs):
  - `RequireCoins = false` (ปิดการเรียกเก็บเหรียญทองเป็นค่าเริ่มต้น)
  - `CoinsBaseCost = 0` และ `CoinsPerLevelIncrement = 0f`
- ปรับ [EnhancementGui.cs](../UI/EnhancementGui.cs) ให้แสดงสถานะ **"ค่าธรรมเนียม: ฟรี (ไม่มีค่าธรรมเนียมเหรียญ)"** เมื่อไม่ได้เปิดใช้งานเหรียญทอง

---

### 1.2 ระบบไอเทมม้วนคัมภีร์ 4 ระดับ (Scroll Items)
พัฒนา [ScrollItemManager.cs](../Core/ScrollItemManager.cs):
- **4 ระดับคัมภีร์**:
  - `ScrollEnhance_Tier1`: **ใบตีบวกระดับ 1 (พื้นฐาน)** สำหรับ **+1 ถึง +5**
  - `ScrollEnhance_Tier2`: **ใบตีบวกระดับ 2 (ขัดเกลา)** สำหรับ **+6 ถึง +10**
  - `ScrollEnhance_Tier3`: **ใบตีบวกระดับ 3 (ประณีต)** สำหรับ **+11 ถึง +15**
  - `ScrollEnhance_Tier4`: **ใบตีบวกระดับ 4 (เทวะ)** สำหรับ **+16 ถึง +20**
- **สไปรท์ไอคอน (Procedural 64x64 Textures)**:
  - สร้างสไปรท์ม้วนกระดาษสาโบราณ พร้อมไม้ม้วนหัวท้าย ริบบิ้น และตราประทับขี้ผึ้งสีตามเทียร์ (เขียว / ฟ้า / ม่วง / ส้มทอง)
  - ย้อมสีโมเดล 3D ที่ตกบนพื้นให้เรืองแสงตามสีประจำเทียร์
- **การลงทะเบียนในระบบเกม**:
  - เชื่อมโยงเข้าสู่ `ObjectDB` (ทั้ง `m_items` และ `m_itemByHash`)
  - เชื่อมโยงเข้าสู่ `ZNetScene` เพื่อให้มีฟิสิกส์ 3D ตกบนพื้นและซิงค์ Multiplayer ผ่าน Network View
  - ลงทะเบียนชื่อและคำอธิบายภาษาไทยและอังกฤษผ่าน `Localization`
- **ระบบหลอมรวม (Scroll Fusion)**:
  - ลงทะเบียนสูตรคราฟต์ที่โต๊ะ Workbench: 3x Tier 1 ➔ 1x Tier 2, 3x Tier 2 ➔ 1x Tier 3, 3x Tier 3 ➔ 1x Tier 4

---

### 1.3 ระบบมอนสเตอร์ดรอปและ Configuration (Drop System)
พัฒนา [ScrollDropManager.cs](../Core/ScrollDropManager.cs) และ [CharacterDropPatches.cs](../Patches/CharacterDropPatches.cs):
- สอดแทรกคัมภีร์เข้าสู่ `CharacterDrop.GenerateDropList` ของมอนสเตอร์
- **การแบ่ง Biomes**:
  - Meadows & Black Forest ➔ Tier 1 (โอกาส 15%)
  - Swamp & Mountain ➔ Tier 2 (โอกาส 10%)
  - Plains & Mistlands ➔ Tier 3 (โอกาส 6%)
  - Ashlands & Deep North ➔ Tier 4 (โอกาส 3%)
- **ตัวคูณระดับดาว**: มอนสเตอร์ 1-2 ดาว ได้รับโบนัสโอกาสดรอปคูณเพิ่ม (`StarLevelMultiplier = 1.5x` ต่อดาว)
- **บอสโลก**: บอสการันตีการดรอป 100% (`BossGuaranteedDrop = true`) ดรอปคัมภีร์ 1-3 ใบตามระดับของบอส

---

### 1.4 การอัปเกรดหน้าต่างตีบวก (UI Integration)
ปรับปรุง [EnhancementGui.cs](../UI/EnhancementGui.cs) และ [EnhancementManager.cs](../Core/EnhancementManager.cs):
- หน้าต่างแสดงชื่อคัมภีร์ประจำระดับ +X สีตามเทียร์ และจำนวนที่ต้องใช้
- แสดงจำนวนคัมภีร์ที่ผู้เล่นมีในช่องเก็บของแบบเรียลไทม์:
  - หากพอ: แสดงสีเขียว `<color=#4ade80>(มี: X ใบ)</color>`
  - หากไม่พอ: แสดงสีแดง `<color=#ef4444>(มี: 0 ใบ - ไม่เพียงพอ)</color>`
- ปุ่มกดตีบวกจะปิดการทำงานและแจ้งเตือนชัดเจนหากขาดคัมภีร์

### 1.5 การจัดการ Configuration และ Hotkey Safeguard
- สร้างไฟล์แม่แบบ [ckforgame.ItemEnhancements.cfg](../Configuration/ckforgame.ItemEnhancements.cfg) ตามรูปแบบ Mod GUID (`ckforgame.<ModName>`) พร้อมคำอธิบายและค่าเริ่มต้นตรงตามข้อตกลงล่าสุด
- อัปเดต `Valheim.ItemEnhancements.csproj` ให้ทำการคัดลอกไฟล์ `.cfg` อัตโนมัติไปยังโฟลเดอร์ Config ของ r2modman และ Steam ทุกครั้งที่บิลด์
- ปรับปรุง [Plugin.cs](../Plugin.cs) ตรวจสอบการเปิดใช้งาน Hotkey (`F8`) โดยไม่ให้ทำงานขณะพิมพ์ข้อความใน Chat, Console หรือตั้งชื่อใน UI กล่องข้อความต่าง ๆ

---

## 2. ผลการตรวจสอบและการคอมไพล์ (Verification Results)

### การทดสอบคอมไพล์ (Build Test)
```bash
dotnet build -c Release
```
**ผลลัพธ์**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:03.60
```
- ไฟล์ DLL บิลด์และถูกส่งไปติดตั้งอัตโนมัติที่:
  - `%APPDATA%\r2modmanPlus-local\Valheim\profiles\<Profile>\BepInEx\plugins\Valheim.ItemEnhancements`
  - `<ValheimPath>\BepInEx\plugins\Valheim.ItemEnhancements`

---

## 3. คู่มือการทดสอบในเกม (Gameplay Verification Guide)

1. **เปิดเกม Valheim** ผ่าน r2modman หรือ Steam
2. **ทดสอบคำสั่งเสกไอเทม (Console)**:
   - กด `F5` เปิดคอนโซล พิมพ์ `devcommands`
   - พิมพ์ `spawn ScrollEnhance_Tier1 5` เพื่อทดสอบเสกใบตีบวกระดับ 1
   - พิมพ์ `spawn ScrollEnhance_Tier2 5`, `spawn ScrollEnhance_Tier3 5`, `spawn ScrollEnhance_Tier4 5`
3. **ทดสอบหน้าต่างตีบวก**:
   - ไปที่โต๊ะคราฟต์ กด `F8` หรือกดปุ่ม `[⚡ ตีบวก]`
   - นำอุปกรณ์มาใส่
   - ตรวจสอบว่าระบบแสดง **"ค่าธรรมเนียม: ฟรี"** และแสดงชื่อใบตีบวกที่ต้องใช้พร้อมจำนวนในกระเป๋า
   - ลองทดสอบตีบวก: คัมภีร์จะถูกหัก 1 ใบต่อครั้ง และไม่มีการหักเหรียญทองใด ๆ
4. **ทดสอบฆ่ามอนสเตอร์**:
   - ฆ่ามอนสเตอร์ในทุ่งหญ้า/ป่าดำ เพื่อดูการดรอปใบตีบวกระดับ 1
   - ฆ่าบอสโลกเพื่อดูการันตีการดรอป 1-3 ใบ
5. **ทดสอบระบบ Cheat Abilities (โหมดโกง)**:
   - เปิดไฟล์ `ckforgame.ItemEnhancements.cfg` ปรับ `EnableCheatAbilities = true`
   - สวมใส่อาวุธ เกราะ โล่ หรือเครื่องประดับที่ตีบวกไว้
   - สังเกต Max HP, Max Stamina, Max Eitr และอัตราฟื้นฟู (Regen) รวมถึงการฮีลที่เพิ่มขึ้นมหาศาลแบบ Real-time
6. **ทดสอบการปรับแต่ง Config**:
   - สามารถเปิดไฟล์ `ValheimItemEnhancements.cfg` ปรับค่า `Tier1_DropChance` ถึง `Tier4_DropChance` ได้ทันที

---

## 4. รายละเอียดฟีเจอร์ Cheat Abilities (โหมดโกงเสริม)

- **การเปิด/ปิด**: ควบคุมผ่านตัวแปร `EnableCheatAbilities = true` / `false` ใน `Cheat.yml` หรือผ่าน Config
- **ความปลอดภัย (Safety Gate)**: ในระบบคำนวณ `YamlConfigManager.GetCumulativeAbilityValue()` จะตรวจสอบ `Cheat?.EnableCheatAbilities == true` ก่อน หากไม่ได้เปิดใช้งาน ระบบจะข้าม `Cheat.Levels` ทันที แม้จะมีการเขียน Ability ไว้ในไฟล์ ทำให้ไม่มีผลกระทบต่อสมดุลเกมทั่วไป
- **Ability IDs ใน Cheat.yml (ไม่มี prefix `Cheat` นำหน้า)**:
  - `MaxHealth`: เพิ่ม Max Health (ค่าตรง เช่น +5 ต่อระดับ สูงสุด +100 HP ที่ +20)
  - `MaxStamina`: เพิ่ม Max Stamina (ค่าตรง เช่น +5 ต่อระดับ สูงสุด +100 Stamina ที่ +20)
  - `MaxEitr`: เพิ่ม Max Eitr (ค่าตรง เช่น +5 ต่อระดับ ทำให้ร่ายเวทได้แม้ไม่ได้กินอาหารเวท)
  - `HealthRegen`: เพิ่มอัตราฟื้นฟูเลือด (% เช่น +5% ต่อระดับ สูงสุด +100%)
  - `StaminaRegen`: เพิ่มอัตราฟื้นฟูสเตมินา (% เช่น +5% ต่อระดับ สูงสุด +100%)
  - `HealingMultiplier`: เพิ่มประสิทธิภาพการฮีลที่ได้รับ (% เช่น +5% ต่อระดับ สูงสุด +100%)
- **ฟื้นฟู Eitr (`EitrRegen`)**: ปลายทางเป็นคุณสมบัติเดียวกันกับของชุดเกราะ จึงใช้ Ability ID `EitrRegen` ใน `Armor.yml` โดยตรง ไม่จำเป็นต้องมีคีย์ซ้ำซ้อนใน Cheat
- **การประสานงานกับระบบเกม (Seamless Harmony Hooks)**:
  - Intercept ค่า Max Health ใน `Player.SetMaxHealth`, Max Stamina ใน `Player.SetMaxStamina`, และ Max Eitr ใน `Player.SetMaxEitr`
  - ซิงค์ทันทีเมื่อสวมใส่/ถอดอุปกรณ์ผ่าน `Humanoid.EquipItem` และ `Humanoid.UnequipItem`
  - เสริมกำลังการฟื้นฟูใน `SEMan.ModifyHealthRegen`, `ModifyStaminaRegen`, และ `ModifyEitrRegen`
  - ขยายผลการฮีลที่ได้รับใน `Character.Heal`
  - แสดงรายละเอียดชัดเจนใน Tooltip ของไอเทมและหน้าต่าง GUI ตีบวกเมื่อเปิดใช้งาน

---

## 5. รายละเอียดระบบแยกไฟล์ YAML และการทำงานของ *.override.yml

- **ไฟล์จัดเก็บ**: อยู่ในโฟลเดอร์ `BepInEx/config/ckforgame.ItemEnhancements/`:
  - `AbilityReference.txt`: เอกสารอ้างอิงกลาง (Single Source of Truth) รวมรายชื่อ Ability ID ทั้งหมด 25 ตัว พร้อมหมวดหมู่ คำอธิบาย และตัวอย่าง YAML
  - `Success.yml`: อัตราสำเร็จ +1 ถึง +20, SafeLevel, กฎล้มเหลว, ค่าธรรมเนียม, การดรอปคัมภีร์
  - `Item.yml`: อาวุธ (Damage, Stamina, Eitr, Backstab), ความทนทาน, น้ำหนัก, เครื่องประดับ พร้อมส่วน `Levels:` กำหนด Ability รายระดับ (+1 ถึง +20)
  - `Armor.yml`: พลังป้องกันเกราะ, โล่ (Block, Deflection, Parry), ลด Stamina กลิ้ง/วิ่ง/กระโดด, EitrRegen พร้อมส่วน `Levels:` กำหนด Ability รายระดับ (+1 ถึง +20)
  - `Cheat.yml`: โหมดโกง (MaxHealth, MaxStamina, MaxEitr, HealthRegen, StaminaRegen, HealingMultiplier) พร้อมส่วน `Levels:`
- **กลไก Override ที่ปลอดภัยต่อการอัปเดต (Safe Overrides)**:
  - ผู้เล่นสามารถสร้างไฟล์ เช่น `Item.override.yml`, `Armor.override.yml`, `Cheat.override.yml` หรือ `Success.override.yml` แล้วใส่เฉพาะฟิลด์หรือระดับที่ต้องการแก้
  - ระบบจะทำ Partial Merge นำค่าจาก override มาทับค่าฐานเฉพาะคีย์ที่ระบุ โดยค่าที่เหลือยังคงอ่านจากไฟล์หลักอย่างถูกต้อง
  - เมื่อ Mod ได้รับการอัปเดต ตัวติดตั้งจะอัปเดตเฉพาะไฟล์ `.yml` หลัก แต่จะไม่แตะต้องไฟล์ `*.override.yml` ของผู้เล่น
- **Hot-Reload ในตัว**:
  - มี `FileSystemWatcher` คอยตรวจจับการเปลี่ยนแปลงไฟล์ในโฟลเดอร์ YAML แบบ Real-time ทันทีที่เซฟไฟล์ ไม่ต้องรีสตาร์ตเกม

---

## 6. การปรับปรุงสถาปัตยกรรมระดับรายขั้น (Level-by-Level Abilities Architecture)

1. **ยกเลิก `Abilities.yml`**: นำคอนฟิกแบบรายระดับเข้าไปรวมกับ `Item.yml` และ `Armor.yml` โดยตรง ทำให้คอนฟิกไอเทมแต่ละประเภท (อาวุธ/เกราะ) อยู่ในไฟล์เดียวจบ ไม่ต้องสลับดูหลายไฟล์
2. **ศูนย์รวมคำอธิบาย `AbilityReference.txt`**: เพื่อไม่ให้มีเอกสารซ้ำซ้อนในแต่ละไฟล์ จึงจัดทำไฟล์ Reference แผ่นเดียวที่แจกแจง Ability ID ทั้ง 25 ตัว ไวยากรณ์ตัวอย่าง และคำอธิบายภาษาไทย พร้อมคัดลอกลงโฟลเดอร์ BepInEx/config อัตโนมัติเมื่อบิลด์
3. **การคำนวณผลรวมสะสม (Cumulative Addition)**: ค่า Ability จากระดับต่ำกว่าจะถูกนำมาบวกสะสมจนถึงระดับปัจจุบันของไอเทมอย่างถูกต้อง และรองรับการ Override รายระดับได้อย่างยืดหยุ่น

---

## 7. การปรับปรุงระบบตรวจสอบระยะโต๊ะคราฟต์ (Crafting Station Proximity & Range Check)

1. **แก้ปัญหาการบังคับ Interact โต๊ะคราฟต์**:
   - เดิมระบบใช้ `Player.GetCurrentCraftingStation()` ซึ่งจะคืนค่าเฉพาะตอนที่ผู้เล่นกด 'E' เพื่อเปิดหน้าต่างคราฟต์ของโต๊ะนั้นอยู่เท่านั้น ส่งผลให้การยืนอยู่ใกล้โต๊ะเฉยๆ ไม่สามารถกดเปิดหน้าต่างตีบวกได้
   - ปรับปรุงใหม่ใน [EnhancementGui.cs](../UI/EnhancementGui.cs) ให้สแกนตำแหน่งโต๊ะคราฟต์รอบตัวผู้เล่น (`CraftingStation.m_allStations`) และวัดระยะห่างจริง (`Vector3.Distance`)
   - ผู้เล่นเพียงแค่เดินเข้าไปใกล้โต๊ะคราฟต์/เตาตีเหล็ก ก็สามารถกดปุ่ม **`F8`** เพื่อเปิดหน้าต่างตีบวกได้ทันที โดยไม่ต้องกด 'E' เปิดหน้าต่างโต๊ะคราฟต์ก่อน
2. **เพิ่มตัวแปรการตั้งค่าระยะห่าง `CraftingStationRange`**:
   - เพิ่มการตั้งค่าใน [Success.yml](../Configuration/Yaml/Success.yml) (และรองรับ `Success.override.yml`):
     ```yaml
     RequireCraftingStation: true      # ต้องอยู่ใกล้โต๊ะคราฟต์หรือเตาตีเหล็กเพื่อเปิดหน้าต่างตีบวก
     CraftingStationRange: 5.0         # รัศมีระยะห่าง (เมตร) ที่อนุญาตให้เปิดได้ (เช่น 5.0m = ยืนหน้าโต๊ะ/ข้างโต๊ะ, 15.0m = ทั่วห้องช่าง)
     ```
   - เพิ่ม Property รองรับใน [YamlConfigManager.cs](../Configuration/YamlConfigManager.cs) และ [ModConfig.cs](../Configuration/ModConfig.cs)
   - ค่าเริ่มต้นตั้งไว้ที่ **5.0 เมตร** ซึ่งเป็นระยะที่เหมาะสมกับการยืนหน้าโต๊ะหรือข้างโต๊ะคราฟต์อย่างเป็นธรรมชาติ

---

## 8. ระบบกำหนดอัตราการดรอปคัมภีร์มอนสเตอร์อย่างละเอียด (Detailed Monster Drops & Drops.yml)

1. **สร้างไฟล์แยกเฉพาะ `Drops.yml` (พร้อมรองรับ `Drops.override.yml`)**:
   - แยกหมวดหมู่การดรอปออกจาก `Success.yml` เพื่อให้ตั้งค่าอัตราการดรอปของคัมภีร์ได้ทุกระดับอย่างละเอียด
   - บันทึกและดึงข้อมูลผ่าน [YamlConfigManager.cs](../Configuration/YamlConfigManager.cs) พร้อมรองรับ Nested Overrides แบบ 2 มิติสำหรับ `BiomeDrops`
2. **ปรับแต่งโอกาสดรอปคัมภีร์ Tier 1 ถึง 4 แยกตาม Biome อย่างอิสระ**:
   - แต่ละ Biome (Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, AshLands, DeepNorth, Ocean) สามารถกำหนดโอกาสดรอป (% Drop Chance) ของคัมภีร์ทั้ง 4 Tier ได้อย่างอิสระ
3. **ระบบมอนสเตอร์ระดับสูง (Elite Monsters)**:
   - กำหนดตัวคูณโอกาสดรอปพิเศษ `EliteDrops.BonusMultiplier` (ค่าเริ่มต้น 2.0x) สำหรับมอนสเตอร์ระดับ Troll, Golem, Abomination, Gjall, Berserker, Morgen ฯลฯ
   - กำหนดโอกาสสุ่มอัปเกรดเป็นคัมภีร์ Tier ถัดไป `EliteDrops.TierUpgradeChance` (ค่าเริ่มต้น 25.0%)
4. **ระบบการดรอปของบอสโลก (Boss Drops)**:
   - กำหนดการันตีการดรอป (`GuaranteedDrop`), จำนวนดรอปต่ำสุด-สูงสุด (`MinAmount`, `MaxAmount`)
   - กำหนด Tier ของคัมภีร์ประจำบอสแต่ละตัวได้โดยตรงผ่าน `BossTiers` (Eikthyr, Elder, Bonemass, Moder, Yagluth, Queen, Fader)
5. **การทำงานร่วมกับระบบเดิม (Backwards Compatibility)**:
   - หากไม่มีไฟล์ `Drops.yml` หรือมีค่าบางส่วนไม่ได้กำหนด ระบบจะใช้ค่า Default fallback ที่กำหนดไว้ เพื่อให้เซฟและคอนฟิกเดิมทำงานได้ต่อเนื่อง 100%

---

## 9. การแก้ไขปัญหา ZNetScene NullReferenceException และไอเทมคัมภีร์ไม่ดรอป (Prefab Instantiation & Drop Fix)

1. **สาเหตุของปัญหา (Root Cause)**:
   - ตอนสร้าง Prefab แม่แบบของม้วนคัมภีร์ใน [ScrollItemManager.cs](../Core/ScrollItemManager.cs) มีการเรียก `UnityEngine.Object.Instantiate(baseItem)` โดยไม่ได้ตั้งค่า `ZNetView.m_forceDisableInit = true`
   - ส่งผลให้ `ZNetView.Awake()` ทำงานบน Prefab แม่แบบทันที และลงทะเบียนตัวมันเองเข้าไปใน `ZNetScene.m_instances` เสมือนเป็นวัตถุในโลกเกม
   - เมื่อเข้าเกม `ZNetScene.RemoveObjects` ตรวจพบว่าวัตถุแม่แบบนี้อยู่นอก Sector จึงสั่งทำลาย (`Destroy`) ตัว Prefab แม่แบบทิ้ง
   - ทำให้เกิดข้อผิดพลาด 2 อย่าง:
     1. `ZNetScene.RemoveObjects` วนลูปตรวจเช็ก `m_instances.Values` แล้วพบ `ZNetView` ที่ถูกทำลายไปแล้ว จึงโยนข้อผิดพลาด `NullReferenceException: Object reference not set to an instance of an object`
     2. เมื่อมอนสเตอร์ตายและสุ่มผ่านเกณฑ์ดรอป 100% `ScrollItemManager.GetScrollPrefab(tier)` ได้รับอ็อบเจกต์ที่ถูกทำลายไปแล้ว ทำให้ไม่มีของดรอปตกสู่พื้นโลกเลย

2. **การแก้ไข (Resolution)**:
   - **ป้องกันการตื่นตัวของ ZNetView ขณะโคลนแม่แบบ**: หุ้ม `Instantiate(baseItem)` ด้วย `ZNetView.m_forceDisableInit = true` ภายในบล็อก `try ... finally` โดยไม่เรียก `ResetZDO()` เพื่อป้องกันข้อผิดพลาด NullReferenceException ในหน้าเมนูเกม (`FejdStartup`) เนื่องจากยังไม่มีการจัดสรร `m_zdo`
   - **ระบบดึง Prefab สำรอง (Lazy Fallback)**: ใน `GetScrollPrefab(tier)` หากพบว่า Cache ว่างหรืออ็อบเจกต์เสียหาย จะค้นหาซ้ำจาก `ObjectDB.instance` หรือ `ZNetScene.instance` อัตโนมัติ
   - **เกราะป้องกันบั๊ก ZNetScene.RemoveObjects (Safety Prefix Patch)**: เพิ่ม Harmony Prefix ใน [CharacterDropPatches.cs](../Patches/CharacterDropPatches.cs) คอยล้าง Instance ที่เป็น Null หรือ ZDO หายออกจาก `m_instances` ก่อนที่ `RemoveObjects` จะเริ่มทำงาน
   - **เพิ่มความยืดหยุ่นและการบันทึก Log**: ปรับปรุง `GetBiomeTierChance` ให้รองรับกรณี Biome Flag ซ้อนทับ และเพิ่ม Log ข้อมูลการดรอปใน [ScrollDropManager.cs](../Core/ScrollDropManager.cs) เมื่อมอนสเตอร์/บอสทำคัมภีร์ตกสู่พื้นโลก
