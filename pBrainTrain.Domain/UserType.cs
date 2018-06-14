namespace pBrainTrain.Domain
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class UserType
    {
        [Key]
        public int UserTypeId { get; set; }

        //and to be a index need to have a max length
        [Required(ErrorMessage ="This is required")]
        [MaxLength(50, ErrorMessage = "The maximun length for field {0} is {1} characters")]
        [Index("UserType_Name_Index",IsUnique =true)] //a name for the index if we need it, the optimal way is always named as the table name, then the field name and finally the word index
        public string Name { get; set; } //normally you can index just a field that is unique, so, we need to tell to entity that
        //but, if is unique, and is a index, it need to be required too

        //creating a relationship bwtween tables, on this case, a user can have one usertype (one to many) but a usertype can have more than one user (many to one)
        [JsonIgnore]
        public virtual ICollection<User> Users { get; set; } //normally the part one to may of the table need to be plurarized 
    }
}
