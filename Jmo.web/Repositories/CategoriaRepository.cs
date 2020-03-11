using Jmo.Web.Data;
using Jmo.Web.Data.Domain;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Jmo.Web.Repositories
{
    public class CategoriaRepository : GenericRepository<Categoria>, ICategoriaRepository
    {
        private readonly DataContext _context;

        public CategoriaRepository(DataContext context) : base(context)
        {
            _context = context;
        }
        public IEnumerable<Categoria> GetCategorias()
        {
            return _context.Categorias.OrderBy(p => p.Nombre);
        }


    }
}
