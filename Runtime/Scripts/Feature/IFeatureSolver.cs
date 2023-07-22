using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib
{
    public interface IFeatureSolver
    {
        Task ComputeFeature();
        Task<Vector3[]> GetInterestPoint();
        Task<bool[]> GetInterestPointInfo();
    }
}
