using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using MeshLib.Flattening;

namespace MeshLib.Example
{
    public class PCATest : MonoBehaviour
    {
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
            IExtractionSolver simplifier = new Extraction.OutsideSurfaceSolver(geometry, true);
            geometry = simplifier.Extract();
            Debug.Log(geometry);
            GraphicModel.mesh = geometry.ToMesh(true);
            yield return new WaitUntil(() => task.IsCompleted);
            IClusterSolver cluster = new Cluster.KmeansCluster(geometry, K);
            task = Task.Run(async () =>
            {
                var infos = await cluster.AsyncComputeCluster();
                var geoSegs = await MeshTool.AsyncGeometrySegmentation(geometry, infos);
                geometry = geoSegs[0];
            });
            yield return new WaitUntil(() => task.IsCompleted);
            GraphicModel.mesh = geometry.ToMesh(true);

            IFlatteningSolver flatteningSolver = new PCASolver(geometry);
            FlatResult[] result = null;
            task = Task.Run(async () => result = await flatteningSolver.AsyncCompute3Dto2D());
            yield return new WaitUntil(() => task.IsCompleted);
            result = result.Normalize();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}