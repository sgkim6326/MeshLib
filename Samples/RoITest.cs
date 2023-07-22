using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MeshLib.Example
{
    public class RoITest : MonoBehaviour
    {
        public MeshFilter GraphicModel;
        public double Y_Max = 1.630548;
        public double Y_Min = 0.4156875;
        public double Y_Offset = 0;
        IEnumerator Start()
        {
            var vertices = GraphicModel.mesh.vertices;
            var normals = GraphicModel.mesh.normals;
            var triangles = GraphicModel.mesh.triangles;
            GeometryInfo geometry = null;
            Task task = Task.Run(async () =>
            {
                try
                {
                    geometry = await MeshTool.AsyncConvertToGeometry(vertices, normals, triangles);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            yield return new WaitUntil(() => task.IsCompleted);
            geometry.GenerateGameObject("test");
            IExtractionSolver roiSolver = new Extraction.RoISolver(geometry, (Y_Min + Y_Offset, Y_Max + Y_Offset), Extraction.RoISolver.Type.Y_Axis);
            task = Task.Run(async () =>
            {
                try
                {
                    geometry = await roiSolver.AsyncExtract();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            yield return new WaitUntil(() => task.IsCompleted);
            geometry.GenerateGameObject("roi - test");
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = new Vector3(0, (float)((Y_Max + Y_Min) / 2 + Y_Offset), 0);
            Gizmos.DrawWireCube(center, new Vector3(10, (float)((Y_Max - Y_Min)), 10));
        }
    }
}