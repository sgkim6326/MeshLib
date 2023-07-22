using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib
{
    public interface IClusterSolver
    {
        Task<Cluster.ClusteredVertex[]> AsyncComputeCluster();
    }
}
