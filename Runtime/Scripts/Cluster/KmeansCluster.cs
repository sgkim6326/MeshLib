using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib.Cluster
{
    public class KmeansCluster : IClusterSolver
    {
        GeometryInfo geometry;
        int k;
		int epoch;
        public KmeansCluster(GeometryInfo geometry, int k, int epoch = 100)
        {
            this.geometry = geometry;
            this.k = k;
			this.epoch = epoch;
        }
		public async Task<ClusteredVertex[]> AsyncComputeCluster()
		{
			int LengthOfVertices = geometry.vertices.Length;
			ClusterGroup[] verticsGroups = new ClusterGroup[k];
			ClusteredVertex[] vertexInfos = new ClusteredVertex[LengthOfVertices];
			Debug.Log("Cluster: initial vertex group centroid (random)...");
			for (int k_index = 0; k_index < k; ++k_index)
			{
				var randomManager = new System.Random(unchecked((int)DateTime.Now.Ticks + k_index));
				verticsGroups[k_index] = new ClusterGroup(geometry.vertices[randomManager.Next(0, LengthOfVertices)], k_index);
			}
			Debug.Log("Cluster: calculate kmeans cluster");
			await Task.Run(() =>
			{
				for (int index = 0; index < LengthOfVertices; ++index)
				{
					vertexInfos[index] = new ClusteredVertex(geometry.vertices[index], index);
				}
				int max = epoch;
				while (epoch-- > 0)
				{
					for (int K_index = 0; K_index < k; ++K_index)
					{
						for (int vertex_index = 0; vertex_index < LengthOfVertices; ++vertex_index)
						{
							double dist = verticsGroups[K_index].DistanceFromCentroid(vertexInfos[vertex_index]);
							vertexInfos[vertex_index].UpdateInfo(verticsGroups[K_index], dist);
						}
					}
					for (int vertex_index = 0; vertex_index < LengthOfVertices; ++vertex_index)
					{
						ClusterGroup group = vertexInfos[vertex_index].group;
						group.ApplyAdditionalInfo(vertexInfos[vertex_index]);
						vertexInfos[vertex_index].SetMaxDistance();
					}
					for (int k_index = 0; k_index < k; ++k_index)
					{
						verticsGroups[k_index].CalculateCentroid();
					}
				}
			});
			Debug.Log("Cluster: complete");
			return vertexInfos;
		}
	}
}
