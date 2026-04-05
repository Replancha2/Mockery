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

    public void LoadTutorial() => SceneManager.LoadScene("Tutorial");

    public void StartNewRun()
    {
        FloorManager.Instance.ResetRun();
        PlayerStats.Instance.ResetForNewRun();
        PlayerInventory.Instance.ResetInventory();
        SceneManager.LoadScene("GamePlay");
        SetState(GameState.Exploring);
    }

    public void GameOver()
    {
        SetState(GameState.GameOver);
        SceneManager.LoadScene("GameOver");
    }

    public void Victory()
    {
        SetState(GameState.Victory);
        SceneManager.LoadScene("GameOver");
    }
}
