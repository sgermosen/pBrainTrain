using Jmo.Infraestructure;
using Jmo.Web.Data.Domain;

namespace Jmo.Web.Repositories
{
    public class AnswerRepository : GenericRepository<Answer>, IAnswerRepository
    {
        private readonly ApplicationDbContext _context;
        public AnswerRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void AddAnswer(Answer answer)
        {
            _context.Answers.Add(answer);
        }
    }
}
