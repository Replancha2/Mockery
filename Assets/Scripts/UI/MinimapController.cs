using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private RawImage minimapImage;

    [Header("Settings")]
    [Tooltip("How many cells are visible in each direction from the player.")]
    [SerializeField] private int viewRadius    = 5;
    [Tooltip("Pixels per dungeon cell on the minimap texture.")]
    [SerializeField] private int pixelsPerCell = 10;

    // -------------------------------------------------------------------------

    private Texture2D    _tex;
    private Color32[]    _pixels;
    private Vector2Int   _lastPos    = new(-999, -999);
    private float        _lastFacing = -1f;
    private DungeonData  _trackedData;

    // Colour palette
    static readonly Color32 ColWall     = new( 20,  20,  20, 255);
    static readonly Color32 ColFloor    = new(100, 100, 100, 255);
    static readonly Color32 ColCorridor = new(135, 135, 135, 255);
    static readonly Color32 ColOOB      = new(  0,   0,   0, 255);
    static readonly Color32 ColPlayer   = new(255, 220,  50, 255);  // yellow
    static readonly Color32 ColFacing   = new(255, 255, 255, 255);  // white arrow tip
    static readonly Color32 ColVendor   = new(100, 160, 255, 255);  // blue
    static readonly Color32 ColBeggar   = new(200, 100, 255, 255);  // purple
    static readonly Color32 ColMiniBoss = new(255,  80,  80, 255);  // red

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        int size = (viewRadius * 2 + 1) * pixelsPerCell;
        _pixels = new Color32[size * size];

        _tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp
        };

        // Black fill
        for (int i = 0; i < _pixels.Length; i++) _pixels[i] = ColOOB;
        _tex.SetPixels32(_pixels);
        _tex.Apply();

        if (minimapImage != null) minimapImage.texture = _tex;
    }

    void Update()
    {
        if (GridMover.Instance == null || DungeonRenderer.Instance == null) return;

        DungeonData data = DungeonRenderer.Instance.CurrentData;
        if (data == null) return;

        // Detect floor change
        if (data != _trackedData)
        {
            _trackedData = data;
            _lastPos     = new Vector2Int(-999, -999);
        }

        Vector2Int pos    = GridMover.Instance.GridPos;
        float      facing = GridMover.Instance.Facing;

        if (pos == _lastPos && Mathf.Approximately(facing, _lastFacing)) return;

        _lastPos    = pos;
        _lastFacing = facing;
        Redraw(data, pos, facing);
    }

    // -------------------------------------------------------------------------

    void Redraw(DungeonData data, Vector2Int player, float facing)
    {
        int diameter = viewRadius * 2 + 1;
        int texSize  = diameter * pixelsPerCell;

        // Fill all cells
        for (int cy = 0; cy < diameter; cy++)
        {
            for (int cx = 0; cx < diameter; cx++)
            {
                int wx = player.x + cx - viewRadius;
                int wy = player.y + cy - viewRadius;

                bool isCenter = (cx == viewRadius && cy == viewRadius);
                Color32 col   = isCenter ? ColPlayer : SampleCell(data, wx, wy);

                int origin = cy * pixelsPerCell * texSize + cx * pixelsPerCell;
                for (int dy = 0; dy < pixelsPerCell; dy++)
                    for (int dx = 0; dx < pixelsPerCell; dx++)
                        _pixels[origin + dy * texSize + dx] = col;
            }
        }

        // Direction indicator — bright white pixel near the leading edge of the player cell
        if (pixelsPerCell >= 4)
        {
            int rx = 0, ry = 0;
            switch (Mathf.RoundToInt(facing) % 360)
            {
                case   0: rx =  0; ry =  1; break;  // North (+Y in grid = +Y in texture)
                case  90: rx =  1; ry =  0; break;  // East
                case 180: rx =  0; ry = -1; break;  // South
                case 270: rx = -1; ry =  0; break;  // West
            }

            int half  = pixelsPerCell / 2;
            int reach = half - 1;

            int arrowX = viewRadius * pixelsPerCell + half + rx * reach;
            int arrowY = viewRadius * pixelsPerCell + half + ry * reach;

            if (arrowX >= 0 && arrowX < texSize && arrowY >= 0 && arrowY < texSize)
                _pixels[arrowY * texSize + arrowX] = ColFacing;
        }

        _tex.SetPixels32(_pixels);
        _tex.Apply();
    }

    Color32 SampleCell(DungeonData data, int x, int y)
    {
        if (x < 0 || x >= data.Width || y < 0 || y >= data.Height)
            return ColOOB;

        if (data.Cells[x, y] == CellType.Wall)
            return ColWall;

        return data.Tags[x, y] switch
        {
            RoomTag.Vendor   => ColVendor,
            RoomTag.Beggar   => ColBeggar,
            RoomTag.MiniBoss => ColMiniBoss,
            _ => data.Cells[x, y] == CellType.Corridor ? ColCorridor : ColFloor
        };
    }
}
