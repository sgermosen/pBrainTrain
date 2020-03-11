using Jmo.Web.Data.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jmo.Web.Repositories
{
    public interface IRepository
    { 
        Task<bool> SaveAllAsync();
         

    }
}