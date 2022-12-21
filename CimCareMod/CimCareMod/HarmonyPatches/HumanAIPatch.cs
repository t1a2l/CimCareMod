using ColossalFramework;
using HarmonyLib;
using CimCareMod.AI;

namespace CimCareMod.HarmonyPatches
{
    [HarmonyPatch(typeof(HumanAI))]
    public static class HumanAIPatch
    {
        [HarmonyPatch(typeof(HumanAI), "FindVisitPlace")]
		[HarmonyPrefix]
        public static bool FindVisitPlace(uint citizenID, ushort sourceBuilding, TransferManager.TransferReason reason)
		{
			Citizen citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID];
			BuildingInfo homeBuildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizen.m_homeBuilding].Info;
			if(reason == TransferManager.TransferReason.ElderCare)
            {
				if(IsSenior(citizenID) && homeBuildingInfo.GetAI() is NursingHomeAI)
                {
					return false;
                }
            }
			else if(reason == TransferManager.TransferReason.ChildCare)
            {
				if(IsChild(citizenID) && homeBuildingInfo.GetAI() is OrphanageAI)
                {
					return false;
                }
            }
			TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
			offer.Priority = Singleton<SimulationManager>.instance.m_randomizer.Int32(8u);
			offer.Citizen = citizenID;
			offer.Position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[sourceBuilding].m_position;
			offer.Amount = 1;
			offer.Active = true;
			Singleton<TransferManager>.instance.AddIncomingOffer(reason, offer);
			return false;
		}

		private static bool IsSenior(uint citizenID)
		{
			return Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Senior;
		}

		private static bool IsChild(uint citizenID)
		{
			return Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Teen;
		}
    }
}
