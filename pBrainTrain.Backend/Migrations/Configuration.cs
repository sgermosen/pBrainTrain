namespace pBrainTrain.Backend.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<pBrainTrain.Backend.Models.LocalDataContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false; //if you dont care your data you put it true, but this is only for development
        }

        protected override void Seed(pBrainTrain.Backend.Models.LocalDataContext context)
        {
             
        }
    }
}
