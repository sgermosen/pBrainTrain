using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Domain
{
    public class Group
    {
        [Key]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [MaxLength(50, ErrorMessage = "The maximun length for field {0} is {1} characters")]
        [Index("Group_Name_Index", IsUnique = true)]
        [Display(Name = "Group")]
        public string Name { get; set; }

        [Display(Name = "User")]
        public int OwnerId { get; set; }

        [JsonIgnore]
        public virtual User Owner { get; set; }
        [JsonIgnore]
        public virtual ICollection<GroupUser> GroupUsers { get; set; }
    }

}
