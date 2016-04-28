using UnityEngine;
using System.Collections.Generic;
using System;

public class Job
{
    //Hold information for queued jobs.
    //Examples: Place furniture, moving objects, working at workbench, etc.

    //Where job is located.
    public Tile tile;

    //How long Job takes
    public float jobTime { get; protected set; }

    //Whether the job requires the character to be in same tile or only adjacent. Used in pathfinding.
    //True = same tile. False = adjacent
    public bool characterStandOnTile {get; protected set;}

    //Name of the thing the job acts on.
    //TODO bad, change this to generic object
    public string jobObjectType { get; protected set; }

    //Call when job is complete (jobTime = 0)
    Action<Job> cbJobComplete;

    //Call when job is cancled
    Action<Job> cbJobCancel;

    //Call when job is worked on
    Action<Job> cbJobWorked;

    public bool canTakeFromStockpile = true;

    public Dictionary<string, Inventory> inventoryRequirements;

    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobTime, Inventory[] invReqs, bool characterStandOnTile = true)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobComplete = cbJobComplete;
        this.jobTime = jobTime;
        this.characterStandOnTile = characterStandOnTile;

        //Deep copy the job requirements
        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (invReqs != null)
        {
            foreach (Inventory inv in invReqs)
            {
                this.inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    protected Job(Job other)
    {
        this.tile = other.tile;
        this.jobObjectType = other.jobObjectType;
        this.cbJobComplete = other.cbJobComplete;
        this.jobTime = other.jobTime;
        this.characterStandOnTile = other.characterStandOnTile;

        //Deep copy the job requirements
        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (other.inventoryRequirements != null)
        {
            foreach (Inventory inv in other.inventoryRequirements.Values)
            {
                this.inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    virtual public Job Clone()
    {
        return new Job(this);
    }

    public void DoWork(float workTime)
    {
        if (HasAllRequiredMaterials() == false)
        {
            Debug.Log("Tried to do work on a job that does not have a required materials.");

            if (cbJobWorked != null)
            {
                cbJobWorked(this);
            }

            return;
        }

        jobTime -= workTime;

        if (cbJobWorked != null)
        {
            cbJobWorked(this);
        }

        if (jobTime <= 0)
        {
            if (cbJobComplete != null)
            {
                cbJobComplete(this);
            }
        }
    }

    public void CancelJob()
    {
        if (cbJobCancel != null)
        {
            cbJobCancel(this);
        }

        tile.world.jobQueue.Remove(this); //TODO tile may be null?
    }

    public bool HasAllRequiredMaterials()
    {
        foreach(Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
            {
                return false;
            }
        }
        return true;
    }

    public int IsItemRequired(Inventory inv)
    {
        //Item not needed
        if (inventoryRequirements.ContainsKey(inv.objectType) == false)
        {
            return 0;
        }

        //Already have needed amount of that item
        if (inventoryRequirements[inv.objectType].stackSize >= inventoryRequirements[inv.objectType].maxStackSize)
        {
            return 0;
        }

        //Need more of that item.
        return inventoryRequirements[inv.objectType].maxStackSize - inventoryRequirements[inv.objectType].stackSize;
    }

    public Inventory GetFirstRequiredInventory()
    {
        foreach (Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
            {
                return inv;
            }
        }

        return null;
    }

    #region Callbacks
    public void RegisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete += cb;
    }

    public void UnregisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete -= cb;
    }

    public void RegisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel += cb;
    }

    public void UnregisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel -= cb;
    }

    public void RegisterJobWorkedCallback(Action<Job> cb)
    {
        cbJobWorked += cb;
    }

    public void UnregisterJobWorkedCallback(Action<Job> cb)
    {
        cbJobWorked -= cb;
    }
    #endregion

}
