using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MeshLib.Simplification;
using System;

namespace MeshLib.Example
{
    public class QEMTest : MonoBehaviour
    {
        public MeshFilter GraphicModel;
        public int TriangleCount;
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

            IExtractionSolver OutsurfExtractor = new MeshLib.Extraction.OutsideSurfaceSolver(geometry);
            GeometryInfo outsurf = OutsurfExtractor.Extract();
            outsurf.GenerateGameObject("outsurf");
            ISimplificationSolver simplifier;// = new MeshLib.Simplification.QEMSolverUsingG3(outsurf, TriangleCount);
            GeometryInfo simplifiedTarget = null;
            //task = Task.Run(async () =>
            //{
            //    try
            //    {
            //        simplifiedTarget = await simplifier.AsyncSimplify();
            //    }
            //    catch (Exception e)
            //    {
            //        Debug.LogException(e);
            //    }
            //});
            //yield return new WaitUntil(() => task.IsCompleted);
            //simplifiedTarget.GenerateGameObject("test by g3");
            simplifier = new MeshLib.Simplification.QEMSolver(outsurf, TriangleCount);
            task = Task.Run(async () => simplifiedTarget = await simplifier.AsyncSimplify());
            yield return new WaitUntil(() => task.IsCompleted);
            simplifiedTarget.GenerateGameObject("test by my lib");
        }
    }
}