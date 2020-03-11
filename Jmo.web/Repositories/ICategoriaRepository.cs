using System.Collections.Generic;
using Jmo.Web.Data.Domain;

namespace Jmo.Web.Repositories
{
    public interface ICategoriaRepository : IGenericRepository<Categoria>
    {
        IEnumerable<Categoria> GetCategorias();
    }
}
