using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventorySpriteController : MonoBehaviour
{
    public GameObject inventoryUIPrefab;

    Dictionary<Inventory, GameObject> inventoryGameObjectMap;

    Dictionary<string, Sprite> inventorySprites;

    World world
    {
        get
        {
            return WorldController.Instance.world;
        }
    }

    // Use this for initialization
    void Start ()
    {
        LoadSprites();

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();

        //Register callback
        world.RegisterInventoryCreated(OnInventoryCreated);

        //If world was loaded from a save file, there may be existing inventory
        foreach (string objectType in world.inventoryManager.inventoryDic.Keys)
        {
            foreach(Inventory inv in world.inventoryManager.inventoryDic[objectType])
            {
                OnInventoryCreated(inv);
            }
        }
    }
    void LoadSprites()
    {
        inventorySprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Inventory/");

        //Debug.Log("LOADED RESOURCE:");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            inventorySprites[s.name] = s;
        }
    }

    public void OnInventoryCreated(Inventory inv)
    {
        // Create a visual GameObject linked to this data.

        Debug.Log("SpriteController: OnInventoryCreated called");

        // This creates a new GameObject and adds it to our scene.
        GameObject inv_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        inventoryGameObjectMap.Add(inv, inv_go);

        inv_go.name = inv.objectType;
        inv_go.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
        inv_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = inv_go.AddComponent<SpriteRenderer>();
        sr.sprite = inventorySprites[inv.objectType]; 
        sr.sortingLayerName = "Inventory";

        if (inv.maxStackSize > 1)
        {
            //Multiple items in a stack, show a visual number that represents stack size
            //Shows a number on top of the item
            
            GameObject ui_go = Instantiate(inventoryUIPrefab);
            ui_go.transform.SetParent(inv_go.transform);
            ui_go.transform.localPosition = Vector3.zero;
            ui_go.GetComponentInChildren<Text>().text = inv.stackSize.ToString();
            
        }


        // TODO add an onChanged callback

    }

    void OnInventoryChanged(Inventory inv)
    {
        //Not working/called yet

        if (inventoryGameObjectMap.ContainsKey(inv) == false)
        {
            Debug.LogError("OnInventoryChanged -- trying to change visuals for Inventory not in our map.");
            return;
        }

        GameObject inv_go = inventoryGameObjectMap[inv];

        //char_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForCharacter(character);
        inv_go.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
    }
}
