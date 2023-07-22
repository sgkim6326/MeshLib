using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MeshLib.Cluster;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MeshLib.Example
{
    public class ClusterTest : MonoBehaviour
    {
        public MeshFilter GraphicModel;
        public int K;
        IEnumerator Start()
        {
            var mesh = GraphicModel.mesh;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;

            MeshLib.GeometryInfo geometry = null;
            Task task = Task.Run(async () =>
            {
                try
                {
                    geometry = await MeshLib.MeshTool.AsyncConvertToGeometry(vertices, normals, triangles);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });

            yield return new WaitUntil(() => task.IsCompleted);
            var ε = geometry.bounds.size.magnitude / 100;
            IUpscaleSolver lbuSolver = new MeshLib.Upscale.LBUSolver(geometry, ε);
            task = Task.Run(async () => geometry = await lbuSolver.AsyncUpscale());
            yield return new WaitUntil(() => task.IsCompleted);
            IExtractionSolver OutsurfExtractor = new MeshLib.Extraction.OutsideSurfaceSolver(geometry);
            geometry = OutsurfExtractor.Extract();
            ClusteredVertex[] kmClusterResult = null;
            ClusteredVertex[] kmpClusterResult = null;
            IClusterSolver kmCluster = new KmeansCluster(geometry, K);
            IClusterSolver kmppCluster = new KmeansPPCluster(geometry, K);
            task = Task.Run(async () =>
            {
                try
                {
                    //resultTest = test.CalculateClusters();
                    kmClusterResult = await kmCluster.AsyncComputeCluster();
                    kmpClusterResult = await kmppCluster.AsyncComputeCluster();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            yield return new WaitUntil(() => task.IsCompleted);
            Color[] kmColorResult = null; ;
            Color[] kmpColorResult = null; ;
            task = Task.Run(async () =>
            {
                kmColorResult = await MeshTool.AsyncReColorizeVertex(kmClusterResult);
                kmpColorResult = await MeshTool.AsyncReColorizeVertex(kmpClusterResult);
            });
            yield return new WaitUntil(() => task.IsCompleted);
            geometry.colors = kmColorResult;
            var go = geometry.GenerateGameObject("kmeans test");
            go.transform.position += new Vector3(2, 0, 0);

            geometry.colors = kmpColorResult;
            var go2 = geometry.GenerateGameObject("kmeans plus plus test");
            go2.transform.position += new Vector3(4, 0, 0);
            Debug.Log("Step 2 finish: find clusters!");
        }
    }
}