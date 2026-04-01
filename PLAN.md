# DCJam — Bard Dungeon Crawler
## 7-Day Implementation Plan

---

## Game Summary

You play as a **Bard** descending into a demon-filled dungeon.
Combat uses **WASD/Arrow key sequences** to cast elemental songs.
Enemies have one element — choose the right counter or take the hit.
**5 floors**, procedurally generated, permadeath.

**Theme:** Elemental Rock Paper Scissors
**Engine:** Unity (URP)
**Key Package:** DOTween (smooth grid movement/rotation)

---

## Element Cycle

```
Fire → Wind → Earth → Water → Fire
(each element beats the one to its right)
```

### Song Sequences (6 keys, WASD or Arrow keys)

| Element | Sequence |
|---------|----------|
| Fire    | ↑ ↑ → ↑ ↑ → |
| Water   | ← ↓ ↓ ← ↓ ↓ |
| Earth   | ↓ ↓ → ↓ ↓ → |
| Wind    | ↑ ← → ↑ ← → |

### Combat Outcomes

| Input | Element Match | Result |
|-------|--------------|--------|
| Wrong keys | — | Lose turn, enemy attacks |
| Correct keys | Wrong element | Chip damage, enemy attacks |
| Correct keys | Right element | Full/bonus damage, enemy does NOT attack |

---

## Folder Structure

```
Assets/
  Scripts/
    Core/         GameManager, FloorManager, DungeonOrchestrator
    Dungeon/      BSPGenerator, DungeonData, DungeonRenderer, BSPNode
    Player/       GridMover, PlayerStats
    Combat/       CombatManager, SongInputHandler, ElementSystem
    Enemies/      EnemyData, EnemyInstance, EnemySpawner
    Items/        ItemData, ItemPickup, ItemSpawner
    UI/           CombatUIController, HUDController, GameOverUI
  ScriptableObjects/
    Enemies/      FireDemon, WaterDemon, EarthDemon, WindDemon, FloorBoss, FinalBoss
    Items/        HealthPotion, LuteString, SheetMusic_Fire/Water/Earth/Wind, Earplugs
  Prefabs/
    Dungeon/      Wall, Floor, Ceiling, Stairs
    Enemies/      EnemyPrefab
    Items/        ItemPickupPrefab
    VFX/          VFX_Fire, VFX_Water, VFX_Earth, VFX_Wind
  Scenes/
    MainMenu, Game, GameOver
```

---

## Day-by-Day Schedule

| Day | Goal |
|-----|------|
| 1 | GameManager, FloorManager, GridMover, Camera in empty scene |
| 2 | BSP generator + DungeonRenderer showing walkable dungeon |
| 3 | Enemies in dungeon, collision triggers combat screen |
| 4 | Full combat loop: song input → damage → enemy dies → back to exploring |
| 5 | Items, stats, floor transitions, death/win conditions |
| 6 | Boss, HUD, audio, main menu, game over screen |
| 7 | Polish, WebGL build, test, upload |

---

## Cut List (if running out of time, in order)

1. Flee mechanic — remove the button
2. Sheet Music / Earplugs passives — items exist but apply no effect
3. Boss phase switch — boss just has more HP
4. Mini-bosses per floor — replace with regular enemies
5. Ceiling rendering — player won't notice much
6. Smooth rotation — snap to 90° instantly instead

---

## Tasks

### PHASE 1 — Project Setup
> Day 1 morning

- [ ] **1.1** Create Unity project with **URP template**
- [ ] **1.2** Install **DOTween** from Asset Store or `dotween.demigiant.com`
- [ ] **1.3** Create folder structure under `Assets/` as listed above
- [ ] **1.4** Create three scenes: `MainMenu`, `Game`, `GameOver`
- [ ] **1.5** Add `GameManager` to the `Game` scene as a persistent singleton

---

### PHASE 2 — Core Architecture & State Machine
> Day 1

- [ ] **2.1** Create `GameState` enum (`MainMenu`, `Exploring`, `InCombat`, `GameOver`, `Victory`)
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
        DOTween.Init();
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void StartNewRun()
    {
        FloorManager.Instance.ResetFloor();
        SceneManager.LoadScene("Game");
        SetState(GameState.Exploring);
    }

    public void GameOver() => SceneManager.LoadScene("GameOver");
    public void Victory()  => SetState(GameState.Victory);
}
```

- [ ] **2.3** Implement `FloorManager` singleton with `CurrentFloor`, `NextFloor()`, `ResetFloor()`, and `GetInputTimeLimit()`

```csharp
// Scripts/Core/FloorManager.cs
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }
    public int CurrentFloor { get; private set; } = 1;
    public const int MaxFloors = 5;

    // Timer window shrinks per floor: 6s → 4s across 5 floors
    public float GetInputTimeLimit() => Mathf.Max(3f, 6f - (CurrentFloor - 1) * 0.5f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetFloor() => CurrentFloor = 1;

    public void NextFloor()
    {
        CurrentFloor++;
        if (CurrentFloor > MaxFloors)
            GameManager.Instance.Victory();
    }
}
```

---

### PHASE 3 — Grid Movement & First-Person Camera
> Day 1

**Why:** This is the most jam-rule-sensitive system. Must be correct before building the dungeon around it. Grid cells are **4 Unity units** wide.

- [ ] **3.1** Create a `Player` GameObject in the `Game` scene with a child `Camera` at local position `(0, 0, 0)`. Eye height is handled in `GridMover.GridToWorld()` (returns Y = 1.6).
- [ ] **3.2** Set camera **FOV to 70–75** for a classic dungeon crawler feel.
- [ ] **3.3** Implement `GridMover` with DOTween smooth movement and rotation. Input is blocked when `GameState != Exploring`.

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

        if (DungeonRenderer.Instance.IsStairs(target))
        {
            FloorManager.Instance.NextFloor();
            FindObjectOfType<DungeonOrchestrator>().GenerateFloor();
            return;
        }

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

- [ ] **3.4** Test: player moves forward/back, turns left/right, transitions are smooth. Movement is blocked at dungeon edges (add a test room temporarily).

---

### PHASE 4 — BSP Dungeon Generation
> Day 2

**Why:** All other systems (rendering, enemy placement, items) consume `DungeonData`. Generate data structure first, render second.

- [ ] **4.1** Create `DungeonData` class with `CellType[,]` grid, spawn positions, enemy/item lists.

```csharp
// Scripts/Dungeon/DungeonData.cs
using UnityEngine;
using System.Collections.Generic;

public enum CellType { Wall, Floor, Corridor }

public class DungeonData
{
    public int Width, Height;
    public CellType[,] Cells;
    public Vector2Int PlayerSpawn;
    public Vector2Int StairsPos;
    public List<Vector2Int> EnemySpawns = new();
    public List<Vector2Int> ItemSpawns  = new();

    public DungeonData(int w, int h)
    {
        Width = w; Height = h;
        Cells = new CellType[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                Cells[x, y] = CellType.Wall;
    }

    public bool IsFloor(Vector2Int p) =>
        p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Height &&
        (Cells[p.x, p.y] == CellType.Floor || Cells[p.x, p.y] == CellType.Corridor);
}
```

- [ ] **4.2** Implement `BSPNode` helper class.

```csharp
// Scripts/Dungeon/BSPNode.cs
using UnityEngine;

public class BSPNode
{
    public RectInt Bounds;
    public BSPNode Left, Right;
    public RectInt Room;
    public bool IsLeaf => Left == null && Right == null;
    public BSPNode(RectInt bounds) { Bounds = bounds; }
}
```

- [ ] **4.3** Implement `BSPGenerator` with `Split()`, `CreateRooms()`, `ConnectRooms()`, enemy/item/stairs placement.

```csharp
// Scripts/Dungeon/BSPGenerator.cs
using UnityEngine;
using System.Collections.Generic;

public class BSPGenerator : MonoBehaviour
{
    public static BSPGenerator Instance { get; private set; }

    [Header("Map Size")]
    public int mapWidth  = 40;
    public int mapHeight = 40;

    [Header("BSP Settings")]
    public int minLeafSize = 8;
    public int maxLeafSize = 16;

    [Header("Spawn Counts")]
    public int enemiesPerFloor = 6;
    public int itemsPerFloor   = 4;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public DungeonData Generate()
    {
        var data  = new DungeonData(mapWidth, mapHeight);
        var root  = new BSPNode(new RectInt(0, 0, mapWidth, mapHeight));
        var leaves = new List<BSPNode>();

        Split(root, leaves);
        CreateRooms(data, leaves);
        ConnectRooms(data, root);
        PlaceStairs(data, leaves);
        PlaceEnemies(data, leaves);
        PlaceItems(data, leaves);

        return data;
    }

    void Split(BSPNode node, List<BSPNode> leaves)
    {
        if (node.Bounds.width <= maxLeafSize && node.Bounds.height <= maxLeafSize)
        {
            leaves.Add(node); return;
        }

        bool splitH = Random.value > 0.5f;
        if (node.Bounds.width  > node.Bounds.height * 1.25f) splitH = false;
        if (node.Bounds.height > node.Bounds.width  * 1.25f) splitH = true;

        int max = (splitH ? node.Bounds.height : node.Bounds.width) - minLeafSize;
        if (max <= minLeafSize) { leaves.Add(node); return; }

        int split = Random.Range(minLeafSize, max);

        if (splitH)
        {
            node.Left  = new BSPNode(new RectInt(node.Bounds.x, node.Bounds.y,
                                                  node.Bounds.width, split));
            node.Right = new BSPNode(new RectInt(node.Bounds.x, node.Bounds.y + split,
                                                  node.Bounds.width, node.Bounds.height - split));
        }
        else
        {
            node.Left  = new BSPNode(new RectInt(node.Bounds.x, node.Bounds.y,
                                                  split, node.Bounds.height));
            node.Right = new BSPNode(new RectInt(node.Bounds.x + split, node.Bounds.y,
                                                  node.Bounds.width - split, node.Bounds.height));
        }

        Split(node.Left,  leaves);
        Split(node.Right, leaves);
    }

    void CreateRooms(DungeonData data, List<BSPNode> leaves)
    {
        bool first = true;
        foreach (var leaf in leaves)
        {
            int w = Random.Range(4, leaf.Bounds.width  - 2);
            int h = Random.Range(4, leaf.Bounds.height - 2);
            int x = leaf.Bounds.x + Random.Range(1, leaf.Bounds.width  - w - 1);
            int y = leaf.Bounds.y + Random.Range(1, leaf.Bounds.height - h - 1);
            leaf.Room = new RectInt(x, y, w, h);

            for (int rx = x; rx < x + w; rx++)
                for (int ry = y; ry < y + h; ry++)
                    data.Cells[rx, ry] = CellType.Floor;

            if (first) { data.PlayerSpawn = new Vector2Int(x + w / 2, y + h / 2); first = false; }
        }
    }

    void ConnectRooms(DungeonData data, BSPNode node)
    {
        if (node.Left == null || node.Right == null) return;
        ConnectRooms(data, node.Left);
        ConnectRooms(data, node.Right);

        Vector2Int a = GetRoomCenter(node.Left);
        Vector2Int b = GetRoomCenter(node.Right);
        CarveHorizontal(data, a, new Vector2Int(b.x, a.y));
        CarveVertical(data,   new Vector2Int(b.x, a.y), b);
    }

    void CarveHorizontal(DungeonData data, Vector2Int from, Vector2Int to)
    {
        int minX = Mathf.Min(from.x, to.x), maxX = Mathf.Max(from.x, to.x);
        for (int x = minX; x <= maxX; x++)
            if (data.Cells[x, from.y] == CellType.Wall)
                data.Cells[x, from.y] = CellType.Corridor;
    }

    void CarveVertical(DungeonData data, Vector2Int from, Vector2Int to)
    {
        int minY = Mathf.Min(from.y, to.y), maxY = Mathf.Max(from.y, to.y);
        for (int y = minY; y <= maxY; y++)
            if (data.Cells[from.x, y] == CellType.Wall)
                data.Cells[from.x, y] = CellType.Corridor;
    }

    Vector2Int GetRoomCenter(BSPNode node)
    {
        if (node.IsLeaf)
            return new Vector2Int(node.Room.x + node.Room.width  / 2,
                                  node.Room.y + node.Room.height / 2);
        return GetRoomCenter(Random.value > 0.5f ? node.Left : node.Right);
    }

    void PlaceStairs(DungeonData data, List<BSPNode> leaves)
    {
        // Last leaf = farthest from player spawn
        BSPNode last = leaves[leaves.Count - 1];
        data.StairsPos = new Vector2Int(
            last.Room.x + last.Room.width  / 2,
            last.Room.y + last.Room.height / 2
        );
    }

    void PlaceEnemies(DungeonData data, List<BSPNode> leaves)
    {
        for (int i = 1; i < leaves.Count && data.EnemySpawns.Count < enemiesPerFloor; i++)
        {
            var pos = new Vector2Int(leaves[i].Room.x + 1, leaves[i].Room.y + 1);
            if (pos != data.StairsPos) data.EnemySpawns.Add(pos);
        }
    }

    void PlaceItems(DungeonData data, List<BSPNode> leaves)
    {
        for (int i = 0; i < itemsPerFloor && i < leaves.Count - 1; i++)
        {
            var room = leaves[i + 1].Room;
            var pos  = new Vector2Int(room.x + room.width - 2, room.y + room.height - 2);
            if (!data.EnemySpawns.Contains(pos) && pos != data.StairsPos)
                data.ItemSpawns.Add(pos);
        }
    }
}
```

- [ ] **4.4** Test: call `Generate()` and log `PlayerSpawn`, `StairsPos`, enemy/item counts to verify output is valid.

---

### PHASE 5 — Dungeon Rendering
> Day 2–3

**Why:** You need to see and walk through the generated dungeon. Keep all prefabs as simple colored primitives for now — art comes last.

- [ ] **5.1** Create dungeon prefabs in Unity editor:
  - `Wall` — cube scaled `(4, 4, 0.2)`, gray material
  - `Floor` — plane scaled `(0.4, 1, 0.4)`, dark gray material
  - `Ceiling` — same as floor, flipped `(Euler 180,0,0)`, slightly lighter
  - `Stairs` — cube, distinct color (e.g. yellow), replace with real art later

- [ ] **5.2** Implement `DungeonRenderer` — instantiates floor/ceiling per walkable cell, walls on edges between walkable and non-walkable cells.

```csharp
// Scripts/Dungeon/DungeonRenderer.cs
using UnityEngine;
using System.Collections.Generic;

public class DungeonRenderer : MonoBehaviour
{
    public static DungeonRenderer Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject ceilingPrefab;
    public GameObject stairsPrefab;

    private DungeonData currentData;
    private List<GameObject> spawnedObjects = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Render(DungeonData data)
    {
        currentData = data;
        foreach (var obj in spawnedObjects) Destroy(obj);
        spawnedObjects.Clear();

        float cell = GridMover.CellSize;

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                var pos2D = new Vector2Int(x, y);
                if (!data.IsFloor(pos2D)) continue;

                Vector3 worldPos = new Vector3(x * cell, 0, y * cell);

                Spawn(floorPrefab,   worldPos,                     Quaternion.identity);
                Spawn(ceilingPrefab, worldPos + Vector3.up * cell, Quaternion.Euler(180, 0, 0));

                if (pos2D == data.StairsPos)
                    Spawn(stairsPrefab, worldPos, Quaternion.identity);

                SpawnWallIfNeeded(data, x, y, -1,  0, 270f);
                SpawnWallIfNeeded(data, x, y,  1,  0,  90f);
                SpawnWallIfNeeded(data, x, y,  0, -1, 180f);
                SpawnWallIfNeeded(data, x, y,  0,  1,   0f);
            }
        }
    }

    void SpawnWallIfNeeded(DungeonData data, int x, int y, int dx, int dy, float rotY)
    {
        if (!data.IsFloor(new Vector2Int(x + dx, y + dy)))
        {
            float cell = GridMover.CellSize;
            Vector3 wallPos = new Vector3(
                (x + dx * 0.5f) * cell,
                cell * 0.5f,
                (y + dy * 0.5f) * cell
            );
            Spawn(wallPrefab, wallPos, Quaternion.Euler(0, rotY, 0));
        }
    }

    GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var obj = Instantiate(prefab, pos, rot, transform);
        spawnedObjects.Add(obj);
        return obj;
    }

    public bool IsWalkable(Vector2Int pos) => currentData != null && currentData.IsFloor(pos);
    public bool IsStairs(Vector2Int pos)   => currentData != null && pos == currentData.StairsPos;
}
```

- [ ] **5.3** Test: generate dungeon, render it, walk through it. Verify walls appear on correct edges, no gaps.

---

### PHASE 6 — Enemy System
> Day 3

- [ ] **6.1** Create `EnemyData` ScriptableObject.

```csharp
// Scripts/Enemies/EnemyData.cs
using UnityEngine;

[CreateAssetMenu(menuName = "DCJam/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public Element element;
    public Element secondPhaseElement; // boss only
    public int maxHP;
    public int attackDamage;
    public bool isBoss;
    public Sprite sprite;              // shown on combat screen
    public GameObject elementVFXPrefab; // colored particle in dungeon
}
```

- [ ] **6.2** Create ScriptableObject assets: `FireDemon`, `WaterDemon`, `EarthDemon`, `WindDemon`, `FloorBoss`, `FinalBoss`.
- [ ] **6.3** Implement `EnemyInstance` — exists in dungeon on a grid cell, shows element VFX.

```csharp
// Scripts/Enemies/EnemyInstance.cs
using UnityEngine;

public class EnemyInstance : MonoBehaviour
{
    public EnemyData data;
    public int CurrentHP { get; private set; }
    public Vector2Int GridPos { get; private set; }

    public void Init(EnemyData d, Vector2Int pos)
    {
        data = d;
        CurrentHP = d.maxHP;
        GridPos = pos;
        transform.position = GridMover.GridToWorld(pos);
        if (d.elementVFXPrefab)
            Instantiate(d.elementVFXPrefab, transform);
    }

    // Returns true if enemy died
    public bool TakeDamage(int amount)
    {
        CurrentHP -= amount;
        return CurrentHP <= 0;
    }
}
```

- [ ] **6.4** Implement `EnemySpawner` — spawns enemies from `DungeonData`, tracks active enemies, handles removal.

```csharp
// Scripts/Enemies/EnemySpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    public EnemyData[] basicEnemies;  // assign in inspector: one per element
    public EnemyData   bossFinalData;
    public GameObject  enemyPrefab;

    private List<EnemyInstance> activeEnemies = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnEnemies(DungeonData data, int floor)
    {
        foreach (var e in activeEnemies) if (e) Destroy(e.gameObject);
        activeEnemies.Clear();

        bool isFinalFloor = floor >= FloorManager.MaxFloors;

        for (int i = 0; i < data.EnemySpawns.Count; i++)
        {
            EnemyData d = (isFinalFloor && i == 0)
                ? bossFinalData
                : basicEnemies[Random.Range(0, basicEnemies.Length)];

            var go = Instantiate(enemyPrefab, transform);
            var ei = go.GetComponent<EnemyInstance>();
            ei.Init(d, data.EnemySpawns[i]);
            activeEnemies.Add(ei);
        }
    }

    public EnemyInstance GetEnemyAt(Vector2Int pos)
        => activeEnemies.Find(e => e != null && e.GridPos == pos);

    public void RemoveEnemy(EnemyInstance e)
    {
        activeEnemies.Remove(e);
        Destroy(e.gameObject);
    }
}
```

- [ ] **6.5** Test: generate floor, walk toward an enemy cell, verify `GetEnemyAt()` returns the correct instance.

---

### PHASE 7 — Element System & Combat Manager
> Day 3–4

- [ ] **7.1** Implement `ElementSystem` static class.

```csharp
// Scripts/Combat/ElementSystem.cs
public enum Element { Fire, Water, Earth, Wind }

public static class ElementSystem
{
    // Cycle: Fire > Wind > Earth > Water > Fire
    public static bool Beats(Element attacker, Element defender) =>
        (attacker == Element.Fire  && defender == Element.Wind)  ||
        (attacker == Element.Wind  && defender == Element.Earth) ||
        (attacker == Element.Earth && defender == Element.Water) ||
        (attacker == Element.Water && defender == Element.Fire);

    public static Element GetCounter(Element e) => e switch
    {
        Element.Fire  => Element.Water,
        Element.Water => Element.Wind,
        Element.Wind  => Element.Earth,
        Element.Earth => Element.Fire,
        _             => Element.Fire
    };

    // 2 = super effective | 1 = neutral | 0 = not effective
    public static int GetMultiplier(Element attack, Element defense)
    {
        if (Beats(attack, defense)) return 2;
        if (Beats(defense, attack)) return 0;
        return 1;
    }
}
```

- [ ] **7.2** Implement `SongInputHandler` — 6-key sequence matching, timer, fires `OnSuccess`/`OnFail` events.

```csharp
// Scripts/Combat/SongInputHandler.cs
using UnityEngine;
using System.Collections.Generic;

public class SongInputHandler : MonoBehaviour
{
    public static SongInputHandler Instance { get; private set; }

    public enum Dir { Up, Down, Left, Right }

    public static readonly Dictionary<Element, Dir[]> Songs = new()
    {
        { Element.Fire,  new[]{ Dir.Up, Dir.Up, Dir.Right, Dir.Up, Dir.Up, Dir.Right } },
        { Element.Water, new[]{ Dir.Left, Dir.Down, Dir.Down, Dir.Left, Dir.Down, Dir.Down } },
        { Element.Earth, new[]{ Dir.Down, Dir.Down, Dir.Right, Dir.Down, Dir.Down, Dir.Right } },
        { Element.Wind,  new[]{ Dir.Up, Dir.Left, Dir.Right, Dir.Up, Dir.Left, Dir.Right } },
    };

    public Element ActiveSong    { get; private set; }
    public int     CurrentIndex  { get; private set; }
    public float   TimeRemaining { get; private set; }
    private bool   isActive;

    public event System.Action<int> OnKeyCorrect; // passes new index
    public event System.Action      OnSuccess;
    public event System.Action      OnFail;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartInput(Element song)
    {
        ActiveSong    = song;
        CurrentIndex  = 0;
        TimeRemaining = FloorManager.Instance.GetInputTimeLimit();
        isActive      = true;
    }

    public void StopInput() => isActive = false;

    void Update()
    {
        if (!isActive) return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0) { Fail(); return; }

        Dir? pressed = GetPressedDir();
        if (pressed == null) return;

        Dir[] sequence = Songs[ActiveSong];
        if (pressed == sequence[CurrentIndex])
        {
            CurrentIndex++;
            OnKeyCorrect?.Invoke(CurrentIndex);
            if (CurrentIndex >= sequence.Length) Success();
        }
        else
        {
            Fail();
        }
    }

    void Success() { isActive = false; OnSuccess?.Invoke(); }
    void Fail()    { isActive = false; OnFail?.Invoke(); }

    Dir? GetPressedDir()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))    return Dir.Up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))  return Dir.Down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  return Dir.Left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return Dir.Right;
        return null;
    }
}
```

- [ ] **7.3** Implement `CombatManager` — orchestrates the combat turn loop, applies damage, triggers enemy attacks, handles death.

```csharp
// Scripts/Combat/CombatManager.cs
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public event System.Action<EnemyInstance> OnCombatStart;
    public event System.Action                OnCombatEnd;

    private EnemyInstance currentEnemy;
    private Element       selectedSong;
    private const int     BaseDamage = 10;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SongInputHandler.Instance.OnSuccess += HandleSuccess;
        SongInputHandler.Instance.OnFail    += HandleFail;
    }

    public void StartCombat(EnemyInstance enemy)
    {
        currentEnemy = enemy;
        GameManager.Instance.SetState(GameState.InCombat);
        OnCombatStart?.Invoke(enemy);
    }

    // Called by CombatUI when player presses a song button
    public void SelectSong(Element song)
    {
        if (!PlayerStats.Instance.SpendMana()) { HandleFail(); return; } // out of mana = forced fail
        selectedSong = song;
        SongInputHandler.Instance.StartInput(song);
    }

    void HandleSuccess()
    {
        int multiplier = ElementSystem.GetMultiplier(selectedSong, currentEnemy.data.element);
        int damage     = PlayerStats.Instance.GetSongDamage(selectedSong, multiplier);

        if (multiplier > 0)
        {
            bool died = currentEnemy.TakeDamage(damage);
            // Boss phase switch at half HP
            if (currentEnemy.data.isBoss &&
                currentEnemy.CurrentHP <= currentEnemy.data.maxHP / 2 &&
                currentEnemy.data.secondPhaseElement != currentEnemy.data.element)
            {
                currentEnemy.data.element = currentEnemy.data.secondPhaseElement;
                CombatUIController.Instance.ShowPhaseChange();
            }
            if (died) { EnemySpawner.Instance.RemoveEnemy(currentEnemy); EndCombat(); return; }
        }

        PlayerStats.Instance.RegenerateMana();
        CombatUIController.Instance.ShowResult(multiplier, damage);

        if (multiplier != 2) EnemyAttacks(); // not super effective = enemy counter-attacks
    }

    void HandleFail()
    {
        PlayerStats.Instance.RegenerateMana();
        CombatUIController.Instance.ShowFailFeedback();
        EnemyAttacks();
    }

    void EnemyAttacks()
    {
        int dmg = currentEnemy.data.attackDamage;
        if (PlayerStats.Instance.HasEarplugs) dmg = Mathf.Max(1, dmg - 2);
        bool dead = PlayerStats.Instance.TakeDamage(dmg);
        if (dead) { GameManager.Instance.GameOver(); return; }
        CombatUIController.Instance.ShowNewTurn();
    }

    public void EndCombat()
    {
        GameManager.Instance.SetState(GameState.Exploring);
        OnCombatEnd?.Invoke();
    }

    public void AttemptFlee()
    {
        if (UnityEngine.Random.value < 0.4f) EndCombat();
        else EnemyAttacks();
    }
}
```

- [ ] **7.4** Test: trigger combat, select a song, input correct sequence → enemy takes damage. Input wrong key → enemy attacks. Verify enemy death removes it from dungeon.

---

### PHASE 8 — Player Stats & Items
> Day 4

- [ ] **8.1** Implement `PlayerStats` singleton with `HP`, `Mana`, `TakeDamage()`, `RestoreHP()`, `SpendMana()`, `RegenerateMana()`, passive item flags.

```csharp
// Scripts/Player/PlayerStats.cs
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    public int maxHP       = 30;
    public int maxMana     = 20;
    public int songManaCost = 3;

    public int CurrentHP   { get; private set; }
    public int CurrentMana { get; private set; }

    public bool    HasEarplugs   { get; private set; }
    public Element BonusElement  { get; private set; } = (Element)(-1);
    public bool    HasSheetMusic => (int)BonusElement >= 0;

    public event System.Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() { CurrentHP = maxHP; CurrentMana = maxMana; }

    public bool TakeDamage(int amount)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnStatsChanged?.Invoke();
        return CurrentHP <= 0;
    }

    public void RestoreHP(int amount)
    {
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        OnStatsChanged?.Invoke();
    }

    public void RestoreMana(int amount)
    {
        CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
        OnStatsChanged?.Invoke();
    }

    public bool SpendMana()
    {
        if (CurrentMana < songManaCost) return false;
        CurrentMana -= songManaCost;
        OnStatsChanged?.Invoke();
        return true;
    }

    public void RegenerateMana()
    {
        CurrentMana = Mathf.Min(maxMana, CurrentMana + 2);
        OnStatsChanged?.Invoke();
    }

    public void ApplyEarplugs()            { HasEarplugs = true;    OnStatsChanged?.Invoke(); }
    public void ApplySheetMusic(Element e) { BonusElement = e;      OnStatsChanged?.Invoke(); }

    public int GetSongDamage(Element song, int multiplier)
    {
        int bonus = (HasSheetMusic && song == BonusElement) ? 5 : 0;
        return (10 + bonus) * multiplier;
    }
}
```

- [ ] **8.2** Create `ItemData` ScriptableObject.

```csharp
// Scripts/Items/ItemData.cs
using UnityEngine;

public enum ItemType { HealthPotion, LuteString, SheetMusic, Earplugs }

[CreateAssetMenu(menuName = "DCJam/ItemData")]
public class ItemData : ScriptableObject
{
    public string   itemName;
    public ItemType type;
    public Element  elementBonus;  // only used if type == SheetMusic
    public Sprite   icon;
    public int      restoreAmount; // for HealthPotion and LuteString
}
```

- [ ] **8.3** Create item assets: `HealthPotion` (restore 10 HP), `LuteString` (restore 8 mana), `SheetMusic_Fire/Water/Earth/Wind`, `Earplugs`.
- [ ] **8.4** Implement `ItemPickup` — exists in dungeon, collected on player step.

```csharp
// Scripts/Items/ItemPickup.cs
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData  data;
    public Vector2Int GridPos;

    public void Collect()
    {
        switch (data.type)
        {
            case ItemType.HealthPotion: PlayerStats.Instance.RestoreHP(data.restoreAmount);     break;
            case ItemType.LuteString:  PlayerStats.Instance.RestoreMana(data.restoreAmount);    break;
            case ItemType.SheetMusic:  PlayerStats.Instance.ApplySheetMusic(data.elementBonus); break;
            case ItemType.Earplugs:    PlayerStats.Instance.ApplyEarplugs();                    break;
        }
        ItemSpawner.Instance.RemoveItem(this);
        Destroy(gameObject);
    }
}
```

- [ ] **8.5** Implement `ItemSpawner` (mirrors `EnemySpawner` — spawn items from `DungeonData`, provide `GetItemAt()`, `RemoveItem()`).

---

### PHASE 9 — Combat UI
> Day 4–5

**Why:** Most visible system. Build functional first, polish last.

- [ ] **9.1** Build combat screen Canvas hierarchy in Unity editor:

```
Canvas (Screen Space - Overlay)
  └── CombatPanel (full screen, semi-transparent dark background)
       ├── EnemySprite        (Image)
       ├── EnemyNameText      (TMP)
       ├── EnemyHPBar         (Slider)
       ├── ElementLabel       ("ELEMENT: FIRE")
       ├── CounterLabel       ("CASTING: WATER")
       ├── SequenceDisplay
       │    └── 6x KeyIcon    (Image — arrow sprites, gray=unlit, white=hit)
       ├── TimerBar           (Slider, red fill)
       ├── SongButtons        (4 Buttons: Fire / Water / Earth / Wind)
       ├── FleeButton
       └── ResultText         ("RESONANT!", "HIT!", "NO EFFECT...", "WRONG KEYS!")
```

- [ ] **9.2** Implement `CombatUIController` — subscribes to `CombatManager` and `SongInputHandler` events, drives all UI updates.

```csharp
// Scripts/UI/CombatUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIController : MonoBehaviour
{
    public static CombatUIController Instance { get; private set; }

    [Header("Panels")]
    public GameObject combatPanel;

    [Header("Enemy Info")]
    public Image            enemySprite;
    public TextMeshProUGUI  enemyNameText;
    public Slider           enemyHPSlider;
    public TextMeshProUGUI  elementLabel;

    [Header("Sequence Display")]
    public Image[]          keyIcons;      // 6 icons
    public Sprite[]         dirSprites;    // index: 0=Up 1=Down 2=Left 3=Right
    public TextMeshProUGUI  counterLabel;

    [Header("Timer & Feedback")]
    public Slider           timerBar;
    public TextMeshProUGUI  resultText;

    [Header("Buttons")]
    public GameObject       songButtonsGroup;

    private EnemyInstance   activeEnemy;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        combatPanel.SetActive(false);
    }

    void Start()
    {
        CombatManager.Instance.OnCombatStart += ShowCombat;
        CombatManager.Instance.OnCombatEnd   += HideCombat;
        SongInputHandler.Instance.OnKeyCorrect += UpdateSequenceDisplay;
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.InCombat) return;
        timerBar.value = SongInputHandler.Instance.TimeRemaining
                       / FloorManager.Instance.GetInputTimeLimit();
    }

    void ShowCombat(EnemyInstance enemy)
    {
        activeEnemy             = enemy;
        combatPanel.SetActive(true);
        enemySprite.sprite      = enemy.data.sprite;
        enemyNameText.text      = enemy.data.enemyName;
        enemyHPSlider.maxValue  = enemy.data.maxHP;
        enemyHPSlider.value     = enemy.CurrentHP;
        elementLabel.text       = $"ELEMENT: {enemy.data.element}";
        resultText.text         = "";
        songButtonsGroup.SetActive(true);
    }

    void HideCombat() => combatPanel.SetActive(false);

    // Wired to each song button's OnClick in the inspector — pass 0/1/2/3
    public void OnSongSelected(int elementIndex)
    {
        Element e          = (Element)elementIndex;
        string counterName = ElementSystem.GetCounter(activeEnemy.data.element).ToString();
        counterLabel.text  = $"COUNTER: {counterName} | CASTING: {e}";
        BuildSequenceDisplay(e);
        songButtonsGroup.SetActive(false);
        CombatManager.Instance.SelectSong(e);
    }

    void BuildSequenceDisplay(Element e)
    {
        var seq = SongInputHandler.Songs[e];
        for (int i = 0; i < keyIcons.Length; i++)
        {
            keyIcons[i].sprite = dirSprites[(int)seq[i]];
            keyIcons[i].color  = Color.gray;
        }
    }

    void UpdateSequenceDisplay(int newIndex)
    {
        for (int i = 0; i < newIndex; i++)
            keyIcons[i].color = Color.white;
    }

    public void ShowResult(int multiplier, int damage)
    {
        resultText.text = multiplier switch
        {
            2 => $"RESONANT!  -{damage} HP",
            1 => $"HIT!  -{damage} HP",
            0 => "NO EFFECT...",
            _ => ""
        };
        enemyHPSlider.value = activeEnemy.CurrentHP;
        Invoke(nameof(ReEnableSongButtons), 1.2f);
    }

    public void ShowFailFeedback()
    {
        resultText.text = "WRONG KEYS!";
        Invoke(nameof(ReEnableSongButtons), 1.2f);
    }

    public void ShowNewTurn()
    {
        enemyHPSlider.value = activeEnemy != null ? activeEnemy.CurrentHP : 0;
        Invoke(nameof(ReEnableSongButtons), 1.2f);
    }

    public void ShowPhaseChange()
    {
        resultText.text    = "— SECOND PHASE! —";
        elementLabel.text  = $"ELEMENT: {activeEnemy.data.element}";
    }

    void ReEnableSongButtons()
    {
        resultText.text = "";
        songButtonsGroup.SetActive(true);
    }
}
```

- [ ] **9.3** Wire song button `OnClick` events in inspector to `OnSongSelected(0/1/2/3)`.
- [ ] **9.4** Add a **Tab-toggleable reference card** overlay showing all 4 song sequences so players don't have to memorize them.
- [ ] **9.5** Test full combat loop end to end: open combat → select element → input sequence → result shown → new turn → enemy dies → return to exploration.

---

### PHASE 10 — HUD & Floor Wiring
> Day 5

- [ ] **10.1** Build HUD Canvas (always visible during exploration): HP bar, Mana bar, Floor number label. Subscribe to `PlayerStats.OnStatsChanged` to update bars.
- [ ] **10.2** Implement `DungeonOrchestrator` — single script that wires together generation, rendering, and spawning on scene start and floor transition.

```csharp
// Scripts/Core/DungeonOrchestrator.cs
using UnityEngine;

public class DungeonOrchestrator : MonoBehaviour
{
    void Start() => GenerateFloor();

    public void GenerateFloor()
    {
        DungeonData data = BSPGenerator.Instance.Generate();
        DungeonRenderer.Instance.Render(data);
        EnemySpawner.Instance.SpawnEnemies(data, FloorManager.Instance.CurrentFloor);
        ItemSpawner.Instance.SpawnItems(data);
        FindObjectOfType<GridMover>().SetGridPosition(data.PlayerSpawn);
    }
}
```

- [ ] **10.3** Implement death condition: `PlayerStats.TakeDamage()` returns `true` → `GameManager.GameOver()` → load `GameOver` scene.
- [ ] **10.4** Implement win condition: `FloorManager.NextFloor()` exceeds `MaxFloors` → `GameManager.Victory()` → show victory screen.
- [ ] **10.5** Test permadeath reset: die, reach game over screen, start new run, verify dungeon regenerates fresh and player stats reset.

---

### PHASE 11 — Main Menu & Game Over Screen
> Day 6

- [ ] **11.1** Build `MainMenu` scene: title, **Start** button (`GameManager.Instance.StartNewRun()`), basic credits.
- [ ] **11.2** Build `GameOver` scene: "YOU FELL" message, floor reached, **Try Again** button.
- [ ] **11.3** Build `Victory` state (can reuse `GameOver` scene with different text): "THE DUNGEON IS PURGED" message.

---

### PHASE 12 — Audio
> Day 6

- [ ] **12.1** Implement `AudioManager` singleton with `PlaySFX(AudioClip)` and `PlayMusic(AudioClip, bool loop)`.
- [ ] **12.2** Source audio from **freesound.org** or **opengameart.org** — search for:
  - `key_correct` — short positive chime
  - `key_wrong` — short buzz/dissonant hit
  - `combat_win` — fanfare sting
  - `combat_hit` — impact sound
  - `player_hurt` — grunt or negative tone
  - `footstep` — stone step
  - `floor_change` — ascending tone / transition
  - `music_explore` — looping ambient dungeon track
  - `music_combat` — looping tense combat track
- [ ] **12.3** Hook `AudioManager.PlaySFX()` calls into: `SongInputHandler.OnKeyCorrect`, `SongInputHandler.OnFail`, `CombatManager.EndCombat()`, `PlayerStats.TakeDamage()`.
- [ ] **12.4** Swap music on `GameManager.OnStateChanged` between `Exploring` and `InCombat`.

---

### PHASE 13 — Polish & Build
> Day 7

- [ ] **13.1** Replace placeholder geometry with final art (or at least add textures/materials to wall/floor prefabs).
- [ ] **13.2** Add colored particle VFX prefabs for each element (`VFX_Fire`, `VFX_Water`, `VFX_Earth`, `VFX_Wind`) — attach to enemy instances in dungeon.
- [ ] **13.3** Verify all jam rules are met:
  - [x] First-person exploration
  - [x] Step movement on square grid
  - [x] 90-degree turns via keyboard
  - [x] Player character with stats
  - [x] Combat mechanic
  - [x] Win condition (defeat floor 5 boss)
  - [x] Death condition (HP = 0)
  - [x] Stat modifier (potions, items)
  - [x] Theme interpretation (Elemental RPS)
- [ ] **13.4** Set build target to **WebGL** in `Project Settings → Player`.
- [ ] **13.5** Test WebGL build locally using a local server (`python -m http.server` in build folder).
- [ ] **13.6** Upload to itch.io — mark as WebGL, set viewport size to `960x600`.
- [ ] **13.7** Write itch.io page description: explain song mechanic and element cycle clearly. Include the reference card as a screenshot.

---

## Unity Editor Setup Guide

Follow these steps after all scripts are written and Unity has compiled without errors.

---

### Step 0 — Prerequisites

1. **TextMeshPro**: Window → TextMeshPro → Import TMP Essential Resources
2. **DOTween**: already installed under `Assets/Plugins/Demigiant/`; if not, run the DOTween Setup from the Tools menu after import
3. **Build Settings** (File → Build Settings): add scenes in this exact order:
   - `Assets/Scenes/MainMenu` (index 0)
   - `Assets/Scenes/Game` (index 1)
   - `Assets/Scenes/GameOver` (index 2)

---

### Step 1 — Create Scenes

1. File → New Scene → save as `Assets/Scenes/MainMenu`
2. File → New Scene → save as `Assets/Scenes/Game`
3. File → New Scene → save as `Assets/Scenes/GameOver`

---

### Step 2 — Create Dungeon Prefabs

Open the `Game` scene. Create these primitive prefabs and save them to `Assets/Prefabs/Dungeon/`:

| Prefab | How to make |
|--------|-------------|
| **Wall** | 3D Object → Cube; Scale `(4, 4, 0.2)`; gray material |
| **Floor** | 3D Object → Plane; Scale `(0.4, 1, 0.4)`; dark gray material |
| **Ceiling** | Duplicate Floor; Rotation `(180, 0, 0)`; slightly lighter material |
| **Stairs** | 3D Object → Cube; Scale `(2, 0.3, 2)`; yellow material |

Drag each into `Assets/Prefabs/Dungeon/` to save as a prefab, then delete from scene.

---

### Step 3 — Create the Enemy Prefab

1. Hierarchy → Create Empty → name it `EnemyPrefab`
2. Add Component → `EnemyInstance`
3. Drag to `Assets/Prefabs/Enemies/EnemyPrefab`; delete from scene

---

### Step 4 — Create the Item Pickup Prefab

1. Hierarchy → 3D Object → Sphere → name it `ItemPickupPrefab`; scale `(0.5, 0.5, 0.5)`
2. Add Component → `ItemPickup`
3. Drag to `Assets/Prefabs/Items/ItemPickupPrefab`; delete from scene

---

### Step 5 — Create ScriptableObject Assets

#### Enemy Data (`Assets/ScriptableObjects/Enemies/`)

Right-click in the folder → Create → DCJam → EnemyData. Create one asset per enemy:

| Asset name | enemyName | element | maxHP | attackDamage | isBoss |
|-----------|-----------|---------|-------|-------------|--------|
| FireDemon | Fire Demon | Fire | 20 | 5 | false |
| WaterDemon | Water Demon | Water | 20 | 5 | false |
| EarthDemon | Earth Demon | Earth | 20 | 5 | false |
| WindDemon | Wind Demon | Wind | 20 | 5 | false |
| FloorBoss | Floor Guardian | Fire | 40 | 8 | true |
| FinalBoss | Demon Lord | Fire | 60 | 10 | true (secondPhaseElement = Wind) |

#### Item Data (`Assets/ScriptableObjects/Items/`)

Right-click → Create → DCJam → ItemData:

| Asset name | itemName | type | restoreAmount |
|-----------|----------|------|--------------|
| HealthPotion | Health Potion | HealthPotion | 10 |
| LuteString | Lute String | LuteString | 8 |
| SheetMusic_Fire | Fire Sheet Music | SheetMusic | 0 (elementBonus = Fire) |
| SheetMusic_Water | Water Sheet Music | SheetMusic | 0 (elementBonus = Water) |
| SheetMusic_Earth | Earth Sheet Music | SheetMusic | 0 (elementBonus = Earth) |
| SheetMusic_Wind | Wind Sheet Music | SheetMusic | 0 (elementBonus = Wind) |
| Earplugs | Earplugs | Earplugs | 0 |

---

### Step 6 — Set Up the Game Scene

Open the `Game` scene. Create all GameObjects below.

#### 6.1 — Singletons GameObject

1. Hierarchy → Create Empty → name `_Singletons`
2. Add Components: `GameManager`, `FloorManager`

#### 6.2 — Player

1. Hierarchy → Create Empty → name `Player`; Position `(0, 0, 0)`
2. Add Component → `GridMover`
3. Add Component → `PlayerStats`
4. Inside `Player`, right-click → Create Empty → name `Camera`; Position `(0, 0, 0)`
5. On the `Camera` child, Add Component → `Camera`
6. Set Camera **Field of View** to `70`
7. Set Camera **Clipping Planes Near** to `0.1`, **Far** to `200`

#### 6.3 — Dungeon Systems

Create these GameObjects (all at root level, position `(0,0,0)`):

**BSPGenerator**
- Create Empty → name `BSPGenerator`
- Add Component → `BSPGenerator`
- Leave default values (mapWidth=40, mapHeight=40, enemiesPerFloor=6, itemsPerFloor=4)

**DungeonRenderer**
- Create Empty → name `DungeonRenderer`
- Add Component → `DungeonRenderer`
- Assign prefabs: Wall, Floor, Ceiling, Stairs from `Assets/Prefabs/Dungeon/`

**DungeonOrchestrator**
- Create Empty → name `DungeonOrchestrator`
- Add Component → `DungeonOrchestrator`

#### 6.4 — Enemy & Item Spawners

**EnemySpawner**
- Create Empty → name `EnemySpawner`
- Add Component → `EnemySpawner`
- **basicEnemies** (size 4): assign FireDemon, WaterDemon, EarthDemon, WindDemon
- **bossFinalData**: assign FinalBoss
- **enemyPrefab**: assign `Assets/Prefabs/Enemies/EnemyPrefab`

**ItemSpawner**
- Create Empty → name `ItemSpawner`
- Add Component → `ItemSpawner`
- **itemPool** (size 7): assign all 7 item SOs
- **itemPickupPrefab**: assign `Assets/Prefabs/Items/ItemPickupPrefab`

#### 6.5 — Combat Systems

**CombatSystems**
- Create Empty → name `CombatSystems`
- Add Component → `CombatManager`
- Add Component → `SongInputHandler`

#### 6.6 — Audio

**AudioManager**
- Create Empty → name `AudioManager`
- Add Component → `AudioManager`
- Add Component → `AudioSource` (×2); assign one to **sfxSource**, one to **musicSource**
- Assign audio clips once you have them (see Phase 12 in the cut list above)

---

### Step 7 — Combat UI Canvas

In the `Game` scene:

1. Hierarchy → UI → Canvas → name `CombatCanvas`; set **Render Mode** to Screen Space - Overlay
2. Inside Canvas, create the following hierarchy:

```
CombatCanvas
  └── CombatPanel (UI → Panel; anchors = stretch-stretch)
       ├── EnemySprite      (UI → Image)
       ├── EnemyNameText    (UI → Text - TextMeshPro)
       ├── EnemyHPBar       (UI → Slider; Interactable = OFF)
       ├── ElementLabel     (UI → Text - TextMeshPro)
       ├── CounterLabel     (UI → Text - TextMeshPro)
       ├── SequenceDisplay  (UI → Empty; add 6 child Images for key icons)
       ├── TimerBar         (UI → Slider; Interactable = OFF)
       ├── SongButtons      (UI → Empty)
       │    ├── FireButton   (UI → Button; label "Fire")
       │    ├── WaterButton  (UI → Button; label "Water")
       │    ├── EarthButton  (UI → Button; label "Earth")
       │    └── WindButton   (UI → Button; label "Wind")
       ├── FleeButton       (UI → Button; label "Flee")
       └── ResultText       (UI → Text - TextMeshPro)
```

3. Create Empty at root of Canvas → name `CombatUIController`; Add Component → `CombatUIController`
4. Assign all fields in the Inspector:
   - **combatPanel** → `CombatPanel`
   - **enemySprite** → `EnemySprite` Image
   - **enemyNameText** → `EnemyNameText` TMP
   - **enemyHPSlider** → `EnemyHPBar` Slider
   - **elementLabel** → `ElementLabel` TMP
   - **counterLabel** → `CounterLabel` TMP
   - **keyIcons** (size 6) → the 6 child Images inside `SequenceDisplay`
   - **dirSprites** (size 4) → arrow sprites (Up, Down, Left, Right) — create simple arrows or import from free assets
   - **timerBar** → `TimerBar` Slider
   - **resultText** → `ResultText` TMP
   - **songButtonsGroup** → `SongButtons` GameObject

5. Wire song button OnClick events:
   - FireButton → OnClick → `CombatUIController.OnSongSelected(0)`
   - WaterButton → OnClick → `CombatUIController.OnSongSelected(1)`
   - EarthButton → OnClick → `CombatUIController.OnSongSelected(2)`
   - WindButton → OnClick → `CombatUIController.OnSongSelected(3)`
6. FleeButton → OnClick → `CombatManager.AttemptFlee()`

---

### Step 8 — HUD Canvas

1. Hierarchy → UI → Canvas → name `HUDCanvas`
2. Inside HUDCanvas:

```
HUDCanvas
  ├── HPBar    (UI → Slider; Interactable = OFF; anchor top-left)
  ├── ManaBar  (UI → Slider; Interactable = OFF; anchor top-left, below HP)
  └── FloorLabel (UI → Text - TextMeshPro; anchor top-right)
```

3. Create Empty → name `HUDController`; Add Component → `HUDController`
4. Assign: **hpBar**, **manaBar**, **floorLabel**

---

### Step 9 — Set Up the MainMenu Scene

Open `MainMenu` scene.

1. UI → Canvas → inside it:
   - TMP Text: "BARD DUNGEON" (title)
   - Button: "START" → OnClick → (runtime) `GameManager.Instance.StartNewRun()` — wire via a small `MainMenuUI` script:

```csharp
// Scripts/UI/MainMenuUI.cs
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button startButton;
    void Start() => startButton.onClick.AddListener(() => GameManager.Instance.StartNewRun());
}
```

2. Add `MainMenuUI` to a GameObject; assign `startButton`.

---

### Step 10 — Set Up the GameOver Scene

Open `GameOver` scene.

1. UI → Canvas → inside it:
   - TMP Text: name `MessageText`
   - TMP Text: name `FloorText`
   - Button: name `TryAgainButton`, label "Try Again"
2. Create Empty → Add Component → `GameOverUI`
3. Assign **messageText**, **floorText**, **tryAgainButton**

---

### Step 11 — Final Checks Before Play

- [ ] All three scenes added to Build Settings (File → Build Settings → Add Open Scenes)
- [ ] `Game` scene is the active/start scene during development (index 1 in build, but set as default for editor play)
- [ ] DOTween initialized (should happen automatically via `GameManager.Awake()`)
- [ ] No missing references (red fields) in any Inspector
- [ ] Press **Play** in the `Game` scene — dungeon should generate, player should move with WASD/arrows
