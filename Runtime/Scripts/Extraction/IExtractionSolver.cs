using System.Threading.Tasks;
using UnityEngine;

namespace MeshLib
{
    public interface IExtractionSolver
    {
        Task<GeometryInfo> AsyncExtract();
        GeometryInfo Extract();
    }
}