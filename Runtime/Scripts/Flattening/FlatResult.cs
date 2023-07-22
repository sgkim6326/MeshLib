using System;
using System.Linq;
using UnityEngine;

namespace MeshLib.Flattening
{
    public class FlatResult
    {
        public int faceID = -1;
        public Vector3 vertex01Position { get; protected set; } = Vector2.zero;
        public Vector3 vertex02Position { get; protected set; } = Vector2.zero;
        public Vector3 vertex03Position { get; protected set; } = Vector2.zero;
        public Vector3 center { get; protected set; } = Vector2.zero;
        public FlatResult(int fi, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            this.faceID = fi;
            this.vertex01Position = p0;
            this.vertex02Position = p1;
            this.vertex03Position = p2;
            center = (p0 + p1 + p2) / 3;
        }
        public double area
        {
            get
            {
                var first = vertex01Position.x * vertex02Position.y + vertex02Position.x * vertex03Position.y + vertex03Position.x * vertex01Position.y;
                var second = vertex01Position.y * vertex02Position.x + vertex02Position.y * vertex03Position.x + vertex03Position.y * vertex01Position.x;
                return Math.Abs(first - second) / 2;
            }
        }
        public override string ToString()
        {
            return string.Format("ID: {0}, p0: ({1:0.00},{2:0.00}), p1: ({3:0.00},{4:0.00}), p2: ({5:0.00},{6:0.00})", faceID, vertex01Position.x, vertex01Position.y, vertex02Position.x, vertex02Position.y, vertex03Position.x, vertex03Position.y);
        }
    }
    public static class FlatteningExtension
    {
        public static FlatResult[] RemoveOutlier(this FlatResult[] results)
        {
            var areas = results.Select(result => result.area).OrderBy(area => area).ToArray();
            var mean = areas.Average();
            double std = 0;
            double sum = areas.Sum(d => Math.Pow(d - mean, 2));
            std = Math.Sqrt((sum) / areas.Count());
            var threshold = 7;
            //var q1 = areas[areas.Length / 4];
            //var q3 = areas[areas.Length * 3 / 4];
            //var iqr = q3 - q1;
            //var weighted_iqr = iqr * 3;
            //var min = q1 - weighted_iqr;
            //var max = q3 + weighted_iqr;
            //var removed = results.Where(result => result.area >= min && result.area <= max).ToArray();
            var removed = results.Where(result => (result.area / std) <= threshold).ToArray();
            if (removed.Length == results.Length) return results;
            Debug.Log(string.Format("이상치 {0}개 제거", results.Length - removed.Length));
            return removed;
        }
        public static FlatResult[] Normalize(this FlatResult[] results)
        {
            FlatResult[] normalized = new FlatResult[results.Length];
            var x_max = float.MinValue;
            var x_min = float.MaxValue;
            var y_max = float.MinValue;
            var y_min = float.MaxValue;
            foreach (var result in results)
            {
                var vertices = new Vector2[3]
                {
                    result.vertex01Position, result.vertex02Position, result.vertex03Position
                };
                foreach (var vertex in vertices)
                {
                    if (vertex.x > x_max) x_max = vertex.x;
                    if (vertex.x < x_min) x_min = vertex.x;
                    if (vertex.y > y_max) y_max = vertex.y;
                    if (vertex.y < y_min) y_min = vertex.y;
                }
            }
            for (int vi = 0; vi < results.Length; ++vi)
            {
                var vertices = new Vector2[3]
                {
                    results[vi].vertex01Position, results[vi].vertex02Position, results[vi].vertex03Position
                };
                for (int idx = 0; idx < 3; ++idx)
                {
                    var position = vertices[idx];
                    position.x = (position.x - x_min) / (x_max - x_min);
                    position.y = (position.y - y_min) / (y_max - y_min);
                    vertices[idx] = position;
                }
                normalized[vi] = new FlatResult(results[vi].faceID, vertices[0], vertices[1], vertices[2]);
            }
            return normalized;
        }
    }
}