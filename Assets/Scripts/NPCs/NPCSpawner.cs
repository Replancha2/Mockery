using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner Instance { get; private set; }

    [Header("Spawn Probabilities")]
    [Range(0f, 1f)] public float vendorSpawnChance = 0.6f;  // 60% chance vendor appears
    [Range(0f, 1f)] public float beggarSpawnChance = 0.4f;  // 40% chance beggar appears

    [Header("NPC Prefabs/References")]
    public GameObject vendorPrefab;
    public GameObject beggarPrefab;

    [Header("Sprites")]
    public Sprite vendorSprite;
    public Sprite beggarSprite;

    [Header("Shared Item Pool")]
    public ItemData[] sharedItemPool;

    private VendorNPC vendorInstance;
    private BeggarNPC beggarInstance;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnNPCs(DungeonData data)
    {
        int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : -1;
        Debug.Log($"[NPCSpawner] Floor {floor}: Vendor target={data.VendorPos}, Beggar target={data.BeggarPos}");

        // Determine if vendor spawns this floor
        bool spawnVendor = Random.value < vendorSpawnChance;
        if (spawnVendor && data.VendorPos != Vector2Int.zero)
        {
            if (vendorInstance == null)
            {
                vendorInstance = CreateVendor(data.VendorPos);
            }
            else
            {
                vendorInstance.gameObject.SetActive(true);
                EnsureVendorItems(vendorInstance);
                vendorInstance.InitForFloor(data.VendorPos);
            }
            Debug.Log($"[NPCSpawner] Floor {floor}: Vendor spawned at {data.VendorPos} (world {GridMover.GridToWorld(data.VendorPos)})");
        }
        else if (vendorInstance != null)
        {
            vendorInstance.gameObject.SetActive(false);
            Debug.Log($"[NPCSpawner] Floor {floor}: Vendor not spawned this floor (target {data.VendorPos})");
        }
        else
        {
            Debug.Log($"[NPCSpawner] Floor {floor}: Vendor not spawned this floor (target {data.VendorPos})");
        }

        // Determine if beggar spawns this floor
        bool spawnBeggar = Random.value < beggarSpawnChance;
        if (spawnBeggar && data.BeggarPos != Vector2Int.zero)
        {
            if (beggarInstance == null)
            {
                beggarInstance = CreateBeggar(data.BeggarPos);
            }
            else
            {
                beggarInstance.gameObject.SetActive(true);
                EnsureBeggarItems(beggarInstance);
                beggarInstance.InitForFloor(data.BeggarPos);
            }
            Debug.Log($"[NPCSpawner] Floor {floor}: Beggar spawned at {data.BeggarPos} (world {GridMover.GridToWorld(data.BeggarPos)})");
        }
        else if (beggarInstance != null)
        {
            beggarInstance.gameObject.SetActive(false);
            Debug.Log($"[NPCSpawner] Floor {floor}: Beggar not spawned this floor (target {data.BeggarPos})");
        }
        else
        {
            Debug.Log($"[NPCSpawner] Floor {floor}: Beggar not spawned this floor (target {data.BeggarPos})");
        }
    }

    private VendorNPC CreateVendor(Vector2Int pos)
    {
        GameObject go = Instantiate(vendorPrefab, transform);
        VendorNPC vendor = go.GetComponent<VendorNPC>();
        if (vendor == null) vendor = go.AddComponent<VendorNPC>();
        
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = vendorSprite;

        EnsureVendorItems(vendor);

        vendor.InitForFloor(pos);
        return vendor;
    }

    private BeggarNPC CreateBeggar(Vector2Int pos)
    {
        GameObject go = Instantiate(beggarPrefab, transform);
        BeggarNPC beggar = go.GetComponent<BeggarNPC>();
        if (beggar == null) beggar = go.AddComponent<BeggarNPC>();
        
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = beggarSprite;

        EnsureBeggarItems(beggar);

        beggar.InitForFloor(pos);
        return beggar;
    }

    private void EnsureVendorItems(VendorNPC vendor)
    {
        if (vendor == null) return;
        if (HasAnyValidItem(vendor.itemPool)) return;

        ItemData[] validItems = GetValidSharedItems();
        if (validItems.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner] No valid items found for VendorNPC. Assign items to sharedItemPool.");
            return;
        }

        vendor.itemPool = validItems;
        Debug.Log($"[NPCSpawner] Assigned {validItems.Length} shared items to VendorNPC.");
    }

    private void EnsureBeggarItems(BeggarNPC beggar)
    {
        if (beggar == null) return;
        if (HasAnyValidItem(beggar.itemPool)) return;

        ItemData[] validItems = GetValidSharedItems();
        if (validItems.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner] No valid items found for BeggarNPC. Assign items to sharedItemPool.");
            return;
        }

        beggar.itemPool = validItems;
        Debug.Log($"[NPCSpawner] Assigned {validItems.Length} shared items to BeggarNPC.");
    }

    private ItemData[] GetValidSharedItems()
    {
        var list = new System.Collections.Generic.List<ItemData>();

        if (sharedItemPool != null)
        {
            foreach (var item in sharedItemPool)
            {
                if (item != null && !list.Contains(item)) list.Add(item);
            }
        }

        if (list.Count == 0)
            Debug.LogWarning("[NPCSpawner] GetValidSharedItems found 0 valid ItemData references.");

        return list.ToArray();
    }

    private static bool HasAnyValidItem(ItemData[] pool)
    {
        if (pool == null || pool.Length == 0) return false;
        foreach (var item in pool)
        {
            if (item != null) return true;
        }
        return false;
    }

    public VendorNPC GetVendor() => vendorInstance;
    public BeggarNPC GetBeggar() => beggarInstance;
}
