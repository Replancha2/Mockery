using UnityEngine;
using UnityEditor;
using System.IO;

public static class BuffAssetGenerator
{
    private const string OutputPath = "Assets/ScriptableObjects/Buffs";

    [MenuItem("Mockery/Generate Buff Assets")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder(OutputPath))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Buffs");

        // --- 9 Spell Damage Buffs ---
        var damageBuffs = new[]
        {
            (element: SpellType.Consonant, value: 2,  name: "Minor Consonant Edge",    desc: "Your consonant spells hit harder. +2 Consonant damage."),
            (element: SpellType.Consonant, value: 4,  name: "Consonant Edge",           desc: "Your consonant spells strike with force. +4 Consonant damage."),
            (element: SpellType.Consonant, value: 6,  name: "Greater Consonant Edge",   desc: "Your consonant spells devastate enemies. +6 Consonant damage."),
            (element: SpellType.Assonant,  value: 2,  name: "Minor Assonant Edge",      desc: "Your assonant spells hit harder. +2 Assonant damage."),
            (element: SpellType.Assonant,  value: 4,  name: "Assonant Edge",            desc: "Your assonant spells strike with force. +4 Assonant damage."),
            (element: SpellType.Assonant,  value: 6,  name: "Greater Assonant Edge",    desc: "Your assonant spells devastate enemies. +6 Assonant damage."),
            (element: SpellType.Dissonant, value: 2,  name: "Minor Dissonant Edge",     desc: "Your dissonant spells hit harder. +2 Dissonant damage."),
            (element: SpellType.Dissonant, value: 4,  name: "Dissonant Edge",           desc: "Your dissonant spells strike with force. +4 Dissonant damage."),
            (element: SpellType.Dissonant, value: 6,  name: "Greater Dissonant Edge",   desc: "Your dissonant spells devastate enemies. +6 Dissonant damage."),
        };

        foreach (var b in damageBuffs)
        {
            string assetName = $"Buff_{b.element}_Damage_{b.value}";
            CreateBuff(assetName, b.name, b.desc, BuffEffectType.SpellDamageBonus, b.element, b.value);
        }

        // --- Potion Heal Bonus ---
        CreateBuff(
            "Buff_Potion_HealBonus",
            "Alchemist's Touch",
            "Your potions are more potent. Potions restore an additional 10% of your max HP.",
            BuffEffectType.PotionHealBonus,
            SpellType.Consonant, // element unused
            10
        );

        // --- Vendor Discount ---
        CreateBuff(
            "Buff_Vendor_Discount",
            "Haggler's Wit",
            "You know how to strike a deal. All vendor prices are reduced by 20%.",
            BuffEffectType.VendorDiscount,
            SpellType.Consonant, // element unused
            20
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BuffAssetGenerator] Created 11 buff assets in {OutputPath}");
    }

    private static void CreateBuff(string assetName, string buffName, string description,
        BuffEffectType effectType, SpellType element, int value)
    {
        string fullPath = $"{OutputPath}/{assetName}.asset";

        // Overwrite if it already exists
        var existing = AssetDatabase.LoadAssetAtPath<BuffData>(fullPath);
        if (existing != null)
        {
            existing.buffName    = buffName;
            existing.description = description;
            existing.effectType  = effectType;
            existing.element     = element;
            existing.value       = value;
            EditorUtility.SetDirty(existing);
            return;
        }

        var buff = ScriptableObject.CreateInstance<BuffData>();
        buff.buffName    = buffName;
        buff.description = description;
        buff.effectType  = effectType;
        buff.element     = element;
        buff.value       = value;

        AssetDatabase.CreateAsset(buff, fullPath);
    }
}
