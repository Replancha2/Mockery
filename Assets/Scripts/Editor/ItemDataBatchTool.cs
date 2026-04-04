#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ItemDataBatchTool : EditorWindow
{
    private const string DefaultFolder = "Assets/ScriptableObjects/GeneratedItems";

    private string targetFolder = DefaultFolder;
    private int generateCount = 12;
    private bool randomizeElementBonus = true;
    private int minPrice = 2;
    private int maxPrice = 15;
    private int maxHpBonus = 8;
    private int maxDefenseBonus = 5;
    private int maxElementBonus = 5;

    private NPCSpawner targetSpawner;

    [MenuItem("Tools/Mockery/Item Database Tool")]
    public static void Open()
    {
        GetWindow<ItemDataBatchTool>("Item Database Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Item Generation", EditorStyles.boldLabel);
        targetFolder = EditorGUILayout.TextField("Target Folder", targetFolder);
        generateCount = EditorGUILayout.IntSlider("Generate Count", generateCount, 1, 100);
        randomizeElementBonus = EditorGUILayout.Toggle("Random Element Type", randomizeElementBonus);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stat Ranges", EditorStyles.boldLabel);
        minPrice = EditorGUILayout.IntField("Min Price", minPrice);
        maxPrice = EditorGUILayout.IntField("Max Price", maxPrice);
        maxHpBonus = EditorGUILayout.IntField("Max HP Bonus", maxHpBonus);
        maxDefenseBonus = EditorGUILayout.IntField("Max DEF Bonus", maxDefenseBonus);
        maxElementBonus = EditorGUILayout.IntField("Max Element Bonus", maxElementBonus);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Random Item Assets"))
        {
            GenerateRandomItems();
        }

        if (GUILayout.Button("Randomize Existing Items In Folder"))
        {
            RandomizeExistingItemsInFolder();
        }

        if (GUILayout.Button("Randomize Selected ItemData Assets"))
        {
            RandomizeSelectedItems();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vendor/Beggar Setup", EditorStyles.boldLabel);
        targetSpawner = (NPCSpawner)EditorGUILayout.ObjectField("Target NPC Spawner", targetSpawner, typeof(NPCSpawner), true);

        if (GUILayout.Button("Assign All ItemData Assets To NPCSpawner Shared Pool"))
        {
            AssignAllItemsToSpawner();
        }
    }

    private void GenerateRandomItems()
    {
        EnsureFolder(targetFolder);

        for (int i = 0; i < generateCount; i++)
        {
            ItemData item = CreateInstance<ItemData>();
            FillRandomItem(item, i);

            string fileName = $"Item_Auto_{item.slot}_{i + 1:000}.asset";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{fileName}");
            AssetDatabase.CreateAsset(item, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ItemDataBatchTool] Generated {generateCount} random ItemData assets in {targetFolder}.");
    }

    private void RandomizeExistingItemsInFolder()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { targetFolder });
        int count = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null) continue;

            FillRandomItem(item, i);
            EditorUtility.SetDirty(item);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ItemDataBatchTool] Randomized {count} existing ItemData assets in {targetFolder}.");
    }

    private void RandomizeSelectedItems()
    {
        Object[] selection = Selection.objects;
        int count = 0;

        for (int i = 0; i < selection.Length; i++)
        {
            ItemData item = selection[i] as ItemData;
            if (item == null) continue;

            FillRandomItem(item, i);
            EditorUtility.SetDirty(item);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ItemDataBatchTool] Randomized {count} selected ItemData assets.");
    }

    private void AssignAllItemsToSpawner()
    {
        if (targetSpawner == null)
        {
            Debug.LogWarning("[ItemDataBatchTool] Assign failed: Target NPC Spawner is not set.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets" });
        var allItems = new List<ItemData>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null) allItems.Add(item);
        }

        targetSpawner.sharedItemPool = allItems.ToArray();
        EditorUtility.SetDirty(targetSpawner);
        EditorSceneManager.MarkSceneDirty(targetSpawner.gameObject.scene);

        Debug.Log($"[ItemDataBatchTool] Assigned {allItems.Count} ItemData assets to NPCSpawner.sharedItemPool on {targetSpawner.name}.");
    }

    private void FillRandomItem(ItemData item, int seedIndex)
    {
        EquipmentSlot slot = (EquipmentSlot)Random.Range(0, System.Enum.GetValues(typeof(EquipmentSlot)).Length);
        SpellType element = (SpellType)Random.Range(0, System.Enum.GetValues(typeof(SpellType)).Length);

        item.slot = slot;
        item.elementBonus = randomizeElementBonus ? element : SpellType.Consonant;
        item.itemName = BuildRandomName(slot, element, seedIndex);

        item.buyPrice = Random.Range(Mathf.Max(1, minPrice), Mathf.Max(minPrice + 1, maxPrice + 1));
        item.hpBonus = Random.Range(0, Mathf.Max(1, maxHpBonus + 1));
        item.defenseBonus = Random.Range(0, Mathf.Max(1, maxDefenseBonus + 1));

        item.AssonantBonus = 0;
        item.DissonantBonus = 0;
        item.ConsonantBonus = 0;

        int elementPower = Random.Range(0, Mathf.Max(1, maxElementBonus + 1));
        switch (item.elementBonus)
        {
            case SpellType.Assonant:
                item.AssonantBonus = elementPower;
                break;
            case SpellType.Dissonant:
                item.DissonantBonus = elementPower;
                break;
            default:
                item.ConsonantBonus = elementPower;
                break;
        }
    }

    private static string BuildRandomName(EquipmentSlot slot, SpellType element, int index)
    {
        string[] prefixes = { "Echo", "Worn", "Silent", "Brave", "Rough", "Ancient", "Shiny", "Broken" };
        string[] suffixes = { "of Rhythm", "of Dust", "of Night", "of Sparks", "of Wind", "of Verse", "of Tone", "of Pulse" };

        string prefix = prefixes[Random.Range(0, prefixes.Length)];
        string suffix = suffixes[Random.Range(0, suffixes.Length)];

        return $"{prefix} {slot} {element} {suffix} #{index + 1}";
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
#endif
