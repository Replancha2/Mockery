using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MinimapSetup
{
    [MenuItem("DCJam/Setup Minimap")]
    static void Setup()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MinimapSetup: No Canvas found in scene. Open the GamePlay scene first.");
            return;
        }

        // Check for duplicate
        if (canvas.GetComponentInChildren<MinimapController>() != null)
        {
            Debug.Log("MinimapSetup: Minimap already set up.");
            Selection.activeGameObject = canvas.GetComponentInChildren<MinimapController>().gameObject;
            return;
        }

        // ── Outer container (dark frame + MinimapController) ──────────────────
        var container = new GameObject("MinimapContainer");
        GameObjectUtility.SetParentAndAlign(container, canvas.gameObject);
        Undo.RegisterCreatedObjectUndo(container, "Create Minimap");

        var cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin        = new Vector2(1f, 1f);   // top-right anchor
        cRect.anchorMax        = new Vector2(1f, 1f);
        cRect.pivot            = new Vector2(1f, 1f);
        cRect.anchoredPosition = new Vector2(-12f, -12f);
        cRect.sizeDelta        = new Vector2(130f, 130f);

        var frame = container.AddComponent<Image>();
        frame.color = new Color(0f, 0f, 0f, 0.75f);

        // ── Inner RawImage (the actual minimap pixels) ─────────────────────────
        var rawGO = new GameObject("MinimapImage");
        GameObjectUtility.SetParentAndAlign(rawGO, container);
        Undo.RegisterCreatedObjectUndo(rawGO, "Create Minimap Image");

        var rRect = rawGO.AddComponent<RectTransform>();
        rRect.anchorMin = Vector2.zero;
        rRect.anchorMax = Vector2.one;
        rRect.offsetMin = new Vector2(6f,  6f);
        rRect.offsetMax = new Vector2(-6f, -6f);

        var rawImage = rawGO.AddComponent<RawImage>();
        rawImage.color = Color.white;

        // ── Wire up MinimapController ──────────────────────────────────────────
        var controller = container.AddComponent<MinimapController>();
        var so         = new SerializedObject(controller);
        so.FindProperty("minimapImage").objectReferenceValue = rawImage;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = container;

        Debug.Log("MinimapSetup: Minimap created in top-right corner. Save the scene (Ctrl+S).");
    }
}
