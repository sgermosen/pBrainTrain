using Jmo.Backend.Data.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jmo.Backend.Repositories
{
    public interface IRepository
    { 
        Task<bool> SaveAllAsync();
         

    }
}