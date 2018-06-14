namespace pBrainTrain.Domain
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    
    public class Status
    {
        [Key]
        public int StatusId { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public ICollection<User> Users { get; set; }
        [JsonIgnore]
        public ICollection<Rol> Rols { get; set; }
        [JsonIgnore]
        public ICollection<UserRol> UserRols { get; set; }
    }
}
