# Walkthrough: Valheim MMORPG Item Enhancement Mod (+1 ถึง +20)

สร้าง Mod ระบบการตีบวกอุปกรณ์สไตล์ MMORPG สำหรับเกม **Valheim** สำเร็จเรียบร้อยสมบูรณ์ พร้อมครอบคลุม Abilities ทั้งหมดของเกม, กำหนดอัตราความสำเร็จใน Config ได้อิสระ 1 ถึง 20, ระบบความปลอดภัยและบทลงโทษ, หน้าต่าง UI ตีบวกพร้อมเสียงเอฟเฟกต์ และป้ายสเตตัสสีตามเทียร์

---

## 1. ผลลัพธ์และสิ่งที่ถูกสร้างขึ้น (What Was Built)

### 1. โครงสร้างโปรเจกต์และไฟล์ซอร์สโค้ด
- [Valheim.ItemEnhancements.csproj](../Valheim.ItemEnhancements.csproj): โปรเจกต์ .NET Framework 4.8 อ้างอิง Unity, TextMeshPro, Valheim Assemblies, BepInEx และ Harmony พร้อมระบบ Auto-Deploy ไปยัง r2modman และ Steam Plugins
- [Plugin.cs](../Plugin.cs): BepInEx Plugin Entry Point จัดการ Lifecycle, Harmony Patching และ Hotkey Listener
- [ModConfig.cs](../Configuration/ModConfig.cs): ระบบจัดการ Config แยกอิสระ:
  - อัตราความสำเร็จสำหรับระดับ 1 ถึง 20 (`Level_01_SuccessRate` ถึง `Level_20_SuccessRate`)
  - กฎการล้มเหลว (`SafeLevel`, `DowngradeOnFail`, `BreakOnFail`, `BreakChanceAboveSafeLevel`)
  - ค่าธรรมเนียมเหรียญทอง (`RequireCoins`, `CoinsBaseCost`, `CoinsPerLevelIncrement`)
  - ตัวคูณ Abilities และ Stats ของอาวุธ เกราะ โล่ และความคล่องตัว
- [StatCalculator.cs](../Core/StatCalculator.cs): คำนวณโบนัสสเตตัส Abilities ทั้งหมดของเกม:
  - ความเสียหายทุกธาตุ: Slash, Pierce, Blunt, Chop, Pickaxe, Fire, Frost, Lightning, Poison, Spirit
  - พลังป้องกันเกราะ (Flat & Percent Armor)
  - พลังบล็อก (Block Power) และแรงปัดป้อง (Deflection Force / Parry)
  - ประหยัด Stamina และ Eitr: โจมตี, ร่ายเวท, บล็อก, กลิ้งหลบ, วิ่ง, กระโดด
  - ลดโทษความเร็วเดิน (-5% ถึง -20% ค่อยๆ หายไปและกลายเป็นบวกความเร็วที่ระดับสูง)
  - ความทนทานสูงสุด (Max Durability) และลดน้ำหนักอุปกรณ์
- [EnhancementManager.cs](../Core/EnhancementManager.cs): จัดการการบันทึกระดับการตีบวกลงใน `m_customData` (ปลอดภัย ไม่ทำลายเซฟเกม), สุ่มคำนวณสำเร็จ/ล้มเหลว, หักเหรียญทอง และสร้าง Tooltip แสดงผลอย่างละเอียด
- [EnhancementGui.cs](../UI/EnhancementGui.cs): หน้าต่าง UI ตีบวกสไตล์ MMORPG:
  - เปรียบเทียบสเตตัสแบบ Real-time (ปัจจุบัน ➔ ระดับถัดไป)
  - แสดง % ความสำเร็จ และความเสี่ยงล้มเหลวชัดเจน
  - ระบบ Suspense อนิเมชันหลอมเหล็กพร้อมเสียงทั่งตีเหล็กและแสงประกายไฟ
  - แบนเนอร์แจ้งผลลัพธ์พร้อมเสียงและข้อความลอยกลางจอ
- [ItemDataPatches.cs](../Patches/ItemDataPatches.cs): Harmony Postfix ขยายสเกล `GetDamage`, `GetArmor`, `GetBlockPower`, `GetDeflectionForce`, `GetMaxDurability`, `GetWeight`, `GetTooltip` และ `GetHoverText`
- [PlayerPatches.cs](../Patches/PlayerPatches.cs): Harmony Postfix ปรับค่าความเร็วเดิน (`GetEquipmentMovementModifier`), Stamina กลิ้งหลบ/วิ่ง/กระโดด/บล็อก, Eitr Regen, Max Carry Weight, และ Attack Stamina/Eitr Reduction
- [InventoryGridPatches.cs](../Patches/InventoryGridPatches.cs): แสดงป้ายระดับ +X สีตามเทียร์บนไอคอนในกระเป๋า และอนุญาตให้คลิกขวาเพื่อเลือกไอเทมเข้าแท่นตีบวก
- [InventoryGuiPatches.cs](../Patches/InventoryGuiPatches.cs): เพิ่มปุ่ม `[⚡ ตีบวก]` ในหน้าต่าง Crafting Station ดั้งเดิมของเกม
- [README.md](../README.md): คู่มือการติดตั้ง การตั้งค่า และวิธีเล่นภาษาไทยฉบับสมบูรณ์
- [docs/implementation_plan.md](implementation_plan.md): แผนงานการพัฒนาและรายละเอียดสถาปัตยกรรมฉบับสมบูรณ์

---

## 2. การตรวจสอบและยืนยันผล (Verification Results)

### การคอมไพล์ (Build Verification)
- ทั้ง Debug และ Release บิลด์ผ่าน 100% ปราศจาก Warning และ Error:
  ```powershell
  dotnet build -c Release
  Build succeeded.
      0 Warning(s)
      0 Error(s)
  ```

### การ Deploy ไฟล์ Plugin
- ระบบคัดลอกไฟล์ `Valheim.ItemEnhancements.dll` ไปยังโฟลเดอร์ BepInEx อัตโนมัติ:
  1. โปรไฟล์ r2modman: `%APPDATA%\r2modmanPlus-local\Valheim\profiles\Sep2026\BepInEx\plugins\Valheim.ItemEnhancements\Valheim.ItemEnhancements.dll` ✅
  2. เกมใน Steam: `C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\Valheim.ItemEnhancements\Valheim.ItemEnhancements.dll` ✅

---

## 3. วิธีการทดสอบในเกม (In-Game Testing)

1. เปิดเกม **Valheim** ผ่าน r2modman หรือ Steam
2. นำตัวละครไปยืนใกล้โต๊ะคราฟต์หรือเตาตีเหล็ก (Workbench / Forge)
3. กดปุ่ม **`F8`** หรือกดปุ่ม **`[⚡ ตีบวก]`** ที่แทรกอยู่ในหน้าต่างโต๊ะคราฟต์
4. เลือกอาวุธ, ชุดเกราะ หรือโล่ เพื่อทดสอบตีบวก
5. สังเกตการเพิ่มขึ้นของสเตตัสทั้งในหน้าต่างเปรียบเทียบและใน Tooltip เมื่อเอาเมาส์ชี้ที่ไอเทม
6. ตรวจสอบไฟล์ Configuration ที่สร้างขึ้นในโฟลเดอร์ `BepInEx/config/com.customs.valheim.itemenhancements.cfg` เพื่อปรับแต่งอัตราความสำเร็จตามต้องการ
