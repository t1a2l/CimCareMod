using ColossalFramework;
using HarmonyLib;
using UnityEngine;
using SeniorCitizenCenterMod.AI;

namespace SeniorCitizenCenterMod.HarmonyPatches
{
    [HarmonyPatch(typeof(ResidentAI))]
    public static class ResidentAIPatch
    {  
		[HarmonyPatch(typeof(ResidentAI), "FindHospital")]
		[HarmonyPrefix]
		public static bool FindHospital(uint citizenID, ushort sourceBuilding, TransferManager.TransferReason reason, ref bool __result)
		{
			if (reason == TransferManager.TransferReason.Dead)
			{
				if (Singleton<UnlockManager>.instance.Unlocked(UnlockManager.Feature.DeathCare))
				{
					__result = true;
					return false;
				}
				Singleton<CitizenManager>.instance.ReleaseCitizen(citizenID);
				__result = false;
				return false;
			}
			if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.HealthCare))
			{
				BuildingManager instance = Singleton<BuildingManager>.instance;
				DistrictManager instance2 = Singleton<DistrictManager>.instance;
				Vector3 position = instance.m_buildings.m_buffer[sourceBuilding].m_position;
				byte district = instance2.GetDistrict(position);
				DistrictPolicies.Services servicePolicies = instance2.m_districts.m_buffer[district].m_servicePolicies;
				TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
				Citizen citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID];
				BuildingInfo homeBuildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizen.m_homeBuilding].Info;
				offer.Priority = 6;
				offer.Citizen = citizenID;
				offer.Position = position;
				offer.Amount = 1;
				bool flag = false;
				if (Singleton<CitizenManager>.exists && Singleton<CitizenManager>.instance != null && Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].m_health >= 40 && (IsChild(citizenID) || IsSenior(citizenID)))
				{
					FastList<ushort> serviceBuildings = Singleton<BuildingManager>.instance.GetServiceBuildings(ItemClass.Service.HealthCare);
					for (int i = 0; i < serviceBuildings.m_size; i++)
					{
						BuildingInfo info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]].Info;
						if ((object)info != null)
						{
							if (IsChild(citizenID) && info.m_class.m_level == ItemClass.Level.Level4)
							{
								reason = TransferManager.TransferReason.ChildCare;
								flag = true;
							}
							else if (IsSenior(citizenID) && info.m_class.m_level == ItemClass.Level.Level5)
							{
								if(!(homeBuildingInfo.GetAI() is NursingHomeAI))
                                {
									reason = TransferManager.TransferReason.ElderCare;
									flag = true;
                                }
							}
						}
					}
				}
				if (flag && Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
				{
					offer.Active = true;
					Singleton<TransferManager>.instance.AddIncomingOffer(reason, offer);
				}
				else if ((servicePolicies & DistrictPolicies.Services.HelicopterPriority) != 0)
				{
					instance2.m_districts.m_buffer[district].m_servicePoliciesEffect |= DistrictPolicies.Services.HelicopterPriority;
					offer.Active = false;
					Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Sick2, offer);
				}
				else if ((instance.m_buildings.m_buffer[sourceBuilding].m_flags & Building.Flags.RoadAccessFailed) != 0 || Singleton<SimulationManager>.instance.m_randomizer.Int32(20u) == 0)
				{
					offer.Active = false;
					Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Sick2, offer);
				}
				else
				{
					offer.Active = Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0;
					Singleton<TransferManager>.instance.AddOutgoingOffer(reason, offer);
				}
				__result = true;
				return false;
			}
			Singleton<CitizenManager>.instance.ReleaseCitizen(citizenID);
			__result = false;
			return false;
		}

		private static bool IsChild(uint citizenID)
		{
			return Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Teen;
		}

		private static bool IsSenior(uint citizenID)
		{
			return Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Senior;
		}
    }
}
