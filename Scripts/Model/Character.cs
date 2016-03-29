using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Character : IXmlSerializable
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

    float speed = 50f; //Tiles per second.

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
        
        // Do not try to do work right after getting a new job. Needs to run through pathfinding to see if job is valid.
        else if (myJob != null && (currTile == destTile || currTile.IsNeighbour(destTile, true)))
        {
            //We have a job and are within the working distance.
            myJob.DoWork(deltaTime);
            return;
        }

        return; 
    }

    void Tick_DoMovement(float deltaTime)
    {
        if (currTile == destTile || myJob == null)
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
                    //TODO Don't try to get same job over and over and spam creating paths if it fails.
                    AbandonJob();
                    return;
                }
            }
            //Path exists, get next tile to move to
            nextTile = pathAStar.DequeueTile();

            //Verify path is still correct Need this because we divide by nextTile.movementCost later.
            if (nextTile.movementCost == 0)
            {
                //TODO handle this better. Should never get this far into the code. Need to regenerate path sooner.
                //Likely a wall got built as the character tried to move onto tile.
                Debug.LogError("Character is trying to move onto an unwalkable tile.");
                nextTile = null;
                pathAStar = null;
                return;
            }
        }

        if (nextTile.IsEnterable() == Enterability.Soon)
        {
            //Need to wait a bit for tile to become available. Door opening for example.
            //Return before moving the character. Character will sit at current location until Enterability = yes.
            return;
        }

        //Distance from Tile A to Tile B //TODO bad sqrt
        float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.X - nextTile.X, 2) + Mathf.Pow(currTile.Y - nextTile.Y, 2));

        //How far can we go this update
        float distThisFrame = speed / nextTile.movementCost * deltaTime;

        //How much is that in terms of percentage
        float percThisFram = distThisFrame / distToTravel;

        //Add that percent to overall percent traveled
        movementPercentage += percThisFram;

        if (movementPercentage >= 1)
        {
            //reached destination tile
            currTile = nextTile;
            movementPercentage = 0;
        }

        return; //true;
    }

    //Character will abandon current job. Requeues the job.
    public void AbandonJob()
    {
        nextTile = destTile = currTile;
        pathAStar = null;
        currTile.world.jobQueue.Enqueue(myJob, false);
        myJob.UnregisterJobCancelCallback(OnJobEnded);
        myJob.UnregisterJobCompleteCallback(OnJobEnded);
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

    #region SaveLoadCode
    public Character() { } //Should only be used for serialization

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", currTile.X.ToString());
        writer.WriteAttributeString("Y", currTile.Y.ToString());

    }

    public void ReadXml(XmlReader reader)
    {
        //nothing here currently

    }

    #endregion
    
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
