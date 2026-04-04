using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuffSelectionUI : MonoBehaviour
{
    public static BuffSelectionUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;

    [Header("Buff Buttons (3)")]
    public Button[]          buffButtons;     // 3 buttons
    public TextMeshProUGUI[] buffNameLabels;  // title per button
    public TextMeshProUGUI[] buffDescLabels;  // description per button

    [Header("Buff Pool")]
    public BuffData[] buffPool;

    private List<BuffData> offered = new();
    private DungeonOrchestrator orchestrator;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    void Start()
    {
        GameManager.Instance.OnStateChanged += OnStateChanged;
        orchestrator = FindFirstObjectByType<DungeonOrchestrator>();

        for (int i = 0; i < buffButtons.Length; i++)
        {
            int idx = i;
            buffButtons[i].onClick.AddListener(() => SelectBuff(idx));
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance) GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state)
    {
        Debug.Log($"BuffSelectionUI got state: {state}");
        if (state == GameState.BuffSelection) Show();
        else                                  panel.SetActive(false);
    }

    void Show()
    {
        Debug.Log("BuffSelectionUI.Show() called");
        offered.Clear();
        var pool = new List<BuffData>(buffPool);

        // Remove already-active buffs to avoid duplicates when possible
        foreach (var active in FloorManager.Instance.ActiveBuffs)
            pool.Remove(active);

        for (int i = 0; i < buffButtons.Length; i++)
        {
            if (pool.Count == 0) { buffButtons[i].gameObject.SetActive(false); continue; }
            buffButtons[i].gameObject.SetActive(true);

            int idx  = Random.Range(0, pool.Count);
            BuffData b = pool[idx];
            pool.RemoveAt(idx);
            offered.Add(b);

            buffNameLabels[i].text = b.buffName;
            buffDescLabels[i].text  = b.description;
        }

        panel.SetActive(true);
    }

    void SelectBuff(int index)
    {
        if (index >= offered.Count) return;
        FloorManager.Instance.ApplyBuff(offered[index]);
        FloorManager.Instance.ApplyStartOfFloorEffects();
        panel.SetActive(false);
        orchestrator.GenerateFloor();
    }
}
