using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class WorldController : MonoBehaviour
{

    public static WorldController Instance { get; protected set; }

    // The world and tile data
    public World world { get; protected set; }

    // Use this for initialization
    //OnEnable rather than start because other scripts need this to be initialized first.
    void OnEnable()
    {
        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        // Create a world with Empty tiles
        world = new World();

        //Center camera at start
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    void Update()
    {
        world.Tick(Time.deltaTime);

        //TODO add speed controls, pause, etc here.
    }

    //temp pathfinding test TODO
    public void pathFindButton()
    {
        world.SetupPathFindingTestRoute();

        Path_TileGraph tileGraph = new Path_TileGraph(world);
    }

    /// <summary>
    /// Gets the tile at the unity-space coordinates
    /// </summary>
    /// <returns>The tile at world coordinate.</returns>
    /// <param name="coord">Unity World-Space coordinates.</param>
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x);
        int y = Mathf.FloorToInt(coord.y);

        return world.GetTileAt(x, y);
    }
}
