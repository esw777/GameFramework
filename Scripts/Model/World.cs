using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class World : IXmlSerializable
{
	// A two-dimensional array to hold our tile data.
	Tile[,] tiles;
    public List<Character> characterList;
    public List<Furniture> furnitureList;

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

	public World(int width, int height)
    {
        SetupWorld(width, height);

        //Make starting character
        Character c = CreateCharacter(GetTileAt(Width / 2, Height / 2));
    }

    public void SetupWorld(int width, int height)
    {
        Width = width;
        Height = height;

        jobQueue = new JobQueue();
        tiles = new Tile[Width, Height];
        characterList = new List<Character>();
        furnitureList = new List<Furniture>();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
            }
        }

        Debug.Log("World created with " + (Width * Height) + " tiles.");

        CreateFurniturePrototypes();
    }

    public void Tick(float deltaTime)
    {

        foreach (Character c in characterList)
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

    public Character CreateCharacter( Tile t)
    {
        Character character = new Character(t);

        characterList.Add(character);

        if (cbCharacterCreated != null)
        {
            cbCharacterCreated(character);
        }

        return character;
    }

	// Debug function for testing out the system TODO
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

    // debug testing function TODO
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


	// Returns the tile data at x and y.
	public Tile GetTileAt(int x, int y)
    {
		if( x >= Width || x < 0 || y >= Height || y < 0)
        {
			//Debug.LogError("Tile ("+x+","+y+") is out of range.");
			return null;
		}
		return tiles[x, y];
	}


	public Furniture PlaceFurniture(string objectType, Tile t)
    {
		//Debug.Log("PlaceInstalledObject");
		// TODO: This function assumes 1x1 tiles -- change this later!

		if( furniturePrototypes.ContainsKey(objectType) == false )
        {
			Debug.LogError("furniturePrototypes doesn't contain a proto for key: " + objectType);
			return null;
		}

		Furniture furn  = Furniture.PlaceInstance( furniturePrototypes[objectType], t);

		if(furn == null)
        {
            Debug.LogError("World - PlaceFurniture - failed to place object");
			return null;
		}

        furnitureList.Add(furn);

		if(cbFurnitureCreated != null)
        {
			cbFurnitureCreated(furn);
		}

        InvalidateTileGraph(); //Can result in a change of movement costs of tiles - affects pathfinding.

        //TODO find better place for this
        t.pendingFurnitureJob = null;

        return furn;
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

    #region SaveLoadCode
    //For serializer - must be parameter-less
    public World() : this(100, 100) { }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        //Save Info Here

        //World Data
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        //Tile Data
        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                writer.WriteStartElement("Tile");
                tiles[x, y].WriteXml(writer);
                writer.WriteEndElement();
            }
        }
        writer.WriteEndElement();

        //Furniture Data
        writer.WriteStartElement("Furnitures");
        foreach (Furniture furn in furnitureList)
        {
            writer.WriteStartElement("Furniture");
            furn.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        //Character Data
        writer.WriteStartElement("Characters");
        foreach (Character c in characterList)
        {
            writer.WriteStartElement("Character");
            c.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        //Load Stuff

        //Load World size
        Width = int.Parse(reader.GetAttribute("Width"));
        Height = int.Parse(reader.GetAttribute("Height"));

        SetupWorld(Width, Height);

        while (reader.Read())
        {
            switch(reader.Name)
            {
                case "Tiles":
                    {
                        ReadXml_Tiles(reader);
                        break;
                    }

                case "Furnitures":
                    {
                        ReadXml_Furnitures(reader);
                        break;
                    }

                case "Characters":
                    {
                        ReadXml_Characters(reader);
                        break;
                    }

                default:
                    break; //TODO


            }
        }
    }

    void ReadXml_Tiles(XmlReader reader)
    {
        Debug.Log("ReadXml_Tiles "); 
        //Load Tiles
        while (reader.Read())
        {
            if (reader.Name != "Tile")
            {
                return; // end of tile section
            }

            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));
            tiles[x, y].ReadXml(reader);

            //Debug.Log("Reading tile: " + x + ", " + y); //This will take several minutes to print
        }
    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        Debug.Log("ReadXml_Furnitures "); 
        //Load Tiles
        while (reader.Read())
        {
            if (reader.Name != "Furniture")
            {
                return; // end of tile section
            }

            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Furniture furn = PlaceFurniture(reader.GetAttribute("objectType"), tiles[x,y]);
            furn.ReadXml(reader);
        }
    }

    void ReadXml_Characters(XmlReader reader)
    {
        Debug.Log("ReadXml_Characters ");
        //Load Tiles
        while (reader.Read())
        {
            if (reader.Name != "Character")
            {
                return; // end of tile section
            }

            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Character c = CreateCharacter(GetTileAt(x, y));
            c.ReadXml(reader);
        }
    }

    #endregion

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
