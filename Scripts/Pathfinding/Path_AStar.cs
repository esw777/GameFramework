using UnityEngine;
using System.Collections.Generic;
using Priority_Queue;

public class Path_AStar
{
    Stack<Tile> path;

    public Path_AStar(World world, Tile tileStart, Tile tileEnd, bool endOnTile)
    {
        //Check if tile graph exists, create one if not.
        if(world.tileGraph == null)
        {
            world.tileGraph = new Path_TileGraph(world);
        }

        //List of all walkable nodes.
        Dictionary<Tile, Path_Node<Tile>> nodes = world.tileGraph.nodes;

        //Make sure start and end tiles exist.
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("Path_AStar: Starting tile is not in list of nodes.");

            //TODO fixme - If character manages to build a wall where they are standing, they get stuck.

            return;
        }

        if (nodes.ContainsKey(tileEnd) == false)
        {
            Debug.LogError("Path_AStar: Ending tile is not in list of nodes.");
            return;
        }

        Path_Node<Tile> start = nodes[tileStart];
        Path_Node<Tile> goal = nodes[tileEnd];

        HashSet<Path_Node<Tile>> ClosedSet = new HashSet<Path_Node<Tile>>();

        /*
        List<Path_Node<Tile>> OpenSet = new List<Path_Node<Tile>>();
        OpenSet.Add(start);
        */

        SimplePriorityQueue<Path_Node<Tile>> OpenSet = new SimplePriorityQueue<Path_Node<Tile>>();
        OpenSet.Enqueue(start, 0);

        Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From = new Dictionary<Path_Node<Tile>, Path_Node<Tile>>();

        Dictionary<Path_Node<Tile>, float> g_score = new Dictionary<Path_Node<Tile>, float>();
        foreach(Path_Node<Tile> n in nodes.Values)
        {
            g_score[n] = Mathf.Infinity;
        }
        g_score[start] = 0;

        Dictionary<Path_Node<Tile>, float> f_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values)
        {
            f_score[n] = Mathf.Infinity;
        }
        f_score[start] = heuristic_cost_estimate(start, goal);

        while (OpenSet.Count > 0)
        {
            Path_Node<Tile> current = OpenSet.Dequeue();

            if (current == goal)
            {
                //Found a path.
                reconstruct_path(Came_From, current, endOnTile);
                return;
            }

            ClosedSet.Add(current);

            foreach (Path_Edge<Tile> neighbour in current.edges)
            {
                //Already in set, skip.
                if (ClosedSet.Contains(neighbour.node))
                {
                    continue;
                }

                float movement_cost_to_neighbour = neighbour.node.data.movementCost * dist_between(current, neighbour.node);

                float temp_g_score = g_score[current] + movement_cost_to_neighbour;

                //If it is in open set and not a better path (lower g_score)
                if(OpenSet.Contains(neighbour.node) && temp_g_score >= g_score[neighbour.node])
                {
                    continue;  
                }

                //else
                Came_From[neighbour.node] = current;
                g_score[neighbour.node] = temp_g_score;
                f_score[neighbour.node] = g_score[neighbour.node] + heuristic_cost_estimate(neighbour.node, goal);

                if(OpenSet.Contains(neighbour.node) == false)
                {
                    OpenSet.Enqueue(neighbour.node, f_score[neighbour.node]);
                }

            } //end foreach neighbour
        } //end while 

        //Reaching here means we have parsed all nodes in OpenSet without reaching goal.
        //This means there is not a valid path from start node to end node.

        //TODO Failure state - error?
        return;

    }

    void reconstruct_path(Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From, Path_Node<Tile> current, bool endOnTile)
    {
        Stack<Tile> total_path = new Stack<Tile>();
        if (endOnTile)
        {
            total_path.Push(current.data); //Character will end up standing on target tile.
        }

        while (Came_From.ContainsKey(current))
        {
            current = Came_From[current]; //Get the previous tile.
            total_path.Push(current.data); //Record that tile.
        }
        //total_path is now a stack with top = startTile and bottom = goalTile.  
        path = total_path;
    }

    float dist_between(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        // We can make assumptions because we know we're workingon a grid at this point.

        // Hori/Vert neighbours have a distance of 1
        if (Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1f;
        }

        // Diag neighbours have a distance of 1.41421356237	
        if (Mathf.Abs(a.data.X - b.data.X) == 1 && Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1.41421356237f;
        }

        // Otherwise, do the actual math.
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );

    }


    float heuristic_cost_estimate(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        //return (((a.data.X - b.data.X) ^ 2) + ((a.data.Y - b.data.Y) ^ 2));
        //TODO figure out why the above causes diagonals to be prefered. Remove square root calculation.

        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );
    }

    public Tile DequeueTile()
    {
        return path.Pop();
    }

    public int Length()
    {
        if (path == null)
        {
            return 0;
        }

        return path.Count;
    }

}
