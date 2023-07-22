using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib
{
	public static class ColorTool
	{
		public static Color[] ColorList = new Color[]
			{
			Color.red,//R
			new Color(1f, 0.5f, 0f),//YR
			Color.yellow,//Y
			new Color(0.5f, 0.8f, 0f),//GY
			new Color(0f, 0.5f, 0f),//G
			new Color(0f, 0.7f, 0.5f),//BG
			new Color(0f, 0.5f, 1f),//B
			new Color(0f, 0f, 71f),//PB
			new Color(0.5f, 0.01f, 0.71f),//P
			new Color(0.71f, 0.01f, 0.5f),//RP
			};
		public static Color GetSegmentationColor(int index)
		{
			return ColorList[index];
		}
	}
	public static class MeshTool
	{
		public static string ToColumnString<T>(this IEnumerable<T> objs)
		{
			List<string> result = new List<string>();
			int length = objs.Count();
			int step = length / 500;
			if (step < 100) step = 1;
			for (int i = 0; i < length; i += step)
			{
				result.Add(objs.ElementAt(i).ToString());
			}
			return string.Join("\n", result);
		}
        public delegate void VertexAction(int idx);
		/// <summary>이웃 정점을 발견했을 때의 이벤트</summary>
		/// <param name="critiaVertex">중심 정점</param>
		/// <param name="neighborVertex">이웃 정점</param>
		public delegate void MeshFindAction(int critiaVertex, int neighborVertex);
		#region 색상 적용 기능
		public static void ReColorizeVertex(MeshFilter renderer, Color color, int[] indexOfVertex)
		{
			var mesh = renderer.mesh;
			var colors = mesh.colors;
			foreach (var index in indexOfVertex)
			{
				colors[index] = color;
			}
			mesh.colors = colors;
			renderer.mesh = mesh;
		}
		public static async Task<Color[]> AsyncReColorizeVertex(Cluster.ClusteredVertex[] clustResult)
		{
			Debug.Log("ReColorize using Cluster Result...");
			int LengthOfVertices = clustResult.Length;
			var colorInfos = new Color[LengthOfVertices];
			await Task.Run(() =>
			{
				for (int vertex_index = 0; vertex_index < LengthOfVertices; ++vertex_index)
				{
					colorInfos[vertex_index] = ColorTool.GetSegmentationColor(clustResult[vertex_index].group.ID);
				}
			});
			return colorInfos;
		}
        #endregion
        #region 주변 정점 찾기 기능
        public static async Task<int> FindNeighborVerticsCnt(Vector3 center, IEnumerable<Vector3> vertices, double dist)
		{
			int count = 0;
			double powDistance = dist * dist;
			await Task.Run(() =>
			{
				foreach (var vertex in vertices)
				{
					if ((vertex - center).sqrMagnitude < powDistance) count++;
				}
			});
			return count;
		}
		public static async Task FindNeighborVertics(GeometryInfo geometry, double dist, MeshFindAction find, VertexAction ready = null, VertexAction finish = null)
		{
			Debug.Log(string.Format("Find neighbor vertices in {0}", dist));
			await Task.Run(() =>
			{
				int[] first = new int[geometry.vertices.Length];
				for (int i = 0; i < geometry.vertices.Length; ++i) first[i] = -1;
				bool[] used = new bool[geometry.vertices.Length];
				int[] next = new int[geometry.faces.Length * 6];
				//Calculate the 근접행렬(incident matrix) ( as linked list )
				int[] incidentVertex = new int[geometry.faces.Length * 6];
				int edgeCount = 0;
				for (int fi = 0; fi < geometry.faces.Length; ++fi)
				{
					int[] indices = geometry.faces[fi].GetIndices();
					for (int i = 0; i < indices.Length; ++i)
					{
						int j1 = indices[(i + 1) % 3];
						int j2 = indices[(i + 2) % 3];
						incidentVertex[edgeCount] = j1;
						next[edgeCount] = first[indices[i]]; first[indices[i]] = edgeCount++;
						incidentVertex[edgeCount] = j2;
						next[edgeCount] = first[indices[i]]; first[indices[i]] = edgeCount++;
					}
				}
				double powDistance = dist * dist;
				for (int vi = 0; vi < geometry.vertices.Length; ++vi)
				{
					ready?.Invoke(vi);
					Vector3 vertex = geometry.vertices[vi];
					for (int vi_2 = 0; vi_2 < geometry.vertices.Length; ++vi_2)
						used[vi_2] = false;
					Queue<int> queue = new Queue<int>();
					queue.Enqueue(vi);
					used[vi] = true;
					while (queue.Count != 0)
					{
						// Get the front element in the queue.
						int idx = queue.Dequeue();
						var idxVec = geometry.vertices[idx];
						// Put the next level vertecies into the queue.
						for (int e = first[idx]; e != -1; e = next[e])
						{
							int idxNext = incidentVertex[e];
							// Expand the next level vertecies.
							if (!used[idxNext])
							{
								var idxNextVec = geometry.vertices[idxNext];
								if ((vertex - idxNextVec).sqrMagnitude <= powDistance)
								{
									queue.Enqueue(incidentVertex[e]);
									used[incidentVertex[e]] = true;
								}
							}
						}
						find.Invoke(vi, idx);
					}
					finish?.Invoke(vi);
				}

			});
		}
        #endregion
        #region 지오메트리 변환 기능
        public static async Task<GeometryInfo[]> AsyncGeometrySegmentation(GeometryInfo geometry, Cluster.ClusteredVertex[] vertices, Plane[] rois = null)
        {
			GeometryInfo[] geometrySegment = null;
			Dictionary<int, List<int>> trisSegment = new Dictionary<int, List<int>>();
			for (int fi = 0; fi < geometry.faces.Length; ++fi)
            {
				Dictionary<Cluster.ClusterGroup, int> clusters = new Dictionary<Cluster.ClusterGroup, int>();
				int[] indices = geometry.faces[fi].GetIndices();
				//var critia = vertices[indices[0]].group;
				//bool isSameGroup = true;
				//for (int idx = 1; idx < indices.Length; ++idx)
				//{
				//	isSameGroup = critia == vertices[indices[idx]].group;
				//	if (isSameGroup == false) break;
				//}
				//if (isSameGroup == false) continue;
				bool isInsideROI = true;
				if(rois != null)
                {
					foreach (var roi in rois)
					{
						isInsideROI = isInsideROI && roi.GetSide(geometry.GetFaceCenter(fi));
						if (isInsideROI == false) break;
					}
				}
				if (isInsideROI == false) continue;
				for (int idx = 0; idx < indices.Length; ++idx)
					clusters[vertices[indices[idx]].group] = 0;
				for (int idx = 0; idx < indices.Length; ++idx)
					clusters[vertices[indices[idx]].group]++;
				Cluster.ClusterGroup max = clusters.Aggregate((pair1, pair2) => pair1.Value > pair2.Value ? pair1 : pair2).Key;
				if (trisSegment.ContainsKey(max.ID) == false) trisSegment[max.ID] = new List<int>();
				trisSegment[max.ID].AddRange(indices);
			}
			geometrySegment = new GeometryInfo[trisSegment.Count];
			for (int si = 0; si < trisSegment.Count; ++si)
            {
				geometrySegment[si] = await AsyncConvertToGeometry(geometry.vertices, geometry.normals, trisSegment[si].ToArray(), geometry.colors, geometry.isInterest);
            }
			return geometrySegment;
		}
		public static GeometryInfo ConvertToGeometry(Mesh mesh)
        {
			var vertexLength = mesh.vertices.Length;
			var faceLength = mesh.triangles.Length / 3;
			List<Face> faces = new List<Face>();
			Dictionary<int, int> all_vertices = new Dictionary<int, int>();
			for (int vi = 0; vi < vertexLength; ++vi)
			{
				all_vertices[vi] = -1;
			}
			int idx = 0;
			for (int fi = 0; fi < faceLength; ++fi)
			{
				int v0 = mesh.triangles[3 * fi];
				int v1 = mesh.triangles[3 * fi + 1];
				int v2 = mesh.triangles[3 * fi + 2];
				if (all_vertices[v0] == -1) all_vertices[v0] = idx++;
				if (all_vertices[v1] == -1) all_vertices[v1] = idx++;
				if (all_vertices[v2] == -1) all_vertices[v2] = idx++;
				faces.Add(new Face(all_vertices[v0], all_vertices[v1], all_vertices[v2]));
			}
			var geometry_indices = all_vertices.Where(vec => vec.Value != -1).ToArray();
			var geometry_Length = all_vertices.Count(vec => vec.Value != -1);
			Face[] geometry_faces = faces.ToArray();
			Vector3[] geometry_vertices = new Vector3[geometry_Length];
			Vector3[] geometry_normals = new Vector3[geometry_Length];
			Color[] geometry_colors = new Color[geometry_Length];
			for (int gi = 0; gi < geometry_Length; ++gi)
			{
				var pair = geometry_indices[gi];
				geometry_vertices[pair.Value] = mesh.vertices[pair.Key];
				geometry_normals[pair.Value] = mesh.normals[pair.Key];
				geometry_colors[pair.Value] = mesh.colors[pair.Key];
			}
			return new GeometryInfo(geometry_vertices, geometry_faces, normals: geometry_normals, colors: geometry_colors);
		}
		public static async Task<GeometryInfo> AsyncConvertToGeometry(Vector3[] vertices, Vector3[] normals, int[] triangles, Color[] colors = null, bool[] isInterest = null, Color? mainColor = null)
		{
			var vertexLength = vertices.Length;
			var faceLength = triangles.Length / 3;
			List<Face> faces = new List<Face>();
			Dictionary<int, int> all_vertices = new Dictionary<int, int>();
			await Task.Run(() =>
			{
				for (int vi = 0; vi < vertexLength; ++vi)
				{
					all_vertices[vi] = -1;
				}
				int idx = 0;
				for (int fi = 0; fi < faceLength; ++fi)
				{
					int v0 = triangles[3 * fi];
					int v1 = triangles[3 * fi + 1];
					int v2 = triangles[3 * fi + 2];
					if (all_vertices[v0] == -1) all_vertices[v0] = idx++;
					if (all_vertices[v1] == -1) all_vertices[v1] = idx++;
					if (all_vertices[v2] == -1) all_vertices[v2] = idx++;
					faces.Add(new Face(all_vertices[v0], all_vertices[v1], all_vertices[v2]));
				}
			});
			var geometry_indices = all_vertices.Where(vec => vec.Value != -1).ToArray();
			var geometry_Length = all_vertices.Count(vec => vec.Value != -1);
			Face[] geometry_faces = faces.ToArray();
			Vector3[] geometry_vertices = new Vector3[geometry_Length];
			Vector3[] geometry_normals = new Vector3[geometry_Length];
			Color[] geometry_colors = new Color[geometry_Length];
			bool[] geometry_interest = new bool[geometry_Length];
			await Task.Run(() =>
			{
				for (int gi = 0; gi < geometry_Length; ++gi)
				{
					var pair = geometry_indices[gi];
					geometry_vertices[pair.Value] = vertices[pair.Key];
					geometry_normals[pair.Value] = normals[pair.Key];
					if (colors == null || colors.Length == 0) geometry_colors[pair.Value] = Color.white;
					else if (colors.Length == vertices.Length) geometry_colors[pair.Value] = colors[pair.Key];
					if (isInterest != null) geometry_interest[pair.Value] = isInterest[pair.Key];
				}
			});
			GeometryInfo geometry = new GeometryInfo(geometry_vertices, geometry_faces, normals: geometry_normals, colors: geometry_colors, mainColor: mainColor);
			geometry.isInterest = geometry_interest;
			return geometry;
		}
        #endregion
	}
}