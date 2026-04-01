using UnityEngine;

public class EnemyInstance : MonoBehaviour
{
    public EnemyData data;
    public int CurrentHP { get; private set; }
    public Vector2Int GridPos { get; private set; }

    public void Init(EnemyData d, Vector2Int pos)
    {
        data = d;
        CurrentHP = d.maxHP;
        GridPos = pos;
        transform.position = GridMover.GridToWorld(pos);
        if (d.elementVFXPrefab)
            Instantiate(d.elementVFXPrefab, transform);
    }

    // Returns true if enemy died
    public bool TakeDamage(int amount)
    {
        CurrentHP -= amount;
        return CurrentHP <= 0;
    }
}
