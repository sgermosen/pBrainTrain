using Jmo.Infraestructure;
using Jmo.Web.Data.Domain;

namespace Jmo.Web.Repositories
{
    public class ChoiseRepository : GenericRepository<Choise>, IChoiseRepository
    {
        private readonly ApplicationDbContext _context;
        public ChoiseRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void AddChoise(Choise Choise)
        {
            _context.Choises.Add(Choise);
        }
    }
}
