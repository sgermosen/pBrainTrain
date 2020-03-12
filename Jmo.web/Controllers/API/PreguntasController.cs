using Jmo.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Jmo.Web.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreguntasController : ControllerBase
    {
        private readonly IPreguntaRepository _repository;
        private readonly ICategoriaRepository _categoriaRepository;
        //private readonly IUserHelper userHelper;

        public PreguntasController(IPreguntaRepository repository, ICategoriaRepository categoriaRepository)// IUserHelper userHelper,)
        {
            _repository = repository;
            //   this.userHelper = userHelper;
            _categoriaRepository = categoriaRepository;
        }

        [HttpGet]
        public IActionResult GetPreguntas()
        {
            return this.Ok(_repository.GetPreguntas());
        }

    }
}