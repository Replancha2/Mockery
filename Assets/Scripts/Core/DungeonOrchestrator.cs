using UnityEngine;

public class DungeonOrchestrator : MonoBehaviour
{
    void Start() => GenerateFloor();

    public void GenerateFloor()
    {
        try
        {
            DungeonData data = BSPGenerator.Instance.Generate();
            DungeonRenderer.Instance.Render(data);
            EnemySpawner.Instance.SpawnEnemies(data, FloorManager.Instance.CurrentFloor);
            ItemSpawner.Instance.SpawnItems(data);
            NPCSpawner.Instance.SpawnNPCs(data);

            var mover = FindFirstObjectByType<GridMover>();
            if (mover == null)
            {
                Debug.LogError("DungeonOrchestrator: GridMover not found in scene!");
                return;
            }

            Debug.Log($"PlayerSpawn grid: {data.PlayerSpawn}  →  world: {GridMover.GridToWorld(data.PlayerSpawn)}");
            mover.SetGridPosition(data.PlayerSpawn);
            GameManager.Instance.SetState(GameState.Exploring);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error generating floor: {ex}\n{ex.StackTrace}");
            GameManager.Instance.SetState(GameState.Exploring);  // Reset state so player can move
        }
    }
}
