using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib.Upscale
{
    public class LBUSolver: IUpscaleSolver
    {
        public GeometryInfo geometry;
        public double thresLine;

        public LBUSolver(GeometryInfo geometry, double thres)
        {
            this.geometry = geometry;
            this.thresLine = thres;
            if (thres <= 0) throw new System.ArgumentException("쓰레스홀드는 0보다 커야합니다");
        }

        public async Task<GeometryInfo> AsyncUpscale()
        {
            var upscaledGeometry = await ComputeUpscale();
            var result = await RemoveDuplication(upscaledGeometry);
            Debug.Log(string.Format("Line based Upscale: complete\n{0}", result.ToString()));
            return result;
        }
        async Task<GeometryInfo> ComputeUpscale()
        {
            Debug.Log("Line based Upscale: compute upscale");
            var vIndex = geometry.vertices.Length;
            Queue<Face> remainedFaces = new Queue<Face>(geometry.faces);
            List<Face> totalFaces = new List<Face>(geometry.faces.Length);
            List<Vector3> totalVertices = new List<Vector3>(geometry.vertices);
            await Task.Run(() =>
            {
                while (remainedFaces.Count != 0 )
                {
                    var cFace = remainedFaces.Dequeue();
                    if (isAllLineSmallerThanThres(totalVertices, cFace))
                    {
                        totalFaces.Add(cFace);
                        continue;
                    }
                    var p0 = totalVertices[cFace.VertexIndex0];
                    var p1 = totalVertices[cFace.VertexIndex1];
                    var p2 = totalVertices[cFace.VertexIndex2];
                    var p01 = (p0 + p1) / 2;
                    var p02 = (p0 + p2) / 2;
                    var p12 = (p1 + p2) / 2;
                    totalVertices.Add(p01);//vIndex
                    totalVertices.Add(p02);//vIndex+1
                    totalVertices.Add(p12);//vIndex+2
                    Face newFace0 = new Face(cFace.VertexIndex0, vIndex, vIndex + 1);
                    Face newFace1 = new Face(vIndex, cFace.VertexIndex1, vIndex + 2);
                    Face newFace2 = new Face(vIndex, vIndex + 2, vIndex + 1);
                    Face newFace3 = new Face(vIndex + 1, vIndex + 2, cFace.VertexIndex2);
                    remainedFaces.Enqueue(newFace0);
                    remainedFaces.Enqueue(newFace1);
                    remainedFaces.Enqueue(newFace2);
                    remainedFaces.Enqueue(newFace3);
                    vIndex += 3;
                }
            });
            return new GeometryInfo(totalVertices, totalFaces);
        }
        async Task<GeometryInfo> RemoveDuplication(GeometryInfo geometry)
        {
            Debug.Log("Line based Upscale: remove duplication");
            Vector3 GetPoint(int index) => geometry.vertices[index];
            Dictionary<Vector3, int> vertices2Indices = new Dictionary<Vector3, int>();
            int vIdx = 0;
            int fIdx = 0;
            Face[] faces = new Face[geometry.faces.Length];
            await Task.Run(() =>
            {
                foreach (var face in geometry.faces)
                {
                    int p0 = -1;
                    int p1 = -1;
                    int p2 = -1;
                    if (vertices2Indices.TryGetValue(GetPoint(face.VertexIndex0), out int i0)) p0 = i0;
                    else
                    {
                        vertices2Indices[GetPoint(face.VertexIndex0)] = vIdx;
                        p0 = vIdx++;
                    }
                    if (vertices2Indices.TryGetValue(GetPoint(face.VertexIndex1), out int i1)) p1 = i1;
                    else
                    {
                        vertices2Indices[GetPoint(face.VertexIndex1)] = vIdx;
                        p1 = vIdx++;
                    }
                    if (vertices2Indices.TryGetValue(GetPoint(face.VertexIndex2), out int i2)) p2 = i2;
                    else
                    {
                        vertices2Indices[GetPoint(face.VertexIndex2)] = vIdx;
                        p2 = vIdx++;
                    }
                    faces[fIdx++] = new Face(p0, p1, p2);
                }
            });
            var vertices = vertices2Indices.OrderBy(pair => pair.Value).Select(pair => pair.Key);
            return new GeometryInfo(vertices, faces);
        }
        bool isAllLineSmallerThanThres(List<Vector3> vertices, Face face)
        {
            Vector3 p0 = vertices[face.VertexIndex0];
            Vector3 p1 = vertices[face.VertexIndex1];
            Vector3 p2 = vertices[face.VertexIndex2];
            var a = (p1 - p0).magnitude;
            var b = (p2 - p1).magnitude;
            var c = (p0 - p2).magnitude;
            return a < this.thresLine && b < this.thresLine && c < this.thresLine;
        }
    }
}
