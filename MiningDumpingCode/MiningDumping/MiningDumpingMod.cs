using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MiningDumpingMod
{
    public sealed class CustomEntityMod : IMod
    {
        public string Name => "MiningDumpingMod";

        public int Version => 1;
        public static Version ModVersion = new Version(0, 0, 3);
        public bool IsUiOnly => false;

        public void ChangeConfigs(Lyst<IConfig> configs)
        {
        }

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
            LogWrite.Info("Initializing ");
        }

        public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
        {
            LogWrite.Info("Register Dependencies ");
        }

        public void RegisterPrototypes(ProtoRegistrator registrator)
        {
            LogWrite.Info("Registrating Prototypes");
            registrator.RegisterAllProducts();
            registrator.RegisterData<MDRegistrator>();
            registrator.RegisterData<MDResearch>();
        }
    }
}
