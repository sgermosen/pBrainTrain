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
    }
}
