using UnityEngine;
using System.Collections.Generic;

public class BSPGenerator : MonoBehaviour
{
    public static BSPGenerator Instance { get; private set; }

    [Header("Map Size")]
    public int mapWidth  = 40;
    public int mapHeight = 40;

    [Header("BSP Settings")]
    public int minLeafSize = 5;
    public int maxLeafSize = 10;

    [Header("Room Shape")]
    [Range(0.35f, 0.90f)] public float minRoomFillRatio = 0.35f;
    [Range(0.45f, 0.98f)] public float maxRoomFillRatio = 0.58f;
    public int roomBoundaryPadding = 1;

    [Header("Corridor Density")]
    [Range(0f, 1f)] public float extraConnectionChance = 0.75f;
    [Range(0, 3)] public int maxExtraConnectionsPerNode = 2;
    [Range(0f, 1f)] public float jaggedCorridorChance = 0.80f;
    [Range(1, 4)] public int jaggedOffsetTiles = 2;

    [Header("Corridor Turns")]
    [Range(1, 4)] public int minCorridorTurns = 2;
    [Range(2, 6)] public int maxCorridorTurns = 4;

    [Header("Spawn Counts")]
    public int enemiesPerFloor = 4;

    [Header("Boss Floor")]
    [SerializeField] private int bossFloorRoomMargin = 2;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public DungeonData Generate()
    {
        var data   = new DungeonData(mapWidth, mapHeight);

        bool isBossFloor = FloorManager.Instance != null && FloorManager.Instance.CurrentFloor >= FloorManager.MaxFloors;
        if (isBossFloor)
        {
            GenerateBossFloor(data);

            int bossWalkableTiles = 0;
            for (int x = 0; x < data.Width; x++)
            {
                for (int y = 0; y < data.Height; y++)
                {
                    if (data.IsFloor(new Vector2Int(x, y)))
                        bossWalkableTiles++;
                }
            }

            Debug.Log($"Generated boss floor: 1 room, {bossWalkableTiles} walkable tiles");
            return data;
        }

        var root   = new BSPNode(new RectInt(0, 0, mapWidth, mapHeight));
        var leaves = new List<BSPNode>();

        Split(root, leaves);
        CreateRooms(data, leaves);
        ConnectRooms(data, root);
        TagRooms(data, leaves);
        PlaceEnemies(data, leaves);

        // Debug: Count walkable tiles
        int walkableTiles = 0;
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (data.IsFloor(new Vector2Int(x, y)))
                    walkableTiles++;
            }
        }
        Debug.Log($"Generated dungeon: {leaves.Count} rooms, {walkableTiles} walkable tiles");

        return data;
    }

    void GenerateBossFloor(DungeonData data)
    {
        int margin = Mathf.Clamp(bossFloorRoomMargin, 1, Mathf.Max(1, Mathf.Min(data.Width, data.Height) / 3));

        int xMin = margin;
        int yMin = margin;
        int xMax = data.Width - margin - 1;
        int yMax = data.Height - margin - 1;

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
                data.Cells[x, y] = CellType.Floor;
        }

        Vector2Int roomCenter = new Vector2Int((xMin + xMax) / 2, (yMin + yMax) / 2);
        data.PlayerSpawn = new Vector2Int(roomCenter.x, Mathf.Clamp(yMin + 2, yMin, yMax));

        Vector2Int bossPos = roomCenter;
        if (bossPos == data.PlayerSpawn)
            bossPos = new Vector2Int(roomCenter.x, Mathf.Clamp(roomCenter.y + 2, yMin, yMax));

        data.EnemySpawns.Clear();
        data.EnemySpawns.Add(bossPos);

        data.MiniBossSpawns.Clear();

        data.VendorPos = Vector2Int.zero;
        data.BeggarPos = Vector2Int.zero;

        data.StairsPos = new Vector2Int(roomCenter.x, Mathf.Clamp(yMax - 1, yMin, yMax));
        MarkTile(data, data.StairsPos, RoomTag.Stairs);
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
            int maxRoomWidth = Mathf.Max(3, leaf.Bounds.width - roomBoundaryPadding * 2);
            int maxRoomHeight = Mathf.Max(3, leaf.Bounds.height - roomBoundaryPadding * 2);

            float minFill = Mathf.Min(minRoomFillRatio, maxRoomFillRatio);
            float maxFill = Mathf.Max(minRoomFillRatio, maxRoomFillRatio);

            int minRoomWidth = Mathf.Clamp(Mathf.RoundToInt(leaf.Bounds.width * minFill), 3, maxRoomWidth);
            int minRoomHeight = Mathf.Clamp(Mathf.RoundToInt(leaf.Bounds.height * minFill), 3, maxRoomHeight);
            int targetRoomWidth = Mathf.Clamp(Mathf.RoundToInt(leaf.Bounds.width * maxFill), minRoomWidth, maxRoomWidth);
            int targetRoomHeight = Mathf.Clamp(Mathf.RoundToInt(leaf.Bounds.height * maxFill), minRoomHeight, maxRoomHeight);

            int w = Random.Range(minRoomWidth, targetRoomWidth + 1);
            int h = Random.Range(minRoomHeight, targetRoomHeight + 1);

            int xMin = leaf.Bounds.x + roomBoundaryPadding;
            int yMin = leaf.Bounds.y + roomBoundaryPadding;
            int xMaxStart = leaf.Bounds.x + leaf.Bounds.width - roomBoundaryPadding - w;
            int yMaxStart = leaf.Bounds.y + leaf.Bounds.height - roomBoundaryPadding - h;

            int x = xMaxStart <= xMin ? xMin : Random.Range(xMin, xMaxStart + 1);
            int y = yMaxStart <= yMin ? yMin : Random.Range(yMin, yMaxStart + 1);
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
        
        Debug.Log($"Connecting rooms: {a} -> {b}");
        CarveCorridorPath(data, a, b, useJagged: Random.value < jaggedCorridorChance);

        int extraAttempts = Random.Range(0, maxExtraConnectionsPerNode + 1);
        for (int i = 0; i < extraAttempts; i++)
        {
            if (Random.value <= extraConnectionChance)
            {
                Vector2Int ea = GetRoomCenter(node.Left);
                Vector2Int eb = GetRoomCenter(node.Right);
                CarveCorridorPath(data, ea, eb, useJagged: true);
            }
        }
    }

    void CarveCorridorPath(DungeonData data, Vector2Int from, Vector2Int to, bool useJagged)
    {
        int minTurns = Mathf.Min(minCorridorTurns, maxCorridorTurns);
        int maxTurns = Mathf.Max(minCorridorTurns, maxCorridorTurns);
        int turns = useJagged ? Random.Range(minTurns, maxTurns + 1) : 1;

        CarveWindingCorridor(data, from, to, turns);
    }

    void CarveWindingCorridor(DungeonData data, Vector2Int from, Vector2Int to, int turns)
    {
        Vector2Int current = from;
        bool horizontalStep = Random.value > 0.5f;

        for (int i = 0; i < turns; i++)
        {
            Vector2Int next = current;

            if (horizontalStep)
            {
                float t = Random.Range(0.2f, 0.8f);
                int baseX = Mathf.RoundToInt(Mathf.Lerp(current.x, to.x, t));
                int jitter = Random.Range(-jaggedOffsetTiles, jaggedOffsetTiles + 1);
                next.x = Mathf.Clamp(baseX + jitter, 1, data.Width - 2);
            }
            else
            {
                float t = Random.Range(0.2f, 0.8f);
                int baseY = Mathf.RoundToInt(Mathf.Lerp(current.y, to.y, t));
                int jitter = Random.Range(-jaggedOffsetTiles, jaggedOffsetTiles + 1);
                next.y = Mathf.Clamp(baseY + jitter, 1, data.Height - 2);
            }

            if (next != current)
            {
                if (horizontalStep)
                    CarveHorizontal(data, current, next);
                else
                    CarveVertical(data, current, next);

                current = next;
            }

            horizontalStep = !horizontalStep;
        }

        CarveSegmentL(data, current, to);
    }

    void CarveSegmentL(DungeonData data, Vector2Int a, Vector2Int b)
    {
        // Randomize L orientation so corridors don't always feel orthogonal in the same way.
        bool horizontalFirst = Random.value > 0.5f;
        if (horizontalFirst)
        {
            Vector2Int bend = new Vector2Int(b.x, a.y);
            CarveHorizontal(data, a, bend);
            CarveVertical(data, bend, b);
        }
        else
        {
            Vector2Int bend = new Vector2Int(a.x, b.y);
            CarveVertical(data, a, bend);
            CarveHorizontal(data, bend, b);
        }
    }

    // Assigns special tags to rooms:
    //   leaves[0]       = PlayerSpawn (already set in CreateRooms)
    //   leaves[last]    = Stairs (+ MiniBoss spawn in same room)
    //   leaves[last-1]  = Vendor (only the NPC tile, not the entire room)
    //   leaves[last-2]  = Beggar (only the NPC tile, not the entire room)
    void TagRooms(DungeonData data, List<BSPNode> leaves)
    {
        int last = leaves.Count - 1;

        // Stairs room
        var stairsRoom = leaves[last].Room;
        data.StairsPos = new Vector2Int(stairsRoom.x + stairsRoom.width / 2,
                                        stairsRoom.y + stairsRoom.height / 2);
        MarkRoom(data, stairsRoom, RoomTag.Stairs);
        data.MiniBossSpawns.Add(new Vector2Int(stairsRoom.x + 1, stairsRoom.y + 1));
        MarkTile(data, data.MiniBossSpawns[0], RoomTag.MiniBoss);

        // Vendor NPC - only mark the specific tile where the vendor is, not the entire room
        if (last >= 1)
        {
            var vendorRoom = leaves[last - 1].Room;
            data.VendorPos = new Vector2Int(vendorRoom.x + vendorRoom.width / 2,
                                            vendorRoom.y + vendorRoom.height / 2);
            MarkTile(data, data.VendorPos, RoomTag.Vendor);
        }

        // Beggar NPC - only mark the specific tile where the beggar is, not the entire room
        if (last >= 2)
        {
            var beggarRoom = leaves[last - 2].Room;
            data.BeggarPos = new Vector2Int(beggarRoom.x + beggarRoom.width / 2,
                                            beggarRoom.y + beggarRoom.height / 2);
            MarkTile(data, data.BeggarPos, RoomTag.Beggar);
        }
    }

    void PlaceEnemies(DungeonData data, List<BSPNode> leaves)
    {
        // Skip leaves[0] (player spawn), last 3 (stairs/vendor/beggar)
        int limit = Mathf.Max(1, leaves.Count - 3);
        for (int i = 1; i < limit && data.EnemySpawns.Count < enemiesPerFloor; i++)
        {
            var pos = new Vector2Int(leaves[i].Room.x + 1, leaves[i].Room.y + 1);
            if (pos != data.StairsPos && pos != data.VendorPos && pos != data.BeggarPos)
                data.EnemySpawns.Add(pos);
        }
    }

    void MarkRoom(DungeonData data, RectInt room, RoomTag tag)
    {
        for (int rx = room.x; rx < room.x + room.width; rx++)
            for (int ry = room.y; ry < room.y + room.height; ry++)
                data.Tags[rx, ry] = tag;
    }

    void MarkTile(DungeonData data, Vector2Int pos, RoomTag tag)
    {
        if (pos.x >= 0 && pos.x < data.Width && pos.y >= 0 && pos.y < data.Height)
            data.Tags[pos.x, pos.y] = tag;
    }

    void CarveHorizontal(DungeonData data, Vector2Int from, Vector2Int to)
    {
        int minX = Mathf.Min(from.x, to.x), maxX = Mathf.Max(from.x, to.x);
        int carvedCount = 0;
        
        // Check if Y is in valid range
        if (from.y < 0 || from.y >= data.Height)
        {
            Debug.LogWarning($"CarveHorizontal: Y coordinate {from.y} out of bounds! Map height: {data.Height}");
            return;
        }
        
        for (int x = minX; x <= maxX; x++)
        {
            if (x >= 0 && x < data.Width)
            {
                if (data.Cells[x, from.y] == CellType.Wall)
                {
                    data.Cells[x, from.y] = CellType.Corridor;
                    carvedCount++;
                }
            }
            else
            {
                Debug.LogWarning($"CarveHorizontal: X coordinate {x} out of bounds!");
            }
        }
        
        Debug.Log($"  Carved horizontal: ({minX},{from.y}) to ({maxX},{from.y}) = {carvedCount} tiles");
    }

    void CarveVertical(DungeonData data, Vector2Int from, Vector2Int to)
    {
        int minY = Mathf.Min(from.y, to.y), maxY = Mathf.Max(from.y, to.y);
        int carvedCount = 0;
        
        // Check if X is in valid range
        if (from.x < 0 || from.x >= data.Width)
        {
            Debug.LogWarning($"CarveVertical: X coordinate {from.x} out of bounds! Map width: {data.Width}");
            return;
        }
        
        for (int y = minY; y <= maxY; y++)
        {
            if (y >= 0 && y < data.Height)
            {
                if (data.Cells[from.x, y] == CellType.Wall)
                {
                    data.Cells[from.x, y] = CellType.Corridor;
                    carvedCount++;
                }
            }
            else
            {
                Debug.LogWarning($"CarveVertical: Y coordinate {y} out of bounds!");
            }
        }
        
        Debug.Log($"  Carved vertical: ({from.x},{minY}) to ({from.x},{maxY}) = {carvedCount} tiles");
    }

    Vector2Int GetRoomCenter(BSPNode node)
    {
        if (node.IsLeaf)
            return new Vector2Int(node.Room.x + node.Room.width  / 2,
                                  node.Room.y + node.Room.height / 2);
        return GetRoomCenter(Random.value > 0.5f ? node.Left : node.Right);
    }
}
