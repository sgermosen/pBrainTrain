using Jmo.Common.Models;
using System.Threading.Tasks;

namespace Jmo.Common.Services
{
    public interface IApiService
    {
        Task<Response> GetListAsync<T>(string urlBase, string servicePrefix, string controller);

        Task<Response> GetListAsync<T>(string urlBase, string servicePrefix, string controller, int id);
    }

}
