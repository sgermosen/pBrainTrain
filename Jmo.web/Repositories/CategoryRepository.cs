using Jmo.Infraestructure;
using Jmo.Web.Data.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Jmo.Web.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public IEnumerable<Category> GetCategories()
        {
            return _context.Categories.OrderBy(p => p.Name);
        }
    }
}
