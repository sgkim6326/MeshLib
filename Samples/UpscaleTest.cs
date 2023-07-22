using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;

namespace MeshLib.Example
{
    public class UpscaleTest : MonoBehaviour
    {
        public MeshFilter GraphicModel;
        public double incircleRaidus;
        public double lineThres;
        IEnumerator Start()
        {
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
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            yield return new WaitUntil(() => task.IsCompleted);
            geometry.GenerateGameObject("test");
            IUpscaleSolver inCircleUpscaler = new Upscale.IncircleUpscaleSolver(geometry, incircleRaidus);
            IUpscaleSolver LBUpscaler = new Upscale.LBUSolver(geometry, lineThres);
            GeometryInfo geometry1 = null, geometry2 = null;
            task = Task.Run(async () =>
            {
                try
                {
                    geometry1 = await inCircleUpscaler.AsyncUpscale();
                    geometry2 = await LBUpscaler.AsyncUpscale();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            yield return new WaitUntil(() => task.IsCompleted);
            geometry1.GenerateGameObject("test1", true);
            geometry2.GenerateGameObject("test2", true);
        }
    }
}