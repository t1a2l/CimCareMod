using System;
using ColossalFramework.UI;
using HarmonyLib;
using SeniorCitizenCenterMod.AI;

namespace SeniorCitizenCenterMod.HarmonyPatches
{
    [HarmonyPatch(typeof(DecorationPropertiesPanel))]
    public class AddFieldPatch
    {
        [HarmonyPatch(typeof(DecorationPropertiesPanel), "AddField",
            new Type[] { typeof(UIComponent), typeof(string), typeof(float), typeof(Type), typeof(string), typeof(int), typeof(object), typeof(object) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool AddField(DecorationPropertiesPanel __instance, UIComponent container, string locale, float width, Type type, string name, int arrayIndex, object target, object initialValue)
		{
			if(target is NursingHomeAI && name.StartsWith("m_workPlaceCount"))
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
