# SST-200 Back-Pressure Turbine Automation - Code Documentation

This documentation explains the code logic for the SST-200 turbine automation pipeline in a flow-wise format.  
It is intended as a living document, so more sections can be added progressively for each flow path and sub-flow.

---

## 1) What this code does

This codebase automates core engineering tasks that are usually manual in SST-200 back-pressure turbine design:

- selecting and preparing Kreisl/DAT templates,
- generating and updating load points,
- launching Kreisl and Turba runs,
- checking ERG results,
- optimizing valve/nozzle behavior,
- validating final power match,
- producing HMBD-aligned output behavior (open/closed variants).

The objective is to reduce repetitive manual work, keep calculations consistent, and shorten turnaround time.

---

## 2) Main flow path first (top-level)

The code follows one top-level dispatch path, then branches into sub-flows:

1. **Main flow path (entry)**
   - `Program` -> `StartExec.Main4(...)` for standard path.
   - `MainExecutedClass.GotoBCD1120()` / `GotoBCD1190()` for executed path.
   - `CustomExecutedClass.Main_CustomFlowPathTest()` for custom path (direct or fallback).

2. **Standard flow path**
   - Base automation flow (`StartExec.Main4`) with template selection + Turba + ERG + valve + power match.

3. **Executed flow path**
   - Criteria-driven flow (`MainExecuted(criteria)`), where criteria can be:
     - `BCD1120`
     - `BCD1190`
     - `Throttle` (limited retry, then custom handoff)

4. **Custom flow path**
   - Heavy optimization flow (`Main_CustomFlowPathTest`) with custom DAT selection, PSO, custom ERG checks, valve optimization, and final power closure.

5. **Additional load points flow**
   - Activated in executed/custom when customer load points exceed base set (`CustomerLoadPoints.Count > 2`), and iterated LP-wise.

---

## 3) HMBD logic families used in code

### A. Open HMBD

- Open cycle **with PST**
- Open cycle **without PST**

### B. Closed HMBD

Closed-cycle handling is entered when:

- `DeaeratorOutletTemp > 0`

Inside this branch, it splits into:

1. **Dump condenser ON** (`DumpCondensor == true`)
2. **Dump condenser OFF** (`DumpCondensor == false`)

Then template choice is refined by:

- PRV feasibility check (`tsatvonp(ExhaustPressure * 0.92 - 0.25) - DeaeratorOutletTemp`),
- ERG presence (`File.Exists(KREISL.ERG)`),
- desuperheater decision (`exhaustTemp < PST`).

---

## 4) Code locations (quick map)

- `src/kreisl.cs`
  - `StartKreisl.MainKreisL(...)` orchestration
  - `FillInputValues()` branch behavior for open/closed conditions
- `src/core/Handlers/KreislDATHandler.cs`
  - `RefreshKreislDAT()` template selection and copy/update logic
- `src/core/HMBD/HMBD_Configuration.cs`
  - HMBD pre-feasibility and extraction behavior
- `src/core/Utilities/PrintPDF.cs`
  - closed-cycle output template combinations for dump/deaerator states

---

## 5) Flowchart - Standard path (starting section)

Below is the current standard-path flowchart (shared and refined for documentation).  
This is the first detailed flow section; executed/custom flows will be added next.

```mermaid
flowchart TD
  A["StartKreisl.MainKreisL"] --> B["Delete .CON/.ERG"]
  B --> C["Create KreislDATHandler"]
  C --> D["RefreshKreislDAT"]
 
 
  subgraph T["Template selection - RefreshKreislDAT"]
    direction TB
 
 
    T1{"If DeaeratorOutletTemperature in Load Point > 0"}
 
 
    T1 -- "No" --> T4{"If Process Steam Temperature in Load Point > 0"}
    T4 -- "No" --> T5["Copy Kreisl template without PST (kreislp1.dat)"]
 
 
    T4 -- "Yes" --> P1{"File.Exists(KREISL.ERG) ?"}
    P1 -- "Yes" --> P2["Find exhaustTemp from KREISL.ERG (ExtractTempForDesuparator, 5, 1)"]
    P1 -- "No"  --> P3["Default to Without Desuperheater when ERG missing"]
 
 
    P2 --> D1{"exhaustTemp < PST ?"}
    D1 -- "Yes" --> T6a["Copy Without Desuperheater template (kreislwDesuperheater.dat)"]
    D1 -- "No"  --> T6b["Copy With Desuperheater template (kreislDesuperheater.dat)"]
    P3 --> T6a
 
 
    T1 -- "Yes" --> T2{"DumpCondensor ?"}
   
    T2 -- "Yes" --> T3{"tsatvonp(ExhaustPressure*0.92 - 0.25) - DeaeratorOutletTemp > 0"}
   
    T3 -- "Yes" --> TDSET["Set IsPRVTemplate = true"]
    TDSET --> ERGCHK{"File.Exists(KREISL.ERG) ?"}
    ERGCHK -- "Yes" --> EXTR["exhaustTemp = ExtractTempForDesuparator(KREISL.ERG, 3, 1)"]
    ERGCHK -- "No"  --> DEFWD["Default: CloseCyclePRVWDDump.DAT (ERG missing)"]
    EXTR --> PRVCMP{"exhaustTemp < PST ?"}
    PRVCMP -- "Yes" --> TD1["Copy CloseCyclePRVWDDump.DAT"]
    PRVCMP -- "No"  --> TD1B["Copy CloseCyclePRVDDump.DAT"]
    DEFWD --> TD1
 
 
    T3 -- "No" --> TD2_START["Set IsPRVTemplate = false"]
    TD2_START --> TD2_ERG{"File.Exists(KREISL.ERG) ?"}
    TD2_ERG -- "Yes" --> TD2_EXTR["exhaustTemp = ExtractTempForDesuparator(KREISL.ERG, 3, 1)"]
    TD2_ERG -- "No" --> TD2_DEF["Default: CloseCyclePRVWDDump.DAT (ERG missing)"]
    TD2_EXTR --> TD2_CMP{"exhaustTemp < PST ?"}
    TD2_CMP -- "Yes" --> TD2_WD["Copy CloseCyclePRVWDDump.DAT"]
    TD2_CMP -- "No" --> TD2_D["Copy CloseCyclePRVDDump.DAT"]
    TD2_WD --> TD2_UPDATE["UpdateTemplatePRVToWPRVInDumpCondensor"]
    TD2_D --> TD2_UPDATE
    TD2_DEF --> TD2_UPDATE
   
    T2 -- "No" --> T7{"tsatvonp(ExhaustPressure*0.92 - 0.25) - DeaeratorOutletTemp > 0"}
   
    T7 -- "Yes" --> TN1_SET["Set IsPRVTemplate = true"]
    TN1_SET --> TN1_ERG{"File.Exists(KREISL.ERG) ?"}
    TN1_ERG -- "Yes" --> TN1_EXTR["exhaustTemp = ExtractTempForDesuparator(KREISL.ERG, 3, 1)"]
    TN1_ERG -- "No" --> TN1_DEF["Default: Copy CloseCyclePRVWD.DAT (ERG missing)"]
    TN1_EXTR --> TN1_CMP{"exhaustTemp < PST ?"}
    TN1_CMP -- "Yes" --> TN1_WD["Copy CloseCyclePRVWD.DAT"]
    TN1_CMP -- "No" --> TN1_D["Copy CloseCyclePRVD.DAT"]
    TN1_DEF --> TN1_END
    TN1_WD --> TN1_END["IsPRVTemplate = true"]
    TN1_D --> TN1_END
   
    T7 -- "No" --> TN2_SET["Set IsPRVTemplate = false"]
    TN2_SET --> TN2_ERG{"File.Exists(KREISL.ERG) ?"}
    TN2_ERG -- "Yes" --> TN2_EXTR["exhaustTemp = ExtractTempForDesuparator(KREISL.ERG, 3, 1)"]
    TN2_ERG -- "No" --> TN2_DEF["Default: Copy CloseCyclePRVWD.DAT (ERG missing)"]
    TN2_EXTR --> TN2_CMP{"exhaustTemp < PST ?"}
    TN2_CMP -- "Yes" --> TN2_WD["Copy CloseCyclePRVWD.DAT"]
    TN2_CMP -- "No" --> TN2_D["Copy CloseCyclePRVD.DAT"]
    TN2_WD --> TN2_UPDATE["UpdateTemplatePRVToWPRV"]
    TN2_D --> TN2_UPDATE
    TN2_DEF --> TN2_UPDATE
  end
 
 
  D --> E["FillClosestTurbineEfficiency"]
  E --> F["GetTurbaCON(ClosestProjectID)"]
  F --> G["InitConfig"]
  G --> H["LaunchKreisL"]
  H --> I["RefreshKreislDAT"]
  I --> J["InitConfig"]
  J --> K["ReferenceDATSelector"]
  K --> L["GenerateLoadPoints"]
  L --> M["PrepareDATFile"]
  M --> N["LaunchTurba"]
  N --> O["ERGResultsCheck"]
  O --> P["UpdateLP5"]
  P --> Q["ERGResultsCheck"]
  Q --> R["ValvePointOptimize"]
  R --> S["FillVari40"]
  S --> T2a["LaunchTurba"]
  T2a --> U["Rename TURBATURBAE1.DAT.CON -> TURBA.CON"]
  U --> V["FillWheelChamberPressure"]
  V --> W["PowerMatch.CheckPower"]
```

### 5.1 Standard flow path chart (clean split, as shared)

To keep the Standard section readable (not messy), the same logic is split into smaller charts exactly like your diagram style.

#### 5.1.1 Main Standard pipeline (7.1 to 7.10)

```mermaid
flowchart TD
  S71["7.1 Reference DAT Selector"] --> S72["7.2 Generate Load Point"]
  S72 --> S73["7.3 Prepare DAT File"]
  S73 --> S74["7.4 Launch Turba"]
  S74 --> S75["7.5 ERG Checks"]
  S75 --> S76["7.6 Update LP5"]
  S76 --> S77["7.7 ERG Checks"]
  S77 --> S78["7.8 Valve Point Optimization"]
  S78 --> S79["7.9 Make Turba and Kreisl connection"]
  S79 --> S710["7.10 Check Power"]
```

This is the base Standard run sequence before deeper sub-logic.

#### 5.1.2 Reference DAT selector detail (7.1.x)

```mermaid
flowchart TD
  S71["7.1 Reference DAT Selector"] --> S711["7.11 Select Standard DAT template"]
  S711 --> S712["7.12 Copy template into testDir"]
```

Explanation:
- `7.11` chooses the correct Standard template from input condition.
- `7.12` copies that template to runtime location (`testDir`) so later steps always work on the active DAT.

#### 5.1.3 Prepare DAT detail (7.3.x)

```mermaid
flowchart TD
  S73["7.3 Prepare DAT File"] --> S731["7.3.1 Fill LP1 in TURBATURBAE1.DAT.DAT"]
  S731 --> S732["7.3.2 Fill other load points in TURBATURBAE1.DAT.DAT"]
  S732 --> S733["7.3.3 Update total load point count"]
  S733 --> S734["7.3.4 Update datFileInitParams except load point data"]
  S734 --> S74["7.4 Launch Turba"]
```

Explanation:
- LP1 is written first, then remaining LPs are appended.
- Load-point count is synchronized with written rows.
- Non-LP init params are refreshed before Turba launch.

#### 5.1.4 Valve optimization detail (7.8.x)

```mermaid
flowchart TD
  S78["7.8 Valve Point Optimization"] --> S781["7.8.1 Read base-load LP deviation + nozzle/group/valve status"]
  S781 --> S782["7.8.2 Check nozzle-group/valve status"]
  S782 --> S783["7.8.3 Evaluate valve-point deviation value"]
  S783 --> S784["7.8.4 Adjust nozzle pair / mass-flow"]
  S784 --> S710["7.10 Check Power"]
```

Explanation:
- This block reduces valve-point deviation iteratively.
- Based on status + deviation band, code applies nozzle pair/mass-flow corrections.
- Control then returns to `7.10 Check Power`.

#### 5.1.5 Turba-Kreisl connection + power decision (7.9.1, 7.10.x)

```mermaid
flowchart TD
  S79["7.9 Make Turba and Kreisl connection"] --> S791["7.9.1 Add varicode 40 in TURBATURBAE1.DAT.DAT"]
  S791 --> S710["7.10 Check Power"]
  S710 --> C1{"Power diff <= 25?"}
  C1 -- "No" --> STOP["Stop calculation"]
  C1 -- "Yes" --> S7101["7.10.1 HMBDUpdateEffKreisl"]
  S7101 --> S7102["7.10.2 Optimize no-load power"]
  S7102 --> S7103{"7.10.3 Bending check for LP5"}
  S7103 -- "Pass" --> S7104{"7.10.4 Check thrust"}
  S7103 -- "Fail" --> S7105["7.10.5 Update LP5 power"]
  S7105 --> S7107{"7.10.7 Bending check for LP5"}
  S7107 -- "Pass" --> S7104
  S7107 -- "Fail" --> EXEC["8. Executed flow path"]
  S7104 -- "Pass" --> S7106{"7.10.6 Check final bending"}
  S7104 -- "Fail" --> EXEC
  S7106 -- "Pass" --> HMBD["10. Create HMBD"]
  S7106 -- "Fail" --> STOP
```

Explanation:
- `7.9.1` adds the coupling marker (`varicode 40`) so Turba-Kreisl handoff is complete.
- `7.10` gates success by power difference first.
- If LP5/bending/thrust checks fail repeatedly, flow escalates to `8. Executed flow path`.
- If all checks pass, flow closes at `10. Create HMBD`.

---

## 6) Theory walkthrough - explain the full standard flowchart

This section explains what each major block in the flowchart is doing and why it exists.

### 6.1 Entry and cleanup (`A -> D`)

The flow starts from `StartKreisl.MainKreisL`, then immediately performs runtime cleanup:

- delete stale `.CON` / `.ERG` files,
- create `KreislDATHandler`,
- call `RefreshKreislDAT`.

Why this matters:

- old simulation artifacts can pollute a new run,
- template selection must happen before running Kreisl/Turba,
- all later calculations depend on this initial DAT state.

### 6.2 Template selection subgraph (`T`)

`RefreshKreislDAT` is the most important decision engine in the standard path.

It first determines whether the request is **closed-cycle-like** or **open-cycle-like**:

- if `DeaeratorOutletTemperature > 0` -> closed-cycle branch,
- else -> open-cycle/PST branch.

#### 6.2.1 Open-cycle / PST side (`T1 = No`)

If no deaerator outlet temperature is provided:

- when `PST <= 0`, it copies `kreislp1.dat` (without PST path),
- when `PST > 0`, it tries to read `KREISL.ERG` and extract exhaust temperature.

Then it compares `exhaustTemp` with `PST`:

- `exhaustTemp < PST` -> choose **without desuperheater** template,
- otherwise -> choose **with desuperheater** template.

If ERG is missing, the flow safely defaults to the **without desuperheater** template.

#### 6.2.2 Closed-cycle with deaerator (`T1 = Yes`)

When `DeaeratorOutletTemperature > 0`, the next split is dump condenser:

- `DumpCondensor == true` (dump ON),
- `DumpCondensor == false` (dump OFF).

Inside both dump ON/OFF paths, code checks PRV feasibility:

- `tsatvonp(ExhaustPressure*0.92 - 0.25) - DeaeratorOutletTemp > 0`.

That condition determines whether `IsPRVTemplate` stays true or false.

After that, it optionally reads `KREISL.ERG` and compares `exhaustTemp < PST` to decide:

- with-desuperheater template vs without-desuperheater template.

For non-PRV outcomes, the selected PRV template is converted using:

- `UpdateTemplatePRVToWPRVInDumpCondensor` (dump ON),
- `UpdateTemplatePRVToWPRV` (dump OFF).

This conversion step is essential because template families are reused and then adjusted to match final mode.

### 6.3 Post-template thermodynamic initialization (`D -> J`)

After template decision:

1. `FillClosestTurbineEfficiency` loads nearest known performance context.
2. `GetTurbaCON(ClosestProjectID)` binds reference project CON data.
3. `InitConfig` hydrates runtime model state.
4. `LaunchKreisL` runs Kreisl with selected template/input.
5. `RefreshKreislDAT` + `InitConfig` run again to sync generated outputs back into the pipeline.

The key idea is: **select -> run -> resync** before entering final DAT/Turba checks.

### 6.4 Main computation pipeline (`K -> R`)

This is the operational sequence:

1. `ReferenceDATSelector` picks final DAT reference.
2. `GenerateLoadPoints` builds LP inputs.
3. `PrepareDATFile` writes LP and control values into DAT.
4. `LaunchTurba` runs turbine simulation.
5. `ERGResultsCheck` validates result quality.
6. `UpdateLP5` modifies LP5 scenario and checks ERG again.
7. `ValvePointOptimize` adjusts valve configuration for convergence/performance.

Why LP5 is checked again:

- LP5 often acts as a corrective or boundary operating point,
- second ERG check ensures the updated point still satisfies constraints.

### 6.5 Final stabilization and power closure (`S -> W`)

After valve optimization:

1. `FillVari40` updates DAT/Kreisl variable settings.
2. `LaunchTurba` runs once more on updated values.
3. `Rename TURBATURBAE1.DAT.CON -> TURBA.CON` normalizes output naming for downstream use.
4. `FillWheelChamberPressure` pushes wheel chamber pressure back to Kreisl/DAT side.
5. `PowerMatch.CheckPower` performs final power closure.

This final block ensures the output is not just feasible, but also aligned with target power behavior.

### 6.6 Fallback and resilience behavior (conceptual)

Across this flow, the code uses practical fallback rules:

- if ERG file does not exist, choose safe default templates,
- if branch-specific PRV mode is not feasible, convert PRV templates to non-PRV variants,
- rerun key steps after major state updates (Kreisl run, LP update, valve optimization).

This makes the pipeline robust to missing intermediate files and branch-dependent state transitions.

---

## 7) Flow-wise content: Executed flow path

### 7.1 Main executed flow (`MainExecutedClass.MainExecuted`)

The executed flow sequence is:

1. initialize counters and criteria limits (`mainCallCounters`, `throttleCounters`),
2. apply retry/fallback gates:
   - `Throttle` only up to `MAX_THROTTLE_CALLS`,
   - `BCD1120` exhaustion -> switch to `BCD1190`,
   - `BCD1190` exhaustion -> move to custom flow (`Main_CustomFlowPathTest`),
3. run HMBD defaults and nearest project selection (`PowerKNN`, `MoveYAndSetParams`),
4. select executed DAT (`ReferenceDATSelectorExecuted`),
5. load DAT and generate LPs (`LoadDatFile`, `GenerateLoadPoints`),
6. write DAT (`PrepareDATFileExecuted`),
7. validate wheel chamber pressure; if invalid, re-run executed selection,
8. launch Turba (`LaunchTurba`),
9. run ERG checks by criteria (`ErgResultsCheckExecuted(criteria, false)`),
10. update LP5 and re-check ERG (`UpdateLP5`, `ErgResultsCheckExecuted(criteria, true)`),
11. valve optimization (`ValvePointOptimize`),
12. final stabilization:
    - `FillVari40`
    - Turba re-launch
    - rename `TURBATURBAE1.DAT.CON` -> `TURBA.CON`
    - fill wheel chamber pressure
13. final power match (`CheckPower`).

### 7.2 Executed criteria sub-flows

- **BCD1120 flow**
  - Uses `ErgResultsCheckBCD1120` in both initial and LP5-updated passes.
  - If call budget is exhausted, auto-handoff to `BCD1190`.

- **BCD1190 flow**
  - Uses `ErgResultsCheckBCD1190` in both initial and LP5-updated passes.
  - If call budget is exhausted, auto-handoff to custom flow.

- **Throttle flow**
  - Uses `ErgResultsCheckThrottle`.
  - Hard-limited retry count; beyond limit, flow returns and effectively shifts toward custom handling path.

---

## 8) Flow-wise content: Custom flow path

### 8.1 Main custom flow (`CustomExecutedClass.Main_CustomFlowPathTest`)

The custom flow sequence is:

1. initialize dependencies and cleanup (`DeleteCONFiles`, `RefreshKreislDAT`),
2. read nearest Turba context and fill input values,
3. set HMBD defaults + initial efficiency setup,
4. generate custom load points (`CustomLoadPointGenerator.GenerateLoadPoints`),
5. pre-feasibility checks (`fillPrefeasibilityDecisionChecks`),
6. pick nearest custom params and custom reference DAT:
   - `GetNearestParams_Custom`
   - delete executed DAT
   - copy custom reference DAT,
7. prepare DAT (`CustomDATFileProcessor.PrepareDatFile`),
8. run base update and optimization:
   - `BCD_UPDATE`
   - PSO flow optimizer (`InvokeTurbineDesigner`),
9. run custom base checks (`ERG_CUSTOM_BASE_CHECKS`),
10. convert/update steam path (`TurnaConvert`, `UpdatePunConvertor`) and launch Turba,
11. select ERG criterion from pre-feasibility decision:
   - custom BCD1120 check or
   - custom BCD1190 check,
12. LP5 update + second criterion check,
13. custom valve point optimization (`CustomValvePointOptimizer`),
14. final closure:
   - `FillVari40`
   - Turba launch
   - rename/load final CON
   - fill wheel chamber pressure
   - custom power match + final checks (`checkFinalTurbine`).

---

## 9) Flow-wise content: Additional load points path

This path appears in both executed and custom flows when:

- `AdditionalLoadPoint.GetInstance().CustomerLoadPoints.Count > 2`

LP-wise sequence:

1. ensure `TURBA.CON` exists (rename fallback if needed),
2. refresh Kreisl DAT and push wheel chamber pressure,
3. iterate customer load points from index 1 onward,
4. for each LP, resolve unknown input dimension and fill LP template:
   - `Pr` (pressure unknown),
   - `T` (temperature unknown),
   - `M` (mass flow unknown),
   - `P` (power unknown),
   - `E` (exhaust pressure unknown),
5. append each generated LP DAT block into final `KREISL.DAT`,
6. if deaerator/PST path is active, update desuperheater fields from Turba ERG,
7. launch Kreisl on merged multi-LP DAT.

---

## 10) Complete flow map (all paths)

`Main Entry` -> `Standard` **or** `Executed` **or** `Custom`

- `Standard` -> single baseline path -> ERG checks -> valve optimization -> power match.
- `Executed` -> `BCD1120 / BCD1190 / Throttle` criteria sub-flow -> fallback chain -> power match.
- `Custom` -> custom DAT + PSO + custom ERG criteria -> valve + final custom checks.
- `Executed/Custom` + additional LP count -> `Additional Load Points LP-wise loop`.

---

## 11) Notes

- This README is code-logic first and intentionally flow-oriented.
- Keep terminology as in code where possible (`DumpCondensor`, `DeaeratorOutletTemp`, `IsPRVTemplate`) to avoid mismatch.
- Extend each section with diagrams and method-level call mapping as documentation evolves.

