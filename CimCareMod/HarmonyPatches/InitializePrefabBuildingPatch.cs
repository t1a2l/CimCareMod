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

        public static void Postfix()
        {
            try
            {
                BuildingInfo childCareBuildingInfo = PrefabCollection<BuildingInfo>.FindLoaded("Child Health Center 01");
                BuildingInfo elderCareBuildingInfo = PrefabCollection<BuildingInfo>.FindLoaded("Eldercare 01");

                float childCareCapcityModifier = CimCareMod.getInstance().getOptionsManager().GetOrphanagesCapacityModifier();
                float elderCareCapcityModifier = CimCareMod.getInstance().getOptionsManager().GetNursingHomesCapacityModifier();

                uint index = 0U;
                for (; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index) 
                {
                    BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);

                    // Check for replacement of AI
                    if (buildingInfo != null && buildingInfo.GetAI() is OrphanageAI orphanagAI)
                    {
                        buildingInfo.m_class = childCareBuildingInfo.m_class;
                        orphanagAI.UpdateCapacity(childCareCapcityModifier);
                        buildingInfo.m_placementMode = BuildingInfo.PlacementMode.Roadside;
                    }
                    else if (buildingInfo != null && buildingInfo.GetAI() is NursingHomeAI nursingHomeAI)
                    {
                        buildingInfo.m_class = elderCareBuildingInfo.m_class;
                        nursingHomeAI.UpdateCapacity(elderCareCapcityModifier);
                        buildingInfo.m_placementMode = BuildingInfo.PlacementMode.Roadside;
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