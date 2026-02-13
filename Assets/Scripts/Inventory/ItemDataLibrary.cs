using UnityEngine;

/// <summary>
/// Creates sample ItemData at runtime so we don't need ScriptableObject assets.
/// Attach to any persistent object (e.g. the Cart or a manager).
/// Other scripts can access items via the static Instance.
/// </summary>
public class ItemDataLibrary : MonoBehaviour
{
    public static ItemDataLibrary Instance { get; private set; }

    // Pre-built item templates
    public ItemData Beans { get; private set; }
    public ItemData Medkit { get; private set; }
    public ItemData Ammo { get; private set; }
    public ItemData GoldBar { get; private set; }
    public ItemData Bomb { get; private set; }
    public ItemData Shotgun { get; private set; }
    public ItemData Crate { get; private set; }
    public ItemData Gem { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
        CreateItems();
    }

    private void CreateItems()
    {
        // Small items (1x1)
        Beans = CreateItem("Beans", 1, 1, 1f, new Color(0.8f, 0.3f, 0.1f), ItemData.ItemType.Supply);
        Gem = CreateItem("Gem", 1, 1, 0.5f, new Color(0.9f, 0.1f, 0.9f), ItemData.ItemType.Valuable);

        // Medium items (1x2, 2x1)
        Medkit = CreateItem("Medkit", 2, 1, 2f, new Color(1f, 0.2f, 0.2f), ItemData.ItemType.Supply);
        Ammo = CreateItem("Ammo", 1, 2, 1.5f, new Color(0.9f, 0.7f, 0.2f), ItemData.ItemType.Supply);

        // Valuable (2x1)
        GoldBar = CreateItem("Gold Bar", 2, 1, 5f, new Color(1f, 0.85f, 0.1f), ItemData.ItemType.Valuable);

        // Large items
        Bomb = CreateItem("Bomb", 2, 2, 4f, new Color(0.3f, 0.3f, 0.3f), ItemData.ItemType.Bomb);
        Shotgun = CreateItem("Shotgun", 1, 4, 6f, new Color(0.5f, 0.35f, 0.2f), ItemData.ItemType.Weapon);
        Crate = CreateItem("Crate", 3, 3, 10f, new Color(0.6f, 0.4f, 0.2f), ItemData.ItemType.Supply);

        Debug.Log("[LIBRARY] ✅ Item library created (8 items)");
    }

    private ItemData CreateItem(string name, int w, int h, float weight, Color color, ItemData.ItemType type)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = name;
        item.width = w;
        item.height = h;
        item.weight = weight;
        item.itemColor = color;
        item.type = type;
        item.canRotate = true;
        item.name = name;
        return item;
    }

    /// <summary>Get a random item from the library.</summary>
    public ItemData GetRandomItem()
    {
        ItemData[] all = { Beans, Gem, Medkit, Ammo, GoldBar, Bomb, Shotgun, Crate };
        return all[Random.Range(0, all.Length)];
    }

    /// <summary>Get a random small/medium item (good for world spawning).</summary>
    public ItemData GetRandomSmallItem()
    {
        ItemData[] small = { Beans, Gem, Medkit, Ammo, GoldBar };
        return small[Random.Range(0, small.Length)];
    }
}
