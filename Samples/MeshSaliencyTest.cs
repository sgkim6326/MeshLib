using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MeshLib.Feature;
using MeshLib.Extraction;
using MeshLib.Cluster;

namespace MeshLib.Example
{
    public class MeshSaliencyTest : MonoBehaviour
    {
        public MeshFilter GraphicModel;
        List<Vector3> interestPointInfo = null;
        public IEnumerator Start()
        {
            interestPointInfo = new List<Vector3>();
            MeshLib.GeometryInfo geometry = null;
            var mesh = GraphicModel.mesh;
            var vertices = mesh.vertices.Select(vec => GraphicModel.transform.TransformPoint(vec)).ToArray();
            var normals = mesh.normals.Select(norm => GraphicModel.transform.TransformDirection(norm)).ToArray();
            var triangles = mesh.triangles;
            Task task = Task.Run(async () => geometry = await MeshTool.AsyncConvertToGeometry(vertices, normals, triangles));
            yield return new WaitUntil(() => task.IsCompleted);
            var ε = geometry.bounds.size.magnitude / 100;
            Debug.Log("앱실론 계산 완료");
            IUpscaleSolver upscaler = new Upscale.LBUSolver(geometry, ε);
            task = Task.Run(async () => geometry = await upscaler.AsyncUpscale());
            yield return new WaitUntil(() => task.IsCompleted);
            Debug.Log("업스케일 완료");
            yield return new WaitForEndOfFrame();
            IExtractionSolver OutsurfExtractor = new OutsideSurfaceSolver(geometry);
            geometry = OutsurfExtractor.Extract();
            geometry.GenerateGameObject("Upscaled OutSurf");
            MeshSaliencySolver saliency = new MeshSaliencySolver(geometry);
            Debug.Log(geometry.ToString());
            yield return new WaitUntil(() => task.IsCompleted);
            task = Task.Run(async () =>
            {
                await saliency.ComputeFeature();
                interestPointInfo.AddRange(await saliency.GetInterestPoint());
            });
            //IClusterSolver cluster = new KmeansCluster(geometry, 4);
            //GeometryInfo[] geos = null;
            //task = Task.Run(async () =>
            //{
            //    try
            //    {
            //        var clusterResult = await cluster.AsyncComputeCluster();
            //        geos = await MeshTool.AsyncGeometrySegmentation(geometry, clusterResult, roi_plane);
            //        foreach (var geo in geos)
            //        {
            //            MeshSaliencySolver saliency = new MeshSaliencySolver(geo);
            //            await saliency.ComputeFeature();
            //            interestPointInfo.AddRange(await saliency.GetInterestPoint());
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        Debug.LogException(e);
            //    }
            //});
            yield return new WaitUntil(() => task.IsCompleted);
        }
        private void OnDrawGizmos()
        {
            if (interestPointInfo == null) return;
            Gizmos.color = Color.red;
            float length = interestPointInfo.Count;
            for (int i = 0; i < length; ++i)
            {
                var vec = interestPointInfo[i];
                if (i < length / 4) Gizmos.color = Color.blue;
                else if (i < length / 2) Gizmos.color = Color.green;
                else if (i < length / 4 * 3) Gizmos.color = Color.yellow;
                else Gizmos.color = Color.red;
                Gizmos.DrawSphere(vec, 0.005f);
            }
        }
    }
}