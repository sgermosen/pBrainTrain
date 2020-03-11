using System.Collections.Generic;
using Jmo.Backend.Data.Domain;

namespace Jmo.Backend.Repositories
{
    public interface ICategoriaRepository : IGenericRepository<Categoria>
    {
        IEnumerable<Categoria> GetCategorias();
    }
}
