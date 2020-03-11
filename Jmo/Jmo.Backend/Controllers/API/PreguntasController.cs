using Jmo.Backend.Helpers;
using Jmo.Backend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Jmo.Backend.Controllers.API
{
    [Route("api/[controller]")]
     [ApiController]
    public class PreguntasController : ControllerBase
    {
        private readonly IPreguntaRepository _repository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IUserHelper userHelper;

        public PreguntasController(IPreguntaRepository repository, IUserHelper userHelper, ICategoriaRepository categoriaRepository)
        {
            _repository = repository;
            this.userHelper = userHelper;
            _categoriaRepository = categoriaRepository;
        }

        [HttpGet]
        public IActionResult GetPreguntas()
        {
            return this.Ok(_repository.GetPreguntas());
        }

    }
}