using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace API.Classes
{
    public class ChangePasswordRequest
    {
        public string Email { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}