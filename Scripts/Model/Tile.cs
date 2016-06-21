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

public enum Enterability { Yes, Never, Soon };

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
    public Inventory inventory; //{ get; protected set; }

    // Furniture is something like a wall, door, or sofa.
    public Furniture furniture {get; protected set;}

    //True if pending furniture job on this tile
    public Job pendingFurnitureJob;

    //The room that this tile is a part of
    public Room room; // { get; protected set; }

    public int X { get; protected set; }
	public int Y { get; protected set; }

    public const float baseTileMovementCost = 1f;

    public float movementCost
    {
        get
        {
            if (Type == TileType.Empty)
                return 0; //not walkable

            if (furniture == null)
                return baseTileMovementCost; //nothing on tile to impede movement

            return baseTileMovementCost * furniture.movementCost; // Get furniture cost multiplier
        }
    }

	// The function we callback any time our type changes
	Action<Tile> cbTileChanged;

	// Initializes a new instance of the Tile class
	public Tile(int x, int y )
    {
		this.X = x;
		this.Y = y;
	}

    public bool UnplaceFurninture()
    {
        //TODO name is dumb

        if (furniture == null)
        {
            return false;
        }

        int width = furniture.Width;
        int height = furniture.Height;

        //Loop for multi-tile objects.
        for (int x_off = X; x_off < (X + width); x_off++)
        {
            for (int y_off = Y; y_off < (Y + height); y_off++)
            {
                Tile t = World.current.GetTileAt(x_off, y_off);
                t.furniture = null;
            }
        }

        return true;
    }


	public bool PlaceFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            //uninstalling furninture
            //FIXME does not work for multi tile.
            return UnplaceFurninture();
        }

        if (objInstance.IsValidPosition(this) == false)
        {
            Debug.LogError("Trying to assign a furniture to a tile that is not valid");
            return false;
        }
        
        //Loop for multi-tile objects.
        for (int x_off = X; x_off < (X + objInstance.Width); x_off++)
        {
            for (int y_off = Y; y_off < (Y + objInstance.Height); y_off++)
            {
                Tile t = World.current.GetTileAt(x_off, y_off);
                t.furniture = objInstance;
            }
        }

		return true;
	}

    public bool PlaceInventory(Inventory inv)
    { 
		if(inv == null)
        {
			inventory = null;
			return true;
		}

		if(inventory != null)
        {
			// There's already inventory here. Try to combine to one large stack

			if(inventory.objectType != inv.objectType)
            {
				Debug.LogError("Tile: PlaceInventory: Trying to assign inventory to a tile that already has some of a different type.");
				return false;
			}

			int numToMove = inv.stackSize;
			if(inventory.stackSize + numToMove > inventory.maxStackSize)
            {
				numToMove = inventory.maxStackSize - inventory.stackSize;
			}

			inventory.stackSize += numToMove;
			inv.stackSize -= numToMove;

			return true;
		}

		// A this point, we know the current inventory is null. Inform the inventory manager that the old stack 
        // is now empty and has to be removed from the the item lists.

		inventory = inv.Clone();
		inventory.tile = this;
		inv.stackSize = 0;

		return true;
    }

//Checks if two tiles are adjacent //TODO this gets called A LOT, good place to optimize.
public bool IsNeighbour(Tile tile, bool diagOk)
    {
        /* fancy way probably not faster...
        return
            Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 ||           //Horizontal and Vertical adjacency
            (diagOk && (Mathf.Abs( this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1))  //Diagonals adjacency
            ;

        */ //TODO Use the North(), South(), etc functions here instead of magic numbers.

        //Same X axis, differ by 1 on Y axis
        if (this.X == tile.X && (this.Y == (tile.Y + 1) || this.Y == ( tile.Y - 1)))
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
        n = World.current.GetTileAt(X, Y + 1);
        ns[0] = n;
        //East
        n = World.current.GetTileAt(X + 1, Y);
        ns[1] = n;
        //South
        n = World.current.GetTileAt(X, Y - 1);
        ns[2] = n;
        //West
        n = World.current.GetTileAt(X - 1, Y);
        ns[3] = n;

        if (diagMovementAllowed)
        {
            //NE
            n = World.current.GetTileAt(X + 1, Y + 1);
            ns[4] = n;
            //SE
            n = World.current.GetTileAt(X + 1, Y - 1);
            ns[5] = n;
            //SW
            n = World.current.GetTileAt(X - 1, Y - 1);
            ns[6] = n;
            //NW
            n = World.current.GetTileAt(X - 1, Y + 1);
            ns[7] = n;
        }

        return ns;
    }

    public Enterability IsEnterable()
    {
        //Returns true if tile is not occupied
        if (movementCost == 0)
        {
            return Enterability.Never;
        }

        //Check furniture if it has special conditions
        if (furniture != null && furniture.IsEnterable != null)
        {
            return furniture.IsEnterable(furniture);
        }

        return Enterability.Yes;
    }

    public Tile North()
    {
        return World.current.GetTileAt(X, Y + 1);
    }

    public Tile South()
    {
        return World.current.GetTileAt(X, Y - 1);
    }

    public Tile East()
    {
        return World.current.GetTileAt(X + 1, Y);
    }

    public Tile West()
    {
        return World.current.GetTileAt(X - 1, Y);
    }

    #region callbacks

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

    #endregion

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
        writer.WriteAttributeString("RoomID", room==null ? "-1" : room.ID.ToString()); //if room is null (wall tiles) just write -1.
        writer.WriteAttributeString("Type", ((int)Type).ToString());

    }

    public void ReadXml(XmlReader reader)
    {
        //Load Tile Data
        room = World.current.GetRoomFromID(int.Parse(reader.GetAttribute("RoomID")));

        Type = (TileType)int.Parse(reader.GetAttribute("Type"));


    }

    #endregion

}
