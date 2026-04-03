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
    public int enemiesPerFloor = 4;
    public int itemsPerFloor   = 3;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public DungeonData Generate()
    {
        var data   = new DungeonData(mapWidth, mapHeight);
        var root   = new BSPNode(new RectInt(0, 0, mapWidth, mapHeight));
        var leaves = new List<BSPNode>();

        Split(root, leaves);
        CreateRooms(data, leaves);
        ConnectRooms(data, root);
        TagRooms(data, leaves);
        PlaceEnemies(data, leaves);
        PlaceItems(data, leaves);

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
        
        Debug.Log($"Connecting rooms: {a} -> {b}");
        CarveHorizontal(data, a, new Vector2Int(b.x, a.y));
        CarveVertical(data,   new Vector2Int(b.x, a.y), b);
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

    void PlaceItems(DungeonData data, List<BSPNode> leaves)
    {
        int limit = Mathf.Max(1, leaves.Count - 3);
        for (int i = 0; i < itemsPerFloor && i < limit - 1; i++)
        {
            var room = leaves[i + 1].Room;
            var pos  = new Vector2Int(room.x + room.width - 2, room.y + room.height - 2);
            if (!data.EnemySpawns.Contains(pos) && pos != data.StairsPos)
                data.ItemSpawns.Add(pos);
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
