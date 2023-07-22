using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using MeshLib.Utility.Queue;
using MathNet.Numerics.LinearAlgebra;
using DLA = MathNet.Numerics.LinearAlgebra.Double;

namespace MeshLib.Simplification
{
    #region 정적 연산자 클래스
    internal static class QuadricExtension
    {
        public static double QuadricError(this Matrix<double> a, Vector3 v)
        {
            return (v.x * a[0, 0] * v.x + v.y * a[1, 0] * v.x + v.z * a[2, 0] * v.x + a[3, 0] * v.x +
               v.x * a[0, 1] * v.y + v.y * a[1, 1] * v.y + v.z * a[2, 1] * v.y + a[3, 1] * v.y +
               v.x * a[0, 2] * v.z + v.y * a[1, 2] * v.z + v.z * a[2, 2] * v.z + a[3, 2] * v.z +
               v.x * a[0, 3] + v.y * a[1, 3] + v.z * a[2, 3] + a[3, 3]);
        }
        public static bool Less(this Vector3 a, Vector3 b)
        {
            if (a.x != b.x) return a.x < b.x;
            if (a.y != b.y) return a.y < b.y;
            return a.z < b.z;
        }
        public static Vector3 MulPosition(this Matrix<double> a, Vector3 b)
        {
            return new Vector3(
                (float)(a[0, 0] * b.x + a[0, 1] * b.y + a[0, 2] * b.z + a[0, 3]),
                (float)(a[1, 0] * b.x + a[1, 1] * b.y + a[1, 2] * b.z + a[1, 3]),
                (float)(a[2, 0] * b.x + a[2, 1] * b.y + a[2, 2] * b.z + a[2, 3]));
        }
        public static void AddPair(this Dictionary<QEMSolver.QuadricKey, QEMSolver.QuadricVertexPair> dic, QEMSolver.QuadricVertex a, QEMSolver.QuadricVertex b)
        {
            QEMSolver.QuadricKey key = QEMSolver.QuadricKey.Make(a, b);
            if (dic.TryGetValue(key, out var value)) return;
            dic[key] = new QEMSolver.QuadricVertexPair(a, b);
        }
        public static void AppendToList<K, T>(this Dictionary<K, List<T>> dic, K key, T newEntry)
        {
            if (dic.TryGetValue(key, out var list)) list.Add(newEntry);
            else dic.Add(key, new List<T> { newEntry });
        }
    }
    #endregion
    public class QEMSolver : ISimplificationSolver
    {
        #region 내부 클래스
        internal class QuadricVertex
        {
            public Vector3 vector;
            public Color color;
            public Matrix<double> quadric;
            public QuadricVertex(Vector3 vector, Color color)
            {
                this.vector = vector;
                this.color = color;
                this.quadric = DLA.DenseMatrix.Create(4, 4, 0);
            }
            public QuadricVertex(Vector3 vector, Color color, Matrix<double> quadric)
            {
                this.vector = vector;
                this.color = color;
                this.quadric = quadric;
            }
        }
        internal class QuadricFace
        {
            public QuadricVertex vertex0;
            public QuadricVertex vertex1;
            public QuadricVertex vertex2;
            public bool Removed;
            public bool Degenerate => vertex0 == vertex1 || vertex0 == vertex2 || vertex1 == vertex2;
            public QuadricFace(QuadricVertex v0, QuadricVertex v1, QuadricVertex v2)
            {
                this.vertex0 = v0;
                this.vertex1 = v1;
                this.vertex2 = v2;
                Removed = false;
            }
            public Vector3 Normal()
            {
                Vector3 e1 = vertex1.vector - vertex0.vector;
                Vector3 e2 = vertex2.vector - vertex0.vector;
                return Vector3.Cross(e1, e2).normalized;
            }
        }
        internal class QuadricVertexPair
        {
            public QuadricVertex A, B;
            public double CachedError;
            public bool Removed;
            public Matrix<double> quadric => A.quadric + B.quadric;
            public Vector3 vector
            {
                get
                {
                    var q = A.quadric + B.quadric;
                    if (Math.Abs(q.Determinant()) > 1e-3)
                    {
                        q[3, 0] = 0;
                        q[3, 1] = 0;
                        q[3, 2] = 0;
                        q[3, 3] = 1;
                        var v = q.Inverse().MulPosition(new Vector3());
                        return v;
                    }
                    //cannot compute best vector with matrix 
                    // look for vest along edge
                    int n = 32;
                    var a = A.vector;
                    var b = B.vector;
                    var bestE = -1d;
                    var bestV = new Vector3();
                    for (int i = 0; i < n; i++)
                    {
                        int frac = i * (1 / n);
                        var v = Vector3.Lerp(a, b, frac);
                        var e = A.quadric.QuadricError(v);
                        if (bestE < 0 || e < bestE)
                        {
                            bestE = e;
                            bestV = v;
                        }
                    }
                    return bestV;
                }
            }
            public Color color
            {
                get
                {
                    var position = vector;
                    var ap = (A.vector - position).magnitude;
                    var bp = (B.vector - position).magnitude;
                    var length = ap + bp;
                    var ratioA = ap / length;
                    var ratioB = bp / length;
                    var colorA = new Vector3(A.color.r, A.color.g, A.color.b);
                    var colorB = new Vector3(B.color.r, B.color.g, B.color.b);
                    var color = colorA * ratioB + colorB * ratioA;
                    return new Color(color.x, color.y, color.z);
                }
            }
            public double error
            {
                get
                {
                    if (CachedError < 0) CachedError = (A.quadric + B.quadric).QuadricError(vector);
                    return CachedError;
                }
            }

            public QuadricVertexPair(QuadricVertex a, QuadricVertex b)
            {
                if (a.vector.Less(b.vector))
                {
                    (a, b) = (b, a);
                }
                A = a;
                B = b;
                CachedError = -1;
                Removed = false;
            }
        }
        internal class QuadricKey
        {
            public Vector3 A, B;
            public QuadricKey(Vector3 a, Vector3 b)
            {
                this.A = a;
                this.B = b;
            }
            public static QuadricKey Make(QuadricVertex a, QuadricVertex b)
            {
                if (a.vector.Less(b.vector)) return new QuadricKey(a.vector, b.vector);
                return new QuadricKey(b.vector, a.vector);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (!(obj is QuadricKey)) return false;
                QuadricKey other = obj as QuadricKey;
                return (this.A == other.A && this.B == other.B);
            }
            public override int GetHashCode()
            {
                return A.GetHashCode() ^ B.GetHashCode();
            }
        }
        #endregion

        public GeometryInfo geometry;
        public int targetCount;

        public QEMSolver(GeometryInfo geometry, int targetCount)
        {
            this.geometry = geometry;
            this.targetCount = targetCount;
        }

        public async Task<GeometryInfo> AsyncSimplify()
        {
            Debug.Log("QEM: gather distinct vertices, pair");
            // gather distinct vertices
            var unique_vertices = await GathrerDistinctVertices();
            await Task.Run(() =>
            {
                // accumulate quadric matrices for each vertex based on its faces
                // assign initial quadric
                foreach(var face in geometry.faces)
                {
                    var quadric = ComputeQuadric(face);
                    unique_vertices[geometry.vertices[face.VertexIndex0]].quadric += quadric;
                    unique_vertices[geometry.vertices[face.VertexIndex1]].quadric += quadric;
                    unique_vertices[geometry.vertices[face.VertexIndex2]].quadric += quadric;
                }
                //vertex -> face map
            });
            var vertexToFaceMap = await ConvertVertexToFaceMap(unique_vertices);
            var unique_pair = await GatherDistinctPair(unique_vertices);
            var vertexToPairMap = await ConvertVertexToPairMap(unique_pair);
            var priorityQueue = await GetErrorPriorityQueue(unique_pair);
            int currentFaceCount = geometry.faceCount;
            Debug.Log("QEM: compute simplification");
            await Task.Run(() =>
            {
                while(currentFaceCount > targetCount && priorityQueue.Count > 0)
                {
                    var p = priorityQueue.Dequeue();
                    if (p.Removed) continue;
                    p.Removed = true;
                    var distinctFaces = new HashSet<QuadricFace>();
                    if (vertexToFaceMap.TryGetValue(p.A, out var a_related_quadric_face))
                        foreach (var f in a_related_quadric_face)
                        {
                            if (!f.Removed)
                            {
                                if (!distinctFaces.Contains(f))
                                    distinctFaces.Add(f);
                            }
                        }

                    if (vertexToFaceMap.TryGetValue(p.B, out var b_related_quadric_face))
                        foreach (var f in b_related_quadric_face)
                        {
                            if (!f.Removed)
                            {
                                if (!distinctFaces.Contains(f))
                                    distinctFaces.Add(f);
                            }
                        }
                    //get related pairs
                    var distintPairs = new HashSet<QuadricVertexPair>();
                    if (vertexToPairMap.TryGetValue(p.A, out var a_related_pair))
                        foreach (var q in a_related_pair)
                        {
                            if (!q.Removed)
                            {
                                if (!distintPairs.Contains(q))
                                    distintPairs.Add(q);
                            }
                        }

                    if (vertexToPairMap.TryGetValue(p.B, out var b_related_pair))
                        foreach (var q in b_related_pair)
                        {
                            if (!q.Removed)
                            {
                                if (!distintPairs.Contains(q))
                                    distintPairs.Add(q);
                            }
                        }

                    //create new vertex
                    QuadricVertex v = new QuadricVertex(p.vector, p.color, p.quadric);

                    //updateFaces
                    var newFaces = new List<QuadricFace>();
                    bool valid = true;
                    foreach (var f in distinctFaces)
                    {
                        var (v1, v2, v3) = (f.vertex0, f.vertex1, f.vertex2);
                        if (v1 == p.A || v1 == p.B)
                            v1 = v;
                        if (v2 == p.A || v2 == p.B)
                            v2 = v;
                        if (v3 == p.A || v3 == p.B)
                            v3 = v;

                        var face = new QuadricFace(v1, v2, v3);
                        if (face.Degenerate)
                            continue;
                        
                        if (Vector3.Dot(face.Normal(), face.Normal()) < 1e-3)
                        {
                            valid = false;
                            break;
                        }
                        newFaces.Add(face);
                    }

                    if (!valid)
                        continue;

                    if (vertexToFaceMap.TryGetValue(p.A, out var a_face))
                        vertexToFaceMap.Remove(p.A);
                    if (vertexToFaceMap.TryGetValue(p.B, out var b_face))
                        vertexToFaceMap.Remove(p.B);

                    foreach (var f in distinctFaces)
                    {
                        f.Removed = true;
                        currentFaceCount--;
                    }

                    foreach (var f in newFaces)
                    {
                        currentFaceCount++;
                        vertexToFaceMap.AppendToList(f.vertex0, f);
                        vertexToFaceMap.AppendToList(f.vertex1, f);
                        vertexToFaceMap.AppendToList(f.vertex2, f);
                    }

                    if (vertexToPairMap.TryGetValue(p.A, out var a_pair))
                        vertexToPairMap.Remove(p.A);

                    if (vertexToPairMap.TryGetValue(p.B, out var b_pair))
                        vertexToPairMap.Remove(p.B);

                    var seen = new Dictionary<Vector3, bool>();

                    foreach (var q in distintPairs)
                    {
                        q.Removed = true;
                        priorityQueue.Remove(q);
                        var (a, b) = (q.A, q.B);

                        if (a == p.A || a == p.B)
                        {
                            a = v;
                        }
                        if (b == p.A || b == p.B)
                        {
                            b = v;
                        }
                        if (b == v)
                        {
                            (a, b) = (b, a);
                            // a = v
                        }
                        if (seen.TryGetValue(b.vector, out bool isSeen1) && isSeen1)
                        {
                            //ignore duplicates
                            continue;
                        }
                        seen[b.vector] = true;

                        var np = new QuadricVertexPair(a, b);
                        priorityQueue.Enqueue(np, np.error);

                        vertexToPairMap.AppendToList(a, np);
                        vertexToPairMap.AppendToList(b, np);
                    }
                }
            });
            Debug.Log("QEM: complete");
            var unique_face = await GatherDistinctFaces(vertexToFaceMap);
            return await ConvertToGeometry(unique_face);
        }

        private async Task<Dictionary<Vector3, QuadricVertex>> GathrerDistinctVertices()
        {
            Dictionary<Vector3, QuadricVertex> distinct_vertices = new Dictionary<Vector3, QuadricVertex>(geometry.vertexCount);
            await Task.Run(() =>
            {
                foreach (var face in geometry.faces)
                {
                    var indices = face.GetIndices();
                    foreach (var idx in indices)
                    {
                        var point = geometry.vertices[idx];
                        if (distinct_vertices.TryGetValue(point, out QuadricVertex qv)) continue;
                        distinct_vertices[point] = new QuadricVertex(point, geometry.colors[idx]);
                    }
                }
            });
            return distinct_vertices;
        }

        private Matrix<double> ComputeQuadric(Face face)
        {
            var v1 = geometry.vertices[face.VertexIndex0];
            var v2 = geometry.vertices[face.VertexIndex1];
            var v3 = geometry.vertices[face.VertexIndex2];
            var e1 = v2 - v1;
            var e2 = v3 - v1;
            var n = Vector3.Cross(e1, e2).normalized;
            double a = n.x;
            double b = n.y;
            double c = n.z;
            double d = -a * v1.x - b * v1.y - c * v1.z;
            Vector<double> vec = Vector<double>.Build.DenseOfArray(new double[] { a, b, c, d });
            return vec.OuterProduct(vec);
        }
        private async Task<Dictionary<QuadricVertex, List<QuadricFace>>> ConvertVertexToFaceMap(Dictionary<Vector3, QuadricVertex> quadricVertices)
        {
            Dictionary<QuadricVertex, List<QuadricFace>> vertexToFaces = new Dictionary<QuadricVertex, List<QuadricFace>>(geometry.vertexCount);
            await Task.Run(() =>
            {
                foreach (var face in geometry.faces)
                {
                    QuadricVertex[] qvs = new QuadricVertex[]{
                    quadricVertices[geometry.vertices[face.VertexIndex0]],
                    quadricVertices[geometry.vertices[face.VertexIndex1]],
                    quadricVertices[geometry.vertices[face.VertexIndex2]] };
                    QuadricFace quadricFace = new QuadricFace(qvs[0], qvs[1], qvs[2]);
                    foreach (var qv in qvs)
                    {
                        if (vertexToFaces.TryGetValue(qv, out var list))
                            list.Add(quadricFace);
                        else
                            vertexToFaces[qv] = new List<QuadricFace>() { quadricFace };
                    }
                }
            });
            return vertexToFaces;
        }
        private async Task<Dictionary<QuadricKey, QuadricVertexPair>> GatherDistinctPair(Dictionary<Vector3, QuadricVertex> quadricVertices)
        {
            Dictionary<QuadricKey, QuadricVertexPair> quadricVertexPairMap = new Dictionary<QuadricKey, QuadricVertexPair>(geometry.faceCount);
            await Task.Run(() =>
            {
                foreach (var face in geometry.faces)
                {
                    QuadricVertex[] qvs = new QuadricVertex[]{
                    quadricVertices[geometry.vertices[face.VertexIndex0]],
                    quadricVertices[geometry.vertices[face.VertexIndex1]],
                    quadricVertices[geometry.vertices[face.VertexIndex2]] };
                    quadricVertexPairMap.AddPair(qvs[0], qvs[1]);
                    quadricVertexPairMap.AddPair(qvs[1], qvs[2]);
                    quadricVertexPairMap.AddPair(qvs[0], qvs[2]);
                }
            });
            return quadricVertexPairMap;
        }
        private async Task<Dictionary<QuadricVertex, List<QuadricVertexPair>>> ConvertVertexToPairMap(Dictionary<QEMSolver.QuadricKey, QuadricVertexPair> uniquePairs)
        {
            Dictionary<QuadricVertex, List<QuadricVertexPair>> vertexPairs = new Dictionary<QuadricVertex, List<QuadricVertexPair>>(geometry.vertexCount);
            await Task.Run(() =>
            {
                foreach(var pair in uniquePairs)
                {
                    if (vertexPairs.TryGetValue(pair.Value.A, out var a_list))
                        a_list.Add(pair.Value);
                    else
                        vertexPairs[pair.Value.A] = new List<QuadricVertexPair>() { pair.Value };
                    if (vertexPairs.TryGetValue(pair.Value.B, out var b_list))
                        b_list.Add(pair.Value);
                    else
                        vertexPairs[pair.Value.B] = new List<QuadricVertexPair>() { pair.Value };
                }
            });
            return vertexPairs;
        }
        private async Task<SimplePriorityQueue<QuadricVertexPair, double>> GetErrorPriorityQueue(Dictionary<QEMSolver.QuadricKey, QuadricVertexPair> uniquePairs)
        {
            var priorityQueue = new SimplePriorityQueue<QuadricVertexPair, double>((f1, f2) => f1 > f2 ? 1 : -1);
            await Task.Run(() =>
            {
                foreach (KeyValuePair<QuadricKey, QuadricVertexPair> item in uniquePairs)
                {
                    priorityQueue.Enqueue(item.Value, item.Value.error);
                }
            });
            return priorityQueue;
        }
        private async Task<HashSet<QuadricFace>> GatherDistinctFaces(Dictionary<QuadricVertex, List<QuadricFace>> vertexToFaceMap)
        {
            var finalDistinctFaces = new HashSet<QuadricFace>();
            await Task.Run(() =>
            {
                foreach (var faces in vertexToFaceMap)
                {
                    foreach (var face in faces.Value)
                    {
                        if (!face.Removed)
                        {
                            if (!finalDistinctFaces.Contains(face))
                                finalDistinctFaces.Add(face);
                        }
                    }
                }
            });
            return finalDistinctFaces;
        }
        private async Task<GeometryInfo> ConvertToGeometry(HashSet<QuadricFace> uniqueFace)
        {
            Dictionary<Vector3, int> verticesToIndices = new Dictionary<Vector3, int>();
            Dictionary<int, Color> colorsToIndices = new Dictionary<int, Color>();
            int vIdx = 0;
            int fIdx = 0;
            Face[] faces = new Face[uniqueFace.Count];
            await Task.Run(() =>
            {
                foreach (var quadricface in uniqueFace)
                {
                    int p0 = -1;
                    int p1 = -1;
                    int p2 = -1;
                    if (verticesToIndices.TryGetValue(quadricface.vertex0.vector, out int i0)) p0 = i0;
                    else
                    {
                        verticesToIndices[quadricface.vertex0.vector] = vIdx;
                        colorsToIndices[vIdx] = quadricface.vertex0.color;
                        p0 = vIdx++;
                    }
                    if (verticesToIndices.TryGetValue(quadricface.vertex1.vector, out int i1)) p1 = i1;
                    else
                    {
                        verticesToIndices[quadricface.vertex1.vector] = vIdx;
                        colorsToIndices[vIdx] = quadricface.vertex1.color;
                        p1 = vIdx++;
                    }
                    if (verticesToIndices.TryGetValue(quadricface.vertex2.vector, out int i2)) p2 = i2;
                    else
                    {
                        verticesToIndices[quadricface.vertex2.vector] = vIdx;
                        colorsToIndices[vIdx] = quadricface.vertex2.color;
                        p2 = vIdx++;
                    }
                    faces[fIdx++] = new Face(p0, p1, p2);
                }
            });
            var vertices = verticesToIndices.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();
            var colors = colorsToIndices.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
            return new GeometryInfo(vertices, faces, colors: colors);
        }
    }
}
