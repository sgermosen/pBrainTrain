using Jmo.Web.Data;
using Jmo.Web.Data.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Jmo.Web.Repositories
{
    public class Repository : IRepository
    {
        private readonly DataContext _context;

        public Repository(DataContext context)
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
