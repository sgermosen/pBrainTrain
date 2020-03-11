using Jmo.Backend.Data.Domain;

namespace Jmo.Backend.Repositories
{
    public interface IRespuestaRepository : IGenericRepository<Respuesta>
    {
        void AddRespuesta(Respuesta respuesta);
    }
}
