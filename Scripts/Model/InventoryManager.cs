using UnityEngine;
using System.Collections.Generic;

public class InventoryManager
{
	// This is a list of all inventories.
	// Later on this will likely be organized by rooms instead
	public Dictionary< string, List<Inventory> > inventoryDic;

	public InventoryManager()
    {
        inventoryDic = new Dictionary< string, List<Inventory> >();
	}

	public bool PlaceInventory(Tile tile, Inventory inv)
    {

		bool tileWasEmpty = tile.inventory == null;

		if( tile.PlaceInventory(inv) == false )
        {
			// The tile did not accept the inventory, therefore stop.
			return false;
		}

		// At this point, "inv" might be an empty stack if it was merged to another stack.
        /*
		if(inv.stackSize == 0)
        {
			if(inventoryDic.ContainsKey(tile.inventory.objectType) )
            {
                inventoryDic[inv.objectType].Remove(inv);
			}
		}*/

		// We may have created a new stack on the tile, if the tile was previously empty.
		if( tileWasEmpty )
        {
			if(inventoryDic.ContainsKey(tile.inventory.objectType) == false )
            {
                inventoryDic[tile.inventory.objectType] = new List<Inventory>();
			}

            inventoryDic[tile.inventory.objectType].Add( tile.inventory );
		}

		return true;
	}
}
