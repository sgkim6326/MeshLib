using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib.Extraction
{
    public class OutsideSurfaceSolver : IExtractionSolver
    {
        bool debug = false;
        GeometryInfo geometry;
        public OutsideSurfaceSolver(GeometryInfo geometry, bool debug = false)
        {
            this.geometry = geometry;
            this.debug = true;
        }
        bool isIsolateFace(Ray ray, Face face)
        {
            bool isIn = false;
            var vertices = geometry.vertices;
            var plane = new Plane(vertices[face.VertexIndex0], vertices[face.VertexIndex1], vertices[face.VertexIndex2]);
            isIn = plane.Raycast(ray, out float dist);
            if (isIn == false) return false;
            var hitPoint = ray.origin + ray.direction * dist;
            return true;
        }
        void debugMsg(string msg)
        {
            if (debug) Debug.Log(msg);
        }
        public async Task<GeometryInfo> AsyncExtract()
        {
            GeometryInfo result = null;
            await Task.Run(() =>
            {
                bool[] isolatedFaceInfo = new bool[geometry.faceCount];
                List<Face> isolatedFaces = new List<Face>();
                for (int fi = 0; fi < geometry.faceCount; ++fi)
                {
                    var dir = geometry.GetFaceNormal(fi);
                    var org = geometry.GetFaceCenter(fi);
                    var ray = new Ray(org, dir);
                    for (int ki = 0; ki < geometry.faceCount; ++ki)
                    {
                        if (ki == fi) continue;
                        if (isolatedFaceInfo[ki] == true) continue;
                        if (isIsolateFace(ray, geometry.faces[ki]) == false) continue;
                        isolatedFaceInfo[ki] = true;
                        isolatedFaces.Add(geometry.faces[ki]);
                    }
                    if(fi % 1000 == 0)  debugMsg(((fi + 1f) / geometry.faceCount).ToString());
                }
                result = new GeometryInfo(geometry.vertices, isolatedFaces, geometry.normals, geometry.colors);
            });
            return result;
        }

        public GeometryInfo Extract()
        {
            var config = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            var mesh = geometry.ToMesh();
            var go = new GameObject();
            go.layer = 1 << LayerMask.GetMask("temp");
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            debugMsg("Extract outside surface");
            List <Face> faces = new List<Face>(geometry.faceCount);
            for (int fi = 0; fi < geometry.faceCount; ++fi)
            {
                bool hit = false;
                var org = go.transform.TransformPoint(geometry.GetFaceCenter(fi));
                var dir = go.transform.TransformDirection(geometry.GetFaceNormal(fi)).normalized;
                Ray ray = new Ray(org + dir * 0.01f, dir);
                hit = Physics.Raycast(ray, out RaycastHit info, LayerMask.GetMask("temp"));
                foreach(var idx in geometry.faces[fi].GetIndices())
                {
                    if (hit == true) break;
                    org = go.transform.TransformPoint(geometry.vertices[idx]);
                    ray.origin = org + dir * 0.01f;
                    hit = Physics.Raycast(ray, out info, LayerMask.GetMask("temp"));
                }
                //bool hit = Physics.SphereCast(org, maxDist, dir, out RaycastHit info, 10f, LayerMask.GetMask("temp"));
                //bool hit = Physics.SphereCast(ray, maxDist, out RaycastHit info, float.MaxValue, LayerMask.GetMask("temp"));
                //Debug.DrawRay(ray.origin, ray.direction, Color.green, 10f);
                //bool hit = Physics.Raycast(ray, out RaycastHit info, LayerMask.GetMask("temp"));
                if (hit == true) continue;
                faces.Add(geometry.faces[fi]);
            }
            UnityEngine.Object.Destroy(go);
            debugMsg("Extraction finish");
            Physics.queriesHitBackfaces = config;
            return new GeometryInfo(geometry.vertices, faces, geometry.normals, geometry.colors);
        }
    }
}