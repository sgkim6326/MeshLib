using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MeshLib;
using MeshLib.Extraction;

namespace MeshLib.Example
{
    public class OutSurfTest : MonoBehaviour
    {
        public MeshFilter GraphicModel;
        IEnumerator Start()
        {
            var collider = GraphicModel.GetComponent<MeshCollider>();
            var vertices = GraphicModel.mesh.vertices;
            var normals = GraphicModel.mesh.normals;
            var triangles = GraphicModel.mesh.triangles;
            var colors = GraphicModel.mesh.colors;
            GeometryInfo geometry = null;
            Task task = Task.Run(async () =>
            {
                try
                {
                    geometry = await MeshTool.AsyncConvertToGeometry(vertices, normals, triangles, colors);
                    IUpscaleSolver upscaler = new Upscale.IncircleUpscaleSolver(geometry, 0.05);
                    geometry = await upscaler.AsyncUpscale();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            yield return new WaitUntil(() => task.IsCompleted);
            IExtractionSolver extractor = new OutsideSurfaceSolver(geometry, debug: true);
            geometry = extractor.Extract();
            geometry.GenerateGameObject("test", true);
            MeshLib.Simplification.QEMSolver qem = new MeshLib.Simplification.QEMSolver(geometry, 50000);
            task = Task.Run(async () => {
                try
                {
                    geometry = await qem.AsyncSimplify();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            yield return new WaitUntil(() => task.IsCompleted);
            Debug.Log(geometry);
            geometry.GenerateGameObject("test-qem", true);
        }
    }
}