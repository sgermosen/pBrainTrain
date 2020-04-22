using Jmo.Infraestructure;
using Jmo.Web.Data.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jmo.Web.Repositories
{
    public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
    {
        private readonly ApplicationDbContext _context;

        public QuestionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public IEnumerable<Question> GetQuestions()
        {
            return _context.Questions
                .Include(p => p.Category)
                .Include(p => p.Answers)
                .OrderBy(p => p.Questionant);
        }

        public IEnumerable<Question> GetQuestionsFromCategory(int id)
        {
            var results = _context.Questions.Where(p => p.CategoryId == id)
                .Include(p => p.Category)
                .Include(p => p.Answers);
            //.OrderBy(p => p.Questionant);
            return results.OrderBy(x => Guid.NewGuid()).ToList();
        }

        public Question GetQuestion(int id)
        {
            return _context.Questions.Where(p => p.Id == id)
                .Include(p => p.Category)
                .Include(p => p.Answers).FirstOrDefault(p => p.Id == id);
        }

        public void AddQuestion(Question question, List<Answer> answers)
        {
            _context.Questions.Add(question);

            foreach (var item in answers)
            {
                item.Question = question;
                _context.Answers.Add(item);
            }
        }

        public void AddQuestion(Question question)
        {
            _context.Questions.Add(question);
        }

        public void UpdateQuestion(Question question)
        {
            _context.Update(question);
        }

        public void RemoveQuestion(Question question)
        {
            _context.Questions.Remove(question);
        }
    }
}
