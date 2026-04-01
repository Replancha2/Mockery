using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }
    public int CurrentFloor { get; private set; } = 1;
    public const int MaxFloors = 5;

    // Timer window shrinks per floor: 6s -> 4s across 5 floors
    public float GetInputTimeLimit() => Mathf.Max(3f, 6f - (CurrentFloor - 1) * 0.5f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetFloor() => CurrentFloor = 1;

    public void NextFloor()
    {
        CurrentFloor++;
        if (CurrentFloor > MaxFloors)
            GameManager.Instance.Victory();
    }
}
