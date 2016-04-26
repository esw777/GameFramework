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
            //Tile already in this room.
            return;
        }

        if (t.room != null)
        {
            //Remove tile from previous room.
            t.room.tiles.Remove(t);
        }

        t.room = this;
        tiles.Add(t);
    }

    public void UnAssignTile(Tile t)
    {
        if (t.room != t.world.GetOutsideRoom())
        {
            tiles.Remove(t);
        }
    }

    //Assigns all tiles to "Outside" room
    public void UnAssignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutsideRoom();
        }
        tiles = new List<Tile>();
    }

    public static void ReCalculateRooms(Tile sourceTile)
    {
        //Called when a furniture that isRoomBorder is removed or destroyed

        if (sourceTile.furniture != null && sourceTile.furniture.isRoomBorder)
        {
            return; //This tile is still a RoomBorder and thus cant be part of a room
        }

        //DoFloodFill(sourceTile, null); //TODO this doesn't work with current DoFloodFill() method
    }

    public static void ReCalculateRooms(Furniture sourceFurniture)
    {
        //TODO if FloodFill algorithm is used, it destroys the existing room regardless. This is probably bad.
        //Issue is currrently avoided by copying old room paremeters into the new room.

        //TODO this function will break when deleting walls is implemented.

        //sourceFurniture is the new piece of furniture potentially causing new rooms to be formed.
        //Check neighbours of the furniture's tile.
        //Do floodfill algorithm to find new rooms.

        //If neighbouring furniture to N and S or E and W are also considered roomBorders, recalculate room.
        //Else the new sourceFurniture will not have created a new room. 
        Tile[] neighbors = sourceFurniture.tile.GetNeighbours(true);
        
        int NeighboringWallCounter = 0;
        for (int i = 0; i < neighbors.Length && NeighboringWallCounter != 2; i++)
        {
            if (neighbors[i].furniture != null && neighbors[i].furniture.isRoomBorder)
            {
                NeighboringWallCounter++;
            }
        }
        
        //Not possible for a new room to have been created, no need to recalculate rooms.
        if (NeighboringWallCounter < 2)
        {
            //Remove the affected tile from its' room.
            sourceFurniture.tile.room.UnAssignTile(sourceFurniture.tile);
            sourceFurniture.tile.room = null;
            return;
        }
        
        //We have at least 2 "wall" neighbours. Can cause a new room to be created.

        Room oldRoom = sourceFurniture.tile.room;

        //Start FloodFill 
        foreach (Tile t in neighbors)
        {
            DoFloodFill(t, oldRoom);
        }

        sourceFurniture.tile.room = null;
        oldRoom.tiles.Remove(sourceFurniture.tile);

        //Delete current room if not outside
        if (oldRoom != sourceFurniture.tile.world.GetOutsideRoom())
        {
            //Old room should have 0 tiles assigned to it at this point.
            //So should only result in this room being removed from world's room list.

            //TODO debug
            if (oldRoom.tiles.Count > 0)
            {
                //This triggered when tile/foor got deleted while building a wall (causing room recalc)
                Debug.LogError("Room:ReCalculateRooms: Room about to be deleted still has tiles.");
            }

            sourceFurniture.tile.world.DeleteRoom(oldRoom);
        }
    }

    protected static void DoFloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            //Probably called by a tile on the edge of the map
            return;
        }

        if (tile.room != oldRoom)
        {
            //This tile was already assigned to a new room by a previous iteration of the flood fill.
            return;
        }

        if (tile.furniture != null && tile.furniture.isRoomBorder)
        {
            //This tile is a wall/door/RoomBorder. If a tile is a border, it cannot be a part of the room.
            return;
        }

        if (tile.Type == TileType.Empty)
        {
            //This tile has no floor - has nothing built on it = outside.
            return;
        }

        //At this point, we have a valid tile that needs to be put into a new room.
        Room newRoom = new Room();
        Queue<Tile> tilesToCheckQueue = new Queue<Tile>();
        tilesToCheckQueue.Enqueue(tile);

        while(tilesToCheckQueue.Count > 0)
        {
            Tile t = tilesToCheckQueue.Dequeue();

            if (t.room == oldRoom)
            {
                newRoom.AssignTile(t);

                Tile[] tileNeighbours = t.GetNeighbours();

                foreach (Tile t2 in tileNeighbours)
                {
                    if (t2 == null || t2.Type == TileType.Empty)
                    {
                        //We have hit edge of map or "outside"
                        //So this new room is actually just the outside.
                        //So we can bail out of the floodfill and delete the inProgress newRoom and reassign to outside.

                        newRoom.UnAssignAllTiles();
                        return;
                    }

                    if (t2.room == oldRoom && (t2.furniture == null || t2.furniture.isRoomBorder == false))
                    {
                        tilesToCheckQueue.Enqueue(t2);
                    }
                } //foreach
            } //if
        } //while

        CopyParameters(oldRoom, newRoom);

        tile.world.AddRoom(newRoom);

    } //DoFloodFill

    //This copies all room parameters from one room into another.
    protected static void CopyParameters(Room sourceRoom, Room targetRoom)
    {
        targetRoom.temperature = sourceRoom.temperature;
        targetRoom.oxygen = sourceRoom.oxygen;
    }

}//Class room
