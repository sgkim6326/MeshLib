using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using DLA = MathNet.Numerics.LinearAlgebra.Double;
using MeshLib.Utility;
using UnityEngine;

namespace MeshLib.Registration
{
    public class ICPSolver : IRegistrationSolver
    {
        int iterateCount;
        double threshold;
        Pose pose;
        int length = 0;
        double scaleRatio = 1;
        Matrix<double> source;
        Matrix<double> target;
        Vector<double> cntsrc;
        Vector<double> cnttrg;
        Vector<double> first_cnt_trg;

        public ICPSolver(Vector3[] source, Vector3[] target, int max, double threshold)
        {
            if (source.Length != target.Length) throw new Exception("ICP: source와 target의 크기가 다릅니다");
            this.iterateCount = max;
            this.threshold = threshold;
            length = source.Length;
            this.source = DLA.DenseMatrix.Create(length, 3, 0);
            this.target = DLA.DenseMatrix.Create(length, 3, 0);
            for (int i = 0; i < length; ++i)
            {
                this.source[i, 0] = source[i].x;
                this.source[i, 1] = source[i].y;
                this.source[i, 2] = source[i].z;
                this.target[i, 0] = target[i].x;
                this.target[i, 1] = target[i].y;
                this.target[i, 2] = target[i].z;
            }
            first_cnt_trg = this.target.ColumnSums() / length;
            cntsrc = this.source.ColumnSums() / length;
            cnttrg = this.target.ColumnSums() / length;
            double scalesrc = this.source.EnumerateRows().Average(row => (row - cntsrc).L2Norm());
            double scaletrg = this.target.EnumerateRows().Average(row => (row - cnttrg).L2Norm());
            scaleRatio = scalesrc / scaletrg;
        }
        public void ComputeRegistration()
        {
            pose = new Pose(Vector3.zero, Quaternion.identity);
            cnttrg = target.ColumnSums() / length;
            target = Matrix<double>.Build.DenseOfColumns(new Vector<double>[3] { target.Column(0) - cnttrg[0], target.Column(1) - cnttrg[1], target.Column(2) - cnttrg[2] });
            target *= DLA.DenseMatrix.CreateIdentity(3) * scaleRatio;
            target = Matrix<double>.Build.DenseOfColumns(new Vector<double>[3] { target.Column(0) + cnttrg[0], target.Column(1) + cnttrg[1], target.Column(2) + cnttrg[2] });
            for (int index = 0; index < iterateCount; ++index)
            {
                var (rotation, translate) = FindBestFit();
                target = Matrix<double>.Build.DenseOfColumns(new Vector<double>[3] { target.Column(0) - cnttrg[0], target.Column(1) - cnttrg[1], target.Column(2) - cnttrg[2] });
                target = (rotation * target.Transpose()).Transpose();
                target = Matrix<double>.Build.DenseOfColumns(new Vector<double>[3] { target.Column(0) + cnttrg[0] + translate[0], target.Column(1) + cnttrg[1] + translate[1], target.Column(2) + cnttrg[2] + translate[2] });
                pose.position += new Vector3((float)translate[0], (float)translate[1], (float)translate[2]);
                Quaternion q = new Quaternion();
                q.w = (float)Math.Sqrt(Math.Max(0, 1 + rotation[0, 0] + rotation[1, 1] + rotation[2, 2])) / 2;
                q.x = (float)Math.Sqrt(Math.Max(0, 1 + rotation[0, 0] - rotation[1, 1] - rotation[2, 2])) / 2;
                q.y = (float)Math.Sqrt(Math.Max(0, 1 - rotation[0, 0] + rotation[1, 1] - rotation[2, 2])) / 2;
                q.z = (float)Math.Sqrt(Math.Max(0, 1 - rotation[0, 0] - rotation[1, 1] + rotation[2, 2])) / 2;
                q.x *= (float)Math.Sign(q.x * (rotation[2, 1] - rotation[1, 2]));
                q.y *= (float)Math.Sign(q.y * (rotation[0, 2] - rotation[2, 0]));
                q.z *= (float)Math.Sign(q.z * (rotation[1, 0] - rotation[0, 1]));
                pose.rotation *= q;
                if ((source - target).RowNorms(2).Sum() < 0.001) break;
            }
        }
        public async Task AsyncComputeRegistration() => await Task.Run(() => ComputeRegistration());
        public void TranslateAndRotate(Transform Target)
        {
            var center = new Vector3((float)first_cnt_trg[0], (float)first_cnt_trg[1], (float)first_cnt_trg[2]) + pose.position;
            Target.position += pose.position;
            pose.rotation.ToAngleAxis(out var angle, out var axis);
            Target.RotateAround(center, axis, angle);
            ScaleAround(Target, center, Target.localScale * (float)scaleRatio);
        }
        (Matrix<double>, Vector<double>) FindBestFit()
        {
            var result = new Matrix<double>[2];
            cntsrc = source.ColumnSums() / length;
            cnttrg = target.ColumnSums() / length;
            var devsrc = Matrix<double>.Build.DenseOfColumns(new Vector<double>[3] { source.Column(0) - cntsrc[0], source.Column(1) - cntsrc[1], source.Column(2) - cntsrc[2] });
            var devtrg = Matrix<double>.Build.DenseOfColumns(new Vector<double>[3] { target.Column(0) - cnttrg[0], target.Column(1) - cnttrg[1], target.Column(2) - cnttrg[2] });
            var H = target.Transpose() * devsrc;
            var svd = H.Svd();
            var Ut = svd.U.Transpose();
            var V = svd.VT.Transpose();
            var rotation = V * Ut;
            if (rotation.Determinant() < 0)
            {
                svd = rotation.Svd();
                var offset = Matrix<double>.Build.DenseOfArray(new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, -1 } });
                V = svd.VT.Transpose() * offset;
                rotation = V * Ut;
            }
            var Translate = cntsrc - rotation * cnttrg;
            return (rotation, Translate);
        }
        void ScaleAround(Transform target, Vector3 pivot, Vector3 newScale)
        {
            Vector3 A = target.localPosition;
            Vector3 B = pivot;
            Vector3 C = A - B;
            float RS = newScale.x / target.localScale.x;
            Vector3 FP = B + C * RS;
            target.localScale = newScale;
            target.localPosition = FP;
        }
    }
}