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

    //Ensures references and links are cleaned up
    void CleanupInventory(Inventory inv)
    {
        if (inv.stackSize == 0)
        {
            if (inventoryDic.ContainsKey(inv.objectType))
            {
                inventoryDic[inv.objectType].Remove(inv);
            }

            if (inv.tile != null)
            {
                inv.tile.inventory = null;
                inv.tile = null;
            }

            if (inv.character != null)
            {
                inv.character.inventory = null;
                inv.character = null;
            }
        }
    }

    //Places inventory into a tile //TODO account for amount variable
	public bool PlaceInventory(Tile tile, Inventory sourceInv, int amount = -1)
    {
		bool tileWasEmpty = tile.inventory == null;

		if( tile.PlaceInventory(sourceInv) == false )
        {
			// The tile did not accept the inventory, therefore stop.
			return false;
		}

        // At this point, "inv" might be an empty stack if it was merged to another stack.
        CleanupInventory(sourceInv);

		// We may have created a new stack on the tile, if the tile was previously empty.
		if( tileWasEmpty )
        {
			if(inventoryDic.ContainsKey(tile.inventory.objectType) == false )
            {
                inventoryDic[tile.inventory.objectType] = new List<Inventory>();
			}

            inventoryDic[tile.inventory.objectType].Add( tile.inventory );

            tile.world.OnInventoryCreated(tile.inventory);
		}

		return true;
	}

    //Places inventory into a job
    public bool PlaceInventory(Job job, Inventory sourceInv, int amount = -1)
    {
        if (job.inventoryRequirements.ContainsKey(sourceInv.objectType) == false)
        {
            Debug.LogError("PlaceInventory (job,inv): Trying to add inventory to a job that does not want it");
            return false;
        }

        int amountToMove;
        if (amount == -1)
            amountToMove = sourceInv.stackSize;
        else
            amountToMove = Mathf.Min(amount, sourceInv.stackSize);

        //Job has room for all entire stack of items
        if (job.inventoryRequirements[sourceInv.objectType].stackSize + amountToMove <= job.inventoryRequirements[sourceInv.objectType].maxStackSize)
        {
            job.inventoryRequirements[sourceInv.objectType].stackSize += amountToMove;
            sourceInv.stackSize -= amountToMove;
        }

        //Job can only accept a partial stack of items
        else
        {
            amountToMove -= (job.inventoryRequirements[sourceInv.objectType].maxStackSize - job.inventoryRequirements[sourceInv.objectType].stackSize);
            job.inventoryRequirements[sourceInv.objectType].stackSize = job.inventoryRequirements[sourceInv.objectType].maxStackSize;
            sourceInv.stackSize -= amountToMove;
        }

        // At this point, "inv" might be an empty stack if it was merged to another stack.
        CleanupInventory(sourceInv);

        return true;
    }

    //Places inventory into a character
    public bool PlaceInventory(Character character, Inventory sourceInv, int amount = -1)
    {
        int amountToMove;
        if (amount == -1)
            amountToMove = sourceInv.stackSize;
        else
            amountToMove = Mathf.Min(amount, sourceInv.stackSize);

        if (character.inventory == null)
        {
            character.inventory = sourceInv.Clone();
            character.inventory.stackSize = 0;  //Potentially iffy later on
            inventoryDic[character.inventory.objectType].Add(character.inventory);
        }

        else if (character.inventory.objectType != sourceInv.objectType)
        {
            Debug.LogError("Character is trying to pick up mismatch inventory object type");
            return false;
        }

        //Character has room for desired stack size
        if (character.inventory.stackSize + amountToMove <= character.inventory.maxStackSize)
        {
            character.inventory.stackSize += amountToMove;
            sourceInv.stackSize -= amountToMove;
        }

        else //Can only transfer partial stack
        {
            amountToMove -= (character.inventory.maxStackSize - character.inventory.stackSize);
            character.inventory.stackSize = character.inventory.maxStackSize;
            sourceInv.stackSize -= amountToMove;
        }

        // At this point, "inv" might be an empty stack if it was merged to another stack.
        CleanupInventory(sourceInv);

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="t">Tile to measure distance from.</param>
    /// <param name="desiredAmount">Desired amount, If no stack has enough, returns largest stack.</param>
    /// <param name="canTakeFromStockpile">Whether to consider stockpiles when finding items</param>
    /// <returns></returns>
    public Inventory GetClosestInventoryOfType(string objectType, Tile t, int desiredAmount, bool canTakeFromStockpile)
    {
        //FIXME, this does not actually return closest stack at the moment.
            //This is due to inventory database not being fully implemented (separated out tile/job/character/room inventories.)
            //Also due to pathfinding not using the room system yet.

        if(inventoryDic.ContainsKey(objectType) == false )
        {
            Debug.Log("GetClosestInventoryOfType: No item of desired type");
            return null;
        }

        foreach (Inventory inv in inventoryDic[objectType])
        {
            if (inv.tile != null)
            {
                if (canTakeFromStockpile || inv.tile.furniture == null || inv.tile.furniture.IsStockpile() == false)
                {
                    return inv;
                }
            }
        }

        return null;
    }
}
