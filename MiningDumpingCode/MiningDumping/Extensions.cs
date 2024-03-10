using Mafi;
using Mafi.Unity.UserInterface.Style;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod;

public static class Extensions
{
    public static RelTile2i Rel4Index(this RelTile2i tile, int idx)
    {
        return new RelTile2i(idx % 5, idx / 5);
    }
    public static void NextModulo(this ref int inPut, int modulo) 
    {
        inPut =  ((inPut + 1) % modulo);
    }
}
