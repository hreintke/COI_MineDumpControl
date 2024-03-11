using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod
{
    [GlobalDependency(RegistrationMode.AsSelf, false)]
    public static class LogWrite
    {
        private static int msgCount = 0;
        private static string Prefix = Assembly.GetExecutingAssembly().GetName().Name + " : ============> ";

        public static void Info(string message) => Mafi.Log.Info($"{Prefix} {++msgCount} {message}");

        public static void InfoDebug(string message) => Mafi.Log.InfoDebug($"{Prefix} {++msgCount} {message}");

        public static void Warning(string message) => Mafi.Log.Warning($"{Prefix} {++msgCount} {message}");

        public static void WarningOnce(string message) => Mafi.Log.WarningOnce($"{Prefix} {++msgCount} {message}");

        public static void Error(string message) => Mafi.Log.Error($"{Prefix} {++msgCount} {message}");

        public static void Exception(Exception e, string message) => Mafi.Log.Exception(e, $"{Prefix} {++msgCount} {message}");
    }
}
