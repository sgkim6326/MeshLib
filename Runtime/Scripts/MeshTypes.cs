using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;

namespace MeshLib
{
    #region Face
    /// <summary>Triangle</summary>
    public class Face
    {
        public int VertexIndex0;
        public int VertexIndex1;
        public int VertexIndex2;
        public Face()
        {
            VertexIndex0 = -1;
            VertexIndex1 = -1;
            VertexIndex2 = -1;
        }
        public Face(int p0, int p1, int p2)
        {
            VertexIndex0 = p0;
            VertexIndex1 = p1;
            VertexIndex2 = p2;
        }
        public Vector3 CaculateTriangleNormal(Vector3[] vertices)
        {
            Vector3 p0 = vertices[VertexIndex0];
            Vector3 p1 = vertices[VertexIndex1];
            Vector3 p2 = vertices[VertexIndex2];
            return Vector3.Cross((p1 - p0).normalized, (p2 - p0).normalized).normalized;
        }
        public double CalculateArea(Vector3[] vertices)
        {
            Vector3 p0 = vertices[VertexIndex0];
            Vector3 p1 = vertices[VertexIndex1];
            Vector3 p2 = vertices[VertexIndex2];
            var faceVec1 = p1 - p0;
            var faceVec2 = p2 - p1;
            var vecArea = Vector3.Cross(faceVec1, faceVec2);
            return vecArea.magnitude/2;//sqrt 적용
        }
        public int[] GetIndices() => new int[3] { VertexIndex0, VertexIndex1, VertexIndex2 };
        public bool IsContainIndex(int index) => index == VertexIndex0 || index == VertexIndex1 || index == VertexIndex2;
    }
    #endregion
    #region GeometryInfo
    public class GeometryInfo
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Color[] colors;
        public bool[] isInterest;
        public Face[] faces;
        public Color materialColor;
        public int vertexCount => vertices.Length;
        public int faceCount => faces.Length;
        public Bounds bounds { get; private set; }
        public int[] triangles
        {
            get
            {
                int[] tris = new int[faces.Length * 3];
                for(int fi = 0; fi < faces.Length; ++fi)
                {
                    tris[3*fi] = faces[fi].VertexIndex0;
                    tris[3*fi+1] = faces[fi].VertexIndex1;
                    tris[3*fi+2] = faces[fi].VertexIndex2;
                }
                return tris;
            }
        }
        public GeometryInfo(IEnumerable<Vector3> vertices, IEnumerable<Face> faces, IEnumerable<Vector3> normals = null, IEnumerable<Color> colors = null, Color? mainColor = null)
        {
            this.vertices = vertices.ToArray();
            var (minPosition, maxPosition) = (this.vertices[0], this.vertices[0]);
            foreach(var vertex in this.vertices)
            {
                     if (vertex.x < minPosition.x) minPosition.x = vertex.x;
                else if (vertex.x > maxPosition.x) maxPosition.x = vertex.x;
                     if (vertex.y < minPosition.y) minPosition.y = vertex.y;
                else if (vertex.y > maxPosition.y) maxPosition.y = vertex.y;
                     if (vertex.z < minPosition.z) minPosition.z = vertex.z;
                else if (vertex.z > maxPosition.z) maxPosition.z = vertex.z;
            }
            this.bounds = new Bounds((minPosition + maxPosition) / 2, maxPosition - minPosition);
            this.faces = faces.ToArray();
            if (normals == null)
            {
                this.normals = new Vector3[this.vertices.Length];
                double[] duplicatedFaceNormal = new double[this.vertices.Length];
                Vector3[] sumFaceNormal = new Vector3[this.vertices.Length];
                for (int vi = 0; vi < this.vertices.Length; ++vi) sumFaceNormal[vi] = Vector3.zero;
                foreach(var face in this.faces)
                {
                    var normal = face.CaculateTriangleNormal(this.vertices);
                    sumFaceNormal[face.VertexIndex0] += normal * (float)face.CalculateArea(this.vertices);
                    sumFaceNormal[face.VertexIndex1] += normal * (float)face.CalculateArea(this.vertices);
                    sumFaceNormal[face.VertexIndex2] += normal * (float)face.CalculateArea(this.vertices);
                    duplicatedFaceNormal[face.VertexIndex0] += face.CalculateArea(this.vertices);
                    duplicatedFaceNormal[face.VertexIndex1] += face.CalculateArea(this.vertices);
                    duplicatedFaceNormal[face.VertexIndex2] += face.CalculateArea(this.vertices);
                }
                for (int vi = 0; vi < this.vertices.Length; ++vi)
                {
                    this.normals[vi] = sumFaceNormal[vi] / (float)duplicatedFaceNormal[vi];
                }
            }
            else this.normals = normals.ToArray();
            if (colors == null || colors.Count() == 0)
            {
                this.colors = new Color[this.vertices.Length];
                for(int c = 0; c < this.vertices.Length; ++c)
                {
                    this.colors[c] = Color.white;
                }
            }
            else this.colors = colors.ToArray();
            if (mainColor == null) this.materialColor = Color.white;
            else this.materialColor = mainColor.Value;
            this.isInterest = new bool[this.vertices.Length];
        }
        public Vector3 GetFaceCenter(int faceIndex)
        {
            var face = faces[faceIndex];
            var p0 = vertices[face.VertexIndex0];
            var p1 = vertices[face.VertexIndex1];
            var p2 = vertices[face.VertexIndex2];
            return (p0 + p1 + p2) / 3;
        }
        public Vector3 GetFaceNormal(int faceIndex)
        {
            var face = faces[faceIndex];
            Vector3 p0 = vertices[face.VertexIndex0];
            Vector3 p1 = vertices[face.VertexIndex1];
            Vector3 p2 = vertices[face.VertexIndex2];
            return Vector3.Cross((p1 - p0).normalized, (p2 - p0).normalized).normalized;
            //Plane plane = new Plane(p0, p1, p2);
            //return plane.normal;
        }
        public GameObject GenerateGameObject(string name, bool recalculateNormal = false)
        {
            Mesh target = new Mesh();
            target.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            target.vertices = vertices;
            target.normals = normals;
            target.triangles = triangles;
            target.colors = colors;
            if (recalculateNormal)
                target.RecalculateNormals();
            GameObject go = new GameObject(name);
            go.AddComponent<MeshFilter>().mesh = target;
            go.AddComponent<MeshRenderer>().material = new Material(Shader.Find("MeshLib/VertexColor"));
            return go;
        }
        public Mesh ToMesh(bool recalculateNormal = false)
        {
            Mesh target = new Mesh();
            target.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            target.vertices = vertices;
            target.normals = normals;
            target.colors = colors;
            target.triangles = triangles;
            if (recalculateNormal)
                target.RecalculateNormals();
            return target;
        }
        public void SaveObj(string path)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach(var vertex in vertices)
                sb.Append(string.Format("v {0} {1} {2}\n", -vertex.x, vertex.y, vertex.z));
            sb.Append("\n");
            foreach(var normal in normals)
                sb.Append(string.Format("vn {0} {1} {2}\n", -normal.x, normal.y, normal.z));
            foreach (var face in faces)
                sb.Append(string.Format("f {0} {1} {2}\n", face.VertexIndex0 + 1, face.VertexIndex1 + 1, face.VertexIndex2 + 1));
            System.IO.File.WriteAllText(path, sb.ToString());
        }
        public override string ToString() => string.Format("Vertices count: {0}, Faces count: {1}, Normals count: {2}, Colors count: {3}", vertexCount, faceCount, normals.Length, colors.Length);
    }
    #endregion
    public class CMYK
    {
        public float c;
        public float m;
        public float y;
        public float k;
        public static CMYK white => new CMYK(0, 0, 0, 0);
        public static CMYK cyan => new CMYK(1, 0, 0, 0);
        public static CMYK magenta => new CMYK(0, 1, 0, 0);
        public static CMYK yellow => new CMYK(0, 0, 1, 0);
        public static CMYK red => new CMYK(0, 1, 1, 0);
        public static CMYK blue => new CMYK(1, 0, 1, 0);
        public static CMYK green => new CMYK(1, 1, 0, 0);
        public static CMYK Lerp(CMYK a, CMYK b, float t)
        {
            var color = CMYK.white;
            color.c = Mathf.Lerp(a.c, b.c, t);
            color.m = Mathf.Lerp(a.m, b.m, t);
            color.y = Mathf.Lerp(a.y, b.y, t);
            color.k = Mathf.Lerp(a.k, b.k, t);
            return color;
        }
        public CMYK(float c, float y, float m, float k)
        {
            this.c = c;
            this.m = m;
            this.y = y;
            this.k = k;
        }
        public CMYK(Color color)
        {
            k = ClampCmyk(1 - Mathf.Max(color.r, color.g, color.b));
            c = ClampCmyk((1 - color.r - k) / (1 - k));
            m = ClampCmyk((1 - color.g - k) / (1 - k));
            y = ClampCmyk((1 - color.b - k) / (1 - k));
        }
        private float ClampCmyk(float value)
        {
            if (value < 0 || float.IsNaN(value)) value = 0;
            return value;
        }
        public Color ToRGB()
        {
            var r = (1 - c) * (1 - k);
            var g = (1 - m) * (1 - k);
            var b = (1 - y) * (1 - k);
            return new Color(r, g, b);
        }
    }
}