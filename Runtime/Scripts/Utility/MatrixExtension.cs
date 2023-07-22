using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;
using System;
using System.Linq;
using System.Collections.Generic;
using CLA = MathNet.Numerics.LinearAlgebra.Complex;
using Complex = System.Numerics.Complex;
using DLA = MathNet.Numerics.LinearAlgebra.Double;

namespace MeshLib.Utility
{
    public static class MatrixExtension
    {
        public static double SqrDist(this Vector<double> matrix) => matrix.L2Norm() * matrix.L2Norm();
        public static Matrix<double> WedgeProduct(this Vector<double> a, Vector<double> b) => (a.ToColumnMatrix() * b.ToRowMatrix()).Transpose();
        public static Matrix<double> MinusByVector(this Matrix<double> matrix, Vector<double> vector)
        {
            if (matrix.RowCount != vector.Count) throw new Exception("MathNet Exception: 행렬 간 나눗셈 오류: 행/열 간 일치를 발견하지 못했습니다");
            Matrix<double> result = DLA.DenseMatrix.Create(matrix.RowCount, matrix.ColumnCount, 0);
            for (int row = 0; row < matrix.RowCount; ++row)
            {
                for (int col = 0; col < matrix.ColumnCount; ++col)
                {
                    result[row, col] = matrix[row, col] / vector[row];
                }
            }
            return result;
        }
        public static Matrix<double> DivideByVector(this Matrix<double> matrix, Vector<double> vector)
        {
            if(matrix.RowCount != vector.Count) throw new Exception("MathNet Exception: 행렬 간 나눗셈 오류: 행/열 간 일치를 발견하지 못했습니다");
            Matrix<double> result = DLA.DenseMatrix.Create(matrix.RowCount, matrix.ColumnCount, 0);
            for (int row = 0; row < matrix.RowCount; ++row)
            {
                for (int col = 0; col < matrix.ColumnCount; ++col)
                {
                    result[row, col] = matrix[row, col] / vector[row];
                }
            }
            return result;
        }
        public static Matrix<double> Cross(this Matrix<double> a, Matrix<double> b)
        {
            int rowLength = a.RowCount;
            int colLength = a.ColumnCount;
            if (colLength != 3 || b.ColumnCount != 3) throw new Exception("MathNet Exception: Cross 연산 시에는 두 행렬 모두 열의 개수가 3이여야 합니다.");
            if (rowLength != b.RowCount) throw new Exception("MathNet Exception: 두 행렬 간 행의 수가 일치하지 않습니다");
            Matrix<double> matrix = DLA.DenseMatrix.Create(rowLength, colLength, 0.0);
            for(int i = 0; i < rowLength; ++i)
            {
                var vec_a = a.Row(i);
                var vec_b = b.Row(i);
                matrix[i, 0] = vec_a[1] * vec_b[2] - vec_a[2] * vec_b[1];
                matrix[i, 1] = -vec_a[0] * vec_b[2] + vec_a[2] * vec_b[0];
                matrix[i, 2] = vec_a[0] * vec_b[1] - vec_a[1] * vec_b[0];
            }
            return matrix;
        }
        public static Vector<Complex> Dot(this Matrix<Complex> a, Matrix<Complex> b, bool RowDot = false)
        {
            if (a.ColumnCount != b.ColumnCount || a.RowCount != b.RowCount) throw new Exception("MathNet Exception: 두 행렬 간 열과 행이 일치하지 않습니다");
            Vector<Complex> vector;
            if (RowDot)
            {
                vector = CLA.DenseVector.Create(a.RowCount, 0);
                for (int r = 0; r < a.RowCount; ++r)
                {
                    for (int c = 0; c < a.ColumnCount; ++c)
                    {
                        vector[r] += a[r, c] * b[r, c];
                    }
                }
                return vector;
            }
            else
            {
                vector = CLA.DenseVector.Create(a.ColumnCount, 0);
                for (int r = 0; r < a.RowCount; ++r)
                {
                    for (int c = 0; c < a.ColumnCount; ++c)
                    {
                        vector[c] += a[r, c] * b[r, c];
                    }
                }
                return vector;
            }
        }
        public static Matrix<Complex> Reshape(this Matrix<Complex> matrix, int rowSize = 1)
        {
            int totalSize = matrix.RowCount * matrix.ColumnCount;
            if (totalSize % rowSize != 0) throw new Exception("MathNet Exception: 잘못된 변환입니다.");
            int newColSize = totalSize / rowSize;
            Matrix<Complex> result = CLA.DenseMatrix.Create(rowSize, newColSize, 0);
            int r_idx = 0; int c_idx = 0;
            for(int r = 0; r < matrix.RowCount; ++r)
            {
                for(int c= 0; c< matrix.ColumnCount; ++c)
                {
                    result[r_idx, c_idx] = matrix[r, c];
                    c_idx++;
                    if(c_idx == newColSize)
                    {
                        c_idx = 0;
                        r_idx++;
                    }
                }
            }
            return result;
        }
        public static Matrix<double> Reshape(this Matrix<double> matrix, int rowSize = 1)
        {
            int totalSize = matrix.RowCount * matrix.ColumnCount;
            if (totalSize % rowSize != 0) throw new Exception("MathNet Exception: 잘못된 변환입니다.");
            int newColSize = totalSize / rowSize;
            Matrix<double> result = DLA.DenseMatrix.Create(rowSize, newColSize, 0);
            int r_idx = 0; int c_idx = 0;
            for (int r = 0; r < matrix.RowCount; ++r)
            {
                for (int c = 0; c < matrix.ColumnCount; ++c)
                {
                    result[r_idx, c_idx] = matrix[r, c];
                    c_idx++;
                    if (c_idx == newColSize)
                    {
                        c_idx = 0;
                        r_idx++;
                    }
                }
            }
            return result;
        }
        public static Matrix<Complex> AdvancedCoerceZero(this Matrix<Complex> a, double threshold)
        {
            Matrix<Complex> result = CLA.DenseMatrix.Build.Sparse(a.RowCount, a.ColumnCount, 0);
            for (int r = 0; r < a.RowCount; ++r)
            {
                for (int c = 0; c < a.ColumnCount; ++c)
                {
                    Complex value = a[r, c];
                    double real = value.Real;
                    double imag = value.Imaginary;
                    if (Math.Abs(real) < threshold) real = 0;
                    if (Math.Abs(imag) < threshold) imag = 0;
                    if (real == 0 && imag == 0) continue;
                    result[r, c] = new Complex(real, imag);
                }
            }
            return result;
        }
        public static Complex Pow(this Complex complex, double num) => Complex.Pow(complex, num);
        public static Complex Sign(this Complex complex) => new Complex(Math.Sign(complex.Real), 0);
        public static Vector<Complex> SolveLargeMatrix(this Matrix<Complex> A, Vector<Complex> b)
        {
            var QR = A.QR(MathNet.Numerics.LinearAlgebra.Factorization.QRMethod.Full);
            //var QR = MathNet.Numerics.LinearAlgebra.Complex.Factorization.SparseQR.Create(A.Conjugate() as CLA.SparseMatrix);
            return QR.Solve(b);
        }
        public static Vector<Complex> AdvancedCoerceZero(this Vector<Complex> a, double threshold)
        {
            Vector<Complex> result = CLA.DenseVector.Build.Sparse(a.Count, 0);
                for (int c = 0; c < a.Count; ++c)
                {
                    Complex value = a[c];
                    double real = value.Real;
                    double imag = value.Imaginary;
                    if (Math.Abs(real) < threshold) real = 0;
                    if (Math.Abs(imag) < threshold) imag = 0;
                    if (real == 0 && imag == 0) continue;
                    result[c] = new Complex(real, imag);
                }
            return result;
        }
        public static string ToStringTable(this Matrix<double> mat)
        {
            string data = string.Format("{0}, {1}\n", mat.RowCount, mat.ColumnCount);
            for(int r = 0; r < mat.RowCount; ++r)
            {
                for(int c = 0; c < mat.ColumnCount; ++c)
                {
                    if (mat[r, c] == 0) continue;
                    data += string.Format("({0:000}, {1:000}), {2}", r, c, mat[r, c]);
                }
            }
            return data;
        }
        public static string ToStringTable(this Matrix<Complex> mat)
        {
            List<string> colData = new List<string>();
            colData.Add(string.Format("{0} x {1}", mat.RowCount, mat.ColumnCount));
            for (int c = 0; c < mat.ColumnCount; ++c)
            {
                List<string> rowData = new List<string>();
                for (int r = 0; r < mat.RowCount; ++r)
                {
                    if (mat[r, c] == 0) continue;
                    rowData.Add(string.Format("({0:000}, {1:000}), {2} + ({3})i", r, c, mat[r, c].Real, mat[r, c].Imaginary));
                }
                colData.Add(string.Join(",", rowData));
            }
            return string.Join("\n", colData);
        }
        public static void SaveToCsvData(this Matrix<Complex> mat, string name)
        {
            List<string> colData = new List<string>();
            for (int r = 0; r < mat.RowCount; ++r)
            {
                List<string> rowData = new List<string>();
                for (int c = 0; c < mat.ColumnCount; ++c)
                {
                    try
                    {
                        var sign = Math.Sign(mat[r, c].Imaginary);
                        if (sign >= 0) rowData.Add(string.Format("{0}+{1}i", mat[r, c].Real, Math.Abs(mat[r, c].Imaginary)));
                        else rowData.Add(string.Format("{0}-{1}i", mat[r, c].Real, Math.Abs(mat[r, c].Imaginary)));
                    }
                    catch (Exception)
                    {
                        rowData.Add("Nan");
                    }
                }
                colData.Add(string.Join(",", rowData));
            }
            string data = string.Join("\n", colData);
            string folderPath = "Assets/Matrix/"; // the path of your project folder

            if (!System.IO.Directory.Exists(folderPath)) // if this path does not exist yet
                System.IO.Directory.CreateDirectory(folderPath);  // it will get created

            var screenshotName = "unity" + name + ".csv";
            System.IO.File.WriteAllText(System.IO.Path.Combine(folderPath, screenshotName), data);
            UnityEngine.Debug.Log(string.Format("Save {0} Matrix to {1}", name, screenshotName));
        }
        public static void SaveToCsvData(this Vector<Complex> vec, string name)
        {
            List<string> d = new List<string>();
            for (int r = 0; r < vec.Count; ++r)
            {
                try
                {
                    var sign = Math.Sign(vec[r].Imaginary);
                    if (sign >= 0) d.Add(string.Format("{0}+{1}i", vec[r].Real, Math.Abs(vec[r].Imaginary)));
                    else d.Add(string.Format("{0}-{1}i", vec[r].Real, Math.Abs(vec[r].Imaginary)));
                }
                catch (Exception)
                {
                    d.Add("Nan");
                }
            }
            string data = string.Join("\n", d);
            string folderPath = "Assets/Matrix/"; // the path of your project folder

            if (!System.IO.Directory.Exists(folderPath)) // if this path does not exist yet
                System.IO.Directory.CreateDirectory(folderPath);  // it will get created

            var screenshotName = "unity"+name + ".csv";
            System.IO.File.WriteAllText(System.IO.Path.Combine(folderPath, screenshotName), data);
            UnityEngine.Debug.Log(string.Format("Save {0} Matrix to {1}", name, screenshotName));
        }
        public static void SaveToCsvData(this Matrix<double> mat, string name)
        {
            List<string> colData = new List<string>();
            for (int r = 0; r < mat.RowCount; ++r)
            {
                List<string> rowData = new List<string>();
                for (int c = 0; c < mat.ColumnCount; ++c)
                {
                    try
                    {
                        rowData.Add(string.Format("{0}", mat[r, c]));
                    }
                    catch (Exception)
                    {
                        rowData.Add("Nan");
                    }
                }
                colData.Add(string.Join(",", rowData));
            }
            string data = string.Join("\n", colData);
            string folderPath = "Assets/Matrix/"; // the path of your project folder

            if (!System.IO.Directory.Exists(folderPath)) // if this path does not exist yet
                System.IO.Directory.CreateDirectory(folderPath);  // it will get created

            var screenshotName = "unity" + name + ".csv";
            System.IO.File.WriteAllText(System.IO.Path.Combine(folderPath, screenshotName), data);
            UnityEngine.Debug.Log(string.Format("Save {0} Matrix to {1}", name, screenshotName));
        }
        public static void SaveToCsvData(this Vector<double> vec, string name)
        {
            List<string> d = new List<string>();
            for (int r = 0; r < vec.Count; ++r)
            {
                 d.Add(string.Format("{0}", vec[r]));
            }
            string data = string.Join("\n", d);
            string folderPath = "Assets/Matrix/"; // the path of your project folder

            if (!System.IO.Directory.Exists(folderPath)) // if this path does not exist yet
                System.IO.Directory.CreateDirectory(folderPath);  // it will get created

            var screenshotName = "unity" + name + ".csv";
            System.IO.File.WriteAllText(System.IO.Path.Combine(folderPath, screenshotName), data);
            UnityEngine.Debug.Log(string.Format("Save {0} Matrix to {1}", name, screenshotName));
        }
    }
}
