using Jmo.Infraestructure;
using Jmo.Web.Data.Domain;

namespace Jmo.Web.Repositories
{
    public class RespuestaRepository : GenericRepository<Respuesta>, IRespuestaRepository
    {
        private readonly ApplicationDbContext _context;
        public RespuestaRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void AddRespuesta(Respuesta respuesta)
        {
            _context.Respuestas.Add(respuesta);
        }
    }
}
