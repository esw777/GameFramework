using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    public GameObject circleCursorPrefab;

    // The world-position of the mouse last frame.
    Vector3 lastFramePosition;
    Vector3 currFramePosition;

    // The world-position start of our left-mouse drag operation
    Vector3 dragStartPosition;
    List<GameObject> dragPreviewGameObjects;

    FurnitureSpriteController fsc;
    BuildModeController bmc;

    bool isDragging = false;

    enum MouseMode
    {
        SELECT,
        BUILD
    }
    MouseMode currentMode = MouseMode.SELECT;

    // Use this for initialization
    void Start()
    {
        dragPreviewGameObjects = new List<GameObject>();
        bmc = GameObject.FindObjectOfType<BuildModeController>();

        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();
    }

    //Returns mouse position in world space
    public Vector3 GetMousePosition()
    {
        return currFramePosition;
    }

    public Tile GetTileUnderMouse()
    {
        return WorldController.Instance.GetTileAtWorldCoord(currFramePosition);
    }

    // Update is called once per frame
    void Update()
    {
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (currentMode == MouseMode.BUILD)
            {
                currentMode = MouseMode.SELECT;
            }

            else
            {
                //TODO
                // Debug.Log("Show game menu");
            }
        }

        //UpdateCursor();
        UpdateDragging();
        UpdateCameraMovement();

        // Save the mouse position from this frame
        // We don't use currFramePosition because we may have moved the camera.
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    void UpdateDragging()
    {
        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Clean up old drag previews
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }

        if (currentMode != MouseMode.BUILD)
        {
            isDragging = false;
            return;
        }

        // Start Drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = currFramePosition;
            isDragging = true;
        }

        else if (isDragging == false) // prevent infinate dragging.
        {
            dragStartPosition = currFramePosition;
        }

        if (bmc.IsObjectDraggable() == false)
        {
            dragStartPosition = currFramePosition;
        }

        if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.Escape))
        {
            //Right mouse button was released.
            //Cancels building / dragging.
            isDragging = false;
        }

        int start_x = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int end_x   = Mathf.FloorToInt(currFramePosition.x + 0.5f);
        int start_y = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int end_y   = Mathf.FloorToInt(currFramePosition.y + 0.5f);

        // We may be dragging in the "wrong" direction, so flip things if needed.
        if (end_x < start_x)
        {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        }
        if (end_y < start_y)
        {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }

        //if (isDragging)
        {
            // Display a preview of the drag area
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.Instance.world.GetTileAt(x, y);
                    if (t != null)
                    {
                        // Display the building hint on top of this tile position
                        if (bmc.buildMode == BuildMode.FURNITURE)
                        {
                            ShowFurnitureSpriteAtTile(bmc.buildModeObjectType, t);                            
                        }

                        else
                        {
                            GameObject go = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                            go.transform.SetParent(this.transform, true);
                            dragPreviewGameObjects.Add(go);
                        }

                    }
                }
            }
        }

        // End Drag
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            // Loop through all the tiles
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.Instance.world.GetTileAt(x, y);

                    if (t != null)
                    {
                        // Call BuildModeController::DoBuild()
                        bmc.DoBuild(t);
                    }
                }
            }
        }
    }

    void UpdateCameraMovement()
    {
        // Handle screen panning
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {   // Right or Middle Mouse Button

            Vector3 diff = lastFramePosition - currFramePosition;
            Camera.main.transform.Translate(diff);

        }

        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");

        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
    }

    void ShowFurnitureSpriteAtTile(string furnitureType, Tile t)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(this.transform, true);
        dragPreviewGameObjects.Add(go);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fsc.GetSpriteForFurniture(furnitureType);
        sr.sortingLayerName = "Jobs";

        //TODO this tinting does not work well if the object is naturally green or red. Need to do custom shader for this.
        if (WorldController.Instance.world.IsFurniturePlacementValid(furnitureType, t))
        {
            //Make it 30% transparent and tint green
            sr.color = new Color(0.5f, 1f, 0.5f, 0.3f);
        }

        else //not valid
        {
            //Make it 30% transparent and tint red
            sr.color = new Color(1f, 0.5f, 0.5f, 0.3f);
        }

        Furniture proto = World.current.furniturePrototypes[furnitureType];

        go.transform.position = new Vector3(t.X + ((proto.Width - 1) / 2f), t.Y + ((proto.Height - 1) / 2f), 0);
    }

    public void StartBuildMode()
    {
        currentMode = MouseMode.BUILD;
    }

    public void StartSelectMode()
    {
        currentMode = MouseMode.SELECT;
    }
}
