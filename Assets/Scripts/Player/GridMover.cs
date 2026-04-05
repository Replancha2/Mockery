using UnityEngine;
using DG.Tweening;
using System.Collections;

public class GridMover : MonoBehaviour
{
    public static GridMover Instance { get; private set; }

    public float moveSpeed = 8f;
    public float turnSpeed = 10f;

    private Vector2Int gridPos;
    private float facing   = 0f; // 0=North 90=East 180=South 270=West
    private bool  isMoving = false;
    private Tween currentTween;
    private Coroutine movementTimeoutCoroutine;

    public Vector2Int GridPos => gridPos;
    public float Facing => facing;

    public const float CellSize = 4f;
    private const float MOVEMENT_TIMEOUT = 2f; // Auto-reset after 2 seconds

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
        
        Debug.Log($"TryMove: from {gridPos} to {target}, world pos: {transform.position}");
        
        if (!DungeonRenderer.Instance.IsWalkable(target))
        {
            // Log detailed info about why movement is blocked
            if (target.x < 0 || target.x >= 40 || target.y < 0 || target.y >= 40)
            {
                Debug.LogWarning($"Cannot move to {target} - out of bounds");
            }
            else
            {
                Debug.LogWarning($"Cannot move to {target} - not walkable (likely a wall or corridor not carved properly)");
            }
            return;
        }

        // NPC rooms — check before enemy check so NPCs aren't blocked by enemies on same tile
        RoomTag tag = DungeonRenderer.Instance.GetRoomTag(target);
        Debug.Log($"  Room tag at {target}: {tag}");

        // Handle NPC interactions
        if (tag == RoomTag.Vendor)
        {
            var vendor = NPCSpawner.Instance.GetVendor();
            if (vendor != null && vendor.gameObject.activeInHierarchy && vendor.GridPos == target)
            {
                vendor.OpenShop();
                return;
            }
        }
        if (tag == RoomTag.Beggar)
        {
            var beggar = NPCSpawner.Instance.GetBeggar();
            if (beggar != null && beggar.gameObject.activeInHierarchy && beggar.GridPos == target)
            {
                beggar.Interact();
                return;
            }
        }

        EnemyInstance enemy = EnemySpawner.Instance.GetEnemyAt(target);
        if (enemy != null) 
        { 
            Debug.Log($"  Blocked by enemy {enemy.name} at {target}"); 
            return; 
        }


        if (DungeonRenderer.Instance.IsStairs(target))
        {
            bool isBossFloor = FloorManager.Instance.CurrentFloor >= FloorManager.MaxFloors;
            if (isBossFloor && EnemySpawner.Instance != null && EnemySpawner.Instance.HasAnyActiveEnemy())
            {
                HUDController.Instance?.Log("The exit is sealed. Defeat the boss first.");
                return;
            }

            Debug.Log($"  Stepping on stairs at {target}");
            // Move player to stairs tile first
            gridPos = target;
            transform.position = GridToWorld(target);
            
            isMoving = true;  // Prevent multiple triggers
            try
            {
                FloorManager.Instance.NextFloor();
                // If BuffSelectionUI isn't in the scene yet, generate the next floor directly
                if (BuffSelectionUI.Instance == null)
                {
                    FloorManager.Instance.ApplyStartOfFloorEffects();
                    FindFirstObjectByType<DungeonOrchestrator>().GenerateFloor();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error transitioning to next floor: {ex}");
            }
            finally
            {
                isMoving = false;  // Always restore movement capability
            }
            return;
        }

        Debug.Log($"  Moving to {target}");
        isMoving = true;
        gridPos  = target;

        // Kill any previous tween
        currentTween?.Kill();

        // Start movement tween with safety callback
        currentTween = transform.DOMove(GridToWorld(target), 1f / moveSpeed)
                 .SetEase(Ease.InOutSine)
                 .OnComplete(() => 
                 {
                     isMoving = false;
                     Debug.Log("Movement completed");
                 });

        // Start timeout coroutine as backup
        if (movementTimeoutCoroutine != null)
            StopCoroutine(movementTimeoutCoroutine);
        movementTimeoutCoroutine = StartCoroutine(MovementTimeout());
    }

    IEnumerator MovementTimeout()
    {
        yield return new WaitForSeconds(MOVEMENT_TIMEOUT);
        if (isMoving)
        {
            Debug.LogWarning($"Movement tween timed out! Force resetting isMoving. Current pos: {gridPos}");
            currentTween?.Kill();
            isMoving = false;
        }
    }

    void ResetMovement()
    {
        if (movementTimeoutCoroutine != null)
            StopCoroutine(movementTimeoutCoroutine);
        currentTween?.Kill();
        isMoving = false;
    }

    void TurnBy(float degrees)
    {
        isMoving = true;
        facing = (facing + degrees + 360f) % 360f;

        // Kill any previous tween
        currentTween?.Kill();

        // Start rotation tween with safety callback
        currentTween = transform.DORotate(new Vector3(0, facing, 0), 1f / turnSpeed, RotateMode.Fast)
                 .SetEase(Ease.InOutSine)
                 .OnComplete(() => 
                 {
                     isMoving = false;
                     Debug.Log("Rotation completed");
                 });

        // Start timeout coroutine as backup
        if (movementTimeoutCoroutine != null)
            StopCoroutine(movementTimeoutCoroutine);
        movementTimeoutCoroutine = StartCoroutine(MovementTimeout());
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
        ResetMovement();
        gridPos = pos;
        transform.position = GridToWorld(pos);
        Debug.Log($"GridMover placed at grid {pos}, world {transform.position}");
    }

    void OnDestroy()
    {
        ResetMovement();
    }

    public Vector2Int GetGridPos() => gridPos;

    public static Vector3 GridToWorld(Vector2Int pos)
        => new Vector3(pos.x * CellSize, 1.6f, pos.y * CellSize);
}
