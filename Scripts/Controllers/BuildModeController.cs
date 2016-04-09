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
        if (buildModeIsObjects)
        {
            string furnitureType = buildModeObjectType;

            //Check if valid position to build
            if (WorldController.Instance.world.IsFurniturePlacementValid(furnitureType, t) && t.pendingFurnitureJob == null)
            {
                Job j;

                if (WorldController.Instance.world.furnitureJobPrototypes.ContainsKey(furnitureType))
                {
                    j = WorldController.Instance.world.furnitureJobPrototypes[furnitureType].Clone();
                    j.tile = t;
                }

                else
                {
                    Debug.LogError("No furniture job prototype for :" + furnitureType);
                    j = new Job(t, furnitureType, FurnitureActions.JobComplete_FurnitureBuilding, 1f, null, false);
                }

                t.pendingFurnitureJob = j;

                //TODO has to be more simple way to do this
                j.RegisterJobCancelCallback((theJob) =>
                {
                    theJob.tile.pendingFurnitureJob = null;
                }
                );

                WorldController.Instance.world.jobQueue.Enqueue(j, true);
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
