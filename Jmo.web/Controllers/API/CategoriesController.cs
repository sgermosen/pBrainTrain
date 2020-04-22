using Jmo.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Jmo.Web.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
       
        public CategoriesController(ICategoryRepository categoryRepository)
        {

            _categoryRepository = categoryRepository;
        }

        [HttpGet]
        public IActionResult GetCategories()
        {
            return this.Ok(_categoryRepository.GetAll());
        }

    }
}