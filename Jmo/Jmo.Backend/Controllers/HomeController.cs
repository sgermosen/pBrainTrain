using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Jmo.Backend.Models;
using Jmo.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Jmo.Backend.Repositories;

namespace Jmo.Backend.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPreguntaRepository _repository;

        public HomeController(IPreguntaRepository repository)
        {
            _repository = repository;
        }
        public async Task<IActionResult> Index()
        {
            var preguntas =  _repository.GetPreguntas();            

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
