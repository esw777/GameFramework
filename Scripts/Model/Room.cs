using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Room
{
    Dictionary<string, float> atmosphericGasses;

    List<Tile> tiles;

    World world;

    public Room(World world)
    {
        this.world = world;
        tiles = new List<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
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
        if (t.room != world.GetOutsideRoom())
        {
            tiles.Remove(t);
        }
    }

    public bool IsOutsideRoom()
    {
        return this == world.GetOutsideRoom();
    }

    public void ChangeGas(string name, float amount)
    {
        if (IsOutsideRoom())
        {
            return;
        }

        if (atmosphericGasses.ContainsKey(name))
        {
            atmosphericGasses[name] += amount;
        }

        else
        {
            atmosphericGasses[name] = amount;
        }

        if (atmosphericGasses[name] < 0)
        {
            atmosphericGasses[name] = 0;
        }
    }

    public float GetGasAmount(string name)
    {
        if (atmosphericGasses.ContainsKey(name))
        {
            return atmosphericGasses[name];
        }

        return 0;
    }

    public float GetGasPercentage(string name)
    {
        if (atmosphericGasses.ContainsKey(name) == false)
        {
            return 0f;
        }

        float t = 0;

        foreach(string n in atmosphericGasses.Keys)
        {
            t += atmosphericGasses[n];
        }

        if (t != 0)
        {
            return atmosphericGasses[name] / t;
        }

        return 0f;
    }

    public string[] GetGasNames()
    {
        return atmosphericGasses.Keys.ToArray();
    }

    //Assigns all tiles to "Outside" room
    public void UnAssignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = world.GetOutsideRoom();
        }
        tiles = new List<Tile>();
    }

    //Combines two rooms. Usually baseRoom should be the lower index room in the world's room list.
    public void MergeRoom(Room baseRoom, Room roomToMerge)
    {
        //Shortcut if merging to outside room
        if (baseRoom == world.GetOutsideRoom())
        {
            roomToMerge.UnAssignAllTiles();
            world.DeleteRoom(roomToMerge);
            return;
        }

        float baseRoomTileCount = baseRoom.tiles.Count;
        float roomToMergeTileCount = roomToMerge.tiles.Count;
        float totalTiles = baseRoomTileCount + roomToMergeTileCount;

        if (totalTiles < 1)
        {
            totalTiles = 1; //prevent divide by 0.
            Debug.LogError("MergeRooms - Totaltiles is less than 1");
        }

        while (roomToMerge.tiles.Count > 0)
        {
            baseRoom.AssignTile(roomToMerge.tiles[0]);
        }

        List<string> tmpkeysList = new List<string>(baseRoom.atmosphericGasses.Keys);

        foreach (string key in tmpkeysList)
        {
            //Both rooms contain same gas
            if (baseRoom.atmosphericGasses.ContainsKey(key) && roomToMerge.atmosphericGasses.ContainsKey(key))
            {
                baseRoom.atmosphericGasses[key] = 
                    (baseRoom.atmosphericGasses[key] * baseRoomTileCount / totalTiles) + 
                    (roomToMerge.atmosphericGasses[key] * roomToMergeTileCount / totalTiles);
            }
            //Only base room contains a gas
            else
            {
                baseRoom.atmosphericGasses[key] *= (baseRoomTileCount / totalTiles);
            }
        }

        tmpkeysList = new List<string>(roomToMerge.atmosphericGasses.Keys);

        //Only merging room contains a gas
        foreach(string key in tmpkeysList)
        {
            if ((baseRoom.atmosphericGasses.ContainsKey(key) == false) && (roomToMerge.atmosphericGasses.ContainsKey(key)))
            {
                baseRoom.atmosphericGasses.Add(key, (roomToMerge.atmosphericGasses[key] * roomToMergeTileCount / totalTiles));
            }
        }

        if (roomToMerge.tiles.Count > 0)
        {
            Debug.LogError("Room did not merge correctly, count = " + roomToMerge.tiles.Count);
        }

        world.DeleteRoom(roomToMerge);
             
    }

    public static void ReCalculateRoomsDelete(Tile sourceTile)
    {
        //Called when a furniture that isRoomBorder is removed or destroyed

        if (sourceTile.furniture != null && sourceTile.furniture.isRoomBorder)
        {
            return; //This tile is still a RoomBorder and thus cant be part of a room
        }

        Tile[] neighbors = sourceTile.GetNeighbours(true);
        int lowestRoomIndex = 65000; //if we ever have more than 65k rooms then the mapsize must be astronomical TODO

        //Find lowest room index out of neighbors
        foreach (Tile t in neighbors)
        {
            int tmp = sourceTile.world.roomList.IndexOf(t.room);
            if (tmp >= 0 && tmp < lowestRoomIndex)
            {
                lowestRoomIndex = tmp;
            }
        }
        //Merge all neighboring rooms into one.
        foreach(Tile t in neighbors)
        {
            if (sourceTile.world.roomList.IndexOf(t.room) > lowestRoomIndex)
            {
                //Merge to lowestRoom
                t.room.MergeRoom(sourceTile.world.roomList[lowestRoomIndex], t.room);
            }
        }

        //Add the newly walkable tile to the room;
        sourceTile.world.roomList[lowestRoomIndex].AssignTile(sourceTile);

        //DoFloodFill(sourceTile, null); //TODO this doesn't work with current DoFloodFill() method
    }

    public static void ReCalculateRoomsAdd(Furniture sourceFurniture, bool initialLoad = false)
    {
        //Issue is currrently avoided by copying old room paremeters into the new room.

        //sourceFurniture is the new piece of furniture potentially causing new rooms to be formed.
        //Check neighbours of the furniture's tile.
        //Do floodfill algorithm to find new rooms.

        if (sourceFurniture.isRoomBorder == false)
        {
            return; //Somehow this got called when it should not have
            Debug.LogError("floodfill add called with non room border furniture");
        }

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
            if (t.room != null && (initialLoad == false || t.room.IsOutsideRoom()))
            {
                DoFloodFill(t, oldRoom);
            }
        }

        sourceFurniture.tile.room = null;
        oldRoom.tiles.Remove(sourceFurniture.tile);

        //Delete current room if not outside
        if (oldRoom.IsOutsideRoom() == false)
        {
            //Old room should have 0 tiles assigned to it at this point.
            //So should only result in this room being removed from world's room list.

            //TODO debug
            if (oldRoom.tiles.Count > 0)
            {
                //This triggered when tile/foor got deleted while building a wall (causing room recalc)
                Debug.LogError("Room:ReCalculateRoomsAdd: Room about to be deleted still has tiles.");
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
        Room newRoom = new Room(oldRoom.world);
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
        foreach(string n in sourceRoom.atmosphericGasses.Keys)
        {
            targetRoom.atmosphericGasses[n] = sourceRoom.atmosphericGasses[n];
        }
    }

}//Class room
