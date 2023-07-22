using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib
{
    public interface IFlatteningSolver
    {
        Vector3 GetFacePosition(Flattening.FlatResult result);
        Task<Flattening.FlatResult[]> AsyncCompute3Dto2D();
    }
}
