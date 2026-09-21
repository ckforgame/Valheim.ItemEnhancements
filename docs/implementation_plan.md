# แผนการพัฒนา Mod: Valheim MMORPG Item Enhancement (+1 ถึง +20)

สร้าง Mod สำหรับ Valheim ภายใต้คอนเซปต์ **"การตีบวกอุปกรณ์สไตล์ MMORPG" (Refinement / Enchantment System)** ตั้งแต่ระดับ **+1 ถึง +20** โดยมุ่งเน้นการขยายขีดความสามารถและ Stats/Abilities ดั้งเดิมทั้งหมดที่เกม Valheim มีอยู่จริง พร้อมระบบกำหนดอัตราความสำเร็จ (Success Rate) และผลลัพธ์การล้มเหลวแบบละเอียดใน Configuration File

---

## 1. จุดเด่นและระบบหลัก (Core Features)

### 1.1 ระดับการตีบวก (+1 ถึง +20)
- รองรับการตีบวกไอเทมสวมใส่ทุกประเภท:
  - **อาวุธ (Weapons)**: ดาบ, ขวาน, กระบอง, มีด, หอก, ง้าว (Atgeir), ธนู (Bows), หน้าไม้ (Crossbows), คทาเวทมนตร์ (Eitr Staffs)
  - **ชุดเกราะ (Armor)**: หมวก, เกราะอก, กางเกง, ผ้าคลุม
  - **โล่ (Shields)**: โล่กลม (Round Shield), โล่ทาวเวอร์ (Tower Shield), โล่บัคเลอร์ (Buckler)
  - **เครื่องประดับ (Accessories / Belts)**: เช่น Megingjord, Wishbone

### 1.2 รองรับ Abilities และ Status ทั้งหมดที่มีในเกม (Existing Valheim Abilities)
Mod จะทำการคำนวณและเพิ่มโบนัสให้ Abilities ของไอเทมตามระดับการตีบวก (+1 ถึง +20):
1. **พลังโจมตี (All Damage Types)**:
   - กายภาพ: ฟัน (Slash), แทง (Pierce), ทุบ (Blunt), โค่นต้นไม้ (Chop), ขุดแร่ (Pickaxe)
   - เวทมนตร์ / ธาตุ: ไฟ (Fire), น้ำแข็ง (Frost), สายฟ้า (Lightning), พิษ (Poison), จิตวิญญาณ (Spirit)
2. **พลังป้องกัน (Armor & Block)**:
   - พลังป้องกันเกราะพื้นฐาน (Base Armor)
   - พลังป้องกันโล่และการปัดป้อง (Block Power, Deflection Force, Timed Block / Parry Bonus)
3. **การประหยัดสตามินาและ Eitr (Stamina & Eitr Efficiency)**:
   - ลดอัตราการใช้ Stamina ในการโจมตี (Attack Stamina Modifier)
   - ลดอัตราการใช้ Eitr ของอาวุธเวทมนตร์ (Attack Eitr Modifier)
   - ลด Stamina ในการบล็อก (Block Stamina Modifier)
   - ลด Stamina ในการกลิ้งหลบ (Dodge Stamina Modifier)
   - ลด Stamina ในการวิ่ง (Run Stamina Modifier)
   - ลด Stamina ในการกระโดด (Jump Stamina Modifier)
4. **ความเร็วในการเคลื่อนที่ (Movement Modifier)**:
   - บรรเทาหรือยกเลิกโทษติดลบความเร็วเคลื่อนที่ของเกราะหนักและโล่ทาวเวอร์ (เช่น -5% หรือ -20% ของเกราะเดิมจะค่อยๆ หายไปเมื่อบวกสูงขึ้น และกลายเป็นบวกความเร็วที่ระดับสูง)
5. **ความทนทานและน้ำหนัก (Durability & Weight)**:
   - เพิ่ม Max Durability ตามระดับการตีบวก (ไอเทมทนทานขึ้นมาก)
   - ลดอัตราการลดทอนความทนทาน (Durability Drain)
   - ลดน้ำหนักของอุปกรณ์ลงเล็กน้อยตามระดับ (Weight Reduction)
6. **ความสามารถพิเศษเฉพาะตัว (Special Bonuses)**:
   - โบนัส Backstab Damage สำหรับมีด/อาวุธลอบสังหาร
   - เพิ่ม Carry Weight สูงสุดสำหรับเข็มขัด Megingjord
   - เพิ่มอัตราฟื้นฟู Eitr Regen สำหรับชุดนักเวท

### 1.3 ระบบ Config อัตราความสำเร็จ (Configurable Success Rates 1-20)
ผู้เล่นและเซิร์ฟเวอร์สามารถปรับแต่งค่าได้อิสระผ่านไฟล์ Configuration (`ValheimItemEnhancements.cfg`):
- **อัตราความสำเร็จแยกรายระดับ 1 ถึง 20**:
  - `Level 1`: ค่าเริ่มต้น 100%
  - `Level 2`: ค่าเริ่มต้น 100%
  - `Level 3`: ค่าเริ่มต้น 95%
  - `Level 4`: ค่าเริ่มต้น 90%
  - `Level 5`: ค่าเริ่มต้น 80%
  - `Level 6`: ค่าเริ่มต้น 70%
  - `Level 7`: ค่าเริ่มต้น 60%
  - `Level 8`: ค่าเริ่มต้น 50%
  - `Level 9`: ค่าเริ่มต้น 40%
  - `Level 10`: ค่าเริ่มต้น 35%
  - `Level 11`: ค่าเริ่มต้น 30%
  - `Level 12`: ค่าเริ่มต้น 25%
  - `Level 13`: ค่าเริ่มต้น 20%
  - `Level 14`: ค่าเริ่มต้น 15%
  - `Level 15`: ค่าเริ่มต้น 12%
  - `Level 16`: ค่าเริ่มต้น 10%
  - `Level 17`: ค่าเริ่มต้น 8%
  - `Level 18`: ค่าเริ่มต้น 5%
  - `Level 19`: ค่าเริ่มต้น 3%
  - `Level 20`: ค่าเริ่มต้น 1%
- **กฎการล้มเหลว (Failure Rules)**:
  - `SafeLevel` (ค่าเริ่มต้น 3 หรือ 4): ระดับที่ปลอดภัยแน่นอน ไม่ลดระดับและไม่แตก
  - `DowngradeOnFail` (ค่าเริ่มต้น `true`): หากล้มเหลวเกินระดับปลอดภัย จะลดระดับลง 1 ขั้น
  - `BreakOnFail` (ค่าเริ่มต้น `false`): ตัวเลือกเปิด/ปิดการแตกสลายของไอเทม (เพื่อป้องกันไอเทมหายหากผู้เล่นไม่ต้องการ)
  - `BreakChance` (ค่าเริ่มต้น 0%): โอกาสแตกเมื่อล้มเหลวในระดับสูง
- **ค่าใช้จ่ายในการตีบวก (Costs & Materials)**:
  - `RequireCoins` (ค่าเริ่มต้น `true`): ใช้เหรียญทอง (Coins) เป็นค่าธรรมเนียมช่างตีเหล็ก
  - `CoinsCostPerLevel` (ปรับได้ตามระดับ)
  - `RequireMaterials` (ตัวเลือกเปิด/ปิดการใช้อัญมณีหรือแร่เพิ่มเติม)

### 1.4 หน้าต่าง UI ตีบวกสไตล์ MMORPG (In-Game Refinement Window)
- ออกแบบหน้าต่าง UI ตามธีมไวกิ้งของ Valheim:
  - ช่องใส่ไอเทมที่ต้องการตีบวก
  - หน้าต่างเปรียบเทียบสเตตัสแบบ Real-time: **ปัจจุบัน (+X)** ➔ **ระดับถัดไป (+X+1)**
  - แสดงโอกาสสำเร็จ (%) สีชัดเจน (เขียว/เหลือง/แดง)
  - แสดงบทลงโทษหากล้มเหลว (ปลอดภัย / ลดระดับ / แตก)
  - แสดงค่าใช้จ่ายและเหรียญทองที่มี
  - ปุ่ม **"⚡ เริ่มตีบวก (Enhance) ⚡"**
  - เสียงเอฟเฟกต์ตีทั่งเหล็ก (Anvil SFX) ประกายไฟ และเสียงยินดีเมื่อสำเร็จ หรือเสียงเหล็กหักเมื่อล้มเหลว
- **การเข้าถึง UI**:
  - มีปุ่ม **"[⚡ ตีบวก]"** ในหน้าต่าง Workbench / Forge / Black Forge
  - หรือกดปุ่มลัด (Hotkey เช่น `F8` หรือ `U` ปรับแต่งได้ใน config)

### 1.5 การแสดงผลในเกม (Visual & Tooltip Presentation)
- ป้ายชื่อระดับสีตาม Tier ในกระเป๋า (Item Slot Badge):
  - `+1 ถึง +3`: สีเขียวสดใส (`#4ade80`)
  - `+4 ถึง +6`: สีฟ้า Cyan (`#22d3ee`)
  - `+7 ถึง +9`: สีน้ำเงิน Rare (`#3b82f6`)
  - `+10 ถึง +12`: สีม่วง Epic (`#a855f7`)
  - `+13 ถึง +15`: สีส้มทอง Legendary (`#f59e0b`)
  - `+16 ถึง +19`: สีแดงเลือดหมู Mythic (`#ef4444`)
  - `+20`: สีทองรุ้งส่องประกาย Divine Prismatic (`#ffd700`)
- Tooltip ของไอเทมแสดงบล็อกสรุปข้อมูลโบนัสที่ได้รับการตีบวกอย่างสวยงามและละเอียด

### 1.6 ความปลอดภัยของข้อมูล (Data Persistence)
- จัดเก็บข้อมูลระดับการตีบวกใน `item.m_customData` (ระบบ Native Serialization ของ Valheim)
- ไม่กระทบต่อ Save เกมเดิม, ใช้งานร่วมกับ Multiplayer ได้อย่างเสถียร และไม่ทำให้ไอเทมสูญหายแม้ถอดหรืออัปเดตม็อด

---

## 2. โครงสร้างไฟล์และสถาปัตยกรรม (Project Structure)

```
Valheim.ItemEnhancements/
├── Valheim.ItemEnhancements.csproj    # .NET Framework 4.8 C# Project
├── Plugin.cs                          # BepInEx Plugin Entry Point & Lifecycle
├── Configuration/
│   └── ModConfig.cs                   # จัดการ BepInEx Configuration (Success Rates 1-20, Stats scaling, Costs)
├── Core/
│   ├── EnhancementManager.cs          # ตรรกะคำนวณการตีบวก, บันทึก/อ่าน customData, สุ่มสำเร็จ/ล้มเหลว
│   └── StatCalculator.cs              # คำนวณสูตร Stats/Abilities โบนัสทั้งหมดของเกม
├── UI/
│   └── EnhancementGui.cs              # หน้าต่าง UI MMORPG ตีบวก (เปรียบเทียบสเตตัส, ปุ่มกด, อนิเมชัน)
├── Patches/
│   ├── ItemDataPatches.cs             # Harmony Patch สำหรับ GetDamage, GetArmor, GetBlockPower, GetTooltip, ฯลฯ
│   ├── PlayerPatches.cs               # Harmony Patch สำหรับ Player Modifiers (Stamina, Movement, Eitr)
│   ├── InventoryGridPatches.cs        # Harmony Patch สำหรับป้ายระดับสีบนไอคอนและคลิกขวาเลือกไอเทม
│   └── InventoryGuiPatches.cs         # Harmony Patch แทรกปุ่มเปิดหน้าต่างตีบวกในแท็บคราฟต์
├── README.md                          # คู่มือการใช้งานและตารางสเตตัสเริ่มต้น
└── docs/
    ├── implementation_plan.md         # แผนงานการพัฒนาและรายละเอียดสถาปัตยกรรม
    └── walkthrough.md                 # บันทึกผลการพัฒนาและการทดสอบ
```

---

## 3. แผนการตรวจสอบและทดสอบ (Verification Plan)

### การทดสอบแบบอัตโนมัติ (Automated / Build Tests)
- รัน `dotnet build -c Release` เพื่อยืนยันว่าโปรเจกต์คอมไพล์ผ่าน 100% โดยไม่มีข้อผิดพลาดหรือ Warning ที่เป็นอันตราย
- ตรวจสอบความถูกต้องของการอ้างอิง Assembly ของ Valheim และ BepInEx

### การทดสอบในเกม (Gameplay Verification)
1. ติดตั้งไฟล์ DLL ที่บิลด์แล้วลงในโฟลเดอร์ BepInEx plugins ของผู้ใช้
2. ตรวจสอบไฟล์ `.cfg` ที่สร้างขึ้นว่ามีตัวเลือก 1-20 ครบถ้วนและสามารถเปลี่ยนค่าแล้วส่งผลทันที
3. ทดสอบการเปิดหน้าต่าง UI ตีบวก นำไอเทม (อาวุธ, ชุดเกราะ, โล่) ใส่ลงในช่อง
4. ทดสอบกดตีบวก:
   - ตรวจสอบอัตราความสำเร็จ
   - ตรวจสอบการลดลงของระดับเมื่อล้มเหลว
   - ตรวจสอบเอฟเฟกต์เสียงและข้อความแจ้งเตือน
5. ตรวจสอบค่าสเตตัส (Damage, Armor, Block, Stamina Reduction, Movement Penalty) ว่าเพิ่มขึ้นจริงในเกมและแสดงใน Tooltip ถูกต้อง
6. บันทึกเกม ออกและเข้าใหม่ เพื่อยืนยันว่าระดับ +X ยังคงอยู่และไม่หายไป
