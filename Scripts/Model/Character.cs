using UnityEngine;
using System.Collections;
using System;

public class Character
{
    public float X
    {
        get
        {
            return Mathf.Lerp(currTile.X, nextTile.X, movementPercentage);
        }
    }

    public float Y
    {
        get
        {
            return Mathf.Lerp(currTile.Y, nextTile.Y, movementPercentage);
        }
    }

    Job myJob;
    Action<Character> cbCharacterChanged;

    public Tile currTile { get; protected set; }

    Tile destTile;
    Tile nextTile;
    Path_AStar pathAStar;


    float movementPercentage;

    float speed = 5f; //Tiles per second.

    public Character(Tile tile)
    {
        currTile = tile;
        destTile = tile;
        nextTile = tile;
    }

    //Manages doing work every tick. Returns true if character did work.
    void Tick_DoJob(float deltaTime)
    {
        if (myJob == null)
        {
            //Try to get new job
            myJob = currTile.world.jobQueue.DeQueue();

            if (myJob != null)
            {
                //Got new job

                //TODO prioritize closer jobs.

                destTile = myJob.tile;
                myJob.RegisterJobCancelCallback(OnJobEnded);
                myJob.RegisterJobCompleteCallback(OnJobEnded);
            }
        }

        if (currTile == destTile || currTile.IsNeighbour(destTile, true))
        {
            //Don't need to move
            if (myJob != null)
            {
                myJob.DoWork(deltaTime);
            }

            return; // true; //work was done, no more character actions can be taken.
        }

        return; // false; //work was done, no more character actions can be taken.
    }

    void Tick_DoMovement(float deltaTime)
    {
        if (currTile == destTile)
        {
            pathAStar = null;
            return; //false; //No movement action is needed.
        }

        if (nextTile == null || nextTile == currTile)
        {
            if (pathAStar == null || pathAStar.Length() == 0)
            {
                //Generate new path
                pathAStar = new Path_AStar(currTile.world, currTile, destTile, myJob.characterStandOnTile);
                if (pathAStar.Length() == 0)
                {
                    Debug.LogError("Character: Tick_DoMovement: Path_AStar returned no path to dest");
                    //TODO requeue job or something instead of destroying it
                    AbandonJob();
                    pathAStar = null;
                    return;
                }

            }
            //Path exists, get next tile to move to
            nextTile = pathAStar.DequeueTile();

            if (nextTile == currTile)
            {
                Debug.LogError("Character: Tick_DoMovement: nextTile = curTile??");
            }

        }

        //Distance from Tile A to Tile B
        float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.X - nextTile.X, 2) + Mathf.Pow(currTile.Y - nextTile.Y, 2));

        //How far can we go this update
        float distThisFrame = speed * deltaTime;

        //How much is that in terms of percentage
        float percThisFram = distThisFrame / distToTravel;

        //Add that percent to overall percent traveled
        movementPercentage += percThisFram;

        if (movementPercentage >= 1)
        {
            //reached destination tile
            currTile = nextTile;
            movementPercentage = 0;

            //Get next tile from pathfinding system
            //If return is null, we have reached destination.

            //TODO callback for reached destination?
            //TODO retain extra move past destination?
        }

        return; //true;
    }

    //Character will abandon current job. Requeues the job.
    public void AbandonJob()
    {
        nextTile = destTile = currTile;
        pathAStar = null;
        currTile.world.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    public void Tick(float deltaTime)
    {
        Tick_DoJob(deltaTime);
        Tick_DoMovement(deltaTime);

        if (cbCharacterChanged != null)
        {
            cbCharacterChanged(this);
        }
    }

    public void setDestinationTile(Tile tile)
    {
        if (currTile.IsNeighbour(tile, true) == false)
        {
            Debug.Log("Character - SetDestinationTile - Destination tile is not adjacent to current tile");
        }

        destTile = tile;
    }

    #region Callbacks
    public void RegisterCharacterChangedCallback(Action<Character> cb)
    {
        cbCharacterChanged += cb;
    }

    public void UnregisterCharacterChangedCallback(Action<Character> cb)
    {
        cbCharacterChanged -= cb;
    }

    void OnJobEnded(Job j)
    {
        //Job completed or cancled
        if (j != myJob)
        {
            Debug.LogError("Character - OnJobEnded - Character is looking at job that is not his. Likely job did not get unregistered.");
            return;
        }

        myJob = null;
    }

    #endregion
}
