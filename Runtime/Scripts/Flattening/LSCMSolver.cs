//#define SAVE
//#define DEBUG_MATRIX
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Providers.SparseSolver;
using DLA = MathNet.Numerics.LinearAlgebra.Double;
using CLA = MathNet.Numerics.LinearAlgebra.Complex;
using MeshLib.Utility;
using Complex = System.Numerics.Complex;

namespace MeshLib.Flattening
{
    public class LSCMSolver : IFlatteningSolver
    {
        
        public GeometryInfo geometry = null;
        public int[] constraints;

        //MeshIO::read
        public LSCMSolver(GeometryInfo geometry, int[] constraint = null)
        {
            this.geometry = geometry;
            this.constraints = constraint;
        }
        public Vector3 GetFacePosition(FlatResult result)
        {
            var face = geometry.faces[result.faceID];
            var p0 = geometry.vertices[face.VertexIndex0];
            var p1 = geometry.vertices[face.VertexIndex1];
            var p2 = geometry.vertices[face.VertexIndex2];
            return (p0 + p1 + p2) / 3;
        }
        public async Task<FlatResult[]> AsyncCompute3Dto2D()
        {
            try
            {
                var verticesMatrixs = await InitParameters();
                var matrix = await ComputeSparseComplexMatrix(verticesMatrixs);
                var lscmResult = await ComputeLSCM(matrix);
                Debug.Log("LSCM: generate converted lscm result");
                var x = lscmResult.Real().Select(xx => (float)xx).ToArray();
                var y = lscmResult.Imaginary().Select(yy => (float)yy).ToArray();
#if SAVE
                lscmResult.Real().SaveToCsvData("x"+DateTime.Now.ToString("HH-m-s.ffffff"));
                lscmResult.Imaginary().SaveToCsvData("y" + DateTime.Now.ToString("HH-m-s.ffffff"));
#endif

#if SAVE
                ((lscmResult.Real() - x_min) / (x_max - x_min)).SaveToCsvData("x_n");
                ((lscmResult.Imaginary() - y_min) / (y_max - y_min)).SaveToCsvData("y_n");
#endif
                FlatResult[] convertedResult = new FlatResult[geometry.faces.Length];
                await Task.Run(() =>
                {
                    for (int i = 0; i < convertedResult.Length; ++i)
                    {
                        var face = geometry.faces[i];
                        var p0 = new Vector2(x[face.VertexIndex0], y[face.VertexIndex0]);
                        var p1 = new Vector2(x[face.VertexIndex1], y[face.VertexIndex1]);
                        var p2 = new Vector2(x[face.VertexIndex2], y[face.VertexIndex2]);
                        convertedResult[i] = new FlatResult(i, p0, p1, p2);
                    }
                });
                Debug.Log("LSCM: complete");
                return convertedResult;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return null;
        }
        private async Task<Matrix<double>[]> InitParameters()
        {
            if (geometry == null) throw new Exception("LSCM: Geometry가 설정되지 않았습니다");
            if (this.constraints == null)
            {
                constraints = await ComputeLongestPath();
                Debug.Log(string.Format("LSCM: pin points = {0}, {1}", constraints[0], constraints[1]));
            }
            Debug.Log("LSCM: convert to matrix");
            int numberOfTriangles = geometry.faces.Length;
            int numberOfVertices = geometry.vertices.Length;
            Matrix<double> vertex1ofFace = DLA.DenseMatrix.Create(numberOfTriangles, 3, 0);
            Matrix<double> vertex2ofFace = DLA.DenseMatrix.Create(numberOfTriangles, 3, 0);
            Matrix<double> vertex3ofFace = DLA.DenseMatrix.Create(numberOfTriangles, 3, 0);
            await Task.Run(() =>
            {
                for (int fi = 0; fi < geometry.faces.Length; ++fi)
                {
                    Vector3 vertex1 = geometry.vertices[geometry.faces[fi].VertexIndex0];
                    Vector3 vertex2 = geometry.vertices[geometry.faces[fi].VertexIndex1];
                    Vector3 vertex3 = geometry.vertices[geometry.faces[fi].VertexIndex2];
                    vertex1ofFace[fi, 0] = vertex1.x;
                    vertex1ofFace[fi, 1] = vertex1.y;
                    vertex1ofFace[fi, 2] = vertex1.z;
                    vertex2ofFace[fi, 0] = vertex2.x;
                    vertex2ofFace[fi, 1] = vertex2.y;
                    vertex2ofFace[fi, 2] = vertex2.z;
                    vertex3ofFace[fi, 0] = vertex3.x;
                    vertex3ofFace[fi, 1] = vertex3.y;
                    vertex3ofFace[fi, 2] = vertex3.z;
                }
            });
            return new Matrix<double>[] { vertex1ofFace, vertex2ofFace, vertex3ofFace };
        }
        private async Task<Vector<Complex>> ComputeLSCM(Matrix<Complex> M)
        {
            int numberOfVertices = geometry.vertices.Length;
            Vector<Complex> U = CLA.DenseVector.Create(numberOfVertices, 0);
            Debug.Log("LSCM: calculate conformal maps");
            await Task.Run(() =>
            {
#if SAVE
                M.SaveToCsvData("M");//M은 동일한 것을 확인
#endif
                Vector<Complex> col_pin_first = M.Column(constraints[0]);
                Vector<Complex> col_pin_second = M.Column(constraints[1]);
                Matrix<Complex> Mp = CLA.DenseMatrix.Build.SparseOfColumnVectors(col_pin_first, col_pin_second);
                Matrix<Complex> Mf = M.RemoveColumn(constraints[1]).RemoveColumn(constraints[0]);
                Vector<Complex> Up = Vector<Complex>.Build.DenseOfArray(new System.Numerics.Complex[2] { new System.Numerics.Complex(0, 0), new System.Numerics.Complex(0, 1) });
#if DEBUG_MATRIX
                Debug.Log(string.Format("LSCM: Matrix Mp\n{0}", Mp.ToStringTable()));
                Debug.Log(string.Format("LSCM: Matrix Mf\n{0}", Mf.ToStringTable()));
                Debug.Log(string.Format("LSCM: Matrix Mf'\n{0}", Mf.ConjugateTranspose().ToStringTable()));
                Debug.Log(string.Format("LSCM: Matrix Up\n{0}", Up.ToString()));
#endif
#if SAVE
                Mf.ConjugateTranspose().Multiply(Mf.Clone()).SaveToCsvData("A'");
                Mf.SaveToCsvData("Mf");//Mf 동일한 것을 확인
                Mp.SaveToCsvData("Mp");//Mp 동일한 것을 확인
                Up.SaveToCsvData("Up");//Up 동일한 것을 확인
#endif
                Debug.Log("LSCM: ready to solve least square method");
                Matrix<Complex> A = (Mf.ConjugateTranspose() * Mf);
                Vector<Complex> b = (-Mf.ConjugateTranspose() * Mp * Up);
#if DEBUG_MATRIX
                Debug.Log(string.Format("LSCM: Matrix A\n{0}", A.ToStringTable()));
                Debug.Log(string.Format("LSCM: Matrix b\n{0}", b.ToString()));
#endif
#if SAVE
                A.SaveToCsvData("A");//Mf 동일한 것을 확인
                b.SaveToCsvData("b");//b 동일한 것을 확인
#endif
                Debug.Log("LSCM: solve least square method");
                if (A.Enumerate().Contains(double.NaN) || b.Enumerate().Contains(double.NaN)) throw new Exception("LSCM Exception: Nan 값 발견");
                Vector<Complex> Uf = A.SolveLargeMatrix(b);
#if SAVE
                Uf.SaveToCsvData("result");
#endif
                Debug.Log(string.Format("LSCM: solve result: {0}", Uf));
                int idx = 0;
                for(int i = 0; i < numberOfVertices; ++i)
                {
                    if (i == constraints[0])
                        //lscmResult[i] = new Vector2((float)Up[0].Real, (float)Up[0].Imaginary);
                        U[i] = Up[0];
                    else if (i == constraints[1]) 
                        //lscmResult[i] = new Vector2((float)Up[1].Real, (float)Up[1].Imaginary);
                        U[i] = Up[1];
                    else
                        //lscmResult[i] = new Vector2((float)Uf[idx].Real, (float)Uf[idx++].Imaginary);
                        U[i] = Uf[idx++];
                }
                var error = (Mf.Multiply(Uf) + Mp.Multiply(Up)).Conjugate().DotProduct(Mf.Multiply(Uf) + Mp.Multiply(Up));
                Debug.Log(string.Format("LSCM: Conformal Error: {0}", error));
                //Uf.SaveToCsvData("x");//Uf 동일한 것을 확인
            });
            return U;
        }
        private async Task<int[]> ComputeLongestPath()
        {
            int numberOfTriangles = geometry.faces.Length;
            int numberOfVertices = geometry.vertices.Length;
            int[] longestPath = new int[2];
            await Task.Run(() =>
            {
                DLA.SparseMatrix edgeMatrix = DLA.SparseMatrix.Create(numberOfVertices, numberOfVertices, 0.0);
                for (int fi = 0; fi < numberOfTriangles; ++fi)
                {
                    var node_global_index_1 = geometry.faces[fi].VertexIndex0;
                    var node_global_index_2 = geometry.faces[fi].VertexIndex1;
                    var node_global_index_3 = geometry.faces[fi].VertexIndex2;

                    //정방향
                    edgeMatrix[node_global_index_1, node_global_index_2] += 1;
                    edgeMatrix[node_global_index_1, node_global_index_3] += 1;
                    edgeMatrix[node_global_index_2, node_global_index_3] += 1;

                    //역방향
                    edgeMatrix[node_global_index_2, node_global_index_1] += 1;
                    edgeMatrix[node_global_index_3, node_global_index_1] += 1;
                    edgeMatrix[node_global_index_3, node_global_index_2] += 1;
                }
                Debug.Log("LSCM: init graph diameter");
                int numberOfIndices = 0;
                foreach(var tuple in edgeMatrix.EnumerateIndexed())
                {
                    if (tuple.Item3 == 1) numberOfIndices++;
                }
                int idx = 0;
                int[] xindices = new int[numberOfIndices];
                int[] yindices = new int[numberOfIndices];
                foreach (var tuple in edgeMatrix.EnumerateIndexed())
                {
                    if (tuple.Item3 == 1)
                    {
                        xindices[idx] = tuple.Item1;
                        yindices[idx] = tuple.Item2;
                        idx++;
                    }
                }
                Matrix<double> boundaryEdgeDuplicate = DLA.DenseMatrix.Create(numberOfIndices, 2, 0);
                for (int i = 0; i < numberOfIndices; ++i)
                {
                    if (xindices[i] < yindices[i])
                    {
                        boundaryEdgeDuplicate[i, 0] = xindices[i];
                        boundaryEdgeDuplicate[i, 1] = yindices[i];
                    }
                    else
                    {
                        boundaryEdgeDuplicate[i, 0] = yindices[i];
                        boundaryEdgeDuplicate[i, 1] = xindices[i];
                    }
                }
                Debug.Log("LSCM: compute graph diameter");
                var boundaryEdge = DLA.DenseMatrix.Build.DenseOfRows(boundaryEdgeDuplicate.EnumerateRows().Distinct());
                var boundaryNodeInds = DLA.DenseVector.Build.DenseOfEnumerable(boundaryEdge.Reshape().Row(0).Enumerate().Distinct());
                idx = (int)boundaryNodeInds[0];
                Vector3 v0 = geometry.vertices[idx];
                float maximumDist = 0;
                int maximumIndex = 0;
                for (int i = 0; i < boundaryNodeInds.Count; ++i)
                {
                    int tempIdx = (int)boundaryNodeInds[i];
                    Vector3 tempV = geometry.vertices[tempIdx];
                    float dist = Vector3.SqrMagnitude(tempV - v0);
                    if (dist > maximumDist)
                    {
                        maximumDist = dist;
                        maximumIndex = tempIdx;
                    }
                }
                longestPath[0] = idx;
                longestPath[1] = maximumIndex;
            });
            return longestPath;
        }
        private async Task<Matrix<Complex>> ComputeSparseComplexMatrix(Matrix<double>[] verticesMatrixs)
        {
            Debug.Log("LSCM: calculate sparse complex matrix");
            int numberOfVertices = geometry.vertices.Length;
            int numberOfTriangles = geometry.faces.Length;
            Matrix<double> vertex1ofFace = verticesMatrixs[0];
            Matrix<double> vertex2ofFace = verticesMatrixs[1];
            Matrix<double> vertex3ofFace = verticesMatrixs[2];
            Matrix<double> edges1 = vertex2ofFace - vertex1ofFace;
            Matrix<double> edges2 = vertex3ofFace - vertex1ofFace;
            Matrix<Complex> result = null;
            //단위 법선 계산
            await Task.Run(() =>
            {
                Matrix<double> normalOfFaces = edges1.Cross(edges2);
                Vector<double> norm_fn = normalOfFaces.PointwiseAbs().PointwisePower(2).RowSums().PointwiseSqrt();
                normalOfFaces = normalOfFaces.DivideByVector(norm_fn);

                //로컬 기저 계산
                Matrix<double> x_ = edges1.DivideByVector(DLA.Vector.Sqrt(DLA.Matrix.Abs(edges1).PointwisePower(2).RowSums()));
                Matrix<double> y_ = normalOfFaces.Cross(x_);//DLA.DenseMatrix.Create(numberOfTriangles, 3, 0);
                
                //좌표 생성
                Vector<Complex> x1 = CLA.DenseVector.Create(numberOfTriangles, 0);
                Vector<Complex> y1 = CLA.DenseVector.Create(numberOfTriangles, 0);
                Vector<Complex> x2 = DLA.Vector.Sqrt(DLA.Matrix.Abs(edges1).PointwisePower(2).RowSums()).ToComplex();
                Vector<Complex> y2 = CLA.DenseVector.Create(numberOfTriangles, 0);
                Vector<Complex> x3 = edges2.ToComplex().Dot(x_.ToComplex(), true);
                Vector<Complex> y3 = edges2.ToComplex().Dot(y_.ToComplex(), true);
                
                //방향 영역
                //(x1 .* y2 - y1 .* x2)은 0이기에 생략
                //y2 .* x3 역시 0
                //(x3 .* y1 - y3 .* x1) 역시 0
                //따라서 남은 값은 x2 .* y3만 존재
                Vector<Complex> dT = CLA.Vector.Sqrt(x2.PointwiseMultiply(y3));
                //dT.SaveToCsvData("dT");

                //희소 행렬 트리플 생성
                Matrix<double> I = DLA.DenseMatrix.Create(numberOfTriangles, 3, 0);
                Matrix<double> J = DLA.DenseMatrix.Create(numberOfTriangles, 3, 0);
                Matrix<Complex> K = CLA.DenseMatrix.Create(numberOfTriangles, 3, 0);
                Vector<Complex> k1 = (x3 - x2).PointwiseDivide(dT) + ((y3 - y2) * Complex.ImaginaryOne).PointwiseDivide(dT);
                Vector<Complex> k2 = (x1 - x3).PointwiseDivide(dT) + ((y1 - y3) * Complex.ImaginaryOne).PointwiseDivide(dT);
                Vector<Complex> k3 = (x2 - x1).PointwiseDivide(dT) + ((y2 - y1) * Complex.ImaginaryOne).PointwiseDivide(dT);
                for (int fi = 0; fi < numberOfTriangles; ++fi)
                {
                    I[fi, 0] = fi;
                    I[fi, 1] = fi;
                    I[fi, 2] = fi;
                    J[fi, 0] = geometry.faces[fi].VertexIndex0;
                    J[fi, 1] = geometry.faces[fi].VertexIndex1;
                    J[fi, 2] = geometry.faces[fi].VertexIndex2;
                    K[fi, 0] = k1[fi];
                    K[fi, 1] = k2[fi];
                    K[fi, 2] = k3[fi];
                }
#if SAVE
                K.SaveToCsvData("K_pre");
#endif
                I = I.Reshape(1);
                J = J.Reshape(1);
                K = K.ConjugateTranspose().Transpose().Reshape(1);
#if SAVE
                I.SaveToCsvData("I");
                J.SaveToCsvData("J");
                K.Row(0).SaveToCsvData("K");
#endif
                var I_array = I.Row(0).Select(e => (int)e).ToArray();
                var J_array = J.Row(0).Select(e => (int)e).ToArray();
                var K_array = K.Row(0).ToArray();

                result = CLA.Matrix.Build.SparseFromCoordinateFormat(numberOfTriangles,numberOfVertices, K_array.Length, I_array, J_array, K_array);
            });
            if (result == null) throw (new Exception("LSCM: Matrix 계산 실패"));
            return result;
        }
    }
}