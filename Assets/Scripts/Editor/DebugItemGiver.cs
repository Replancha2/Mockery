#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DebugItemGiver : EditorWindow
{
    private List<ItemData> items = new();
    private Vector2 scroll;

    [MenuItem("Tools/Debug Item Giver")]
    public static void Open() => GetWindow<DebugItemGiver>("Item Giver");

    void OnEnable() => RefreshItems();

    void RefreshItems()
    {
        items.Clear();
        var guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null) items.Add(item);
        }
        items.Sort((a, b) => string.Compare(a.itemName, b.itemName));
    }

    void OnGUI()
    {
        bool inPlayMode = Application.isPlaying;
        bool hasInventory = inPlayMode && PlayerInventory.Instance != null;

        EditorGUILayout.Space(4);

        if (!inPlayMode)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to give items.", MessageType.Info);
        }
        else if (!hasInventory)
        {
            EditorGUILayout.HelpBox("PlayerInventory not found in scene.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Click to give item to player.", MessageType.None);
        }

        EditorGUILayout.Space(4);

        using (new EditorGUI.DisabledScope(!hasInventory))
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EquipmentSlot? lastSlot = null;
            foreach (var item in items)
            {
                if (item.slot != lastSlot)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(item.slot.ToString(), EditorStyles.boldLabel);
                    lastSlot = item.slot;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{item.itemName}  (stats: {item.TotalStats()})", GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Give", GUILayout.Width(50)))
                    {
                        var result = PlayerInventory.Instance.AcquireItem(item);
                        string msg = result switch
                        {
                            PlayerInventory.ItemAcquireResult.Equipped         => $"Equipped {item.itemName} ({item.slot})",
                            PlayerInventory.ItemAcquireResult.Swapped          => $"Swapped {item.itemName} into {item.slot} slot",
                            PlayerInventory.ItemAcquireResult.StoredInBackpack => $"Sent {item.itemName} to backpack",
                            _                                                  => $"Gave {item.itemName}"
                        };
                        Debug.Log($"[DebugItemGiver] {msg}");
                        HUDController.Instance?.Log(msg);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Refresh List")) RefreshItems();
    }
}
#endif
