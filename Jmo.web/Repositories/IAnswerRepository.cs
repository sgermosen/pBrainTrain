using Jmo.Web.Data.Domain;

namespace Jmo.Web.Repositories
{
    public interface IChoiseRepository : IGenericRepository<Choise>
    {
        void AddChoise(Choise Choise);
    }
}
