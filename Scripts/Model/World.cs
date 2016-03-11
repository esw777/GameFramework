using UnityEngine;
using System.Collections.Generic;
using System;

public class World
{

	// A two-dimensional array to hold our tile data.
	Tile[,] tiles;
    List<Character> characters;

    //The pathfinding graph used to navigate world.
    public Path_TileGraph tileGraph;

	Dictionary<string, Furniture> furniturePrototypes;

	// The tile width of the world.
	public int Width { get; protected set; }

	// The tile height of the world
	public int Height { get; protected set; }

	Action<Furniture> cbFurnitureCreated;
    Action<Tile> cbTileChanged;
    Action<Character> cbCharacterCreated;

    public JobQueue jobQueue;

	/// <summary>
	/// Initializes a new instance of the <see cref="World"/> class.
	/// </summary>
	/// <param name="width">Width in tiles.</param>
	/// <param name="height">Height in tiles.</param>
	public World(int width = 100, int height = 100)
    {
		Width = width;
		Height = height;

        jobQueue = new JobQueue();
		tiles = new Tile[Width,Height];
        characters = new List<Character>();
        
        for (int x = 0; x < Width; x++)
        {
			for (int y = 0; y < Height; y++)
            {
				tiles[x,y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
			}
		}

		Debug.Log ("World created with " + (Width*Height) + " tiles.");

		CreateFurniturePrototypes();
    }

    public void Tick(float deltaTime)
    {

        foreach (Character c in characters)
        {
            c.Tick(deltaTime);
        }

    }

	void CreateFurniturePrototypes()
    {
		furniturePrototypes = new Dictionary<string, Furniture>();

		furniturePrototypes.Add("Wall", 
			Furniture.CreatePrototype(
								"Wall",
								0,	// Impassable
								1,  // Width
								1,  // Height
								true // Links to neighbours and "sort of" becomes part of a large object
							)
		);
	}

    public Character createCharacter( Tile t)
    {
        Character character = new Character(t);

        characters.Add(character);

        if (cbCharacterCreated != null)
        {
            cbCharacterCreated(character);
        }

        return character;
    }

	/// <summary>
	/// A function for testing out the system
	/// </summary>
	public void RandomizeTiles()
    {
		Debug.Log ("RandomizeTiles");
		for (int x = 0; x < Width; x++)
        {
			for (int y = 0; y < Height; y++)
            {

				if(UnityEngine.Random.Range(0, 2) == 0)
                {
					tiles[x,y].Type = TileType.Empty;
				}
				else {
					tiles[x,y].Type = TileType.Floor;
				}

			}
		}
	}

    //testing function temp TODO
    public void SetupPathFindingTestRoute()
    {
        int l = Width / 2 - 5;
        int b = Height / 2 - 5;

        for (int x = l-5; x < l + 15; x++)
        {
            for (int y = b-5; y < b + 15; y++)
            {
                tiles[x, y].Type = TileType.Floor;

                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        PlaceFurniture("Wall", tiles[x, y]);
                    }
                }
            }
        }
    }


	/// <summary>
	/// Gets the tile data at x and y.
	/// </summary>
	/// <returns>The <see cref="Tile"/>.</returns>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	public Tile GetTileAt(int x, int y)
    {
		if( x >= Width || x < 0 || y >= Height || y < 0)
        {
			//Debug.LogError("Tile ("+x+","+y+") is out of range.");
			return null;
		}
		return tiles[x, y];
	}


	public void PlaceFurniture(string objectType, Tile t)
    {
		//Debug.Log("PlaceInstalledObject");
		// TODO: This function assumes 1x1 tiles -- change this later!

		if( furniturePrototypes.ContainsKey(objectType) == false )
        {
			Debug.LogError("furniturePrototypes doesn't contain a proto for key: " + objectType);
			return;
		}

		Furniture obj = Furniture.PlaceInstance( furniturePrototypes[objectType], t);

		if(obj == null)
        {
            Debug.LogError("World - PlaceFurniture - failed to place object");
			return;
		}

		if(cbFurnitureCreated != null)
        {
			cbFurnitureCreated(obj);
		}

        InvalidateTileGraph(); //Can result in a change of movement costs of tiles - affects pathfinding.

        //TODO find better place for this
        t.pendingFurnitureJob = null;
	}

    public bool IsFurniturePlacementValid(string furnitureType, Tile t)
    {
        return furniturePrototypes[furnitureType].IsValidPosition(t);
    }

    public Furniture getFurniturePrototype(string objectType)
    {
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("World - getFurniturePrototype - no prototype found for string" + objectType);
            return null;
        }

        return furniturePrototypes[objectType];
    }

    //Called whenever a change in the world would affect pathfinding.
    //Destroys old tileGraph. Forcing any new pathfinding calls to create a new one.
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }


    #region callbacks
    public void RegisterFurnitureCreated(Action<Furniture> callbackfunc)
    {
		cbFurnitureCreated += callbackfunc;
	}

	public void UnregisterFurnitureCreated(Action<Furniture> callbackfunc)
    {
		cbFurnitureCreated -= callbackfunc;
	}

    public void RegisterTileChangedCallback(Action<Tile> callbackFunc)
    {
        cbTileChanged += callbackFunc;
    }

    public void UnregisterTileChangedCallback(Action<Tile> callbackFunc)
    {
        cbTileChanged -= callbackFunc;
    }

    public void RegisterCharacterCreated(Action<Character> callbackfunc)
    {
        cbCharacterCreated += callbackfunc;
    }

    public void UnregisterCharacterCreated(Action<Character> callbackfunc)
    {
        cbCharacterCreated -= callbackfunc;
    }

    //Called when a tile changes
    void OnTileChanged(Tile t)
    {
        if (cbTileChanged != null)
        {
            cbTileChanged(t);
        }

        InvalidateTileGraph(); //Affects pathfinding
    }
    #endregion
}
