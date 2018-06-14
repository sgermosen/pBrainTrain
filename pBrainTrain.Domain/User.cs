namespace pBrainTrain.Domain
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class User
    {
        [Key] //saying to the entity witch field is the primarykey
        public int UserId { get; set; }

        [Required(ErrorMessage ="The field {0} is required")]
        [MaxLength(50,ErrorMessage ="The maximun length for field {0} is {1} characters")] //the {0} parameter is the name of the field (will display the displayName if it have one)
        [Display(Name ="First Name")] //Display, is how we want that shows on the view
        public string FirstName { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [MaxLength(50, ErrorMessage = "The maximun length for field {0} is {1} characters")] //the {0} parameter is the name of the field (will display the displayName if it have one)
        [Display(Name = "First Name")] //Display, is how we want that shows on the view
        public string LastFirstName { get; set; }

        [Display(Name = "User Type")]
        public int UserTypeId { get; set; }

        [DataType(DataType.ImageUrl)] //we can add restriction of what type of and element can be nested on each field, for example, on this field as string i can insert any type of characters that i want, but, with this type of restriction, Razor knows what can pass from the view to the controller
        public string Picture { get; set; }

        [DataType(DataType.EmailAddress)] //we can add restriction of what type of and element can be nested on each field, for example, on this field as string i can insert any type of characters that i want, but, with this type of restriction, Razor knows what can pass from the view to the controller
        public string Email { get; set; }
        
        [JsonIgnore]
        public virtual UserType UserType { get; set; }  //this part of the relation dont need be plurarized
        [JsonIgnore]
        public virtual Status Status { get; set; }
        [JsonIgnore]
        public ICollection<UserRol> UserRols { get; set; }

    }
}
