using UnityEngine;
using System.Collections;
using System;

// Inventory are things that are lying on the floor/stockpile, like a bunch of metal bars
// or potentially a non-installed copy of furniture (e.g. a cabinet still in the box from Ikea)

public class Inventory
{
    Action<Inventory> cbInventoryChanged;
    
    public string objectType = "Steel Plate";
	public int maxStackSize = 50;

    protected int _stackSize = 1;
	public int stackSize
    {
        get { return _stackSize; }
        set
        {
            if (_stackSize != value)
            {
                _stackSize = value;
                if (cbInventoryChanged != null)
                {
                    cbInventoryChanged(this);
                }
            }
        }
    }

    //One of these must be null, can't be assigned to both a tile and character at same time. //TODO
	public Tile tile;
	public Character character;

	public Inventory()
    {
		
	}

    public Inventory(string objectType, int maxStackSize, int stackSize)
    {
        this.objectType = objectType;
        this.maxStackSize = maxStackSize;
        this.stackSize = stackSize;
    }

	protected Inventory(Inventory other)
    {
		objectType   = other.objectType;
		maxStackSize = other.maxStackSize;
		stackSize    = other.stackSize;
	}

	public virtual Inventory Clone()
    {
		return new Inventory(this);
	}

    #region callbacks


    // Registers a function to be called back when our tile type changes.
    public void RegisterInventoryChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged += callback;
    }

    // Unregisters a callback.
    public void UnregisterInventoryChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged -= callback;
    }

    #endregion

}
