using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    private EnemyInstance enemyInstance;
    private float moveTimer;
    private const float MoveInterval = 1.5f; // seconds between moves
    private const int SightRange = 5;        // max distance to see player
    private const int SearchMovesAfterLosingSight = 4;
    private const int SearchRadiusAroundLastKnown = 2;
    private bool playerInSight;
    private bool hasLastKnownPlayerPos;
    private Vector2Int lastKnownPlayerPos;
    private int searchMovesRemaining;

    void Start()
    {
        enemyInstance = GetComponent<EnemyInstance>();
        moveTimer = Random.Range(0f, MoveInterval);
        playerInSight = false;
        hasLastKnownPlayerPos = false;
        searchMovesRemaining = 0;
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Exploring) return;

        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0)
        {
            UpdatePlayerSight();
            
            if (playerInSight)
                ChasePlayer();
            else if (hasLastKnownPlayerPos)
                SearchForPlayer();
            else
                MoveRandomly();

            moveTimer = MoveInterval;
        }
    }

    void UpdatePlayerSight()
    {
        if (GridMover.Instance == null)
        {
            playerInSight = false;
            return;
        }

        if (ShouldAlwaysTrackPlayer())
        {
            playerInSight = true;
            lastKnownPlayerPos = GridMover.Instance.GridPos;
            hasLastKnownPlayerPos = true;
            searchMovesRemaining = SearchMovesAfterLosingSight;
            return;
        }

        Vector2Int playerPos = GridMover.Instance.GridPos;
        Vector2Int enemyPos = enemyInstance.GridPos;
        int distance = Mathf.Abs(playerPos.x - enemyPos.x) + Mathf.Abs(playerPos.y - enemyPos.y); // Manhattan distance

        // Check if in range AND has line of sight
        if (distance <= SightRange && HasLineOfSight(enemyPos, playerPos))
        {
            playerInSight = true;
            lastKnownPlayerPos = playerPos;
            hasLastKnownPlayerPos = true;
            searchMovesRemaining = SearchMovesAfterLosingSight;
        }
        else
        {
            playerInSight = false;
        }
    }

    bool HasLineOfSight(Vector2Int from, Vector2Int to)
    {
        // Bresenham line algorithm to check all tiles between from and to
        List<Vector2Int> line = GetBresenhamLine(from, to);
        
        foreach (var pos in line)
        {
            if (!DungeonRenderer.Instance.IsWalkable(pos))
                return false;
        }

        return true;
    }

    List<Vector2Int> GetBresenhamLine(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> line = new();
        
        int x0 = from.x;
        int y0 = from.y;
        int x1 = to.x;
        int y1 = to.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector2Int(x0, y0));

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return line;
    }

    void ChasePlayer()
    {
        Vector2Int playerPos = GridMover.Instance.GridPos;
        MoveToward(playerPos);
    }

    void SearchForPlayer()
    {
        if (searchMovesRemaining <= 0)
        {
            hasLastKnownPlayerPos = false;
            return;
        }

        Vector2Int enemyPos = enemyInstance.GridPos;
        if (enemyPos == lastKnownPlayerPos)
        {
            Vector2Int probeTarget = GetRandomSearchTile(lastKnownPlayerPos);
            MoveToward(probeTarget);
            searchMovesRemaining--;
            return;
        }

        MoveToward(lastKnownPlayerPos);
        searchMovesRemaining--;
    }

    void MoveToward(Vector2Int targetPos)
    {
        Vector2Int enemyPos = enemyInstance.GridPos;
        
        // If already adjacent to player, stop moving and let combat handle attacks
        int distance = Mathf.Abs(targetPos.x - enemyPos.x) + Mathf.Abs(targetPos.y - enemyPos.y);
        if (distance <= 1)
        {
            // Adjacent: stand still and combat system handles attacks
            return;
        }

        Vector2Int direction = Vector2Int.zero;

        // Move toward player (prioritize closer axis)
        if (Mathf.Abs(targetPos.x - enemyPos.x) > Mathf.Abs(targetPos.y - enemyPos.y))
        {
            direction = targetPos.x > enemyPos.x ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            direction = targetPos.y > enemyPos.y ? Vector2Int.up : Vector2Int.down;
        }

        Vector2Int newPos = enemyPos + direction;

        if (DungeonRenderer.Instance.IsWalkable(newPos) && EnemySpawner.Instance.GetEnemyAt(newPos) == null)
        {
            enemyInstance.SetGridPos(newPos);
            return;
        }

        // If direct step is blocked, try any step that gets closer.
        TryAlternateCloserStep(targetPos, enemyPos, distance);
    }

    void TryAlternateCloserStep(Vector2Int targetPos, Vector2Int enemyPos, int currentDistance)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int candidate = enemyPos + directions[i];
            if (!DungeonRenderer.Instance.IsWalkable(candidate)) continue;
            if (EnemySpawner.Instance.GetEnemyAt(candidate) != null) continue;

            int nextDistance = Mathf.Abs(targetPos.x - candidate.x) + Mathf.Abs(targetPos.y - candidate.y);
            if (nextDistance < currentDistance)
            {
                enemyInstance.SetGridPos(candidate);
                return;
            }
        }
    }

    Vector2Int GetRandomSearchTile(Vector2Int center)
    {
        for (int attempts = 0; attempts < 8; attempts++)
        {
            int dx = Random.Range(-SearchRadiusAroundLastKnown, SearchRadiusAroundLastKnown + 1);
            int dy = Random.Range(-SearchRadiusAroundLastKnown, SearchRadiusAroundLastKnown + 1);
            Vector2Int candidate = new Vector2Int(center.x + dx, center.y + dy);

            if (!DungeonRenderer.Instance.IsWalkable(candidate)) continue;
            if (EnemySpawner.Instance.GetEnemyAt(candidate) != null) continue;
            return candidate;
        }

        return center;
    }

    bool ShouldAlwaysTrackPlayer()
    {
        if (enemyInstance == null || enemyInstance.data == null) return false;
        if (!enemyInstance.data.isBoss) return false;
        if (FloorManager.Instance == null) return false;
        return FloorManager.Instance.CurrentFloor >= FloorManager.MaxFloors;
    }

    void MoveRandomly()
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int dir = directions[Random.Range(0, directions.Length)];
        Vector2Int newPos = enemyInstance.GridPos + dir;

        if (DungeonRenderer.Instance.IsWalkable(newPos) && EnemySpawner.Instance.GetEnemyAt(newPos) == null)
        {
            enemyInstance.SetGridPos(newPos);
        }
    }
}