using UnityEngine;
using System.Collections;

public static class FurnitureActions
{
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        //Debug.Log("Door_UpdateAction called");

        if (furn.GetParameter("is_opening") >= 1)
        {
            furn.ChangeParameter("openness", deltaTime * 4);

            if (furn.GetParameter("openness") >= 1) //Stay open for a second
            {
                //Start closing the door.
                furn.SetParameter("is_opening", 0);
            }
        }

        else
        {
            furn.ChangeParameter("openness", -1 * deltaTime * 4);
        }

        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")));

        //TODO this gets called every frame - bad
        if (furn.cbOnChanged != null)
        {
            furn.cbOnChanged(furn);
        }
    }

    public static Enterability Door_IsEnterable(Furniture furn)
    {
        //If this is called, then something wants to enter the same tile as the door. So open the door
        furn.SetParameter("is_opening", 1);

        if (furn.GetParameter("openness") >= 1)
        {
            return Enterability.Yes;
        }

        return Enterability.Soon;
    }


    public static void JobComplete_FurnitureBuilding(Job theJob)
    {
        WorldController.Instance.world.PlaceFurniture(theJob.jobObjectType, theJob.tile);

        theJob.tile.pendingFurnitureJob = null;
    }

    public static Inventory[] Stockpile_GetItemsFromFilter()
    {
        //TODO reads data from UI to get item filter defined by user for a stockpile;
        return new Inventory[1] { new Inventory("Steel Plate", 50, 0) };
    }

    public static void Stockpile_UpdateAction(Furniture furn, float deltaTime)
    {
        //Need to ensure that there is a job on the queue that is asking for:
        //1. Empty tile - any valid object time to be hauled here
        //2. Non-Empty, but below maxStackSize - more of the current item type to be hauled here.

        //TODO: this function should not be called every frame. Can be run based on trigger - Initial creation, item deliveries / pickups, UI filer is changed.

        if (furn.tile.inventory != null && furn.tile.inventory.stackSize >= furn.tile.inventory.maxStackSize)
        {
            //Full, do not need to do anything.
            furn.ClearJobs();
            return;
        }

        if (furn.JobCount() > 0)
        {
            return; //already have a job, do not create another one
        }

        //TODO debug
        if (furn.tile.inventory != null && furn.tile.inventory.maxStackSize == 0)
        {
            Debug.LogError("Stockpile has a stack with 0 size - something screwed up");
            return;
        }

        //TODO - In future, stockpiles next to each other should combine into 1 large "furniture" object rather than being individual objects.

        //Request new items
        Inventory[] reqInv;

        //No existing items, get any item type delivered
        if (furn.tile.inventory == null)
        {
            reqInv = Stockpile_GetItemsFromFilter();
        }

        //Only path left is stockpile has a partial stacksize of an item.
        else //if (furn.tile.inventory.stackSize < furn.tile.inventory.maxStackSize)
        {
            //Room for more items of same type that is already here
            Inventory tmpInv = furn.tile.inventory.Clone();
            tmpInv.maxStackSize -= tmpInv.stackSize;
            tmpInv.stackSize = 0;

            reqInv = new Inventory[] { tmpInv };
        }

        Job j = new Job(
            furn.tile,
            null,
            null,
            0,
            reqInv,
            true
        );

        j.canTakeFromStockpile = false;
        j.RegisterJobWorkedCallback(Stockpile_JobWorked);
        furn.AddJob(j);
    }

    static void Stockpile_JobWorked(Job j)
    {
        j.tile.furniture.RemoveJob(j);

        //TODO Change this when we have more than one loose item type (will match what the update action has to do for creating a job.
        foreach (Inventory inv in j.inventoryRequirements.Values)
        {
            j.tile.world.inventoryManager.PlaceInventory(j.tile, inv);
        }
    }

    public static void OxygenGenerator_UpdateAction(Furniture furn, float deltaTime)
    {
        if (furn.tile.room.GetGasAmount("O2") < 0.20f)
        {
            furn.tile.room.ChangeGas("O2", 0.01f * deltaTime); //TODO replace hardcoded values

            //TODO consume electricity if running, etc.
        }

        //else dont use electricity
    }
}
