using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

// TileType is the base type of the tile. In some tile-based games, that might be
// the terrain type. For us, we only need to differentiate between empty space
// and floor (a.k.a. the station structure/scaffold). Walls/Doors/etc... will be
// InstalledObjects sitting on top of the floor.
public enum TileType { Empty, Floor };

public class Tile : IXmlSerializable
{
	private TileType _type = TileType.Empty;
	public TileType Type
    {
		get { return _type; }
		set {
			TileType oldType = _type;
			_type = value;
			// Call the callback and let things know we've changed.

			if(cbTileChanged != null && oldType != _type)
                cbTileChanged(this);
		}
	}

	// LooseObject is something like a drill or a stack of metal sitting on the floor
	Inventory inventory;

	// Furniture is something like a wall, door, or sofa.
	public Furniture furniture {get; protected set;}

    //True if pending furniture job on this tile
    public Job pendingFurnitureJob;

    // We need to know the context in which we exist. Probably. Maybe.
    public World world { get; protected set; }

	public int X { get; protected set; }
	public int Y { get; protected set; }

    public float movementCost
    {
        get
        {
            if (Type == TileType.Empty)
                return 0; //not walkable

            if (furniture == null)
                return 1; //nothing on tile to impede movement

            return 1 * furniture.movementCost; // Get furniture cost multiplier
        }
    }

	// The function we callback any time our type changes
	Action<Tile> cbTileChanged;

	// Initializes a new instance of the Tile class
	public Tile( World world, int x, int y )
    {
		this.world = world;
		this.X = x;
		this.Y = y;
	}

	// Registers a function to be called back when our tile type changes.
	public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged += callback;
	}
	
	// Unregisters a callback.
	public void UnregisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged -= callback;
	}

	public bool PlaceFurniture(Furniture objInstance)
    {
		if(objInstance == null)
        {
			// We are uninstalling whatever was here before.
			furniture = null;
			return true;
		}

		// objInstance isn't null
		if(furniture != null)
        {

			Debug.LogError("Trying to assign a furniture to a tile that already has one!");
			return false;
		}

		// At this point, everything's fine!

		furniture = objInstance;
		return true;
	}
	
    //Checks if two tiles are adjacent
    public bool IsNeighbour(Tile tile, bool diagOk)
    {
        /* fancy way probably not faster...
        return
            Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 ||           //Horizontal and Vertical adjacency
            (diagOk && (Mathf.Abs( this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1))  //Diagonals adjacency
            ;

        */

        //Same X axis, differ by 1 on Y axis
        if (this.X == tile.X && (this.Y == (tile.Y +1) || this.Y ==( tile.Y -1)))
        {
            return true;
        }
        //Same Y axis, differ by 1 on X axis
        if (this.Y == tile.Y && (this.X == (tile.X + 1) || this.X == (tile.X - 1)))
        {
            return true;
        }

        //Check diagonals
        if (diagOk)
        {
            if (this.X == (tile.X + 1) && this.Y == (tile.Y + 1))
            {
                return true;
            }

            if (this.X == (tile.X + 1) && this.Y == (tile.Y - 1))
            {
                return true;
            }

            if (this.X == (tile.X - 1) && this.Y == (tile.Y + 1))
            {
                return true;
            }

            if (this.X == (tile.X - 1) && this.Y == (tile.Y - 1))
            {
                return true;
            }
        }

        return false;
    }

    public Tile[] GetNeighbours( bool diagMovementAllowed = false)
    {
        Tile[] ns;

        if (diagMovementAllowed == false)
        {
            ns = new Tile[4]; // Tile order: N E S W
        }

        else
        {
            ns = new Tile[8]; // Tile order: N E S W NE SE SW NW
        }

        Tile n; //potential neighbour

        //North
        n = world.GetTileAt(X, Y + 1);
        ns[0] = n;
        //East
        n = world.GetTileAt(X + 1, Y);
        ns[1] = n;
        //South
        n = world.GetTileAt(X, Y - 1);
        ns[2] = n;
        //West
        n = world.GetTileAt(X - 1, Y);
        ns[3] = n;

        if (diagMovementAllowed)
        {
            //NE
            n = world.GetTileAt(X + 1, Y + 1);
            ns[4] = n;
            //SE
            n = world.GetTileAt(X + 1, Y - 1);
            ns[5] = n;
            //SW
            n = world.GetTileAt(X - 1, Y - 1);
            ns[6] = n;
            //NW
            n = world.GetTileAt(X - 1, Y + 1);
            ns[7] = n;
        }

        return ns;
    }

    #region SaveLoadCode
    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        //Save Tile Data
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("Type", ((int)Type).ToString());

    }

    public void ReadXml(XmlReader reader)
    {
        //Load Tile Data
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));


    }

    #endregion

}
