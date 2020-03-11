using Jmo.Backend.Data;
using Jmo.Backend.Data.Domain;

namespace Jmo.Backend.Repositories
{
    public class RespuestaRepository : GenericRepository<Respuesta>, IRespuestaRepository
    {
        private readonly DataContext _context;
        public RespuestaRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public void AddRespuesta(Respuesta respuesta)
        {
            _context.Respuestas.Add(respuesta);
        }
    }
}
