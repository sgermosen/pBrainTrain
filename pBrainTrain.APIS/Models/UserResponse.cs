
using pBrainTrain.Domain;
using System.Collections.Generic;

namespace pBrainTrain.APIS.Models
{
    public class UserResponse
    {
        public int UserId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int UserTypeId { get; set; }

        public string Picture { get; set; }

        public string Email { get; set; }

        public int? CountryId { get; set; }

        public int StatusId { get; set; }


        public UserType UserType { get; set; }  //this part of the relation dont need be plurarized

        public Status Status { get; set; }

        public Country Country { get; set; }


        public List<UserRol> UserRols { get; set; }

    }
}