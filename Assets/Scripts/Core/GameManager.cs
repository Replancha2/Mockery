using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public event System.Action<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        DOTween.Init();
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void StartNewRun()
    {
        FloorManager.Instance.ResetRun();
        PlayerStats.Instance.ResetForNewRun();
        PlayerInventory.Instance.ResetInventory();
        SceneManager.LoadScene("Game");
        SetState(GameState.Exploring);
    }

    public void GameOver()  => SceneManager.LoadScene("GameOver");
    public void Victory()   => SceneManager.LoadScene("Victory");
}
