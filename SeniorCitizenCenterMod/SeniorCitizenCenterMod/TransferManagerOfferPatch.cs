using System;
using HarmonyLib;

namespace SeniorCitizenCenterMod
{
    [HarmonyPatch(typeof(TransferManager))]
    public class TransferManagerOfferPatch
    {
		[HarmonyPatch("AddIncomingOffer")]
		[HarmonyPrefix]
		public static bool AddIncomingOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
			if (material == TransferManager.TransferReason.ElderCare && offer.Citizen != 0)
            {
                var instance = CitizenManager.instance.m_citizens.m_buffer[offer.Citizen].m_instance;
				var sourceBuildingID = CitizenManager.instance.m_instances.m_buffer[instance].m_sourceBuilding;
                var targetBuildingID = CitizenManager.instance.m_instances.m_buffer[instance].m_targetBuilding;
				var sourceBuilding = BuildingManager.instance.m_buildings.m_buffer[sourceBuildingID];
				var targetBuilding = BuildingManager.instance.m_buildings.m_buffer[targetBuildingID];
				float radius = (float) (targetBuilding.Width + targetBuilding.Length) * 2.5f;
				var distance = GetDistance(targetBuilding.m_position.x, sourceBuilding.m_position.x, targetBuilding.m_position.y, sourceBuilding.m_position.y);
				if(targetBuilding.Info.GetAI() is NursingHomeAI && distance > radius)
                {
					return false;
                }
            }
			return true;
        }

		private static float GetDistance(float x1, float y1, float x2, float y2) 
		{
			var R = 6371; // Radius of the earth in km
			var dLat = ToRadians(x2-x1);
			var dLon = ToRadians(y2-y1); 
			var a = 
				Math.Sin(dLat/2) * Math.Sin(dLat/2) +
				Math.Cos(ToRadians(x1)) * Math.Cos(ToRadians(x2)) * 
				Math.Sin(dLon/2) * Math.Sin(dLon/2);

			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a)); 
			var d = R * c; // Distance in km
			return (float)d;
		}

		private static float ToRadians(float deg) 
		{
			return (float)(deg * (Math.PI/180));
		}
	}
}
