# Mockery — Bard Dungeon Crawler
## Implementation Plan

---

## Game Summary

You play as a **Bard** captured by demons. Equipped only with your **Libro de Insultos** (insult book), you must escape a demon prison floor by floor.

Combat uses **WASD sequences** (Helldivers-style) to cast insults. Enemies have randomly generated elemental resistances — use the right type or face the consequences.

**Permadeath.** Die on any floor → restart the entire run.

**Theme:** Assonant / Dissonant / Consonant (Elemental RPS)
**Engine:** Unity (URP)
**Key Package:** DOTween

---

## Element Cycle

```
Dissonant → Consonant → Assonant → Dissonant
(each beats the one to its right)
```

| Your spell       | Beats           | Loses to        |
|------------------|-----------------|-----------------|
| Dissonant      | Consonant      | Assonant        |
| Consonant       | Assonant        | Dissonant     |
| Assonant         | Dissonant     | Consonant      |

### Spell Sequences (WASD input, Helldivers-style)

Each spell shows directional arrows in the Libro UI. Player inputs the sequence to cast.

| Spell Type   | Sequence    |
|--------------|-------------|
| Assonant     | ↑ ↑ → ↑    |
| Dissonant  | ← ↓ ↓ ←   |
| Consonant   | ↓ → → ↓   |

> **Design note:** Combat is turn-based. Each turn has a time window. Faster input = more time remaining = player advantage (can act again before timer resets). Players who memorize sequences can skip looking at the book and gain speed.

### Enemy Resistance System

Every enemy has a **randomly rolled** resistance profile across the 3 elements:

| Resistance Tier | Effect            |
|-----------------|-------------------|
| Immune          | 0× damage         |
| Normal          | 1× damage         |
| Weak            | 2× damage         |

Each enemy always has exactly one of each tier (one immune, one normal, one weak). The player must figure out the weakness through trial and error or memory.

---

## Roguelike Systems

### Between-Floor Buffs
After clearing a floor and before entering the next, the player is offered **3 random buffs** to choose from. Buffs affect skills or main stats (e.g., "+15% Assonant damage", "+10 max HP", "DOT resistance", "gold multiplier").

### Permadeath
Dying on any floor returns the player to Floor 1. All progress lost. No checkpoints.

---

## NPCs (2 per dungeon floor)

### Vendor
- Appears in a dedicated room each floor.
- Sells **equipment only** (no buffs).
- Currency: gold dropped by enemies.

### Beggar (Monea)
- Appears in a dedicated room each floor.
- Give him 1 gold coin → random outcome:
  - **Good:** Drops a buff or item.
  - **Nothing:** Walks away.
  - **Bad:** Stabs you for significant HP loss.

---

## Equipment Slots (5)

| Slot     | Flavor Name |
|----------|-------------|
| Head     | Cabeza      |
| Chest    | Pecho       |
| Legs     | Pantalones  |
| Feet     | Zapatos     |
| Weapon   | Pluma (feather — all weapons are quills) |

Equipment is **stat-only**, no visual cosmetics. Stats include: HP, defense, spell damage per type, speed, gold find, etc.

---

## Enemies

### Regular Enemies
- Spawn randomly throughout dungeon floors.
- Drop **gold** and **items** on death (chance-based).
- Each has a randomly rolled resistance profile (see above).
- **3 abilities** available to each enemy:
  1. **Heal** — Restores a portion of their own HP.
  2. **DOT** — Applies a damage-over-time effect to the player.
  3. **Power Strike** — Deals slightly more damage than a normal attack.

### Mini-Boss
- Spawns in the **room adjacent to the exit/stairs** of every floor.
- Is a regular enemy type but with:
  - Significantly more HP.
  - Increased base damage.
- Must be defeated to access the stairs.

### Dragon (Final Boss)
- Appears on the last floor.
- **Only has damage abilities** — no self-healing.
- **Phase mechanic:** Every 25% HP lost, the Dragon rotates its resistance profile.
  - e.g., starts Immune to Dissonant → at 75% HP becomes Immune to Assonant → etc.
- Defeating the Dragon = win condition.

---

## Folder Structure

```
Assets/
  Scripts/
    Core/         GameManager, FloorManager, RunManager, DungeonOrchestrator
    Dungeon/      BSPGenerator, DungeonData, DungeonRenderer, BSPNode, RoomTag
    Player/       GridMover, PlayerStats, EquipmentManager
    Combat/       CombatManager, SpellInputHandler, ElementSystem, BuffSystem
    Enemies/      EnemyData, EnemyInstance, EnemySpawner, EnemyAI
    Items/        ItemData, ItemPickup, ItemSpawner, EquipmentData
    NPCs/         VendorNPC, BeggarNPC, NPCSpawner
    Buffs/        BuffData, BuffPool, BuffSelectionUI
    UI/           CombatUIController, HUDController, GameOverUI, VictoryUI,
                  LibroUI (spell book input display), BuffPickUI, ShopUI
  ScriptableObjects/
    Enemies/      EnemyBase (variants: Dissonant, Consonant, Assonant, MiniBoss)
    Items/        EquipmentItems (per slot), GoldPickup
    Buffs/        BuffDefinitions (15–20 buffs)
    Boss/         DragonBoss
  Prefabs/
    Dungeon/      Wall, Floor, Ceiling, Stairs, NPCRoom
    Enemies/      EnemyPrefab, MiniBossPrefab, DragonPrefab
    Items/        ItemPickupPrefab, GoldPickupPrefab
    NPCs/         VendorPrefab, BeggarPrefab
    VFX/          VFX_Assonant, VFX_Dissonant, VFX_Consonant
  Scenes/
    MainMenu, Game, GameOver, Victory
```

---

## Day-by-Day Schedule

| Day | Goal |
|-----|------|
| 1   | GameManager, FloorManager, RunManager, GridMover, Camera in empty scene |
| 2   | BSP generator + DungeonRenderer with room tagging (NPC rooms, mini-boss room) |
| 3   | Enemies in dungeon, collision triggers combat, spell input system (LibroUI) |
| 4   | Full combat loop: input → element check → damage with resistances → enemy dies → back to exploring |
| 5   | Equipment slots, vendor NPC, beggar NPC, gold economy |
| 6   | Buff selection between floors, permadeath, mini-boss, Dragon boss with phase rotation |
| 7   | Polish, HUD, audio, main menu, game over / victory screens, WebGL build |

---

## Cut List (if running out of time, in order)

1. Timed turn window — make turns fully static (no time pressure)
2. Beggar NPC — keep vendor only
3. Equipment visual feedback — items just show stat numbers
4. Mini-boss room lock — treat mini-boss as a regular enemy on that tile
5. Dragon phase rotation — Dragon just has more HP
6. Buff selection UI polish — show 3 text buttons, no card animations
7. Ceiling rendering — player won't notice much

---

## Tasks

### PHASE 1 — Project Setup
> Day 1 morning

- [ ] **1.1** Create Unity project with **URP template**
- [ ] **1.2** Install **DOTween** from Asset Store or `dotween.demigiant.com`
- [ ] **1.3** Create folder structure under `Assets/` as listed above
- [ ] **1.4** Create scenes: `MainMenu`, `Game`, `GameOver`, `Victory`
- [ ] **1.5** Add `GameManager` to the `Game` scene as a persistent singleton

---

### PHASE 2 — Core Architecture & State Machine
> Day 1

- [ ] **2.1** Create `GameState` enum: `MainMenu`, `Exploring`, `InCombat`, `BuffSelection`, `Shopping`, `GameOver`, `Victory`
- [ ] **2.2** Implement `GameManager` singleton with `SetState()`, `StartNewRun()`, `GameOver()`, `Victory()`

```csharp
// Scripts/Core/GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public event System.Action<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        DG.Tweening.DOTween.Init();
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void StartNewRun()
    {
        RunManager.Instance.ResetRun();
        SceneManager.LoadScene("Game");
        SetState(GameState.Exploring);
    }

    public void GameOver() => SceneManager.LoadScene("GameOver");
    public void Victory()  => SceneManager.LoadScene("Victory");
}
```

- [ ] **2.3** Implement `RunManager` singleton — tracks current floor, player buffs, gold, and handles floor transitions + buff selection trigger

```csharp
// Scripts/Core/RunManager.cs
using UnityEngine;
using System.Collections.Generic;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }
    public int CurrentFloor { get; private set; } = 1;
    public int Gold { get; private set; } = 0;
    public List<BuffData> ActiveBuffs { get; private set; } = new();
    public const int MaxFloors = 5;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetRun()
    {
        CurrentFloor = 1;
        Gold = 0;
        ActiveBuffs.Clear();
    }

    public void AddGold(int amount) => Gold += amount;
    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        return true;
    }

    public void NextFloor()
    {
        CurrentFloor++;
        if (CurrentFloor > MaxFloors)
            GameManager.Instance.Victory();
        else
            GameManager.Instance.SetState(GameState.BuffSelection);
    }

    public float GetTurnTimeLimit() => Mathf.Max(3f, 6f - (CurrentFloor - 1) * 0.5f);
}
```

---

### PHASE 3 — Grid Movement & First-Person Camera
> Day 1

- [ ] **3.1** Create `Player` GameObject in `Game` scene with child `Camera` at local `(0, 0, 0)`. Eye height handled in `GridToWorld()` (Y = 1.6).
- [ ] **3.2** Set camera **FOV 70–75**.
- [ ] **3.3** Implement `GridMover` with DOTween. Input blocked when `GameState != Exploring`.

```csharp
// Scripts/Player/GridMover.cs
using UnityEngine;
using DG.Tweening;

public class GridMover : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float turnSpeed = 10f;
    private Vector2Int gridPos;
    private float facing = 0f; // 0=North 90=East 180=South 270=West
    private bool isMoving = false;
    public const float CellSize = 4f;

    void Update()
    {
        if (isMoving) return;
        if (GameManager.Instance.CurrentState != GameState.Exploring) return;
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            TryMove(GetForwardDir());
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            TryMove(-GetForwardDir());
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            TurnBy(-90f);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            TurnBy(90f);
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int target = gridPos + dir;
        if (!DungeonRenderer.Instance.IsWalkable(target)) return;

        EnemyInstance enemy = EnemySpawner.Instance.GetEnemyAt(target);
        if (enemy != null) { CombatManager.Instance.StartCombat(enemy); return; }

        ItemPickup item = ItemSpawner.Instance.GetItemAt(target);
        if (item != null) item.Collect();

        RoomTag tag = DungeonRenderer.Instance.GetRoomTag(target);
        if (tag == RoomTag.Vendor)  { VendorNPC.Instance.OpenShop(); return; }
        if (tag == RoomTag.Beggar)  { BeggarNPC.Instance.Interact(); return; }
        if (tag == RoomTag.Stairs)  { RunManager.Instance.NextFloor(); return; }

        isMoving = true;
        gridPos = target;
        transform.DOMove(GridToWorld(target), 1f / moveSpeed)
                 .SetEase(Ease.InOutSine)
                 .OnComplete(() => isMoving = false);
    }

    void TurnBy(float degrees)
    {
        isMoving = true;
        facing = (facing + degrees + 360f) % 360f;
        transform.DORotate(new Vector3(0, facing, 0), 1f / turnSpeed, RotateMode.Fast)
                 .SetEase(Ease.InOutSine)
                 .OnComplete(() => isMoving = false);
    }

    Vector2Int GetForwardDir() => Mathf.RoundToInt(facing) switch
    {
        0   => Vector2Int.up,
        90  => Vector2Int.right,
        180 => Vector2Int.down,
        270 => Vector2Int.left,
        _   => Vector2Int.up
    };

    public void SetGridPosition(Vector2Int pos)
    {
        gridPos = pos;
        transform.position = GridToWorld(pos);
    }

    public static Vector3 GridToWorld(Vector2Int pos)
        => new Vector3(pos.x * CellSize, 1.6f, pos.y * CellSize);
}
```

---

### PHASE 4 — BSP Dungeon Generation
> Day 2

- [ ] **4.1** Create `CellType` enum and `RoomTag` enum. Create `DungeonData`.

```csharp
// Scripts/Dungeon/DungeonData.cs
using UnityEngine;
using System.Collections.Generic;

public enum CellType { Wall, Floor, Corridor }
public enum RoomTag  { None, PlayerSpawn, Vendor, Beggar, MiniBoss, Stairs }

public class DungeonData
{
    public int Width, Height;
    public CellType[,] Cells;
    public RoomTag[,]  Tags;
    public Vector2Int PlayerSpawn;
    public Vector2Int StairsPos;
    public List<Vector2Int> EnemySpawns = new();
    public List<Vector2Int> ItemSpawns  = new();

    public DungeonData(int w, int h)
    {
        Width = w; Height = h;
        Cells = new CellType[w, h];
        Tags  = new RoomTag[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                Cells[x, y] = CellType.Wall;
    }

    public bool IsFloor(Vector2Int p) =>
        p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Height &&
        (Cells[p.x, p.y] == CellType.Floor || Cells[p.x, p.y] == CellType.Corridor);
}
```

- [ ] **4.2** Implement `BSPNode` helper.
- [ ] **4.3** Implement `BSPGenerator`:
  - Standard BSP split + room creation + corridor connections.
  - After generating rooms, tag: **PlayerSpawn room**, **Stairs room** (with MiniBoss tag on entry tile), **Vendor room**, **Beggar room**.
  - Remaining rooms get enemy/item spawns.
- [ ] **4.4** Implement `DungeonRenderer`: instantiates wall/floor/ceiling prefabs from `DungeonData`.
- [ ] **4.5** Implement `DungeonOrchestrator`: calls Generate → Render → PlaceEnemies → PlaceItems → PlaceNPCs on scene load and floor transition.

---

### PHASE 5 — Element System
> Day 3

- [ ] **5.1** Define `SpellType` enum: `Assonant`, `Dissonant`, `Consonant`.
- [ ] **5.2** Implement `ElementSystem` static class with `GetMultiplier(SpellType attack, ResistanceTier tier)`.

```csharp
// Scripts/Combat/ElementSystem.cs
public enum SpellType { Assonant, Dissonant, Consonant }

// Dissonant beats Consonant
// Consonant beats Assonant
// Assonant beats Dissonant
public enum ResistanceTier { Immune, Normal, Weak }

public static class ElementSystem
{
    // Returns damage multiplier: 0 (immune), 1 (normal), 2 (weak)
    public static float GetMultiplier(ResistanceTier tier) => tier switch
    {
        ResistanceTier.Immune => 0f,
        ResistanceTier.Normal => 1f,
        ResistanceTier.Weak   => 2f,
        _ => 1f
    };

    // Returns which element beats the given element
    public static SpellType GetCounter(SpellType e) => e switch
    {
        SpellType.Assonant    => SpellType.Consonant,
        SpellType.Dissonant => SpellType.Assonant,
        SpellType.Consonant  => SpellType.Dissonant,
        _ => e
    };
}
```

- [ ] **5.3** Implement `EnemyResistance` — randomly assigns one Immune/Normal/Weak per element (each tier used exactly once).

```csharp
// Scripts/Enemies/EnemyResistance.cs
using System.Collections.Generic;
using UnityEngine;

public class EnemyResistance
{
    public Dictionary<SpellType, ResistanceTier> Tiers = new();

    public EnemyResistance()
    {
        var elements = new List<SpellType>
            { SpellType.Assonant, SpellType.Dissonant, SpellType.Consonant };
        var tiers = new List<ResistanceTier>
            { ResistanceTier.Immune, ResistanceTier.Normal, ResistanceTier.Weak };

        // Shuffle tiers
        for (int i = tiers.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (tiers[i], tiers[j]) = (tiers[j], tiers[i]);
        }

        for (int i = 0; i < elements.Count; i++)
            Tiers[elements[i]] = tiers[i];
    }

    public ResistanceTier Get(SpellType e) => Tiers[e];
}
```

---

### PHASE 6 — Spell Input System (LibroUI)
> Day 3

- [ ] **6.1** Define spell sequences as `SpellDefinition` ScriptableObjects: `SpellType`, `KeyCode[]` sequence, name, description.
- [ ] **6.2** Implement `SpellInputHandler`:
  - Active only during `InCombat` state.
  - Tracks current input progress against all defined sequences.
  - On match → raises `OnSpellCast(SpellDefinition)`.
  - On wrong key → `OnInputError`.
  - Optional: time window per turn (`RunManager.GetTurnTimeLimit()`).
- [ ] **6.3** Implement `LibroUI`:
  - Displays the 3 spell sequences with directional arrow icons.
  - Highlights progress as keys are pressed.
  - Shows a turn timer bar if timed turns are enabled.

---

### PHASE 7 — Combat System
> Day 4

- [ ] **7.1** Implement `PlayerStats` with HP, maxHP, base damage per element, defense.
- [ ] **7.2** Implement `EnemyData` ScriptableObject: name, HP, base damage, ability weights.
- [ ] **7.3** Implement `EnemyInstance`: holds runtime state (current HP, `EnemyResistance`, active DOTs), exposes `TakeDamage()`, `Act()`.
- [ ] **7.4** Implement enemy `Act()` — chooses from 3 abilities based on weighted random:
  - **Heal:** Restore X% of max HP.
  - **DOT:** Apply a damage-over-time debuff to the player (ticks for N turns).
  - **Power Strike:** Deal `baseDamage * 1.5` to player.
- [ ] **7.5** Implement `CombatManager`:
  - Transitions game to `InCombat`.
  - Player turn: waits for `SpellInputHandler` to fire, applies damage with element multiplier.
  - Enemy turn: calls `EnemyInstance.Act()`.
  - On enemy death: drop gold/item (chance), return to `Exploring`.
  - On player death: `GameManager.GameOver()`.

```csharp
// Scripts/Combat/CombatManager.cs (skeleton)
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }
    private EnemyInstance currentEnemy;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartCombat(EnemyInstance enemy)
    {
        currentEnemy = enemy;
        GameManager.Instance.SetState(GameState.InCombat);
        // Show combat UI, start player turn
    }

    public void PlayerCastSpell(SpellDefinition spell)
    {
        float mult = ElementSystem.GetMultiplier(currentEnemy.Resistance.Get(spell.Element));
        int dmg = Mathf.RoundToInt(PlayerStats.Instance.GetSpellDamage(spell.Element) * mult);
        currentEnemy.TakeDamage(dmg);
        if (currentEnemy.IsDead) EndCombat(true);
        else StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        currentEnemy.Act(); // applies effect to player
        if (PlayerStats.Instance.IsDead) GameManager.Instance.GameOver();
        else StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        // reset input handler, update UI
    }

    void EndCombat(bool playerWon)
    {
        if (playerWon)
        {
            currentEnemy.DropLoot();
            EnemySpawner.Instance.RemoveEnemy(currentEnemy);
        }
        GameManager.Instance.SetState(GameState.Exploring);
    }
}
```

---

### PHASE 8 — Equipment System
> Day 5

- [ ] **8.1** Create `EquipmentSlot` enum: `Head`, `Chest`, `Legs`, `Feet`, `Feather`.
- [ ] **8.2** Create `EquipmentData` ScriptableObject: slot, stat bonuses (HP, defense, spell damage per element, gold find, etc.).
- [ ] **8.3** Implement `EquipmentManager` on Player: holds one `EquipmentData` per slot, exposes `Equip()`, `GetStatBonus()`.
- [ ] **8.4** `PlayerStats` reads bonuses from `EquipmentManager` when calculating damage, defense, max HP.

---

### PHASE 9 — NPCs
> Day 5

#### Vendor

- [ ] **9.1** Implement `VendorNPC`:
  - Generates a stock of 3–4 random `EquipmentData` items on floor generation.
  - `OpenShop()` triggers `ShopUI` and sets state to `Shopping`.
  - Player can buy items with gold (`RunManager.SpendGold()`).

#### Beggar

- [ ] **9.2** Implement `BeggarNPC`:
  - `Interact()` prompts "Give 1 gold?" 
  - If player has gold and accepts: `RunManager.SpendGold(1)`, roll outcome:
    - 40% → Give random buff or item.
    - 30% → Nothing.
    - 30% → Stab player for 30–40% of max HP.
  - One interaction per floor.

---

### PHASE 10 — Buff System
> Day 6

- [ ] **10.1** Create `BuffData` ScriptableObject: display name, description, effect type (enum), value.
- [ ] **10.2** Create a `BuffPool` with 15–20 buff definitions.
- [ ] **10.3** Implement `BuffSystem` on Player: stores active buffs, exposes `ApplyBuff()`, `GetBonusFor()`.
- [ ] **10.4** Implement `BuffSelectionUI`: triggered when `GameState == BuffSelection`. Shows 3 random `BuffData` cards. On selection, calls `BuffSystem.ApplyBuff()` then triggers next floor generation.

Example buffs:
```
+15% Assonant damage
+15% Dissonant damage
+15% Consonant damage
+20 max HP
+10 base defense
-25% DOT duration received
+1 gold per kill
Heal 20 HP on combat start
```

---

### PHASE 11 — Mini-Boss & Dragon Boss
> Day 6

#### Mini-Boss

- [ ] **11.1** Mini-boss is a regular `EnemyData` with `IsMini = true` flag: HP ×2.5, damage ×2.
- [ ] **11.2** `BSPGenerator` tags the room before the stairs as `MiniBoss`. `EnemySpawner` places a mini-boss there.
- [ ] **11.3** Stairs tile is **locked** until mini-boss is defeated (`EnemySpawner.IsRoomClear(StairsAdjacentRoom)`).

#### Dragon Boss

- [ ] **11.4** Create `DragonBossData` ScriptableObject: high HP, damage, phase thresholds `[0.75, 0.50, 0.25]`.
- [ ] **11.5** Implement `DragonBossInstance extends EnemyInstance`:
  - On `TakeDamage()`: check if HP crossed a 25% threshold → rotate resistance profile.
  - Resistance rotates in order: profile A → B → C → D (4 profiles, each with a different immune element cycling through).
  - Only damage abilities (Power Strike × 3 variants with different elements), no healing.
- [ ] **11.6** Dragon spawns on Floor 5. Victory triggers on Dragon death.

---

### PHASE 12 — HUD, Audio & Polish
> Day 7

- [ ] **12.1** HUD: HP bar, current floor, gold count, active buff icons, DOT indicator.
- [ ] **12.2** Main menu scene with Start / Quit.
- [ ] **12.3** Game Over screen: floor reached, prompt to restart.
- [ ] **12.4** Victory screen: congratulations, run stats.
- [ ] **12.5** Audio: ambient dungeon loop, spell cast SFX per element type, enemy hurt/death, NPC interaction sounds.
- [ ] **12.6** WebGL build — test in browser, upload.

---

## Notes & Open Questions

- **Turn timing:** Fully static turns (no timer) is the safe fallback. Add timer only if time permits.
- **Dragon resistance rotation:** Needs playtesting — 4 phases with different immune elements means the player may need to swap spell types mid-fight. Ensure all 3 spell types remain useful.
- **Beggar balance:** 30% stab chance is punishing. May need to tune if playtesters never interact with the Beggar.
- **Buff count:** A 5-floor run = 4 buff selections. With 15–20 buff definitions, repetition is possible. Add a "no duplicates" check if time permits.

---

## Unity Wiring Guide

### 1. Scenes

Create and save 4 scenes, add all to **File → Build Settings** in this order:

| Index | Scene |
|-------|-------|
| 0 | MainMenu |
| 1 | Game |
| 2 | GameOver |
| 3 | Victory |

Scene name strings in code (`SceneManager.LoadScene(...)`) must match exactly.

---

### 2. Persistent Singletons GameObject

In the **Game** scene, create one empty GameObject named `_Managers`. Add all of these components to it:

- `GameManager`
- `FloorManager`
- `PlayerStats`
- `AudioManager`
- `VendorNPC`
- `BeggarNPC`

For `AudioManager`, add two child GameObjects (`SFX`, `Music`), each with an `AudioSource`. Wire them to `Sfx Source` and `Music Source` in the inspector, then assign all audio clips.

`_Managers` uses `DontDestroyOnLoad` and persists for the entire session.

---

### 3. Game Scene Hierarchy

```
_Managers               ← persistent singletons
_Dungeon
  BSPGenerator          ← BSPGenerator component
  DungeonRenderer       ← DungeonRenderer component
  DungeonOrchestrator   ← DungeonOrchestrator component
  EnemySpawner          ← EnemySpawner component
  ItemSpawner           ← ItemSpawner component
  Combat                ← CombatManager + SongInputHandler components
Player                  ← GridMover component
  Main Camera           ← tag: MainCamera, FOV: 73, local pos (0,0,0)
Canvas                  ← Screen Space Overlay, CanvasScaler 1920×1080
  HUD
  CombatPanel
  BuffSelectionPanel
  ShopPanel
  BeggarPanel
EventSystem
```

---

### 4. ScriptableObjects to Create

#### EnemyData — right-click → Create → Mockery → EnemyData

| Asset | enemyName | maxHP | attackDamage | isBoss | healW | dotW | powerW | goldMin | goldMax | dropChance |
|-------|-----------|-------|--------------|--------|-------|------|--------|---------|---------|------------|
| Enemy_Grito | Grito Demoniaco | 20 | 5 | false | 0.2 | 0.4 | 0.4 | 2 | 6 | 0.25 |
| Enemy_Susurro | Susurro Maldito | 15 | 7 | false | 0.2 | 0.4 | 0.4 | 2 | 5 | 0.25 |
| Enemy_Eco | Eco del Abismo | 25 | 4 | false | 0.3 | 0.3 | 0.4 | 3 | 7 | 0.3 |
| Enemy_Dragon | El Dragon | 120 | 15 | true | 0 | 0.3 | 0.7 | 0 | 0 | 0 |

#### ItemData — right-click → Create → Mockery → ItemData

| Asset | itemName | slot | stat bonus | buyPrice |
|-------|----------|------|------------|----------|
| Item_SombreroRoto | Sombrero Roto | Head | hpBonus=5 | 4 |
| Item_TunicaVieja | Túnica Vieja | Chest | defenseBonus=2 | 5 |
| Item_PantalonRemendado | Pantalón Remendado | Legs | hpBonus=3, defenseBonus=1 | 4 |
| Item_ZapatosDeBardo | Zapatos de Bardo | Feet | AssonantBonus=3 | 5 |
| Item_PlumaDelDiablo | Pluma del Diablo | Feather | DissonantBonus=5 | 8 |
| Item_PlumaDeLuz | Pluma de Luz | Feather | ConsonantBonus=5 | 8 |

#### BuffData — right-click → Create → Mockery → BuffData

| Asset | buffName | effectType | element | value |
|-------|----------|------------|---------|-------|
| Buff_HP | Alma Robusta | HPBonus | — | 15 |
| Buff_Def | Piel Gruesa | DefenseBonus | — | 3 |
| Buff_Ason | Voz Assonant | SpellDamageBonus | Assonant | 5 |
| Buff_Disc | Cacofonía | SpellDamageBonus | Dissonant | 5 |
| Buff_Cons | Armonía Pura | SpellDamageBonus | Consonant | 5 |
| Buff_DOT | Resistencia al Veneno | DOTResistance | — | 2 |
| Buff_Heal | Adrenalina | HealOnCombatStart | — | 8 |
| Buff_Gold | Dedos de Oro | GoldBonus | — | 2 |

Fill in `description` on each — shown in `BuffSelectionUI`.

---

### 5. Prefabs

#### EnemyPrefab
- Empty GameObject + `EnemyInstance` component
- Optional: child SpriteRenderer for in-world representation
- Save to `Prefabs/Enemies/`

#### ItemPickupPrefab
- Empty GameObject + `ItemPickup` component
- Child: small cube or sprite so it's visible in the dungeon
- Save to `Prefabs/Items/`

#### Dungeon tile prefabs (save to `Prefabs/Dungeon/`)

| Prefab | Setup |
|--------|-------|
| WallPrefab | Quad facing +Z, 4u × 4u |
| FloorPrefab | Quad rotated −90° X, 4u × 4u |
| CeilingPrefab | Same as floor (renderer flips on spawn) |
| StairsPrefab | Any visual marker — ramp or colored tile |

---

### 6. Inspector Field Wiring

#### BSPGenerator
```
Map Width          40
Map Height         40
Min Leaf Size       8
Max Leaf Size      16
Enemies Per Floor   4
Items Per Floor     3
```

#### DungeonRenderer
```
Wall Prefab    → WallPrefab
Floor Prefab   → FloorPrefab
Ceiling Prefab → CeilingPrefab
Stairs Prefab  → StairsPrefab
```

#### EnemySpawner
```
Basic Enemies  → [Enemy_Grito, Enemy_Susurro, Enemy_Eco]
Boss Data      → Enemy_Dragon
Enemy Prefab   → EnemyPrefab
```

#### ItemSpawner
```
Item Pool          → [all ItemData assets]
Item Pickup Prefab → ItemPickupPrefab
```

#### VendorNPC (on _Managers)
```
Item Pool   → [all ItemData assets]
Stock Size  3
```

#### BeggarNPC (on _Managers)
```
Chance Good      0.4
Chance Nothing   0.3
Stab HP Percent  0.35
Buff Pool        → [all BuffData assets]
Item Pool        → [all ItemData assets]
```

#### PlayerStats (on _Managers)
```
Max HP             30
Base Spell Damage  10
```

---

### 7. Canvas & UI Wiring

Canvas settings: **Screen Space — Overlay**, CanvasScaler → Scale With Screen Size, Reference 1920×1080, Match 0.5.

#### HUD panel — add `HUDController`
```
HUD
  HPBar          ← Slider
  FloorLabel     ← TMP   "Piso 1 / 5"
  GoldLabel      ← TMP   "Oro: 0"
  DOTLabel       ← TMP   "⚠ MALDITO"  (starts hidden — script toggles it)
```
Wire `HUDController`:
```
Hp Bar      → HPBar
Floor Label → FloorLabel
Gold Label  → GoldLabel
Dot Label   → DOTLabel
```

#### CombatPanel — add `CombatUIController`, **start inactive**
```
CombatPanel
  EnemySprite          ← Image
  EnemyNameText        ← TMP
  EnemyHPSlider        ← Slider
  ResistanceLabels/
    LabelAssonant      ← TMP  "Assonant: ?"
    LabelDissonant   ← TMP  "Dissonant: ?"
    LabelConsonant    ← TMP  "Consonant: ?"
  KeyIcons/
    Icon0 Icon1 Icon2 Icon3   ← Image ×4
  TimerBar             ← Slider
  ResultText           ← TMP
  SongButtonsGroup/
    BtnAssonant        ← Button
    BtnDissonant     ← Button
    BtnConsonant      ← Button
```
Wire `CombatUIController`:
```
Combat Panel       → CombatPanel GameObject
Enemy Sprite       → EnemySprite
Enemy Name Text    → EnemyNameText
Enemy HP Slider    → EnemyHPSlider
Resistance Labels  → [LabelAssonant, LabelDissonant, LabelConsonant]  (size 3)
Key Icons          → [Icon0, Icon1, Icon2, Icon3]  (size 4)
Dir Sprites        → [UpSprite, DownSprite, LeftSprite, RightSprite]  (size 4)
Timer Bar          → TimerBar
Result Text        → ResultText
Song Buttons Group → SongButtonsGroup
```
Button OnClick events (set in inspector):
```
BtnAssonant    → CombatUIController.OnSongSelected(0)
BtnDissonant → CombatUIController.OnSongSelected(1)
BtnConsonant  → CombatUIController.OnSongSelected(2)
```

#### BuffSelectionPanel — add `BuffSelectionUI`, **start inactive**
```
BuffSelectionPanel
  TitleText      ← TMP  "Elige un Buff"
  BuffButton0    ← Button
    BuffName0    ← TMP  (child)
    BuffDesc0    ← TMP  (child)
  BuffButton1 / BuffButton2  (same structure)
```
Wire `BuffSelectionUI`:
```
Panel            → BuffSelectionPanel
Buff Buttons     → [BuffButton0, BuffButton1, BuffButton2]
Buff Name Labels → [BuffName0, BuffName1, BuffName2]
Buff Desc Labels → [BuffDesc0, BuffDesc1, BuffDesc2]
Buff Pool        → [all BuffData assets]
```
No manual OnClick needed — listeners are added in code.

#### ShopPanel — add `ShopUI`, **start inactive**
```
ShopPanel
  GoldLabel      ← TMP
  ItemButton0    ← Button
    ItemName0 / ItemPrice0 / ItemDesc0  ← TMP children
  ItemButton1 / ItemButton2  (same structure)
  CloseButton    ← Button  "Cerrar"
```
Wire `ShopUI`:
```
Panel            → ShopPanel
Item Buttons     → [ItemButton0, ItemButton1, ItemButton2]
Item Name Labels → [ItemName0, ItemName1, ItemName2]
Item Price Labels → [ItemPrice0, ItemPrice1, ItemPrice2]
Item Desc Labels → [ItemDesc0, ItemDesc1, ItemDesc2]
Gold Label       → GoldLabel
Close Button     → CloseButton
```

#### BeggarPanel — add `BeggarUI`, **start inactive**
```
BeggarPanel
  MessageText    ← TMP
```
Wire `BeggarUI`:
```
Panel        → BeggarPanel
Message Text → MessageText
```

---

### 8. MainMenu Scene

Add `GameManager` (or the `_Managers` prefab) to the MainMenu scene — the singleton guard will destroy duplicates if it's already alive. Add a Canvas with a Start button wired to `GameManager.Instance.StartNewRun()`.

---

### 9. GameOver & Victory Scenes

Both use `GameOverUI`. Wire `Message Text`, `Floor Text`, and `Try Again Button` → `GameManager.Instance.StartNewRun()`. The script checks `GameState.Victory` to decide which message to display.

---

### 10. Pre-Play Checklist

**Build Settings**
- [ ] 4 scenes added in order: MainMenu(0), Game(1), GameOver(2), Victory(3)
- [ ] Scene name strings in code match exactly

**_Managers**
- [ ] All 6 components present
- [ ] VendorNPC.itemPool, BeggarNPC.buffPool, BeggarNPC.itemPool assigned

**_Dungeon**
- [ ] DungeonRenderer: all 4 prefabs wired
- [ ] EnemySpawner: basicEnemies (3), bossData, enemyPrefab
- [ ] ItemSpawner: itemPool, itemPickupPrefab
- [ ] CombatManager + SongInputHandler present in scene

**Canvas**
- [ ] CombatPanel, BuffSelectionPanel, ShopPanel, BeggarPanel all **start inactive**
- [ ] CombatUIController: 4 keyIcons, 3 resistanceLabels, 4 dirSprites, 3 spell buttons wired
- [ ] BuffSelectionUI.buffPool has ≥3 assets
- [ ] ShopUI and BeggarUI have their singleton instances in the scene

**Player**
- [ ] GridMover on Player root
- [ ] Child camera tagged MainCamera, FOV 73

**ScriptableObjects**
- [ ] ≥3 EnemyData (regular) + 1 boss
- [ ] ≥5 ItemData (one per slot minimum)
- [ ] ≥3 BuffData

---

### 11. Quick Smoke Test Sequence

Once wired, verify this flow in Play mode:

1. Dungeon generates → player spawns, can walk around
2. Walk into enemy tile → CombatPanel opens, spell buttons visible
3. Press a spell button → 4 key icons appear, timer counts down
4. Input correct sequence (e.g. `W W D W` for Assonant) → resistance label updates from `?` to `DEBIL/NORMAL/INMUNE`
5. Enemy dies → panel closes, loot drops, gold increments in HUD
6. Walk into stairs room → mini-boss blocks the way
7. Defeat mini-boss → step on stairs → BuffSelectionPanel opens with 3 choices
8. Pick a buff → dungeon regenerates, Floor 2 begins
9. Walk into Vendor room → ShopPanel opens, items listed with prices
10. Walk into Beggar room → gold spent, outcome shown in BeggarPanel
11. Reach Floor 5 → Dragon spawns, resistance rotates at each 25% HP threshold
12. Kill Dragon → Victory scene loads
