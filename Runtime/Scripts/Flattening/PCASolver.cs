using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;

namespace MeshLib.Flattening
{
    public class PCASolver : IFlatteningSolver
    {
        private class CoordinateSystem
        {
            public readonly Quaternion orientation;
            public readonly Vector3 origin;
            public CoordinateSystem(Vector3 origin, Vector3 forward, Vector3 up)
            {
                this.origin = origin;
                this.orientation = Quaternion.LookRotation(forward, up);
            }
            public Vector3 TransformVector(Vector3 vector)
            {
                return orientation * vector;
            }
            public Vector3 TransformPoint(Vector3 point)
            {
                return origin + TransformVector(point);
            }
            public Vector3 InverseTransformVector(Vector3 vector)
            {
                // Quaternion inverse is cheaper than a matrix inverse: just 3 negations.
                // The compiler may be able to combine this with the multiplication below.
                var inverse = Quaternion.Inverse(orientation);
                return inverse * vector;
            }
            public Vector3 InverseTransformPoint(Vector3 point)
            {
                return InverseTransformVector(point - origin);
            }
        }
        private class PCAResult
        {
            public Vector3 center;
            public Vector3 axis_x;
            public Vector3 axis_y;
            public Vector3 axis_z;
            public override string ToString()
            {
                return string.Format("Result\n" +
                    "Center: ({0},{1},{2})\n" +
                    "Axis_0: ({3},{4},{5})\n" +
                    "Axis_1: ({6},{7},{8})\n" +
                    "Axis_2: ({9},{10},{11})",
                    center.x, center.y, center.z,
                    axis_x.x, axis_x.y, axis_x.z,
                    axis_y.x, axis_y.y, axis_y.z,
                    axis_z.x, axis_z.y, axis_z.z);
            }
        }
        public GeometryInfo geometry;
        public PCASolver(GeometryInfo geometry)
        {
            this.geometry = geometry;
        }
        public async Task<FlatResult[]> AsyncCompute3Dto2D()
        {
            FlatResult[] results = new FlatResult[geometry.faceCount];
            await Task.Run(() =>
            {
                Matrix<double> matrix = DenseMatrix.Create(geometry.vertexCount, 3, 0);
                for (int i = 0; i < geometry.vertexCount; ++i)
                {
                    matrix[i, 0] = geometry.vertices[i].x;
                    matrix[i, 1] = geometry.vertices[i].y;
                    matrix[i, 2] = geometry.vertices[i].z;
                }
                var cov = ComputeCovarianceMatrix(matrix);
                var pca = ComputePCA(cov);
                var projected_points = ProjectToPlane(pca);
                var reduced_points = ReduceDimension(pca, projected_points);
                for (int fi = 0; fi < geometry.faceCount; ++fi)
                {
                    var face = geometry.faces[fi];
                    var indices = face.GetIndices();
                    var vertex = new Vector3[3];
                    for (var idx = 0; idx < 3; ++idx)
                    {
                        vertex[idx] = reduced_points[indices[idx]];
                    }
                    results[fi] = new FlatResult(fi, vertex[0], vertex[1], vertex[2]);
                }
            });
            return results;
        }
        private Vector2[] ReduceDimension(PCAResult result, Vector3[] points)
        {
            var coordinate = new CoordinateSystem(result.center, result.axis_x, result.axis_z);
            var reduced_vertices = new Vector2[geometry.vertexCount];
            for (int vi = 0; vi < geometry.vertexCount; ++vi)
            {
                var point = coordinate.InverseTransformPoint(points[vi]);
                reduced_vertices[vi] = new Vector2(point.x, point.z);
            }
            return reduced_vertices;
        }
        private Vector3[] ProjectToPlane(PCAResult result)
        {
            var plane = new Plane(result.axis_z, result.center);
            var projected_vertices = new Vector3[geometry.vertexCount];
            for (int vi = 0; vi < geometry.vertexCount; ++vi)
            {
                projected_vertices[vi] = plane.ClosestPointOnPlane(geometry.vertices[vi]);
            }
            return projected_vertices;
        }
        //https://gist.github.com/jojonki/88bfd8130eec0552841b5db9550bda5a
        private Matrix<double> ComputeCovarianceMatrix(Matrix<double> matrix)
        {
            var len = matrix.RowCount;
            var dim = matrix.ColumnCount;
            var avg = matrix.ColumnSums() / len;
            var cov = DenseMatrix.Create(dim, dim, 0);
            for (int r = 0; r < dim; ++r)
            {
                for (int c = 0; c < dim; ++c)
                {
                    double element = 0;
                    for (int idx = 0; idx < len; ++idx)
                    {
                        var vector = matrix.Row(idx);
                        element += (vector[r] - avg[r]) * (vector[c] - avg[c]);
                    }
                    if (len != 1) element /= (len - 1);
                    cov[r, c] = element;
                }
            }
            return cov;
        }
        private PCAResult ComputePCA(Matrix<double> covariance_matrix)
        {
            var eigens = covariance_matrix.Evd();
            var eigenVectors = eigens.EigenVectors.EnumerateColumns();
            var eigenValues = eigens.EigenValues.Enumerate();
            var sortedAxis = eigenVectors.Zip(eigenValues, (vector, value) => new Vector3((float)vector[0], (float)vector[1], (float)vector[2]) * (float)value.Real).OrderBy(vec => vec.sqrMagnitude).ToArray();
            PCAResult result = new PCAResult();
            result.center = geometry.vertices.Aggregate(Vector3.zero, (a, b) => a + b) / geometry.vertexCount;
            result.axis_z = sortedAxis[0];
            result.axis_y = sortedAxis[1];
            result.axis_x = sortedAxis[2];
            return result;
        }
        public Vector3 GetFacePosition(FlatResult result)
        {
            throw new NotImplementedException();
        }
    }
}
