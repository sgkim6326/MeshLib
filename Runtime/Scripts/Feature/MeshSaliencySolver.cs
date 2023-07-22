using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using MeshLib.Utility;
using DLA = MathNet.Numerics.LinearAlgebra.Double;

namespace MeshLib.Feature
{
    public class MeshSaliencySolver : IFeatureSolver
    {
        public GeometryInfo geometry;
        private double[] totalSaliency;
        public MeshSaliencySolver(GeometryInfo geometry)
        {
            this.geometry = geometry;
            totalSaliency = new double[geometry.vertices.Length];
        }
        public async Task<Vector3[]> GetInterestPoint()
        {
            if (totalSaliency.Length == 0) throw new Exception("Mesh Saliency가 계산되지 않았습니다. 먼저 계산해주세요");
            List<Vector4> result = new List<Vector4>(totalSaliency.Length);
            await Task.Run(() =>
            {
                double avgTotalSaliency = totalSaliency.Average();
                //Vector<double> temp = DLA.DenseVector.Build.DenseOfArray(totalSaliency);
                //temp.SaveToCsvData("Saliency.csv");
                //var std = totalSaliency.Sum(value => Math.Pow(value - avgTotalSaliency, 2));
                //std = Math.Sqrt(std / totalSaliency.Count());
                Debug.Log(string.Format("min: {0}, avg: {1}, max: {2}", totalSaliency.Min(), avgTotalSaliency, totalSaliency.Max()));
                for (int vi = 0; vi < totalSaliency.Length; ++vi)
                {
                    if (totalSaliency[vi] > avgTotalSaliency)
                    {
                        Vector4 value = geometry.vertices[vi];
                        value.w = (float)totalSaliency[vi];
                        result.Add(value);
                    }
                }
            });
            Debug.Log(string.Format("Saliency: complete extract"));
            //var values = result.OrderBy(val => val.w).Select(vec => new Vector3(vec.x, vec.y, vec.z)).ToArray();
            //Vector3[] firstQuartile = new Vector3[values.Length / 2];
            //Array.Copy(values, values.Length - values.Length / 2, firstQuartile, 0, firstQuartile.Length);
            //return firstQuartile;
            return result.OrderBy(val => val.w).Select(vec => new Vector3(vec.x, vec.y, vec.z)).ToArray();
        }
        public async Task<bool[]> GetInterestPointInfo()
        {
            if (totalSaliency.Length == 0) throw new Exception("Mesh Saliency가 계산되지 않았습니다. 먼저 계산해주세요");
            bool[] result = new bool[totalSaliency.Length];
            await Task.Run(() =>
            {
                double avgTotalSaliency = totalSaliency.Average();
                Debug.Log(string.Format("min: {0}, avg: {1}, max: {2}", totalSaliency.Min(), avgTotalSaliency, totalSaliency.Max()));
                for (int vi = 0; vi < totalSaliency.Length; ++vi)
                {
                    if (totalSaliency[vi] > avgTotalSaliency) result[vi] = true;
                    else result[vi] = false;
                }
            });
            Debug.Log(string.Format("Saliency: complete extract"));
            return result;
        }
        public async Task ComputeFeature()
        {
            Vector3 MIN = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 MAX = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Debug.Log("Saliency: initialize");

            Debug.Log("Saliency: calculate vertex shape operator");
            foreach (var vertex in geometry.vertices)
            {
                MAX.x = Mathf.Max(vertex.x, MAX.x);
                MAX.y = Mathf.Max(vertex.y, MAX.y);
                MAX.z = Mathf.Max(vertex.z, MAX.z);
                MIN.x = Mathf.Min(vertex.x, MIN.x);
                MIN.y = Mathf.Min(vertex.y, MIN.y);
                MIN.z = Mathf.Min(vertex.z, MIN.z);
            }
            // Calculate each vertecies' shape operator
            Matrix<double>[] shapeOperators = new Matrix<double>[geometry.vertices.Length];
            double[] vertexArea = new double[geometry.vertices.Length];
            for (int vi = 0; vi < geometry.vertices.Length; ++vi)
            {
                vertexArea[vi] = 0f;
                shapeOperators[vi] = DLA.DenseMatrix.Create(3,3,0);
            }
            Debug.Log("Saliency: calculate face area");
            for (int fi = 0; fi < geometry.faces.Length; ++fi)
            {
                // Calculate the face's area
                double faceArea = geometry.faces[fi].CalculateArea(geometry.vertices);
                int[] indices = geometry.faces[fi].GetIndices();
                for (int idx = 0; idx < indices.Length; ++idx)
                {
                    int i = indices[idx];
                    int j = indices[(idx + 1) % 3];
                    // Get vertex i and j's normal vectors.
                    Vector<double> normal_i = Vector<double>.Build.DenseOfArray(new double[3] { geometry.normals[i].x, geometry.normals[i].y, geometry.normals[i].z });
                    Vector<double> normal_j = Vector<double>.Build.DenseOfArray(new double[3] { geometry.normals[j].x, geometry.normals[j].y, geometry.normals[j].z });
                    Vector<double> vertex_i = Vector<double>.Build.DenseOfArray(new double[3] { geometry.vertices[i].x, geometry.vertices[i].y, geometry.vertices[i].z });
                    Vector<double> vertex_j = Vector<double>.Build.DenseOfArray(new double[3] { geometry.vertices[j].x, geometry.vertices[j].y, geometry.vertices[j].z });

                    // For vertex i, update the relative part of its shape operator
                    var Tij = ((DLA.DenseMatrix.CreateIdentity(3) - normal_i.WedgeProduct(normal_i)) * (vertex_i - vertex_j)).Normalize(2);
                    double kappa_ij = 2 * (normal_i.DotProduct(vertex_j - vertex_i)) / (vertex_i - vertex_j).SqrDist();
                    // Maintain vi's shape operator
                    shapeOperators[i] = shapeOperators[i] + Tij.WedgeProduct(Tij) * (kappa_ij * faceArea);
                    vertexArea[i] += faceArea;

                    // For vertex j, update the relative part of its shape operator
                    var Tji = ((DLA.DenseMatrix.CreateIdentity(3) - normal_j.WedgeProduct(normal_j)) * (vertex_j - vertex_i)).Normalize(2);
                    double kappa_ji = 2 * (normal_j.DotProduct(vertex_i - vertex_j)) / (vertex_i - vertex_j).SqrDist();

                    // Maintain vj's shape operator
                    shapeOperators[j] = shapeOperators[j] + Tji.WedgeProduct(Tji) * (kappa_ji * faceArea);
                    vertexArea[i] += faceArea;

                }
            }
            //C(v)는 정점 v의 평균 곡률
            Debug.Log("Saliency: calculate mean curvature");
            for (int vi = 0; vi < geometry.vertices.Length; ++vi)
            {
                shapeOperators[vi] = shapeOperators[vi] * (1f / vertexArea[vi]);
            }
            vertexArea = null;
            double[] meanCurvature = new double[geometry.vertices.Length];
            for (int vi = 0; vi < geometry.vertices.Length; ++vi)
            {
                Vector<double> E1 = DLA.DenseVector.Build.DenseOfArray(new double[] { 1.0, 0.0, 0.0 });
                Vector<double> normal_vi = DLA.DenseVector.Build.DenseOfArray(new double[] { geometry.normals[vi].x, geometry.normals[vi].y, geometry.normals[vi].z });
                bool isMinus = (E1 - normal_vi).L2Norm() > (-E1 - normal_vi).L2Norm();
                // Diagnoalization by the Householder transform
                Vector<double> w_vi = (isMinus ? E1 - normal_vi : E1 + normal_vi).Normalize(2);
                Matrix<double> Q_vi = DLA.DenseMatrix.CreateIdentity(3) - (w_vi.WedgeProduct(w_vi).Transpose() * 2.0);
                Matrix<double> M_vi = Q_vi.Transpose() * shapeOperators[vi] * Q_vi;
                // Calculate the mean curvature by M_vi's trace;
                meanCurvature[vi] = M_vi[1, 1] + M_vi[2, 2];
            }
            shapeOperators = null;
            Debug.Log("Saliency: calculate multiscale saliency maps");
            //Calculate the 근접행렬(incident matrix) ( as linked list )
            double diagonalLength = (MAX - MIN).magnitude;
            double ε = 0.003 * diagonalLength;
            double[,] saliency = new double[7, geometry.vertices.Length];
            double[] maxSaliency = new double[7];
            for (int level = 2; level <= 6; ++level)
            {
                maxSaliency[level] = float.MinValue;
            }
            //우선 6ε 이내의 모든 이웃 정점들을 수집
            double[] g_numerator_1 = new double[7];
            double[] g_numerator_2 = new double[7];
            double[] g_denominator_1 = new double[7];
            double[] g_denominator_2 = new double[7];
            await MeshTool.FindNeighborVertics(geometry, 6 * ε,
                ready: (vi) =>
                {
                    for (int level = 2; level <= 6; ++level)
                    {
                        saliency[level, vi] = 0;
                        g_numerator_1[level] = g_numerator_2[level] = 0;
                        g_denominator_1[level] = g_denominator_2[level] = 0;
                    }
                },
                find: (cvi, nvi) =>
                {
                    float sqr_dist = (geometry.vertices[cvi] - geometry.vertices[nvi]).sqrMagnitude;
                    for (int level = 2; level <= 6; ++level)
                    {
                        double current_σ = level * ε;
                        double current_σ_pow = current_σ * current_σ;
                        if (sqr_dist <= current_σ_pow)
                        {
                            double factor = Math.Exp(-sqr_dist / (2 * current_σ_pow));
                            g_numerator_1[level] += meanCurvature[nvi] * factor;
                            g_denominator_1[level] += factor;
                        }
                        if (sqr_dist <= 4 * current_σ_pow)
                        {
                            double factor = Math.Exp(-sqr_dist / (8 * current_σ_pow));
                            g_numerator_2[level] += meanCurvature[nvi] * factor;
                            g_denominator_2[level] += factor;
                        }
                    }
                },
                finish: (vi) =>
                {
                    for (int level = 2; level <= 6; ++level)
                    {
                        saliency[level, vi] = Math.Abs(g_numerator_1[level] / g_denominator_1[level]
                            - g_numerator_2[level] / g_denominator_2[level]);
                        if (double.IsNaN(saliency[level, vi])) saliency[level, vi] = 0;
                        maxSaliency[level] = Math.Max(maxSaliency[level], saliency[level, vi]);
                    }
                }
            );
            Debug.Log("Saliency: sum nonlinear normalized");
            Vector<double> localMaxSaliency = DLA.DenseVector.Create(7, 0);
            await MeshTool.FindNeighborVertics(geometry, 6 * ε,
                ready: (vi) =>
                {
                    totalSaliency[vi] = 0.0f;
                    for (int level = 2; level <= 6; ++level)
                    {
                        localMaxSaliency[level] = float.MinValue;
                    }
                },
                find: (vi, nvi) =>
                {
                    for (int level = 2; level <= 6; ++level)
                        localMaxSaliency[level] = Math.Max(localMaxSaliency[level], saliency[level, nvi]);
                },
                finish: (vi) =>
                {
                    double saliencySum = 0;
                    for (int level = 2; level <= 6; level++)
                    {
                        double factor = (maxSaliency[level] - localMaxSaliency[level]) * (maxSaliency[level] - localMaxSaliency[level]);
                        totalSaliency[vi] += saliency[level, vi] * factor;
                        saliencySum += factor;
                    }
                    if (saliencySum == 0) totalSaliency[vi] = 0;
                    else totalSaliency[vi] /= saliencySum;
                }
            );
            Debug.Log(string.Format("Saliency: complete\n{0}", Vector<double>.Build.DenseOfEnumerable(totalSaliency)));
        }
    }
}