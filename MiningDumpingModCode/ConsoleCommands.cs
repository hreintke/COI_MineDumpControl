using Mafi;
using Mafi.Core.Console;
using Mafi.Core.Entities;
using Mafi.Core.Factory.Transports;
using Mafi.Core.Input;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain.Trees;
using Mafi.Unity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod;

[GlobalDependency(RegistrationMode.AsSelf)]
internal class ConsoleCommands
{
    private EntitiesManager entitiesManager;
    private TreesManager treesManager;

    public ConsoleCommands(EntitiesManager em, TreesManager tm

          )
    {
        entitiesManager = em;
        treesManager = tm;
    }



    [ConsoleCommand(true, false, null, null)]
    internal string show_mining()
    {
        foreach (var mt in entitiesManager.GetAllEntitiesOfType<MDTower>())
        {
            foreach(var d in mt.ManagedDesignations)
            {
                LogWrite.Info($"{mt.Id} {d.Area.ToString()} {d.IsMiningFulfilled} ");
                if (!d.IsMiningFulfilled)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        var thisTile = d.OriginTileCoord + new RelTile2i().Rel4Index(i);
                        if (!d.IsMiningFulfilledAt(d.OriginTileCoord + new RelTile2i().Rel4Index(i)))
                        {
                            LogWrite.Info($"not ff {(d.OriginTileCoord + new RelTile2i().Rel4Index(i)).ToString()}");
                            if (treesManager.TryGetStump(new TreeId(new Tile2iSlim((ushort)thisTile.X, (ushort)thisTile.Y)), out TreeStumpData tsd))
                            {
                                LogWrite.Info($"Stump found {tsd.TreeProto.Id}");
                            }                            ;
                        }
                    }

                }

            }

             
        }
        LogWrite.Info("Stumps");
        foreach (var s in treesManager.Stumps)
        {
            LogWrite.Info($"{s.Key.ToString()} {s.Value.ToString()}");
        }
        

        return $"OK";
    }
}
