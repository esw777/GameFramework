using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

// InstalledObjects are things like walls, doors, and furniture (e.g. a sofa)

public class Furniture : IXmlSerializable
{
    public Dictionary<string, float> furnitureParameters;
    public Action<Furniture, float> updateActions;

    public Func<Furniture, Enterability> IsEnterable;

    public void tick(float deltaTime)
    {
        if (updateActions != null)
        {
            updateActions(this, deltaTime);
        }
    }

	// This represents the BASE tile of the object -- but in practice, large objects may actually occupy
	// multile tiles.
	public Tile tile {get; protected set;}

	// This "objectType" will be queried by the visual system to know what sprite to render for this object
	public string objectType {get; protected set;}

	// This is a multipler. So a value of "2" here, means you move twice as slowly (i.e. at half speed)
	// Tile types and other environmental effects may be combined.
	// For example, a "rough" tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
	// would have a total movement cost of (2+3+3 = 8), so you'd move through this tile at 1/8th normal speed.
	// NOTE: If movementCost = 0, then this tile is impassible. (e.g. a wall).
	public float movementCost { get; protected set; } 

    public bool isRoomBorder { get; protected set; }

    // For example, a table might be 3x2
    int width;
	int height;

	public bool linksToNeighbour {get; protected set;}

    //TODO make get/set functions for furnitureParameters and call onchanged in the set rather than make this public.
    public Action<Furniture> cbOnChanged; 

    Func<Tile, bool> funcPositionValidation;

	// TODO: Implement larger objects
	// TODO: Implement object rotation

    //public due to serializer reqs 
	public Furniture()
    {
        furnitureParameters = new Dictionary<string, float>();
	}

    //Copy constructor
    protected Furniture(Furniture furn)
    {
        this.objectType = furn.objectType;
        this.movementCost = furn.movementCost;
        this.isRoomBorder = furn.isRoomBorder;
        this.width = furn.width;
        this.height = furn.height;
        this.linksToNeighbour = furn.linksToNeighbour;

        this.furnitureParameters = new Dictionary<string, float>(furn.furnitureParameters);

        if (furn.updateActions != null)
        {
            this.updateActions = (Action<Furniture, float>)furn.updateActions.Clone();
        }

        this.IsEnterable = furn.IsEnterable;
    }

    virtual public Furniture Clone()
    {
        return new Furniture(this);
    }

	public Furniture( string objectType, float movementCost = 1f, int width=1, int height=1, bool linksToNeighbour=false, bool isRoomBorder = false )
    {
		this.objectType = objectType;
        this.movementCost = movementCost;
        this.isRoomBorder = isRoomBorder;
        this.width = width;
        this.height = height;
        this.linksToNeighbour = linksToNeighbour;

        this.funcPositionValidation = this.__IsValidPosition;

        furnitureParameters = new Dictionary<string, float>();
    }

    static public Furniture PlaceInstance(Furniture proto, Tile tile)
    {
        if (proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("Furniture - PlaceInstance - Invalid Position.");
            return null;
        }

        Furniture obj = proto.Clone();
        obj.tile = tile;

        // FIXME: This assumes we are 1x1!
        if (tile.PlaceFurniture(obj) == false)
        {
            // For some reason, we weren't able to place our object in this tile.
            // (Probably it was already occupied.)

            // Do NOT return our newly instantiated object.
            // (It will be garbage collected.)
            return null;
        }

        if (obj.linksToNeighbour)
        {
            // This type of furniture links itself to its neighbours,
            // so we should inform our neighbours that they have a new
            // buddy.  Just trigger their OnChangedCallback.

            Tile t;
            int x = tile.X;
            int y = tile.Y;

            t = tile.world.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                // We have a Northern Neighbour
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                // We have a Eastern Neighbour
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                // We have a Southern Neighbour
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                // We have a Western Neighbour
                t.furniture.cbOnChanged(t.furniture);
            }
        }//endif

        return obj;
    }//end PlaceInstance()

    public bool IsValidPosition(Tile t)
    {
        return funcPositionValidation(t);
    }

    //Returns true if object is able to be placed at x,y position.
    public bool __IsValidPosition(Tile t)
    {
        if (t.furniture != null)
        {
            //Already something here
            return false;
        }

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

    #region SaveLoadCode
    //For serializer - must be parameter-less

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString("objectType", objectType);

        foreach( string k in furnitureParameters.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", furnitureParameters[k].ToString()); //This is a bit iffy, test this more later.

            writer.WriteEndElement();
        }



    }

    public void ReadXml(XmlReader reader)
    {
        //X, Y, and objectType should have already been set before this function is called.

        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                furnitureParameters[k] = v;
            } while (reader.ReadToNextSibling("Param"));


        }
    }

    #endregion

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
