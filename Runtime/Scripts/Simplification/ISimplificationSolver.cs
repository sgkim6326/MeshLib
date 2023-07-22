using System.Threading.Tasks;

namespace MeshLib
{
    public interface ISimplificationSolver
    {
        Task<GeometryInfo> AsyncSimplify();
    }
}
