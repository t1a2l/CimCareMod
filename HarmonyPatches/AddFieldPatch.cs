using System;
using ColossalFramework.UI;
using HarmonyLib;
using CimCareMod.AI;

namespace CimCareMod.HarmonyPatches
{
    [HarmonyPatch(typeof(DecorationPropertiesPanel))]
    public class AddFieldPatch
    {
        [HarmonyPatch(typeof(DecorationPropertiesPanel), "AddField",
            [typeof(UIComponent), typeof(string), typeof(float), typeof(Type), typeof(string), typeof(int), typeof(object), typeof(object)],
            [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool AddField(DecorationPropertiesPanel __instance, UIComponent container, string locale, float width, Type type, string name, int arrayIndex, object target, object initialValue)
		{
			if((target is NursingHomeAI || target is OrphanageAI) && name.StartsWith("m_workPlaceCount"))
            {
                return false;
            }
            else 
            {
              return true;
            }
		}


    }
}
