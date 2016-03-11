using UnityEngine;
using System.Collections;
using System;

public class Job
{
    //Hold information for queued jobs.
    //Examples: Place furniture, moving objects, working at workbench, etc.

    //Where job is located.
    public Tile tile { get; protected set; }

    //How long Job takes
    float jobTime = 1f;

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

    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobTime = 1f, bool characterStandOnTile = true)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobComplete = cbJobComplete;
        this.jobTime = jobTime;
        this.characterStandOnTile = characterStandOnTile;
    }

    public void DoWork(float workTime)
    {
        jobTime -= workTime;

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
    }

    #region Callbacks
    public void RegisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete += cb;
    }

    public void RegisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel += cb;
    }
    public void UnregisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete -= cb;
    }

    public void UnregisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel -= cb;
    }
    #endregion

}
