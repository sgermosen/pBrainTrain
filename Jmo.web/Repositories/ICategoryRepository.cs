using Jmo.Web.Data.Domain;
using System.Collections.Generic;

namespace Jmo.Web.Repositories
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        IEnumerable<Category> GetCategories();
    }
}
