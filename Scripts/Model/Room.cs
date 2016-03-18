using UnityEngine;
using System.Collections.Generic;

public class Room
{
    //temp example room variables
    public float temperature = 0f;
    public float oxygen = 0f;

    List<Tile> tiles;

    public Room()
    {
        tiles = new List<Tile>();
    }

    public void AssignTile(Tile t)
    {
        if (tiles.Contains(t))
        {
            return;
        }

        t.room = this;
        tiles.Add(t);
    }

    public void UnAssignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutsideRoom();
        }
        tiles = null;
        tiles = new List<Tile>();
    }

    public static void DoRoomFloodFill(Furniture sourceFurniture)
    {
        //sourceFurniture is the new piece of furniture potentially causing new rooms to be formed.
        //Check NESW neighbours of the furniture's tile.
        //Do floodfill algorithm to find new rooms.

        //TODO consider something like the following if optimization is needed
        //If neighbouring furniture to N and S or E and W are also considered roomBorders, recalculate room.
        //Else the new sourceFurniture will not have created a new room. 

        //Delete current room if not outside
        if (sourceFurniture.tile.room != sourceFurniture.tile.world.GetOutsideRoom())
        {
            sourceFurniture.tile.world.DeleteRoom(sourceFurniture.tile.room); //Assigns all tiles in current room to the outside room
        }

        //Do floodfill to find new room structure
    }

}
