using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour
{

    public static WorldController Instance { get; protected set; }

    // The world and tile data
    public World world { get; protected set; }

    static bool loadWorld = false;

    // Use this for initialization
    //OnEnable rather than start because other scripts need this to be initialized first.
    void OnEnable()
    {
        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        if (loadWorld)
        {
            loadWorld = false;
            CreateWorldFromSaveFile();
        }

        else
        {
            CreateEmptyWorld();
        }
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

    // returns the tile at world coordinate.
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);

        return world.GetTileAt(x, y);
    }

    public void NewWorld()
    {
        Debug.Log("NewWorld Clicked");

        //Reload the scene. This will gurantee all old references die out and get garbage collected.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }

    public void SaveWorld()
    {
        Debug.Log("SaveWorld Clicked");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();

        serializer.Serialize(writer, world);
        writer.Close();

        //Debug.Log(writer.ToString());

        //TODO actual save file rather than piggybacking off of Unity. Will probably break in the default web player.
        PlayerPrefs.SetString("SaveGame0", writer.ToString());

    }
    
    public void LoadWorld()
    {
        //Debug.Log("LoadWorld Clicked");

        loadWorld = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CreateEmptyWorld()
    {
        //Debug.Log("CreateEmptyWorld()");
        // Create a world with Empty tiles
        world = new World();

        //Center camera at start
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    void CreateWorldFromSaveFile()
    {
        //Debug.Log("CreateEmptyWorldFromSave()");

        // Create a world from save file data
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(PlayerPrefs.GetString("SaveGame0"));

        //Debug.Log("Loaded" + reader.ToString());

        world = (World)serializer.Deserialize(reader);
        reader.Close();


        //Center camera at start
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);

    }

}
