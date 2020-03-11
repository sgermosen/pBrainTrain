using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Domain;

namespace Backend.Classes
{
    [NotMapped]
    public class UserRequest : User
    {
        public string Password { get; set; }

        public byte[] ImageArray { get; set; }
    }

}