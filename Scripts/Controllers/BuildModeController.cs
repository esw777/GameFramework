using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour
{
    bool buildModeIsObjects = false;
    TileType buildModeTile = TileType.Floor;
    string buildModeObjectType;

    // Use this for initialization
    void Start()
    {

    }

    public void DoBuild(Tile t)
    {
        if (buildModeIsObjects == true)
        {
            string furnitureType = buildModeObjectType;

            //Check if valid position to build
            if (WorldController.Instance.world.IsFurniturePlacementValid(furnitureType, t) && t.pendingFurnitureJob == null)
            {
                //Queues up a job to build the furniture. Lambda
                Job j = new Job(t, furnitureType, (theJob) =>
                {
                    WorldController.Instance.world.PlaceFurniture(furnitureType, theJob.tile);
                },
                1,
                false
                );

                t.pendingFurnitureJob = j;

                //TODO wtf is this crap
                j.RegisterJobCancelCallback((theJob) =>
                {
                    theJob.tile.pendingFurnitureJob = null;

                }
                );

                WorldController.Instance.world.jobQueue.Enqueue(j);
            }
        }
        else
        {
            // We are in tile-changing mode.
            t.Type = buildModeTile;
        }
    }

    #region SetModes
    public void SetMode_BuildFloor()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Floor;
    }

    public void SetMode_Bulldoze()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Empty;
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        // Wall is not a Tile!  Wall is an "Furniture" that exists on TOP of a tile.
        buildModeIsObjects = true;
        buildModeObjectType = objectType;
    }
    #endregion
}
