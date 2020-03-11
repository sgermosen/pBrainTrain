using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Domain;

namespace Backend.Models
{
    [NotMapped]
    public class UserView: User
    {
        [Display(Name="Picture")]
        public HttpPostedFileBase PictureFile { get; set; }
        [Display(Name = "Favorite League")]
        public int FavoriteLeagueId { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage="El campo {0} es requerido")]
        [StringLength(20,ErrorMessage="La longitud maxima para el campo {0} debe ser de {1}")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmacion deben coincidir")]
        [Display(Name = "Password Confirm")]
        public string PasswordConfirm { get; set; }


    }
}