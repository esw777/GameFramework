using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public enum BuildMode
{
    FLOOR,
    FURNITURE,
    DECONSTRUCT
}


public class BuildModeController : MonoBehaviour
{
    public BuildMode buildMode = BuildMode.FLOOR;
    TileType buildModeTile = TileType.Floor;
    public string buildModeObjectType;

    //These can/should be factored out later TODO
    GameObject furniturePreview;
    FurnitureSpriteController fsc;
    MouseController mouseController;

    // Use this for initialization
    void Start()
    {
        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();
        mouseController = GameObject.FindObjectOfType<MouseController>();

        furniturePreview = new GameObject();
        furniturePreview.transform.SetParent(this.transform);
        furniturePreview.AddComponent<SpriteRenderer>().sortingLayerName = "Jobs";
        furniturePreview.SetActive(false);
    }

    public bool IsObjectDraggable()
    {
        if (buildMode == BuildMode.FLOOR || buildMode == BuildMode.DECONSTRUCT)
        {
            //floors
            return true;
        }

        Furniture proto = WorldController.Instance.world.furniturePrototypes[buildModeObjectType];

        //1x1 objects can be dragged.
        return proto.Width == 1 && proto.Height == 1;
    }

    public void DoBuild(Tile t)
    {
        if (buildMode == BuildMode.FURNITURE)
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
                    Debug.LogError("No furniture job prototype for: " + furnitureType);
                    j = new Job(t, furnitureType, FurnitureActions.JobCompleted_FurnitureBuilding, 1f, null, false);
                }

                j.furniturePrototype = WorldController.Instance.world.furniturePrototypes[furnitureType];

                t.pendingFurnitureJob = j;

                //TODO has to be more simple way to do this
                j.RegisterJobStoppedCallback((theJob) =>
                    {
                        theJob.tile.pendingFurnitureJob = null;
                    }
                );

                WorldController.Instance.world.jobQueue.Enqueue(j, true);
            }
        }

        else if (buildMode == BuildMode.FLOOR)
        {
            // We are in tile-changing mode.
            t.Type = buildModeTile;
        }

        else if (buildMode == BuildMode.DECONSTRUCT)
        {
            //TODO
            if (t.furniture != null)
            {
                t.furniture.Deconstruct();
            }
        }

        else
        {
            Debug.LogError("Unknown build mode ");
        }
    }

    #region SetModes
    public void SetMode_BuildFloor()
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = TileType.Floor;
        mouseController.StartBuildMode();
    }

    public void SetMode_RemoveFloor()
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = TileType.Empty;
        mouseController.StartBuildMode();
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        buildMode = BuildMode.FURNITURE;
        buildModeObjectType = objectType;
        mouseController.StartBuildMode();
    }

    public void SetMode_Deconstruct()
    {
        buildMode = BuildMode.DECONSTRUCT;
        mouseController.StartBuildMode();
    }
    #endregion
}
