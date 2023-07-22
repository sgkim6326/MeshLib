using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib.Cluster
{
    public class KmeansPPCluster : IClusterSolver
    {
        GeometryInfo geometry;
        int k;
		int epoch;
        public KmeansPPCluster(GeometryInfo geometry, int k, int epoch = 100)
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
			var initCentroid = CalculateInitCentroid();
			for (int k_index = 0; k_index < k; ++k_index)
			{
				var randomManager = new System.Random(unchecked((int)DateTime.Now.Ticks + k_index));
				verticsGroups[k_index] = new ClusterGroup(initCentroid[k_index], k_index);
			}
			Debug.Log("Cluster: calculate kmeans++ cluster");
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
							//정점과 그룹 간 거리를 측정
							double dist = verticsGroups[K_index].DistanceFromCentroid(vertexInfos[vertex_index]);
							//그룹과 거리가 더 작으면 해당 정점의 그룹을 업데이트
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

		Vector3[] CalculateInitCentroid()
        {
			Vector3[] centroids = new Vector3[k];
			int LengthOfVertices = geometry.vertices.Length;
			var randomManager = new System.Random(unchecked((int)DateTime.Now.Ticks));
			var randomVertexIndex = randomManager.Next(0, LengthOfVertices);
			centroids[0] = geometry.vertices[randomVertexIndex];
			for (int ki = 1; ki < k; ++ki)
            {
				//각 점들에 대하여 가장 가까운 Centroid와의 거리를 저장
				float[] minDists = new float[LengthOfVertices];
				for(int vi = 0; vi < LengthOfVertices; ++vi)
                {
					var minDist = float.MaxValue;
					for(int ci = 0; ci < ki; ++ci)
                    {
						var dist = Vector3.SqrMagnitude(geometry.vertices[vi] - centroids[ci]);
						minDist = Mathf.Min(minDist, dist);
                    }
					minDists[vi] = minDist;
                }
				centroids[ki] = geometry.vertices[Array.IndexOf(minDists, minDists.Max())];
			}
			return centroids;
		}
	}
}
