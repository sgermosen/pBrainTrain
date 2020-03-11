using Jmo.Web.Data.Domain;

namespace Jmo.Web.Repositories
{
    public interface IRespuestaRepository : IGenericRepository<Respuesta>
    {
        void AddRespuesta(Respuesta respuesta);
    }
}
