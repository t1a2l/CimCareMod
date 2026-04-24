using CimCareMod.Utils;
using HarmonyLib;

namespace CimCareMod.HarmonyPatches
{
    [HarmonyPatch(typeof(PackageHelper), "ResolveLegacyTypeHandler")]
    static class OldAssetsCompatibilityPatch
    {
        // 'SeniorCitizenCenterMod.NursingHomeAi, SeniorCitizenCenterMod, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
        [HarmonyPostfix]
        public static void Postfix(ref string __result)
	    {
            string[] temp = __result.Split(',');
            if(temp != null && temp.Length > 1 && temp[1].Trim() == "SeniorCitizenCenterMod")
            {
                if(temp[0] == "SeniorCitizenCenterMod.NursingHomeAi" || temp[0] == "SeniorCitizenCenterMod.NursingHomeAI")
                {
                    __result = "CimCareMod.AI.NursingHomeAI, CimCareMod, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
                }
            }
		    Logger.LogInfo(__result);
	    }
    }
}
