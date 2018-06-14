using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace pBrainTrain.Domain
{
    public class DataContext : DbContext
    {
        public DataContext() :base("DefaultConnection")
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();    
        }

        public System.Data.Entity.DbSet<pBrainTrain.Domain.Country> Countries { get; set; }

        public System.Data.Entity.DbSet<pBrainTrain.Domain.Status> Status { get; set; }

        public System.Data.Entity.DbSet<pBrainTrain.Domain.Rol> Rols { get; set; }

        public System.Data.Entity.DbSet<pBrainTrain.Domain.UserRol> UserRols { get; set; }
        
        public System.Data.Entity.DbSet<pBrainTrain.Domain.UserType> UserTypes { get; set; }

        public System.Data.Entity.DbSet<pBrainTrain.Domain.User> Users { get; set; }

    }
}
