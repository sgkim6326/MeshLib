using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MeshLib;

public class COATest : MonoBehaviour
{
    GeometryInfo geometry;
    public Material GUITexture;
    public Material VertexColor;

    public float thres;
    public MeshFilter src;
    IEnumerator Start()
    {
        var mesh = src.GetComponent<MeshFilter>().mesh;
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var triangles = mesh.triangles;
        Task task = Task.Run(async () => geometry = await MeshTool.AsyncConvertToGeometry(vertices, normals, triangles));
        yield return new WaitUntil(() => task.IsCompleted);
        IUpscaleSolver upscaleSolver = new MeshLib.Upscale.LBUSolver(geometry, thres);
        task = Task.Run(async () => geometry = await upscaleSolver.AsyncUpscale());
        yield return new WaitUntil(() => task.IsCompleted);
        src.gameObject.SetActive(false);
        MeshFilter dst = geometry.GenerateGameObject("Upscaled").GetComponent<MeshFilter>();
        var COA = Vector3.zero;
        for(int fi = 0; fi < geometry.faceCount; ++fi)
        {
            COA += geometry.GetFaceCenter(fi);
        }
        COA /= geometry.faceCount;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        go.localScale = Vector3.one / 10;
        Destroy(go.GetComponent<Collider>());
        var mat = new Material(GUITexture);
        mat.color = Color.red;
        go.GetComponent<MeshRenderer>().material = mat;

        var colors = new Color[geometry.vertexCount];
        var minDist = geometry.vertices.Min((v) => Vector3.Distance(v, COA)) * 1.01;
        for(int vi=0; vi< geometry.vertexCount; ++vi)
        {
            var vertex = geometry.vertices[vi];
            colors[vi] = (Vector3.Distance(vertex, COA) <= minDist) ? Color.green : Color.white;
        }

        IFeatureSolver saliency = new MeshLib.Feature.MeshSaliencySolver(geometry);
        Vector3[] interestPoints = null;
        task = Task.Run(async () =>
        {
            await saliency.ComputeFeature();
            interestPoints = await saliency.GetInterestPoint();
        });
        yield return new WaitUntil(() => task.IsCompleted);
        var newMesh = new Mesh();
        newMesh.vertices = interestPoints;
        newMesh.SetIndices(Enumerable.Range(0, interestPoints.Length).ToArray(), MeshTopology.Points, 0);
        var interestGo = new GameObject("MeshSaliency");
        interestGo.AddComponent<MeshFilter>().mesh = newMesh;
        interestGo.AddComponent<MeshRenderer>().material = VertexColor;
        //mesh = dst.mesh;
        //mesh.colors = colors;
        //dst.mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
