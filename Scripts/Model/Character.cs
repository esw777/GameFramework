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

    //Background stuff TODO testing - is not used yet
    public CharacterRelations  myCharacterRelations { get; protected set; }
    public CharacterDetails    myCharacterDetails   { get; protected set; }
    public CharacterSkills     myCharacterSkills    { get; protected set; }

    Job myJob;
    Action<Character> cbCharacterChanged;

    public Tile currTile { get; protected set; }

    Tile _destTile;
    Tile destTile
    {
        get { return _destTile; }
        set
        {
            if (_destTile != value)
            {
                _destTile = value;
                pathAStar = null;
            }
        }
    }


    Tile nextTile;
    Path_AStar pathAStar;

    //Item being carried / hauled
    public Inventory inventory;

    float movementPercentage;

    float speed = 5f; //Tiles per second.

    public Character() { } //Should only be used for serialization

    public Character(Tile tile)
    {
        currTile = tile;
        destTile = tile;
        nextTile = tile;

        //createCharacterSpecifics(); //TODO
    }

    void createCharacterSpecifics()
    {
        myCharacterRelations = new CharacterRelations(this, null, null);
        myCharacterSkills = new CharacterSkills();
        myCharacterDetails = new CharacterDetails("Default-Bob", false, 42, "Atlantis", true);
    }

    void GetNewJob()
    {
        //TODO prioritize closer jobs.
        myJob = currTile.world.jobQueue.DeQueue();

        if (myJob == null)
        {
            return;
        }

        myJob.RegisterJobCancelCallback(OnJobEnded);
        myJob.RegisterJobCompleteCallback(OnJobEnded);

        //Check if job is reachable - valid path to jobsite.
        destTile = myJob.tile;
        pathAStar = new Path_AStar(currTile.world, currTile, destTile, myJob.characterStandOnTile);
        if (pathAStar.Length() == 0)
        {
            Debug.LogError("Tick_DoJob: Path_AStar return no valid path");
            AbandonJob();
            pathAStar = null;
            destTile = currTile;
        }
    }

    //Manages doing work every tick.
    //TODO this is getting complicated, consider state machine for currentAction/currentTask
        //Whether or not to stand on tile (pick something up) or next to it (build a wall) becomes convoluted in current setup.
        //Also will greatly help implementation of picking up items from multiple tiles before doing a delivery.

    void Tick_DoJob(float deltaTime)
    {
        if (myJob == null)
        {
            GetNewJob();
            if (myJob == null)
            {
                //No job was retrieved from queue, do nothing
                return;
            }
        }

        //We have a reachable job

        //Check if job has all needed materials
        if (myJob.HasAllRequiredMaterials() == false)
        {
            //Is the character currently carrying a required item?
            if (inventory != null)
            {
                if (myJob.IsItemRequired(inventory) > 0)
                {
                    //Yes - Deliver items to jobsite - Walk to job tile
                    if (currTile == myJob.tile) //TODO stand next to broke || currTile.IsNeighbour(myJob.tile, true))
                    {
                        //Already at job site, deliver items.
                        currTile.world.inventoryManager.PlaceInventory(myJob, inventory);

                        myJob.DoWork(0); //Triggers jobWorked callbacks. Some jobs care about stack size changes.

                        //Check if we have extra
                        if (inventory.stackSize <= 0)
                        {
                            inventory = null;
                        }

                        else
                        {
                            Debug.LogError("Character: Tick_DoJob: Character is still carrying stuff after trying to deliver items to jobsite");
                            if (currTile.world.inventoryManager.PlaceInventory(currTile, inventory) == false)
                            {
                                Debug.LogError("Character: Tick_DoJob: Character tried to drop item into an invalid tile.");
                                //FIXME: Again, cast the item into the abyss, memory leak. Enables character logic to not break.
                                inventory = null;
                            }
                        }
                    }

                    else
                    {
                        //Need to walk to jobsite.
                        destTile = myJob.tile;
                        return;
                    }
                }

                else
                {
                    //Carrying a non-required item for this job. Just drop it for now.
                    //TODO verify empty tile, walk to empty tile to drop it.
                    if (currTile.world.inventoryManager.PlaceInventory(currTile, inventory) == false)
                    {
                        Debug.LogError("Character: Tick_DoJob: Character tried to drop item into an invalid tile.");
                        //FIXME: Again, cast the item into the abyss, memory leak. Enables character logic to not break.
                        inventory = null;
                    }
                }
            }

            //No - Goto a tile with required item and pick it up.
            else
            {
                //TODO: This is very much 1/2 implemented and not optimized.
                //Already at the tile where we want to pick something up from.
                //if (currTile == destTile || currTile.IsNeighbour(destTile, true))
                //{
                    if ((currTile.inventory != null) && 
                        (myJob.canTakeFromStockpile || currTile.furniture == null || currTile.furniture.IsStockpile() == false) &&
                        (myJob.IsItemRequired(currTile.inventory) > 0))
                    {
                        //Already standing on tile with required items.
                        currTile.world.inventoryManager.PlaceInventory(
                            this, 
                            currTile.inventory, 
                            myJob.IsItemRequired(currTile.inventory)
                            );
                    }
                //}

                else //Need to move to a tile with the item.
                {
                    Inventory requiredInventory = myJob.GetFirstRequiredInventory();

                    Inventory supplierInventory = currTile.world.inventoryManager.GetClosestInventoryOfType(
                        requiredInventory.objectType,
                        currTile,
                        requiredInventory.maxStackSize - requiredInventory.stackSize,
                        myJob.canTakeFromStockpile
                        );

                    if (supplierInventory == null)
                    {
                        Debug.Log("No tile contains object of type '" + requiredInventory.objectType + "' to satisfy job reqs");
                        AbandonJob();
                        return;
                    }

                    destTile = supplierInventory.tile;
                    return;
                }
            }
            return; //Do not do work on the job if materials are still needed.
        }

        // Job has all required materials. Goto job tile to do work.
        destTile = myJob.tile;

        //We are in range to work on the job. Do work.
        if (currTile == destTile) //TODO fix doing stuff next to tiles || currTile.IsNeighbour(destTile, true))
        {
            //We have a job and are within the working distance.
            myJob.DoWork(deltaTime);
            return;
        }

        Debug.LogError("something funky - likely character is doing a job that has no defined job");
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

            /* Not needed currently. Causes a more common issue where character builds a wall on the tile he is standing on and gets stuck.
            if (nextTile.movementCost == 0)
            {
                //TODO handle this better. Should never get this far into the code. Need to regenerate path sooner.
                //Likely a wall got built as the character tried to move onto tile.
                Debug.LogError("Character is trying to move onto an unwalkable tile.");
                nextTile = null;
                pathAStar = null;
                return;
            }
            */
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

    void OnJobEnded(Job j)
    {
        //Job completed or cancled
        if (j != myJob)
        {
            Debug.LogError("Character - OnJobEnded - Character is looking at job that is not his. Likely job did not get unregistered.");
            return;
        }

        j.UnregisterJobCancelCallback(OnJobEnded);
        j.UnregisterJobCompleteCallback(OnJobEnded);

        myJob = null;
    }

    #region relations

    #endregion

    #region SaveLoadCode

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

    #endregion
}
