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
  - `%APPDATA%\r2modmanPlus-local\Valheim\profiles\Sep2026\BepInEx\plugins\Valheim.ItemEnhancements`
  - `C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\Valheim.ItemEnhancements`

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

- **การเปิด/ปิด**: ควบคุมผ่านหมวด `[10. Cheat Abilities (ความสามารถพิเศษเสริม / โหมดโกง)]` ด้วยตัวแปร `EnableCheatAbilities = true` / `false`
- **ความจุสเตตัส (Capacities)**:
  - `CheatMaxHealthPerLevel = 5` (+100 HP ที่ระดับ +20 ต่อชิ้นบนเกราะ/โล่/เครื่องประดับ)
  - `CheatMaxStaminaPerLevel = 5` (+100 Stamina ที่ระดับ +20 ต่อชิ้นบนเกราะ/อาวุธ/เครื่องประดับ)
  - `CheatMaxEitrPerLevel = 5` (+100 Eitr ที่ระดับ +20 ต่อชิ้น ทำให้ร่ายเวทได้แม้ไม่ได้กินอาหารเวทมนตร์!)
- **อัตราการฟื้นฟูและการฮีล (Regenerations & Healing)**:
  - `CheatHealthRegenPerLevel = 0.05` (+100% ที่ระดับ +20)
  - `CheatStaminaRegenPerLevel = 0.05` (+100% ที่ระดับ +20)
  - `CheatEitrRegenPerLevel = 0.05` (+100% ที่ระดับ +20)
  - `CheatHealingMultiplierPerLevel = 0.05` (+100% ปริมาณฮีลที่ได้รับ)
- **การประสานงานกับระบบเกม (Seamless Harmony Hooks)**:
  - Intercept ค่า Max Health ใน `Player.SetMaxHealth`, Max Stamina ใน `Player.SetMaxStamina`, และ Max Eitr ใน `Player.SetMaxEitr`
  - ซิงค์ทันทีเมื่อสวมใส่/ถอดอุปกรณ์ผ่าน `Humanoid.EquipItem` และ `Humanoid.UnequipItem`
  - เสริมกำลังการฟื้นฟูใน `SEMan.ModifyHealthRegen`, `ModifyStaminaRegen`, และ `ModifyEitrRegen`
  - ขยายผลการฮีลที่ได้รับใน `Character.Heal`
  - แสดงรายละเอียดชัดเจนใน Tooltip ของไอเทมและหน้าต่าง GUI ตีบวกเมื่อเปิดใช้งาน
