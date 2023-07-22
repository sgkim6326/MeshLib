using System;
using UnityEngine;

namespace MeshLib.Cluster
{
    [Serializable]
    public class ClusteredVertex
    {
        public double x;
        public double y;
        public double z;
        public ClusterGroup group { get; protected set; }
        public int index { get; protected set; }
        public double minDistance { get; protected set; }

        public ClusteredVertex(Vector3 vector, int index)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
            group = null;
            this.index = index;
            minDistance = double.MaxValue;
        }
        public Vector3 vector => new Vector3((float)x, (float)y, (float)z);
        public void UpdateInfo(ClusterGroup group, double distance)
        {
            if (distance < minDistance)
            {
                minDistance = distance;
                this.group = group;
            }
        }
        public void SetMaxDistance() => minDistance = double.MaxValue;
        public double DistanceFromVertexInfo(ClusteredVertex other) => (other.x - x) * (other.x - x) + (other.y - y) * (other.y - y) + (other.z - z) * (other.z - z);
    }
}