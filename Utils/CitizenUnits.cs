using System;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace CimCareMod.Utils
{
    public static class CitizenUnits
    {
        public static bool CreateUnits(out uint firstUnit, ref Randomizer randomizer, CitizenUnit.Flags flag, ushort building, int homeCount = 0, int workCount = 0)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            firstUnit = 0u;
            workCount = (workCount + 4) / 5;
            int num = homeCount + workCount;
            if (num == 0)
            {
                return true;
            }
            CitizenUnit citizenUnit = default;
            uint num2 = 0u;
            for (int i = 0; i < num; i++)
            {
                if (instance.m_units.CreateItem(out var item, ref randomizer))
                {
                    if (i == 0)
                    {
                        firstUnit = item;
                    }
                    else
                    {
                        citizenUnit.m_nextUnit = item;
                        instance.m_units.m_buffer[num2] = citizenUnit;
                    }
                    citizenUnit = new CitizenUnit
                    {
                        m_flags = CitizenUnit.Flags.Created
                    };
                    if (i < homeCount)
                    {
                        citizenUnit.m_flags |= flag;
                        citizenUnit.m_goods = 200;
                    }
                    else if (i < homeCount + workCount)
                    {
                        citizenUnit.m_flags |= CitizenUnit.Flags.Work;
                    }
                    citizenUnit.m_building = building;
                    num2 = item;
                    continue;
                }
                instance.ReleaseUnits(firstUnit);
                firstUnit = 0u;
                return false;
            }
            instance.m_units.m_buffer[num2] = citizenUnit;
            instance.m_unitCount = (int)(instance.m_units.ItemCount() - 1);
            return true;
        }

        public static void EnsureCitizenUnits(ushort buildingID, ref Building data, CitizenUnit.Flags flag, int homeCount = 0, int workCount = 0)
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.Collapsed)) != Building.Flags.None)
            {
                return;
            }
            Citizen.Wealth wealthLevel = Citizen.GetWealthLevel((ItemClass.Level)data.m_level);
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = 0u;
            uint num2 = data.m_citizenUnits;
            int num3 = 0;
            while (num2 != 0)
            {
                ref CitizenUnit.Flags flags = ref instance.m_units.m_buffer[num2].m_flags;
                if ((flags & CitizenUnit.Flags.Home) != 0)
                {
                    flags &= ~CitizenUnit.Flags.Home;
                    flags |= flag;
                }
                if ((flags & flag) != CitizenUnit.Flags.None)
                {
                    instance.m_units.m_buffer[num2].SetWealthLevel(wealthLevel);
                    homeCount--;
                }
                if ((flags & CitizenUnit.Flags.Work) != CitizenUnit.Flags.None)
                {
                    workCount -= 5;
                }
                num = num2;
                num2 = instance.m_units.m_buffer[num2].m_nextUnit;
                if (++num3 > instance.m_units.m_size)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            homeCount = Mathf.Max(0, homeCount);
            workCount = Mathf.Max(0, workCount);
            if (homeCount == 0 && workCount == 0)
            {
                return;
            }
            if (CreateUnits(out uint firstUnit, ref Singleton<SimulationManager>.instance.m_randomizer, flag, buildingID, homeCount, workCount))
            {
                if (num != 0)
                {
                    instance.m_units.m_buffer[num].m_nextUnit = firstUnit;
                }
                else
                {
                    data.m_citizenUnits = firstUnit;
                }
            }
        }

        public static void GetCimCareFlagsBehaviour(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, CitizenUnit.Flags flag, ref int aliveCount, ref int totalCount, ref int homeCount, ref int aliveHomeCount, ref int emptyHomeCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                if ((instance.m_units.m_buffer[num].m_flags & flag) != 0)
                {
                    int aliveCount2 = 0;
                    int totalCount2 = 0;
                    instance.m_units.m_buffer[num].GetCitizenHomeBehaviour(ref behaviour, ref aliveCount2, ref totalCount2);
                    if (aliveCount2 != 0)
                    {
                        aliveHomeCount++;
                        aliveCount += aliveCount2;
                    }

                    if (totalCount2 != 0)
                    {
                        totalCount += totalCount2;
                    }
                    else
                    {
                        emptyHomeCount++;
                    }

                    homeCount++;
                }

                num = instance.m_units.m_buffer[num].m_nextUnit;
                if (++num2 > instance.m_units.m_size)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public static void SetHome(uint citizenID, ushort buildingID, uint unitID, string state, CitizenUnit.Flags flag)
        {
            var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID];
            CitizenUnit.Flags flags = CitizenUnit.Flags.None;
            if (citizen.m_homeBuilding != 0)
            {
                BuildingManager instance = Singleton<BuildingManager>.instance;
                if (state == "In")
                {
                    flags = CitizenUnit.Flags.Home;
                }
                else if (state == "Out")
                {
                    flags = flag;
                }
                if (flags != CitizenUnit.Flags.None)
                {
                    citizen.RemoveFromUnits(citizenID, instance.m_buildings.m_buffer[citizen.m_homeBuilding].m_citizenUnits, flags);
                    citizen.m_homeBuilding = 0;
                }
                
            }
            if (unitID != 0)
            {
                BuildingManager instance2 = Singleton<BuildingManager>.instance;
                CitizenManager instance3 = Singleton<CitizenManager>.instance;
                if (citizen.AddToUnit(citizenID, ref instance3.m_units.m_buffer[unitID]))
                {
                    citizen.m_homeBuilding = instance3.m_units.m_buffer[unitID].m_building;
                    citizen.WealthLevel = Citizen.GetWealthLevel(instance2.m_buildings.m_buffer[citizen.m_homeBuilding].Info.m_class.m_level);
                }
            }
            else if (buildingID != 0)
            {
                BuildingManager instance4 = Singleton<BuildingManager>.instance;
                if (state == "In")
                {
                    flags = flag;
                }
                else if (state == "Out")
                {
                    flags = CitizenUnit.Flags.Home;
                }
                if (flags != CitizenUnit.Flags.None && citizen.AddToUnits(citizenID, instance4.m_buildings.m_buffer[buildingID].m_citizenUnits, flags))
                {
                    citizen.m_homeBuilding = buildingID;
                    citizen.WealthLevel = Citizen.GetWealthLevel(instance4.m_buildings.m_buffer[citizen.m_homeBuilding].Info.m_class.m_level);
                } 
            }
        }
    }
}
