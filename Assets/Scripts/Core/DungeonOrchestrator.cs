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
        GameManager.Instance.SetState(GameState.Exploring);
    }
}
