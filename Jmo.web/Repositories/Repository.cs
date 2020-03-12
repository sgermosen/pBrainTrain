using Jmo.Infraestructure;
using Jmo.Web.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Jmo.Web.Repositories
{
    public class Repository : IRepository
    {
        private readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public bool PreguntaExists(int id)
        {
            return _context.Preguntas.Any(p => p.Id == id);
        }

    }
}
