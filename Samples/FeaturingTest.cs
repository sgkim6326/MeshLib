using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MeshLib.Feature;

namespace MeshLib.Example
{
    public class FeaturingTest : MonoBehaviour
    {
        public MeshFilter GraphicModel;
        public Material VertexColor;

        IEnumerator Start()
        {
            MeshLib.GeometryInfo geometry = null;
            var mesh = GraphicModel.mesh;
            var vertices = mesh.vertices.Select(vec => GraphicModel.transform.TransformPoint(vec)).ToArray();
            var normals = mesh.normals.Select(norm => GraphicModel.transform.TransformDirection(norm)).ToArray();
            var triangles = mesh.triangles;
            Task task = Task.Run(async () => geometry = await MeshTool.AsyncConvertToGeometry(vertices, normals, triangles));
            yield return new WaitUntil(() => task.IsCompleted);

            IFeatureSolver saliency = new MeshSaliencySolver(geometry);
            Debug.Log(geometry.ToString());
            yield return new WaitUntil(() => task.IsCompleted);
            Vector3[] interestPoints = null;
            task = Task.Run(async () =>
            {
                await saliency.ComputeFeature();
                interestPoints = await saliency.GetInterestPoint();
            });
            yield return new WaitUntil(() => task.IsCompleted);

            Debug.Log(interestPoints.Length);
            var newMesh = new Mesh();
            newMesh.vertices = interestPoints;
            newMesh.SetIndices(Enumerable.Range(0, interestPoints.Length).ToArray(), MeshTopology.Points, 0);
            var go = new GameObject("MeshSaliency");
            go.AddComponent<MeshFilter>().mesh = newMesh;
            go.AddComponent<MeshRenderer>().material = VertexColor;
        }
    }
}