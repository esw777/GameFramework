using Priority_Queue;


public class Path_Node<T> : FastPriorityQueueNode
{
    public T data;

    public Path_Edge<T>[] edges; //Nodes leading out from this node


}
