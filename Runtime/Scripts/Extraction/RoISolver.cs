using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib.Extraction
{
    public class RoISolver : IExtractionSolver
    {
        public enum Type
        {
            X_Axis, Y_Axis, Z_Axis
        }
        (double, double) roiRange;
        Type roiType;
        GeometryInfo geometry;

        public RoISolver(GeometryInfo geometry, (double, double) range, Type type)
        {
            this.geometry = geometry;
            this.roiRange = range;
            this.roiType = type;
        }
        public async Task<GeometryInfo> AsyncExtract()
        {
            GeometryInfo geoWithRoI = await ComputeRoI();
            var result = await RemoveDuplication(geoWithRoI);
            Debug.Log(string.Format("RoI: complete\n{0}", result.ToString()));
            return geoWithRoI;
        }

        async Task<GeometryInfo> ComputeRoI()
        {
            Debug.Log("RoI: compute remained vertex/face");
            var vIndex = geometry.vertices.Length;
            Queue<Face> remainedFaces = new Queue<Face>(geometry.faces);
            List<Face> totalFaces = new List<Face>(geometry.faces.Length);
            List<Vector3> totalVertices = new List<Vector3>(geometry.vertices);
            await Task.Run(() =>
            {
                while (remainedFaces.Count != 0)
                {
                    var cFace = remainedFaces.Dequeue();
                    int safedVertexCnt = CalculateSafedVertexCnt(totalVertices, cFace);
                    if (safedVertexCnt == 3)
                    {
                        totalFaces.Add(cFace);
                        continue;
                    }
                    else if (safedVertexCnt == 0) continue;
                    var safedVertexIndices = GetSafedVertexIndices(totalVertices, cFace);
                    var p0 = totalVertices[cFace.VertexIndex0];
                    var p1 = totalVertices[cFace.VertexIndex1];
                    var p2 = totalVertices[cFace.VertexIndex2];
                    if (safedVertexCnt == 1)
                    {
                        if (cFace.VertexIndex0 == safedVertexIndices[0])
                        {
                            var np1 = SlicedPoint(p0, p1);
                            var np2 = SlicedPoint(p0, p2);
                            totalVertices.Add(np1);//vIndex
                            totalVertices.Add(np2);//vIndex+1
                            Face newFace = new Face(cFace.VertexIndex0, vIndex, vIndex + 1);
                            remainedFaces.Enqueue(newFace);
                        }
                        else if (cFace.VertexIndex1 == safedVertexIndices[0])
                        {
                            var np0 = SlicedPoint(p1, p0);
                            var np2 = SlicedPoint(p1, p2);
                            totalVertices.Add(np0);//vIndex
                            totalVertices.Add(np2);//vIndex+1
                            Face newFace = new Face(vIndex, cFace.VertexIndex1, vIndex + 1);
                            remainedFaces.Enqueue(newFace);
                        }
                        else
                        {
                            var np0 = SlicedPoint(p2, p0);
                            var np1 = SlicedPoint(p2, p1);
                            totalVertices.Add(np0);//vIndex
                            totalVertices.Add(np1);//vIndex+1
                            Face newFace = new Face(vIndex, vIndex + 1, cFace.VertexIndex2);
                            remainedFaces.Enqueue(newFace);
                        }
                    }
                    else
                    {
                        if (safedVertexIndices.Contains(cFace.VertexIndex0)
                            && safedVertexIndices.Contains(cFace.VertexIndex1))
                        {
                            var np02 = SlicedPoint(p0, p2);
                            var np12 = SlicedPoint(p1, p2);
                            totalVertices.Add(np02);//vIndex
                            totalVertices.Add(np12);//vIndex+1
                            Face newFace0 = new Face(cFace.VertexIndex0, cFace.VertexIndex1, vIndex + 1);
                            Face newFace1 = new Face(cFace.VertexIndex0, vIndex + 1, vIndex);
                            remainedFaces.Enqueue(newFace0);
                            remainedFaces.Enqueue(newFace1);
                        }
                        else if (safedVertexIndices.Contains(cFace.VertexIndex0)
                            && safedVertexIndices.Contains(cFace.VertexIndex2))
                        {
                            var np01 = SlicedPoint(p0, p1);
                            var np21 = SlicedPoint(p2, p1);
                            totalVertices.Add(np01);//vIndex
                            totalVertices.Add(np21);//vIndex+1
                            Face newFace0 = new Face(cFace.VertexIndex0, vIndex, vIndex + 1);
                            Face newFace1 = new Face(cFace.VertexIndex0, vIndex + 1, cFace.VertexIndex2);
                            remainedFaces.Enqueue(newFace0);
                            remainedFaces.Enqueue(newFace1);
                        }
                        else
                        {
                            var np10 = SlicedPoint(p1, p0);
                            var np20 = SlicedPoint(p2, p0);
                            totalVertices.Add(np10);//vIndex
                            totalVertices.Add(np20);//vIndex+1
                            Face newFace0 = new Face(vIndex, cFace.VertexIndex1, cFace.VertexIndex2);
                            Face newFace1 = new Face(vIndex, cFace.VertexIndex2, vIndex + 1);
                            remainedFaces.Enqueue(newFace0);
                            remainedFaces.Enqueue(newFace1);
                        }
                    }
                    vIndex += 2;
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
        Vector3 SlicedPoint(Vector3 src, Vector3 dst)
        {
            var (min, max) = roiRange;
            Vector3 newPoint = Vector3.zero;
            var dir = (dst - src).normalized;
            double x_value = 0;
            double y_value = 0;
            double z_value = 0;
            switch (roiType)
            {
                case Type.X_Axis:
                    if (dst.x > max) x_value = max;
                    else x_value = min;
                    if (dir.y == 0) y_value = src.y;
                    else y_value = dir.y * (x_value - src.x) / dir.x + src.y;
                    if (dir.z == 0) z_value = src.z;
                    else z_value = dir.z * (x_value - src.x) / dir.x + src.z;
                    newPoint = new Vector3((float)x_value, (float)y_value, (float)z_value);
                    break;
                case Type.Y_Axis:
                    if (dst.y > max) y_value = max - 1e-5;
                    else y_value = min + 1e-5;
                    if (dir.x == 0) x_value = src.x;
                    else x_value = dir.x * (y_value - src.y) / dir.y + src.x;
                    if (dir.z == 0) z_value = src.z;
                    else z_value = dir.z * (y_value - src.y) / dir.y + src.z;
                    if (double.IsNaN(x_value) || double.IsNaN(z_value)
                        || double.IsInfinity(x_value) || double.IsInfinity(x_value))
                    {
                        Debug.Log(string.Format("check\ndir: {0}, {1}, {2}\n" +
                            "src: {0}, {1}, {2}\n" +
                            "dst: {0}, {1}, {2}", dir.x, dir.y, dir.z, src.x, src.y, src.z, dst.x, dst.y, dst.z));
                    }
                    newPoint = new Vector3((float)x_value, (float)y_value, (float)z_value);
                    break;
                case Type.Z_Axis:
                    if (dst.z > max) z_value = max;
                    else z_value = min;
                    if (dir.x == 0) x_value = src.x;
                    else x_value = dir.x * (z_value - src.z) / dir.z + src.x;
                    if (dir.y == 0) y_value = src.y;
                    else y_value = dir.y * (z_value - src.z) / dir.z + src.y;
                    newPoint = new Vector3((float)x_value, (float)y_value, (float)z_value);
                    break;
            }
            return newPoint;
        }
        int[] GetSafedVertexIndices(List<Vector3> vertices, Face face)
        {
            var (min, max) = roiRange;
            int[] safedVertexIndices = new int[2];
            int i = 0;
            foreach (var idx in face.GetIndices())
            {
                var point = vertices[idx];
                switch (roiType)
                {
                    case Type.X_Axis:
                        if (point.x >= min && point.x <= max) safedVertexIndices[i++] = idx;
                        break;
                    case Type.Y_Axis:
                        if (point.y >= min && point.y <= max) safedVertexIndices[i++] = idx;
                        break;
                    case Type.Z_Axis:
                        if (point.z >= min && point.z <= max) safedVertexIndices[i++] = idx;
                        break;
                }
            }
            return safedVertexIndices;
        }
        int CalculateSafedVertexCnt(List<Vector3> vertices, Face face)
        {
            int safedVertexCnt = 0;
            var (min, max) = roiRange;
            foreach (var idx in face.GetIndices())
            {
                var point = vertices[idx];
                switch (roiType)
                {
                    case Type.X_Axis:
                        if (point.x >= min && point.x <= max) ++safedVertexCnt;
                        break;
                    case Type.Y_Axis:
                        if (point.y >= min && point.y <= max) ++safedVertexCnt;
                        break;
                    case Type.Z_Axis:
                        if (point.z >= min && point.z <= max) ++safedVertexCnt;
                        break;
                }
            }
            return safedVertexCnt;
        }
        public GeometryInfo Extract() => throw new System.NotImplementedException();
    }
}