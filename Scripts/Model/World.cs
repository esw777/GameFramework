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
    public List<Character>  characterList;
    public List<Furniture>  furnitureList;
    public List<Room>       roomList;

    public InventoryManager inventoryManager;

    //The pathfinding graph used to navigate world.
    public Path_TileGraph tileGraph;

	public Dictionary<string, Furniture> furniturePrototypes;
    public Dictionary<string, Job> furnitureJobPrototypes;

	// The tile width of the world.
	public int Width { get; protected set; }

	// The tile height of the world
	public int Height { get; protected set; }

	Action<Furniture> cbFurnitureCreated;
    Action<Character> cbCharacterCreated;
    Action<Inventory> cbInventoryCreated;
    Action<Tile> cbTileChanged;

    public JobQueue jobQueue;

	public World(int width, int height)
    {
        GenerateWorld();

        SetupWorld(width, height);

        //Make starting character
        CreateCharacter(GetTileAt(Width / 2, Height / 2));
    }

    //For serializer / loading
    public World() : this(100, 100) { }
    //public World() { }

    //TODO make this better.
    public Room GetOutsideRoom()
    {
        return roomList[0];
    }

    public void AddRoom(Room r)
    {
        roomList.Add(r);
    }

    public void DeleteRoom(Room r)
    {
        if (r == GetOutsideRoom())
        {
            Debug.LogError("World:DeleteRoom: Trying to delete the outside room - bad.");
            return;
        }

        roomList.Remove(r);
        r.UnAssignAllTiles();
    }

    public void GenerateWorld()
    {
        //Generates history of the world

        //Create list of significant places
        /*
        for (int i = 0, i < numSigPlaces; i++)
        {

        //Create the starting population. 
            Place place = new Place("name", typeOfPlace);
            significantPlaceList.Add(place);
        }
        */
        

    }

    public void SetupWorld(int width, int height)
    {
        //Generates the objects in the world
        Width = width;
        Height = height;

        jobQueue = new JobQueue();

        tiles = new Tile[Width, Height];
        roomList = new List<Room>();
        roomList.Add(new Room()); //"Outside" is considered one giant room. An empty map will be one giant room.

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
                tiles[x, y].room = roomList[0]; //Room 0 is "outside" 
            }
        }

        CreateFurniturePrototypes();

        characterList = new List<Character>();
        furnitureList = new List<Furniture>();
        inventoryManager = new InventoryManager();
    }

    public void Tick(float deltaTime)
    {
        foreach (Character c in characterList)
        {
            c.Tick(deltaTime);
        }

        foreach(Furniture f in furnitureList)
        {
            f.tick(deltaTime);
        }

    }

	void CreateFurniturePrototypes()
    {
        //TODO read furniture data/types from an external file rather than hardcoding - for mods
		furniturePrototypes = new Dictionary<string, Furniture>();
        furnitureJobPrototypes = new Dictionary<string, Job>();

		furniturePrototypes.Add("Wall", 
			new Furniture(
								"Wall",
								0,  // Movecost, 0 = impassable
                                1,  // Width
								1,  // Height
								true, // Links to neighbours and "sort of" becomes part of a large object
                                true  // isRoomBorder - the "Room" code will consider this furniture type a border
                            )
		);
        furnitureJobPrototypes.Add("Wall",
                new Job(null, "Wall", FurnitureActions.JobComplete_FurnitureBuilding, 1f, new Inventory[] { new Inventory("Steel Plate", 5, 0) }, false)
        );

        
        furniturePrototypes.Add("Door",
            new Furniture(
                        "Door",
                        2,  // Movecost
                        1,  // Width
                        1,  // Height
                        false, // Links to neighbours and "sort of" becomes part of a large object
                        true  // isRoomBorder - the "Room" code will consider this furniture type a border
                    )
        );

        furniturePrototypes["Door"].SetParameter("openness", 0);
        furniturePrototypes["Door"].SetParameter("is_opening", 0);
        furniturePrototypes["Door"].RegisterUpdateAction(FurnitureActions.Door_UpdateAction);
        furniturePrototypes["Door"].IsEnterable += FurnitureActions.Door_IsEnterable;

        furniturePrototypes.Add("Stockpile",
            new Furniture(
                        "Stockpile",
                        1,  // Movecost, 0 = impassable
                        1,  // Width
                        1,  // Height
                        true, // Links to neighbours and "sort of" becomes part of a large object
                        false  // isRoomBorder - the "Room" code will consider this furniture type a border
                    )
        );
        furniturePrototypes["Stockpile"].RegisterUpdateAction(FurnitureActions.Stockpile_UpdateAction);
        furniturePrototypes["Stockpile"].tint = new Color32( 186, 31, 31, 255); //Dark red ish
        furnitureJobPrototypes.Add("Stockpile",
        new Job(
            null,
            "Stockpile", 
            FurnitureActions.JobComplete_FurnitureBuilding, 
            -1, 
            null)
        );


        furniturePrototypes.Add("Oxygen_Generator",
            new Furniture(
                    "Oxygen_Generator",
                    10,  // Movecost
                    2,  // Width
                    2,  // Height
                    false, // Links to neighbours and "sort of" becomes part of a large object
                    false  // isRoomBorder - the "Room" code will consider this furniture type a border
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

        //Redefine rooms if needed
        if (furn.isRoomBorder)
        {
            Room.ReCalculateRooms(furn);
        }

		if(cbFurnitureCreated != null)
        {
			cbFurnitureCreated(furn);
		}

        //Movement cost is determined by multiplying furnCost against the tileBaseCost. If these are equal, then tile movement cost is unchanged.
        //Thus no reason to regenerate pathfinding.
        if (furn.movementCost != Tile.baseTileMovementCost)
        {
            //Movement costs of tile has changed - affects pathfinding.
            InvalidateTileGraph(); 
        }

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

    public void OnInventoryCreated(Inventory inv)
    {
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(inv);
        }
    }

    #region SaveLoadCode
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
                if (tiles[x, y].Type != TileType.Empty)
                {
                    writer.WriteStartElement("Tile");
                    tiles[x, y].WriteXml(writer);
                    writer.WriteEndElement();
                }
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

        //DEBUG TODO remove
        Inventory testInv = new Inventory("Steel Plate", 50, 50);
        Tile t = GetTileAt(Width / 2, Height / 2);
        inventoryManager.PlaceInventory(t, testInv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.inventory);
        }

        testInv = new Inventory("Steel Plate", 50, 49);
        t = GetTileAt(Width / 2 + 2, Height / 2);
        inventoryManager.PlaceInventory(t, testInv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.inventory);
        }

        testInv = new Inventory("Steel Plate", 50, 6);
        t = GetTileAt(Width / 2 + 1, Height / 2 + 2);
        inventoryManager.PlaceInventory(t, testInv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.inventory);
        }

    }

    void ReadXml_Tiles(XmlReader reader)
    {
        //Debug.Log("ReadXml_Tiles ");
        //Load Tiles

        //Make sure there is at least one Tile to read in.
        if (reader.ReadToDescendant("Tile"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                tiles[x, y].ReadXml(reader);

            } while (reader.ReadToNextSibling("Tile"));
        }
    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        //Debug.Log("ReadXml_Furnitures ");
        //Load Tiles
        if (reader.ReadToDescendant("Furniture"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Furniture furn = PlaceFurniture(reader.GetAttribute("objectType"), tiles[x, y]);
                furn.ReadXml(reader);

            } while (reader.ReadToNextSibling("Furniture"));
        }
    }

    void ReadXml_Characters(XmlReader reader)
    {
        //Debug.Log("ReadXml_Characters ");
        //Load Tiles
        if (reader.ReadToDescendant("Character"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Character c = CreateCharacter(GetTileAt(x, y));
                c.ReadXml(reader);

            } while (reader.ReadToNextSibling("Character"));
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

    public void RegisterInventoryCreated(Action<Inventory> callbackfunc)
    {
        cbInventoryCreated += callbackfunc;
    }

    public void UnregisterInventoryCreated(Action<Inventory> callbackfunc)
    {
        cbInventoryCreated -= callbackfunc;
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
