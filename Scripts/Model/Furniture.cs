using UnityEngine;
using System.Collections;
using System;

// InstalledObjects are things like walls, doors, and furniture (e.g. a sofa)

public class Furniture
{

	// This represents the BASE tile of the object -- but in practice, large objects may actually occupy
	// multile tiles.
	public Tile tile {get; protected set;}

	// This "objectType" will be queried by the visual system to know what sprite to render for this object
	public string objectType {get; protected set;}

	// This is a multipler. So a value of "2" here, means you move twice as slowly (i.e. at half speed)
	// Tile types and other environmental effects may be combined.
	// For example, a "rough" tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
	// would have a total movement cost of (2+3+3 = 8), so you'd move through this tile at 1/8th normal speed.
	// SPECIAL: If movementCost = 0, then this tile is impassible. (e.g. a wall).
	public float movementCost; 

	// For example, a sofa might be 3x2 (actual graphics only appear to cover the 3x1 area, but the extra row is for leg room.)
	int width;
	int height;

	public bool linksToNeighbour {get; protected set;}

	Action<Furniture> cbOnChanged;

    Func<Tile, bool> funcPositionValidation;

	// TODO: Implement larger objects
	// TODO: Implement object rotation

	protected Furniture()
    {
		
	}

	static public Furniture CreatePrototype( string objectType, float movementCost = 1f, int width=1, int height=1, bool linksToNeighbour=false )
    {
		Furniture obj = new Furniture();

		obj.objectType = objectType;
		obj.movementCost = movementCost;
		obj.width = width;
		obj.height = height;
		obj.linksToNeighbour = linksToNeighbour;

        obj.funcPositionValidation = obj.__IsValidPosition;

		return obj;
	}

	static public Furniture PlaceInstance( Furniture proto, Tile tile )
    {
        if(proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("Furniture - PlaceInstance - Invalid Position.");
            return null;
        }

		Furniture obj = new Furniture();

		obj.objectType = proto.objectType;
		obj.movementCost = proto.movementCost;
		obj.width = proto.width;
		obj.height = proto.height;
		obj.linksToNeighbour = proto.linksToNeighbour;

		obj.tile = tile;

		// FIXME: This assumes we are 1x1!
		if( tile.PlaceFurniture(obj) == false )
        {
			// For some reason, we weren't able to place our object in this tile.
			// (Probably it was already occupied.)

			// Do NOT return our newly instantiated object.
			// (It will be garbage collected.)
			return null;
		}

		if(obj.linksToNeighbour)
        {
			// This type of furniture links itself to its neighbours,
			// so we should inform our neighbours that they have a new
			// buddy.  Just trigger their OnChangedCallback.

			Tile t;
			int x = tile.X;
			int y = tile.Y;

			t = tile.world.GetTileAt(x, y+1);
			if(t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
            {
				// We have a Northern Neighbour with the same object type as us, so
				// tell it that it has changed by firing is callback.
				t.furniture.cbOnChanged(t.furniture);
			}
			t = tile.world.GetTileAt(x+1, y);
			if(t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
            {
				t.furniture.cbOnChanged(t.furniture);
			}
			t = tile.world.GetTileAt(x, y-1);
			if(t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
            {
				t.furniture.cbOnChanged(t.furniture);
			}
			t = tile.world.GetTileAt(x-1, y);
			if(t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
            {
				t.furniture.cbOnChanged(t.furniture);
			}

		}

		return obj;
	}

    public bool IsValidPosition(Tile t)
    {
        return funcPositionValidation(t);
    }

    //Returns true if object is able to be placed at x,y position.
    public bool __IsValidPosition(Tile t)
    {
        if (t.Type != TileType.Floor)
        {
            //invalid position
            return false;
        }

        return true;
    }

    //Returns true is there is a wall pair at N/S or E/W and target tile is valid for placement
    public bool __IsValidPosition_Door(Tile t)
    {
        if (__IsValidPosition(t) == false)
        {
            return false;
        }
        //TODO check for walls
        //if ()

        return true;
    }



    #region callbacks
    public void RegisterOnChangedCallback(Action<Furniture> callbackFunc)
    {
        cbOnChanged += callbackFunc;
    }

    public void UnregisterOnChangedCallback(Action<Furniture> callbackFunc)
    {
        cbOnChanged -= callbackFunc;
    }
    #endregion
}
