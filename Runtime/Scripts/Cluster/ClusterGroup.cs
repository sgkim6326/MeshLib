using System;
using UnityEngine;

namespace MeshLib.Cluster
{
    [Serializable]
    public class ClusterGroup
    {
        [Serializable]
        public struct Centroid
        {
            public double x;
            public double y;
            public double z;
            public Centroid(Vector3 vector)
            {
                x = vector.x;
                y = vector.y;
                z = vector.z;
            }
        }
        public Centroid centroid;
        public int ID = -1;
        public int Count { get; protected set; }
        double sum_x = 0;
        double sum_y = 0;
        double sum_z = 0;
        public ClusterGroup(Vector3 init, int id)
        {
            centroid = new Centroid(init);
            this.ID = id;
            Count = 0;
        }
        public void ApplyAdditionalInfo(ClusteredVertex info)
        {
            sum_x += info.x;
            sum_y += info.y;
            sum_z += info.z;
            Count++;
        }
        public void CalculateCentroid()
        {
            centroid.x = sum_x / Count;
            centroid.y = sum_y / Count;
            centroid.z = sum_z / Count;
            sum_x = 0;
            sum_y = 0;
            sum_z = 0;
            Count = 0;
        }
        public double DistanceFromCentroid(ClusteredVertex other) => (other.x - centroid.x) * (other.x - centroid.x) + (other.y - centroid.y) * (other.y - centroid.y) + (other.z - centroid.z) * (other.z - centroid.z);
    }
}