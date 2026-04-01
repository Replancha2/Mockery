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
