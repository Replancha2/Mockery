using UnityEngine;
using DG.Tweening;

public class GridMover : MonoBehaviour
{
    public static GridMover Instance { get; private set; }

    public float moveSpeed = 8f;
    public float turnSpeed = 10f;

    private Vector2Int gridPos;
    private float facing = 0f; // 0=North 90=East 180=South 270=West
    private bool isMoving = false;

    public Vector2Int GridPos => gridPos;

    public const float CellSize = 4f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (isMoving) return;
        if (GameManager.Instance.CurrentState != GameState.Exploring) return;
        HandleInput();
    }

    void HandleInput()
    {
        // Prevent movement while attempting spell casting
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return;

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
        if (enemy != null) { Debug.Log($"Enemy in the way! Combat will occur adjacent."); return; }

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
