using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(pBrainTrain.Backend.Startup))]
namespace pBrainTrain.Backend
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
