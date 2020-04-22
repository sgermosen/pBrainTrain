using Jmo.Web.Data.Domain;

namespace Jmo.Web.Repositories
{
    public interface IAnswerRepository : IGenericRepository<Answer>
    {
        void AddAnswer(Answer answer);
    }
}
