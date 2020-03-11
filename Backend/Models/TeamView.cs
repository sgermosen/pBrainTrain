using System.Web;
using Domain;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class TeamView: Team
    {
        [Display(Name = "Logo")]
        public HttpPostedFileBase LogoFile { get; set; }
    }
}