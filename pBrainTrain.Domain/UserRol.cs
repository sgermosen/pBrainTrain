namespace pBrainTrain.Domain
{
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    public class UserRol
    {
        [Key]
        public int UserRolId { get; set; }

        public int RolId { get; set; }

        public int UserId { get; set; }

        public int StatusId { get; set; }

        [JsonIgnore]
        public virtual Status Status { get; set; }
        [JsonIgnore]
        public virtual Rol Rol { get; set; }
        [JsonIgnore]
        public virtual User User { get; set; }

    }
}
