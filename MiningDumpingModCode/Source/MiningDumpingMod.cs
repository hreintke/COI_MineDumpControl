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

        public int Version => (typeof(CustomEntityMod).Assembly.GetName().Version.Major * 100) +
                                (typeof(CustomEntityMod).Assembly.GetName().Version.Minor * 10) +
                                (typeof(CustomEntityMod).Assembly.GetName().Version.Build);

        public static Version ModVersion => typeof(CustomEntityMod).Assembly.GetName().Version;

        public bool IsUiOnly => false;

        public Option<IConfig> ModConfig { get; }

        public void ChangeConfigs(Lyst<IConfig> configs)
        {
        }

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
            LogWrite.Info($"Initializing v = {Version}");
        }

        public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
        {
//            LogWrite.Info("Register Dependencies ");
        }

        public void RegisterPrototypes(ProtoRegistrator registrator)
        {
            LogWrite.Info("Registrating Prototypes");
            registrator.RegisterAllProducts();
            registrator.RegisterData<MDRegistrator>();
            registrator.RegisterData<MDResearch>();
        }

        public void EarlyInit(DependencyResolver resolver)
        {
        }
    }
}
