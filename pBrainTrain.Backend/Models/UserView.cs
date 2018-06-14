using pBrainTrain.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace pBrainTrain.Backend.Models
{
    [NotMapped] //because we dont want than this go to database, is just for show some things
    public class UserView : User
    {
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password")] //the name of the field that we want to compare with
        public string PasswordConfirm { get; set; }
        
        public HttpPostedFileBase ImageFile { get; set; }

    }
}