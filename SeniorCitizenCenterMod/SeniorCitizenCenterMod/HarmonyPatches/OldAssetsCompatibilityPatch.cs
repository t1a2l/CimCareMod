using HarmonyLib;

namespace SeniorCitizenCenterMod.HarmonyPatches
{
    [HarmonyPatch(typeof(PackageHelper), "ResolveLegacyTypeHandler")]
    static class OldAssetsCompatibilityPatch
    {
        // 'SeniorCitizenCenterMod.NursingHomeAi, SeniorCitizenCenterMod, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
        [HarmonyPostfix]
        public static void Postfix(ref string __result)
	    {
            string[] temp = __result.Split(',');
            if(temp[1] == " SeniorCitizenCenterMod")
            {
                if(temp[0] == "SeniorCitizenCenterMod.NursingHomeAi")
                {
                    __result = "SeniorCitizenCenterMod.NursingHomeAI, SeniorCitizenCenterMod, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
                }
            }
		    Logger.logInfo(__result);
	    }
    }
}
