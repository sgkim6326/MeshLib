namespace MeshLib.Utility.Queue
{
    //https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/blob/master/Priority%20Queue/GenericPriorityQueueNode.cs
    public class GenericPriorityQueueNode<TPriority>
    {
        /// <summary>
        /// The Priority to insert this node at.
        /// Cannot be manually edited - see queue.Enqueue() and queue.UpdatePriority() instead
        /// </summary>
        public TPriority Priority { get; protected internal set; }

        /// <summary>
        /// Represents the current position in the queue
        /// </summary>
        public int QueueIndex { get; internal set; }

        /// <summary>
        /// Represents the order the node was inserted in
        /// </summary>
        public long InsertionIndex { get; internal set; }


#if DEBUG
        /// <summary>
        /// The queue this node is tied to. Used only for debug builds.
        /// </summary>
        public object Queue { get; internal set; }
#endif
    }
}