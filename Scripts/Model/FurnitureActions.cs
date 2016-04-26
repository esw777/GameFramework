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

    public static void Stockpile_UpdateAction(Furniture furn, float deltaTime)
    {
        //Need to ensure that there is a job on the queue that is asking for:
            //1. Empty tile - any valid object time to be hauled here
            //2. Non-Empty, but below maxStackSize - more of the current item type to be hauled here.

        if (furn.JobCount() > 0)
        {
            return; //already have a job, do not create another one
        }

        if (furn.tile.inventory == null)
        {
            //empty, ask for anything to be brought here
            Job j = new Job(
                furn.tile,
                null,
                null,
                0,
                new Inventory[1] { new Inventory("Steel Plate", 50, 0) }, //Once inventory filters are added, those filters need to be applied to this list.
                true
                );

            j.RegisterJobWorkedCallback(Stockpile_JobWorked);
            
            furn.AddJob(j);
        }

        else if (furn.tile.inventory.stackSize < furn.tile.inventory.maxStackSize)
        {
            //Room for more items
            Inventory reqInv = furn.tile.inventory.Clone();
            reqInv.maxStackSize -= reqInv.stackSize;
            reqInv.stackSize = 0;

            Job j = new Job(
                furn.tile,
                null,
                null,
                0,
                new Inventory[1] { new Inventory("Steel Plate", 50, 0) }, //Once inventory filters are added, those filters need to be applied to this list.
                true
            );

            j.RegisterJobWorkedCallback(Stockpile_JobWorked);

            furn.AddJob(j);
        }

        else //Tile is full
        {
            return;
        }
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

}
