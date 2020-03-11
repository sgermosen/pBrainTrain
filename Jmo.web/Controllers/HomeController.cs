using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Jmo.Web.Models;
using Jmo.Web.Repositories;

namespace Jmo.Web.Controllers
{
    public class HomeController : Controller
    {
      private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        
        private readonly IPreguntaRepository _repository;

        public HomeController(IPreguntaRepository repository)
        {
            _repository = repository;
        }
        public IActionResult Index()
        {
            var preguntas = _repository.GetPreguntas();

            return View(preguntas);
        }

        public IActionResult Privacy()
        {
            return View();
        }

       

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
