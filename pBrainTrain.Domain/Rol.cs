using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pBrainTrain.Domain
{
    public class Rol
    {
        [Key]
        public int RolId { get; set; }

        [Display(Name = "Rol")]
        public string Name { get; set; }

        public string Description { get; set; }

        public int StatusId { get; set; }


        [JsonIgnore]
        public virtual Status Status { get; set; }
        [JsonIgnore]
        public ICollection<UserRol> UserRols { get; set; }
    }
}
