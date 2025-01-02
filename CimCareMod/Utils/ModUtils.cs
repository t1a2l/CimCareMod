using System.Linq;
using System.Collections.Generic;
using ColossalFramework.Plugins;

namespace CimCareMod.Utils
{
    /// <summary>
    /// Class that manages interactions with other mods, including compatibility and functionality checks.
    /// </summary>


    internal static class ModUtils
    {
        /// <summary>
        /// Returns the filepath of the current mod assembly.
        /// </summary>
        /// <returns>Mod assembly filepath</returns>


        internal static string GetAssemblyPath()
        {
            // Get list of currently active plugins.
            IEnumerable<PluginManager.PluginInfo> plugins = PluginManager.instance.GetPluginsInfo();

            return plugins.First().modPath;

        }
    }
}
