using Jmo.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Jmo.Web.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionRepository _repository;

        public QuestionsController(IQuestionRepository repository)
        {
            _repository = repository;
        }

        //[HttpGet]
        //public IActionResult GetQuestions()
        //{
        //    return this.Ok(_repository.GetQuestions());
        //}

        [HttpGet("{id:int}")]
        public IActionResult GetQuestionsFromCategory(int id)
        {
            return this.Ok(_repository.GetQuestionsFromCategory(id));
        }

    }
}