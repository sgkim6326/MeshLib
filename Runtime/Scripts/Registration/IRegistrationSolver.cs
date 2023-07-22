using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using System.Threading.Tasks;

namespace MeshLib
{
    public interface IRegistrationSolver
    {
        void TranslateAndRotate(Transform Target);
        void ComputeRegistration();
        Task AsyncComputeRegistration();
    }
}
