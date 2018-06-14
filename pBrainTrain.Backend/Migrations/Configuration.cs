namespace pBrainTrain.Backend.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<pBrainTrain.Backend.Models.LocalDataContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true; //if you dont care your data you put it true, but this is only for development
            //in normal situations, the database fields didnt change of name, so, normally we disabled this options, but in this case that we are creating from scrash and making a lot of changes when the development think in a better idea for do something from another way, is a required enabled this option
        }

        protected override void Seed(pBrainTrain.Backend.Models.LocalDataContext context)
        {
             
        }
    }
}
