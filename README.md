# S7ProjectParser

Offline **Siemens STEP 7 (.s7p) project parser** for .NET Framework 4.0 — extracts the
variable table of a classic STEP 7 project as a **tree** with fully resolved
**communication absolute addresses**, without needing STEP 7, TIA Portal, or a live PLC.

- ✅ Pure file parser — open a `.s7p` project and read the result; no communication
  (no reading/writing PLC values, no runtime operations)
- ✅ **Tree output**: project → device → block → variable (structs, UDT instances,
  arrays, and structure sections as tree nodes)
- ✅ **Array expansion**: every array element becomes a node with its own address
- ✅ **Communication absolute addresses** formatted by the parser itself
  (s7netplus dot syntax: `DB1.DBX8.0`, `MW2`, `T5`, `Z3`)
- ✅ **Multi-station projects**: browse variables per device/station
- ✅ Correct decoding of **Chinese (GBK / ANSI code page)** and other ANSI projects
- ✅ Output object shapes aligned with **S7CommPlusDriver** (`VarInfo`, `PObject`,
  `DatablockInfo`)
- ✅ **Zero third-party dependencies**, .NET Framework 4.0, C# 4.0 syntax
- ✅ Two demos: a WinForms variable-tree browser and a console table dumper

## Repository structure

```
S7ProjectParser/
├── S7ProjectParser.sln            solution
├── CHANGELOG.txt                  change history (Chinese)
├── api_smoke.ps1                  API smoke test (PowerShell)
└── src/
    ├── S7ProjectParser/           core library (no third-party dependencies)
    │   ├── DbfReader.cs           dBase III DBF + .DBT memo reader (STEP 7 variant)
    │   ├── AnsiText.cs            ANSI code page text decoding (fixes Chinese mojibake)
    │   ├── S7DataType.cs          bit-level size/alignment table of STEP 7 base types
    │   ├── Mc5Parser.cs           MC5 (SCL-like) structure code parser — tree output,
    │   │                          array expansion, bit-level address calculation
    │   ├── S7ProjectParser.cs     main parse flow (device discovery / YDBs / ombstx)
    │   ├── PlcVariableTree.cs     variable tree model (PlcVariableNode/PlcBlock/PlcDevice)
    │   ├── S7Project.cs           parser entry point (Open/Close/Browse/type info/…)
    │   └── ItemAddress.cs / VarInfo.cs / PObject.cs / Softdatatype.cs …
    │                              output objects (aligned with S7CommPlusDriver)
    ├── S7ProjectExplorer/         WinForms GUI demo (variable tree browser)
    └── S7ProjectParser.Console/   console demo (prints the variable table)
```

## Build

Requires Visual Studio 2017 (MSBuild 15.0) and the .NET Framework 4.0 targeting pack:

```bat
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" S7ProjectParser.sln /p:Configuration=Debug
```

## Quick start

```csharp
using S7ProjectParser;

var project = new S7Project();

// Open: parameter is the .s7p file or the project directory
int rc = project.Open(@"D:\...\STEP7Sample1\test");
if (rc != 0) { Console.WriteLine(S7Project.ErrorText(rc)); return; }   // error text in Chinese

// Browse: ALL leaf entries of the active device (same semantics as the driver)
List<VarInfo> vars;
rc = project.Browse(out vars);
foreach (VarInfo v in vars)
{
    // v.Name           = "DB1.SAMPLE_ARRAY[3]"   (full symbol path, array elements indexed)
    // v.AccessSequence = "DB1.DBW14"             (communication absolute address)
    // v.Softdatatype   = 5                       (INT)
    Console.WriteLine("{0,-16} / {1,-24} / {2}",
        v.Name, v.AccessSequence, Softdatatype.Types[v.Softdatatype]);
}

project.Close();
```

**Multi-station projects** — get device info first, then browse per device:

```csharp
foreach (PlcDevice dev in project.GetDevices())
{
    string cpu = dev.CpuName + " / " + dev.CpuMlfb + " / " + dev.CpuFirmware;
    // e.g. "CPU 315-2 DP / 6ES7 315-2AH14-0AB0 / V3.3", plus StationName and Slot
    List<VarInfo> deviceVars;
    rc = project.BrowseDevice(dev, out deviceVars);   // this device's leaves
}
```

## API overview

Entry class `S7Project` (namespace `S7ProjectParser`). Errors are a simple own
code set (`0` = OK … `6` = internal error, text via `S7Project.ErrorText(int)`).

| API | Purpose |
|---|---|
| `Open(path)` / `Close()` / `IsOpen` | open / close a project |
| `Browse(out List<VarInfo>)` | all leaf entries of the active device |
| `GetDevices()` / `BrowseDevice(dev, out vars)` | device info / per-device browsing |
| `GetDeviceCount()` / `GetDeviceName(i)` / `SetActiveDevice(i)` | device switching (0-based index) |
| `GetListOfDatablocks(out blocks)` | data-block list |
| `getTypeInfoByRelId(relId)` | type info by relId (lazy loading) |
| `GetTypeInformation(relId, out objects)` | type info incl. recursive struct members |
| `GetCommentsXml(relId, out lc, out dc)` | comment XML (minimal empty structure offline) |

Driver-aligned conventions:

- data block relId = block number + `0x8A0E0000`;
- symbol-area relIds: Inputs=`0x90010000`, Outputs=`0x90020000`, Merker=`0x90030000`,
  S7Timers=`0x90050000`, S7Counters=`0x90060000`;
- struct/UDT instances point to child type info via `OffsetInfoType` RelationId
  (allocated from `0x90070000`, loaded lazily by `getTypeInfoByRelId`).

`Browse` returns every leaf in one call (array elements individually, struct members
recursively, section containers flattened, unsupported soft data types skipped) —
pair it with the GUI's true lazy loading (children are fetched via
`getTypeInfoByRelId` only when a node is expanded).

## Addresses: what you get, what you don't

Three address forms exist in the STEP 7 world; the parser outputs exactly one of them:

| Form | Example | Where |
|---|---|---|
| STEP 7 symbol-table address | `DB1:8.0`, `MD4` | internal only (`ItemAddress.AbsoluteAddress`) |
| TIA Portal hex access ID | `8A0E0001.A` | not used by this library |
| **Communication absolute address** | `DB1.DBX8.0`, `MW2`, `T5`, `Z3` | **public output** |

The parser formats the communication absolute address itself (s7netplus dot syntax)
according to the soft data type — the resulting strings can be passed to communication
libraries directly:

| Soft data type | Output | Example |
|---|---|---|
| BOOL | `DB{n}.DBX{byte}.{bit}` / `M{byte}.{bit}` | `DB1.DBX8.0`, `M2.3` |
| BYTE / CHAR / USINT / SINT | `DB{n}.DBB{byte}` / `MB{byte}` | `DB1.DBB8` |
| WORD / INT / UINT / DATE / S5TIME | `DB{n}.DBW{byte}` / `MW{byte}` | `DB1.DBW8`, `MW2` |
| DWORD / DINT / UDINT / REAL / TIME | `DB{n}.DBD{byte}` / `MD{byte}` | `DB1.DBD8`, `MD4` |
| timer / counter | `T{number}` / `Z{number}` | `T5`, `Z3` |

## How addresses are computed

The MC5 structure code stored in the project does **not** contain addresses. The
parser keeps a bit counter across each DB and lays out variables exactly like the
STEP 7 compiler:

| Type | Alignment (bits) | Size (bits) |
|---|---|---|
| BOOL | none (packed bitwise) | 1 |
| BYTE / CHAR | 8 | 8 |
| INT / WORD / DATE / S5TIME … | 16 | 16 |
| DINT / DWORD / REAL / TIME … | 16 | 32 |
| POINTER | 16 | 48 |
| DATE_AND_TIME | 16 | 64 |
| ANY | 16 | 80 |
| STRING [n] | 16 | (2+n)×8 |

- arrays start on a 2-byte boundary; element address = aligned base + index × element size;
- multi-dimensional BOOL arrays pack the last dimension bitwise, every other dimension
  starts on a fresh byte boundary;
- each STRING array element is aligned to 2 bytes on its own;
- struct/UDT instances are re-parsed recursively (their members keep feeding the counter).

## Demos

**`S7ProjectParser.Console`** — call sequence like the driver's DriverTest minus the
communication part (`Open → Browse → print table → Close`). Real output
(STEP7Sample1, default active device):

```
====================== VARIABLENHAUSHALT ======================
SYMBOLIC-NAME/ACCESS-SEQUENCE/TYP
DB1.BOOL1        / DB1.DBX0.0               / Bool
DB1.INT1         / DB1.DBW2                 / Int
DB1.FLOAT1       / DB1.DBD4                 / Real
DB2.BOOL1        / DB2.DBX0.0               / Bool
DB2.INT1         / DB2.DBW2                 / Int
DB2.FLOAT1       / DB2.DBD4                 / Real
MArea.PLC1_FLOAT1 / MD4                      / DWord
MArea.PLC1_INT1  / MW2                      / Word
MArea.PLC1_BOOL1 / M0.0                     / Bool
===============================================================
```

**`S7ProjectExplorer`** — WinForms variable-tree browser (layout modeled after
S7CommPlusGUIBrowser): open a project, browse the tree (rooted at **station names**,
one root per station in multi-station projects), select a node to see its symbol
path / data type / communication absolute address, and locate symbols by name.

## Testing

Validated against two real sample projects — STEP7Sample1 (created on a Chinese
Windows, GBK encoded) and STEP7Sample2 (German, multi-device). The PowerShell
smoke test exercises the full API surface (open/close, device switching, data-block
list, type info, browse, address formatting, error codes):

```
powershell -File api_smoke.ps1
```

## Known limitations

- Encoding cannot be detected from the files (the "language driver" byte is always 0);
  text is decoded with the system ANSI code page, exactly like STEP 7 does.
- Array expansion increases node counts significantly (an `ARRAY[1..20] OF BYTE`
  produces 20 leaf nodes) — this is by design.
- Classic STEP 7 projects only (non-optimized access — `OptAddress` is always 0);
  TIA Portal projects and their hex access IDs are not supported.
