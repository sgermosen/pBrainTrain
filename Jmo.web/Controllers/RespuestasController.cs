using Jmo.Web.Data.Domain;
using Jmo.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Jmo.Web.Controllers
{
    public class RespuestasController : Controller
    {
        private readonly IPreguntaRepository _preguntaRepository;
        private readonly IRespuestaRepository _repository;

        public RespuestasController(IRespuestaRepository repository, IPreguntaRepository reguntaRepository)
        {
            _repository = repository;
            _preguntaRepository = reguntaRepository;
        }

        public async Task<IActionResult> Create(int id)
        {
            var preg = _preguntaRepository.GetPregunta(id);

            var resp = new Respuesta { PreguntaId = preg.Id };

            return View(resp);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Respuesta rpt)
        {
            if (!ModelState.IsValid)
                return View();
            rpt.Id = 0;

          await  _repository.CreateAsync(rpt);
            
            return RedirectToAction("Detail", "Preguntas", new { id = rpt.PreguntaId });
        }
    }
}