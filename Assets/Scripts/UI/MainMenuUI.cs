using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button startButton;
    public Button quitButton;

    void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartPressed);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartPressed);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    void OnStartPressed()
    {
        GameManager.Instance.LoadTutorial();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
