using Jmo.Web.Data.Domain;
using System.Collections.Generic;

namespace Jmo.Web.Repositories
{
    public interface IQuestionRepository : IGenericRepository<Question>
    {
        IEnumerable<Question> GetQuestionsFromCategory(int id);

        IEnumerable<Question> GetQuestions();

        Question GetQuestion(int id);

        void AddQuestion(Question question);
        void AddQuestion(Question question, List<Answer> answers);
        void UpdateQuestion(Question question);

        void RemoveQuestion(Question question);
    }
}
