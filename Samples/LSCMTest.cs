//#define SAVE
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Text.RegularExpressions;
using Complex = System.Numerics.Complex;
using CLA = MathNet.Numerics.LinearAlgebra.Complex;
using DLA = MathNet.Numerics.LinearAlgebra.Double;
using System;
using MeshLib.Cluster;

namespace MeshLib.Example
{
    public class LSCMTest : MonoBehaviour
    {
        public string MeshPath = "";
        public MeshFilter GraphicModel;
        public int K = 4;
        IEnumerator Start()
        {
            var vertices = GraphicModel.mesh.vertices;
            var normals = GraphicModel.mesh.normals;
            var triangles = GraphicModel.mesh.triangles;


            GeometryInfo geometry = null;
            Task task = Task.Run(async () => geometry = await MeshTool.AsyncConvertToGeometry(vertices, normals, triangles));
            yield return new WaitUntil(() => task.IsCompleted);
            GeometryInfo[] geoSegment = null;
            MeshLib.Cluster.ClusteredVertex[] infos = null;
            IClusterSolver cluster = new KmeansCluster(geometry, K);
            task = Task.Run(async () => infos = await cluster.AsyncComputeCluster());
            yield return new WaitUntil(() => task.IsCompleted);
            task = Task.Run(async () => geoSegment = await MeshTool.AsyncGeometrySegmentation(geometry, infos));
            yield return new WaitUntil(() => task.IsCompleted);
            geometry = geoSegment[0];
            geometry.GenerateGameObject("test");
            var t = geometry.vertices.Select(vec => new double[3] { vec.x, vec.y, vec.z }).ToArray();
            DLA.Matrix verticesMatrix = DLA.DenseMatrix.Create(t.Length, 3, 0);
            for (int i = 0; i < verticesMatrix.RowCount; ++i)
            {
                verticesMatrix[i, 0] = t[i][0];
                verticesMatrix[i, 1] = t[i][1];
                verticesMatrix[i, 2] = t[i][2];
            }
            var tt = geometry.faces.Select(vec => vec.GetIndices()).ToArray();
            DLA.Matrix triangleMatrix = DLA.DenseMatrix.Create(tt.Length, 3, 0);
            for (int i = 0; i < triangleMatrix.RowCount; ++i)
            {
                triangleMatrix[i, 0] = tt[i][0] + 1;
                triangleMatrix[i, 1] = tt[i][1] + 1;
                triangleMatrix[i, 2] = tt[i][2] + 1;
            }
#if SAVE
        verticesMatrix.SaveToCsvData("vertex");
        triangleMatrix.SaveToCsvData("triangle");
#endif
            var lscm = new MeshLib.Flattening.LSCMSolver(geometry);
            MeshLib.Flattening.FlatResult[] result = null;
            task = Task.Run(async () => result = await lscm.AsyncCompute3Dto2D());
            yield return new WaitUntil(() => task.IsCompleted);
            var center = result.OrderBy(item => Vector2.Distance(new Vector2(0.5f, 0.5f), item.center)).First();
            var sp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sp.transform.localScale = Vector3.one / 100;
            sp.transform.position = lscm.GetFacePosition(center);
        }
    }
}