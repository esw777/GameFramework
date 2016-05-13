using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour
{
    public bool buildModeIsObjects = false;
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
        if (buildModeIsObjects == false)
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
                    Debug.LogError("No furniture job prototype for: " + furnitureType);
                    j = new Job(t, furnitureType, FurnitureActions.JobComplete_FurnitureBuilding, 1f, null, false);
                }

                j.furniturePrototype = WorldController.Instance.world.furniturePrototypes[furnitureType];

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
        mouseController.StartBuildMode();
    }

    public void SetMode_Bulldoze()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Empty;
        mouseController.StartBuildMode();
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        buildModeIsObjects = true;
        buildModeObjectType = objectType;
        mouseController.StartBuildMode();
    }
    #endregion
}
