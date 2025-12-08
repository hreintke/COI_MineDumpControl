using Mafi;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Entities;
using Mafi.Core.Input;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain.Designation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod;

[GlobalDependency(RegistrationMode.AsAllInterfaces, false, false)]
public class MDTowerCommandsProcessor :
        ICommandProcessor<MDAreaChangedCmd>,
        IAction<MDAreaChangedCmd> 
    {
    
    private readonly EntitiesManager entitiesManager;

    public MDTowerCommandsProcessor(EntitiesManager em)
    {
        entitiesManager = em;
    }

    public void Invoke(MDAreaChangedCmd cmd)
    {
        MDTower entity;
        if (!this.entitiesManager.TryGetEntity<MDTower>(cmd.mdTowerId, out entity))
        {
            cmd.SetResultError(string.Format("Failed to get MD tower with ID {0}.", (object)cmd.mdTowerId));
        }
        else
        {
            entity.editMinableArea(cmd.Area);
            cmd.SetResultSuccess();
        }
    }
}
