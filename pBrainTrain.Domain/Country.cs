namespace pBrainTrain.Domain
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class Country
    {
        public int CountryId { get; set; }

        public string Name { get; set; }

        public string Demonym { get; set; }

        [JsonIgnore]
        public virtual ICollection<User>  Users { get; set; }
    }
}
