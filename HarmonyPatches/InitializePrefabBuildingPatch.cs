using System;
using HarmonyLib;
using CimCareMod.AI;
using CimCareMod.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CimCareMod.HarmonyPatches
{
    [HarmonyPatch(typeof(BuildingInfo), "InitializePrefab")]
    public static class InitializePrefabBuildingPatch
    { 
        public static void Prefix(BuildingInfo __instance)
        {
            try
            {
                if (__instance.m_class.m_service == ItemClass.Service.HealthCare &&  __instance.name.Contains("OR123") && __instance.GetAI() is not OrphanageAI)
                {
                    var oldAI = __instance.GetComponent<PrefabAI>();
                    Object.DestroyImmediate(oldAI);
                    var newAI = (PrefabAI)__instance.gameObject.AddComponent<OrphanageAI>();
                    PrefabUtil.TryCopyAttributes(oldAI, newAI, false);
                } 
                else if (__instance.m_class.m_service == ItemClass.Service.HealthCare &&  __instance.name.Contains("NH123") && __instance.GetAI() is not NursingHomeAI)
                {
                    var oldAI = __instance.GetComponent<PrefabAI>();
                    Object.DestroyImmediate(oldAI);
                    var newAI = (PrefabAI)__instance.gameObject.AddComponent<NursingHomeAI>();
                    PrefabUtil.TryCopyAttributes(oldAI, newAI, false);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void Postfix(BuildingInfo __instance)
        {
            try
            {
                if (__instance.GetAI() is OrphanageAI orphanagAI)
                {
                    BuildingInfo childCareBuildingInfo = PrefabCollection<BuildingInfo>.FindLoaded("Child Health Center 01");
                    float childCareCapcityModifier = Mod.GetInstance().GetOptionsManager().GetOrphanagesCapacityModifier();
                    __instance.m_class = childCareBuildingInfo.m_class;
                    orphanagAI.UpdateCapacity(childCareCapcityModifier);
                    __instance.m_placementMode = BuildingInfo.PlacementMode.Roadside;
                }
                else if (__instance.GetAI() is NursingHomeAI nursingHomeAI)
                {
                    BuildingInfo elderCareBuildingInfo = PrefabCollection<BuildingInfo>.FindLoaded("Eldercare 01");
                    float elderCareCapcityModifier = Mod.GetInstance().GetOptionsManager().GetNursingHomesCapacityModifier();
                    __instance.m_class = elderCareBuildingInfo.m_class;
                    nursingHomeAI.UpdateCapacity(elderCareCapcityModifier);
                    __instance.m_placementMode = BuildingInfo.PlacementMode.Roadside;
                    if (__instance.m_class.m_level != ItemClass.Level.Level5)
                    {
                        __instance.m_class.m_level = ItemClass.Level.Level5;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}