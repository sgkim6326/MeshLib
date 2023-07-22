using System.Threading.Tasks;

namespace MeshLib
{
    public interface IUpscaleSolver
    {
        Task<GeometryInfo> AsyncUpscale();
    }
}