using System.Threading.Tasks;

namespace Jmo.Web.Repositories
{
    public interface IRepository
    {
        Task<bool> SaveAllAsync();
    }
}