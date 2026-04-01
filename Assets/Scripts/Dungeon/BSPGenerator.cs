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
