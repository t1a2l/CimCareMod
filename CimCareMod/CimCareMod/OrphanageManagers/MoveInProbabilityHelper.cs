using System;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using CimCareMod.Utils;

namespace CimCareMod.OrphanageManagers
{
    public class MoveInProbabilityHelper 
    {
        private static readonly bool LOG_CHANCES = true;

        private static readonly float BASE_CHANCE_VALUE = 0f;
        private static readonly float AGE_MAX_CHANCE_VALUE = 100f;
        private static readonly float DISTANCE_MAX_CHANCE_VALUE = 100f;
        private static readonly float FAMILY_STATUS_MAX_CHANCE_VALUE = 100f;
        private static readonly float QUALITY_MAX_CHANCE_VALUE = 200f;
        private static readonly float WORKER_MAX_CHANCE_VALUE = 100f;
        private static readonly float MAX_CHANCE_VALUE = AGE_MAX_CHANCE_VALUE + DISTANCE_MAX_CHANCE_VALUE + FAMILY_STATUS_MAX_CHANCE_VALUE + QUALITY_MAX_CHANCE_VALUE + WORKER_MAX_CHANCE_VALUE;
        private static readonly float NO_CHANCE = -(MAX_CHANCE_VALUE * 10);
        private static readonly float CHILD_AGE_RANGE = Citizen.AGE_LIMIT_TEEN;

        public static bool checkIfShouldMoveIn(uint[] familyWithChildren, ref Building buildingData, ref Randomizer randomizer, float operationRadius, int quality, ref NumWorkers numWorkers) 
        {
            float chanceValue = BASE_CHANCE_VALUE;

            Logger.logInfo(LOG_CHANCES, "---------------------------------");

            // Age 
            chanceValue += getAgeChanceValue(familyWithChildren);

            // Distance
            chanceValue += getDistanceChanceValue(familyWithChildren, ref buildingData, operationRadius);

            // Family Status
            chanceValue += getFamilyStatusChanceValue(familyWithChildren);

            // Wealth
            chanceValue += getWealthChanceValue(familyWithChildren, quality);

            // Workers
            chanceValue += getWorkersChanceValue(ref numWorkers);

            // Check for no chance
            if (chanceValue <= 0) {
                Logger.logInfo(LOG_CHANCES, "MoveInProbabilityHelper.checkIfShouldMoveIn -- No Chance: {0}", chanceValue);
                return false;
            }

            // Check against random value
            uint maxChance = (uint) MAX_CHANCE_VALUE;
            int randomValue = randomizer.Int32(maxChance);
            Logger.logInfo(LOG_CHANCES, "MoveInProbabilityHelper.checkIfShouldMoveIn -- Total Chance Value: {0} -- Random Number: {1} -- result: {2}", chanceValue, randomValue, randomValue <= chanceValue);
            return randomValue <= chanceValue;
        }

        // need to understand the calculation
        private static float getAgeChanceValue(uint[] familyWithChildren) 
        {
            float averageChildrenAge = MoveInProbabilityHelper.getAverageAgeOfChildren(familyWithChildren);
            float chanceValue = (averageChildrenAge / CHILD_AGE_RANGE) * AGE_MAX_CHANCE_VALUE;
            Logger.logInfo(LOG_CHANCES, "MoveInProbabilityHelper.getAgeChanceValue -- Age Chance Value: {0} -- Average Age: {1} -- ", chanceValue, averageChildrenAge);
            return Math.Min(chanceValue, AGE_MAX_CHANCE_VALUE);
        }

        private static float getAverageAgeOfChildren(uint[] familyWithChildren) 
        {
            OrphanageManager orphanageManager = OrphanageManager.getInstance();
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            int numChildren = 0;
            int combinedAge = 0;
            foreach (uint familyMember in familyWithChildren) 
            {
                if (orphanageManager.isChild(familyMember)) 
                {
                    numChildren++;
                    combinedAge += citizenManager.m_citizens.m_buffer[familyMember].Age;
                }
            }

            if (numChildren == 0) 
            {
                return 0f;
            }

            return combinedAge / (float) numChildren;
        }

        private static float getDistanceChanceValue(uint[] familyWithChildren, ref Building buildingData, float operationRadius) 
        {
            // Get the home for the family
            ushort homeBuilding = MoveInProbabilityHelper.getHomeBuildingIdForFamily(familyWithChildren);
            if (homeBuilding == 0) 
            {
                // homeBuilding should never be 0, but if it is return NO_CHANCE to prevent this family from being chosen 
                Logger.logError(LOG_CHANCES, "MoveInProbabilityHelper.getDistanceChanceValue -- Home Building was 0 when it shouldn't have been");
                return NO_CHANCE;
            }

            // Get the distance between the child's home and this Orphanage
            float distance = Vector3.Distance(buildingData.m_position, Singleton<BuildingManager>.instance.m_buildings.m_buffer[homeBuilding].m_position);

            // Calulate the chance modifier based on distance
            float distanceChanceValue = ((operationRadius - distance) / operationRadius) * DISTANCE_MAX_CHANCE_VALUE;
            Logger.logInfo(LOG_CHANCES, "MoveInProbabilityHelper.getDistanceChanceValue -- Distance Chance Value: {0} -- Distance: {1}", distanceChanceValue, distance);

            // Max negative value is -150
            return Mathf.Max(DISTANCE_MAX_CHANCE_VALUE * -2f, distanceChanceValue);
        }

        private static ushort getHomeBuildingIdForFamily(uint[] familyWithChildren) 
        {
            foreach (uint familyMember in familyWithChildren) 
            {
                if (familyMember != 0) 
                {
                    return Singleton<CitizenManager>.instance.m_citizens.m_buffer[familyMember].m_homeBuilding;
                }
            }

            return 0;
        }

        private static float getFamilyStatusChanceValue(uint[] familyWithChildren) 
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;

            // Determin the family status
            bool hasAdults = false;
            bool hasSeniors = false;
            int numChildren = 0;
            foreach (uint familyMember in familyWithChildren) 
            {
                if (familyMember == 0) 
                {
                    continue;
                }

                int age = citizenManager.m_citizens.m_buffer[familyMember].Age;
                var age_group = Citizen.GetAgeGroup(age);
                if (age > Citizen.AGE_LIMIT_TEEN && age < Citizen.AGE_LIMIT_ADULT) 
                {
                    hasAdults = true;
                } 
                else if (age > Citizen.AGE_LIMIT_ADULT) 
                {
                    hasSeniors = true;
                } 
                else if(age_group == Citizen.AgeGroup.Child || age_group == Citizen.AgeGroup.Teen)
                {
                    numChildren++;
                }
            }

            // Caluclate the chances
            float chance = FAMILY_STATUS_MAX_CHANCE_VALUE;

            // If adults and seniors live in the house, 95% less chance for this factor
            if (hasAdults && hasSeniors) 
            {
                chance -= FAMILY_STATUS_MAX_CHANCE_VALUE * 0.95f;
            }
            // If adults live in the house without seniors, 85% less chance for this factor
            else if (hasAdults && !hasSeniors) 
            {
                chance -= FAMILY_STATUS_MAX_CHANCE_VALUE * 0.85f;
            }
            // If seniors live in the house without adults, 45% less chance for this factor
            else if (hasSeniors && !hasAdults) {
                chance -= FAMILY_STATUS_MAX_CHANCE_VALUE * 0.45f;
            }
            // If no adults and no seniors, 100% chance for this factor
            else {
                chance -= FAMILY_STATUS_MAX_CHANCE_VALUE;
            }

            Logger.logInfo(LOG_CHANCES, "MoveInProbabilityHelper.getFamilyStatusChanceValue -- Family Chance Value: {0} -- hasAdults: {1} -- hasSeniors: {2}, -- numChildren: {3}", chance, hasAdults, hasSeniors, numChildren);
            return chance;
        }

        private static float getWealthChanceValue(uint[] familyWithChildren, int quality) 
        {
            Citizen.Wealth wealth = getFamilyWealth(familyWithChildren);
            float chance = NO_CHANCE;
            switch (quality) 
            {
                case 1:
                    // Quality 1's should be mainly for Low Wealth citizens, but not impossible for medium
                    switch (wealth) 
                    {
                        case Citizen.Wealth.High:
                            chance = QUALITY_MAX_CHANCE_VALUE * -2f;
                            break;
                        case Citizen.Wealth.Medium:
                            chance = QUALITY_MAX_CHANCE_VALUE * -0.25f;
                            break;
                        case Citizen.Wealth.Low:
                            chance = QUALITY_MAX_CHANCE_VALUE * 1f;
                            break;
                    }
                    break;
                case 2:
                    // Quality 2's should be for both medium and low wealth citizens
                    switch (wealth) 
                    {
                        case Citizen.Wealth.High:
                            chance = QUALITY_MAX_CHANCE_VALUE * -1f;
                            break;
                        case Citizen.Wealth.Medium:
                            chance = QUALITY_MAX_CHANCE_VALUE * 0.5f;
                            break;
                        case Citizen.Wealth.Low:
                            chance = QUALITY_MAX_CHANCE_VALUE * 0.5f;
                            break;
                    }
                    break;
                case 3:
                    // Quality 3 are ideal for medium wealth citizens, but possible for all
                    switch (wealth) 
                    {
                        case Citizen.Wealth.High:
                            chance = QUALITY_MAX_CHANCE_VALUE * 0.2f;
                            break;
                        case Citizen.Wealth.Medium:
                            chance = QUALITY_MAX_CHANCE_VALUE * 1f;
                            break;
                        case Citizen.Wealth.Low:
                            chance = QUALITY_MAX_CHANCE_VALUE * 0.2f;
                            break;
                    }
                    break;
                case 4:
                    // Quality 4's start to become hard for low wealth citizens and more suited for medium to high wealth citizens
                    switch (wealth) 
                    {
                        case Citizen.Wealth.High:
                            chance = QUALITY_MAX_CHANCE_VALUE * 0.5f; ;
                            break;
                        case Citizen.Wealth.Medium:
                            chance = QUALITY_MAX_CHANCE_VALUE * 0.5f;
                            break;
                        case Citizen.Wealth.Low:
                            chance = QUALITY_MAX_CHANCE_VALUE * -1f;
                            break;
                    }
                    break;
                case 5:
                    // Quality 5's are best suited for high wealth citizens, but some medium wealth citizens can afford it
                    switch (wealth) 
                    {
                        case Citizen.Wealth.High:
                            chance = QUALITY_MAX_CHANCE_VALUE * 1f;
                            break;
                        case Citizen.Wealth.Medium:
                            chance = 0.0f;
                            break;
                        case Citizen.Wealth.Low:
                            chance = QUALITY_MAX_CHANCE_VALUE * -2f;
                            break;
                    }
                    break;
            }

            Logger.logInfo(LOG_CHANCES, "MoveInProbabilityHelper.getQualityLevelChanceValue -- Wealth Chance Value: {0} -- Family Wealth: {1} -- Building Quality: {2}", chance, wealth, quality);
            return chance;
        }

        private static Citizen.Wealth getFamilyWealth(uint[] familyWithChildren) 
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            
            // Get the average wealth of all adults and seniors in the house
            int total = 0;
            int numCounted = 0;
            foreach (uint familyMember in familyWithChildren) 
            {
                if (familyMember != 0) 
                {
                    if (citizenManager.m_citizens.m_buffer[familyMember].Age > Citizen.AGE_LIMIT_YOUNG) 
                    {
                        total += (int) citizenManager.m_citizens.m_buffer[familyMember].WealthLevel;
                        numCounted++;
                    }
                }
            }

            // Should never happen but prevent possible division by 0
            if (numCounted == 0) 
            {
                return Citizen.Wealth.Low;
            }
            
            int wealthValue = Convert.ToInt32(Math.Round(total / (double) numCounted, MidpointRounding.AwayFromZero));
            return (Citizen.Wealth) wealthValue;
        }

        private static float getWorkersChanceValue(ref NumWorkers numWorkers) {
            float chance = WORKER_MAX_CHANCE_VALUE;
            
            // Check for missing uneducated workers
            if (numWorkers.maxNumUneducatedWorkers > 0 && numWorkers.numUneducatedWorkers < numWorkers.maxNumUneducatedWorkers) 
            {
                chance -= (((float) numWorkers.maxNumUneducatedWorkers - (float) numWorkers.numUneducatedWorkers) / (float) numWorkers.maxNumUneducatedWorkers) * 0.15f * WORKER_MAX_CHANCE_VALUE;
            }

            // Check for missing educated workers
            if (numWorkers.maxNumEducatedWorkers > 0 && numWorkers.numEducatedWorkers < numWorkers.maxNumEducatedWorkers) 
            {
                chance -= (((float) numWorkers.maxNumEducatedWorkers - (float) numWorkers.numEducatedWorkers) / (float) numWorkers.maxNumEducatedWorkers) * 0.45f * WORKER_MAX_CHANCE_VALUE;
            }

            // Check for missing well educated workers
            if (numWorkers.maxNumWellEducatedWorkers > 0 && numWorkers.numWellEducatedWorkers < numWorkers.maxNumWellEducatedWorkers) 
            {
                chance -= (((float) numWorkers.maxNumWellEducatedWorkers - (float) numWorkers.numWellEducatedWorkers) / (float) numWorkers.maxNumWellEducatedWorkers) * 0.25f * WORKER_MAX_CHANCE_VALUE;
            }

            // Check for missing highly educated workers
            if (numWorkers.maxNumHighlyEducatedWorkers > 0 && numWorkers.numHighlyEducatedWorkers < numWorkers.maxNumHighlyEducatedWorkers) 
            {
                chance -= (((float) numWorkers.maxNumHighlyEducatedWorkers - (float) numWorkers.numHighlyEducatedWorkers) / (float) numWorkers.maxNumHighlyEducatedWorkers) * 0.15f * WORKER_MAX_CHANCE_VALUE;
            }

            if (LOG_CHANCES) 
            {
                Logger.logInfo(LOG_CHANCES, "MoveInProbabilityHelper.getQualityLevelChanceValue -- Worker Chance Value: {0} -- Missing Uneducated: {1} -- Missing Educated: {2} -- Missing Well Educated: {3} -- Missing Highly Educated: {4}", chance, (numWorkers.maxNumUneducatedWorkers - numWorkers.numUneducatedWorkers), (numWorkers.maxNumEducatedWorkers - numWorkers.numEducatedWorkers), (numWorkers.maxNumWellEducatedWorkers - numWorkers.numWellEducatedWorkers), (numWorkers.maxNumHighlyEducatedWorkers - numWorkers.numHighlyEducatedWorkers));
            }
            return chance;
        }
    }
}