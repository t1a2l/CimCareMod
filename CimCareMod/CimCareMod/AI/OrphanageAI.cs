using System;
using System.Text;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using System.Threading;
using CimCareMod.Utils;
using CimCareMod.Managers;

namespace CimCareMod.AI 
{
    public class OrphanageAI : PlayerBuildingAI 
    {
        private static readonly float[] QUALITY_VALUES = { 0, 10, 25, 50, 75, 125 };

        private Randomizer randomizer = new Randomizer(97);

        [CustomizableProperty("Uneducated Workers", "Workers", 0)]
        public int numUneducatedWorkers = 5;

        [CustomizableProperty("Educated Workers", "Workers", 1)]
        public int numEducatedWorkers = 5;

        [CustomizableProperty("Well Educated Workers", "Workers", 2)]
        public int numWellEducatedWorkers = 5;

        [CustomizableProperty("Highly Educated Workers", "Workers", 3)]
        public int numHighlyEducatedWorkers = 4;

        [CustomizableProperty("Number of Rooms")]
        public int numRooms = 25;
        private float capacityModifier = 1.0f;

        [CustomizableProperty("Operation Radius")]
        public float operationRadius = 500f;

        [CustomizableProperty("Quality (values: 1-5 including 1 and 5)")]
        public int quality = 3;

	    public int m_healthCareAccumulation = 100;

        public int HealthCareAccumulation => UniqueFacultyAI.IncreaseByBonus(UniqueFacultyAI.FacultyBonus.Medicine, m_healthCareAccumulation);

        public override Color GetColor(ushort buildingId, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode)             
        {
            // This is a copy from ResidentialBuildingAI
            InfoManager.InfoMode infoModeCopy = infoMode;
            switch (infoModeCopy) {
                case InfoManager.InfoMode.Health:
                    if (Singleton<InfoManager>.instance.CurrentSubMode == InfoManager.SubInfoMode.WindPower)
			        {
				        if ((data.m_flags & Building.Flags.Active) != 0)
				        {
					        return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
				        }
				        return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
			        }
			        return base.GetColor(buildingId, ref data, infoMode, subInfoMode);
                case InfoManager.InfoMode.Density:
                    if (ShowConsumption(buildingId, ref data) && data.m_citizenCount != 0)
			        {
				        int children = data.m_children;
				        if (children == 0)
				        {
					        return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
				        }
				        return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_negativeColor;
			        }
			        return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                default:
                    switch (infoModeCopy - 17) 
                    {
                        case InfoManager.InfoMode.None:
                            if (this.ShowConsumption(buildingId, ref data)) 
                            {
                                return Color.Lerp(Singleton<InfoManager>.instance.m_properties.m_neutralColor, Color.Lerp(Singleton<ZoneManager>.instance.m_properties.m_zoneColors[2], Singleton<ZoneManager>.instance.m_properties.m_zoneColors[3], 0.5f) * 0.5f, (float) (0.200000002980232 + (double) Math.Max(0, this.quality - 1) * 0.200000002980232));
                            }
                            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                        case InfoManager.InfoMode.Water:
                            if (!this.ShowConsumption(buildingId, ref data) || (int) data.m_citizenCount == 0)
                                return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                            InfoManager.SubInfoMode currentSubMode = Singleton<InfoManager>.instance.CurrentSubMode;
                            int num4;
                            int num5;
                            if (currentSubMode == InfoManager.SubInfoMode.Default) {
                                num4 = (int) data.m_education1 * 100;
                                num5 = (int) data.m_teens + (int) data.m_youngs + (int) data.m_adults + (int) data.m_seniors;
                            } else if (currentSubMode == InfoManager.SubInfoMode.WaterPower) {
                                num4 = (int) data.m_education2 * 100;
                                num5 = (int) data.m_youngs + (int) data.m_adults + (int) data.m_seniors;
                            } else {
                                num4 = (int) data.m_education3 * 100;
                                num5 = (int) data.m_youngs * 2 / 3 + (int) data.m_adults + (int) data.m_seniors;
                            }
                            if (num5 != 0)
                                num4 = (num4 + (num5 >> 1)) / num5;
                            int num6 = Mathf.Clamp(num4, 0, 100);
                            return Color.Lerp(Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int) infoMode].m_negativeColor, Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int) infoMode].m_targetColor, (float) num6 * 0.01f);
                        default:
                            return this.handleOtherColors(buildingId, ref data, infoMode, subInfoMode);
                    }
            }
        }

        private Color handleOtherColors(ushort buildingId, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode) {
            switch (infoMode) 
            {
                case InfoManager.InfoMode.Happiness:
                    if (ShowConsumption(buildingId, ref data)) 
                    {
                        return Color.Lerp(Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int) infoMode].m_negativeColor, Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int) infoMode].m_targetColor, (float) Citizen.GetHappinessLevel((int) data.m_happiness) * 0.25f);
                    }
                    return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                case InfoManager.InfoMode.Garbage:
                    if (m_garbageAccumulation == 0)
                        return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    return base.GetColor(buildingId, ref data, infoMode, subInfoMode);
                default:
                    return base.GetColor(buildingId, ref data, infoMode, subInfoMode);
            }
        }

        public override void GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, float elevation)
	    {
		    mode = InfoManager.InfoMode.Health;
		    subMode = InfoManager.SubInfoMode.WindPower;
	    }

        public override ImmaterialResourceManager.ResourceData[] GetImmaterialResourceRadius(ushort buildingID, ref Building data)
	    {
		    return new ImmaterialResourceManager.ResourceData[1]
		    {
			    new ImmaterialResourceManager.ResourceData
			    {
				    m_resource = ImmaterialResourceManager.Resource.ChildCare,
				    m_radius = operationRadius
			    }
		    };
	    }

        public override void CreateBuilding(ushort buildingID, ref Building data)
	    {
            // Ensure quality is within bounds
            if (quality < 1) {
                quality = 1;
            } else if (quality > 5) {
                quality = 5;
            }
		    base.CreateBuilding(buildingID, ref data);
		    int workCount = numUneducatedWorkers + numEducatedWorkers + numWellEducatedWorkers + numHighlyEducatedWorkers;
		    Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, getModifiedCapacity(), workCount, 0, 0, 0);
        }

        public override void BuildingLoaded(ushort buildingID, ref Building data, uint version)
	    {
		    base.BuildingLoaded(buildingID, ref data, version);
           
            // Validate the capacity and adjust accordingly - but don't create new units, that will be done by EnsureCitizenUnits
            float capcityModifier = CimCareMod.getInstance().getOptionsManager().getOrphanagesCapacityModifier();
            this.updateCapacity(capcityModifier);
            this.validateCapacity(buildingID, ref data, false);

		    int workCount = numUneducatedWorkers + numEducatedWorkers + numWellEducatedWorkers + numHighlyEducatedWorkers;
		    EnsureCitizenUnits(buildingID, ref data, getModifiedCapacity(), workCount, 0, 0);
	    }

        public override void EndRelocating(ushort buildingID, ref Building data)
	    {
		    base.EndRelocating(buildingID, ref data);

            // Validate the capacity and adjust accordingly - but don't create new units, that will be done by EnsureCitizenUnits
            float capcityModifier = CimCareMod.getInstance().getOptionsManager().getOrphanagesCapacityModifier();
            this.updateCapacity(capcityModifier);
            this.validateCapacity(buildingID, ref data, false);

		    int workCount = numUneducatedWorkers + numEducatedWorkers + numWellEducatedWorkers + numHighlyEducatedWorkers;
		    EnsureCitizenUnits(buildingID, ref data, getModifiedCapacity(), workCount, 0, 0);
	    }

        protected override void ManualActivation(ushort buildingID, ref Building buildingData) 
        {
            int childCareAccumulation = HealthCareAccumulation;
		    if (childCareAccumulation != 0)
		    {
			    Vector3 position = buildingData.m_position;
			    position.y += m_info.m_size.y;
                Singleton<NotificationManager>.instance.AddEvent(NotificationEvent.Type.Happy, position, 1.5f);
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.GainHappiness, ImmaterialResourceManager.Resource.DeathCare, QUALITY_VALUES[quality], operationRadius);
            }
        }

        protected override void ManualDeactivation(ushort buildingID, ref Building buildingData) 
        {
            if ((buildingData.m_flags & Building.Flags.Collapsed) != 0)
		    {
			    Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.Happy, ImmaterialResourceManager.Resource.Abandonment, -buildingData.Width * buildingData.Length, 64f);
			    return;
		    }
		    int childCareAccumulation = HealthCareAccumulation;
		    if (childCareAccumulation != 0)
		    {
			    Vector3 position = buildingData.m_position;
			    position.y += m_info.m_size.y;
                Singleton<NotificationManager>.instance.AddEvent(NotificationEvent.Type.Sad, position, 1.5f);
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.LoseHappiness, ImmaterialResourceManager.Resource.DeathCare, -QUALITY_VALUES[quality], operationRadius);
		    }
        }

        public override void PlacementSucceeded()
	    {
		    base.PlacementSucceeded();
		    Singleton<BuildingManager>.instance.m_elderCareNotUsed?.Disable();
	    }

        public override void UpdateGuide(GuideController guideController)
	    {
		    Singleton<BuildingManager>.instance.m_childCareNotUsed?.Activate(guideController.m_childCareNotUsed, m_info);
		    base.UpdateGuide(guideController);
	    }

        public override float GetCurrentRange(ushort buildingID, ref Building data)
	    {
		    int num = data.m_productionRate;
		    if ((data.m_flags & (Building.Flags.Evacuating | Building.Flags.Active)) != Building.Flags.Active)
		    {
			    num = 0;
		    }
		    else if ((data.m_flags & Building.Flags.RateReduced) != 0)
		    {
			    num = Mathf.Min(num, 50);
		    }
		    int budget = Singleton<EconomyManager>.instance.GetBudget(m_info.m_class);
		    num = PlayerBuildingAI.GetProductionRate(num, budget);
		    return (float)num * operationRadius * 0.01f;
	    }

        protected override void HandleWorkAndVisitPlaces(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount, ref int aliveVisitorCount, ref int totalVisitorCount, ref int visitPlaceCount) 
        {
            workPlaceCount += numUneducatedWorkers + numEducatedWorkers + numWellEducatedWorkers + numHighlyEducatedWorkers;
		    GetWorkBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount);
		    HandleWorkPlaces(buildingID, ref buildingData, numUneducatedWorkers, numEducatedWorkers, numWellEducatedWorkers, numHighlyEducatedWorkers, ref behaviour, aliveWorkerCount, totalWorkerCount);
        }

        public override void SimulationStep(ushort buildingID, ref Building buildingData, ref Building.Frame frameData) 
        {
            base.SimulationStep(buildingID, ref buildingData, ref frameData);
        }

        protected  override void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
	    {
			Citizen.BehaviourData behaviour = default(Citizen.BehaviourData);
			int aliveCount = 0;
			int totalCount = 0;
            int homeCount = 0;
            int aliveWorkerCount = 0;
	        int totalWorkerCount = 0;
            int aliveHomeCount = 0;
            int emptyHomeCount = 0;

            GetHomeBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveCount, ref totalCount, ref homeCount, ref aliveHomeCount, ref emptyHomeCount);
            GetWorkBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount);

            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            byte district = districtManager.GetDistrict(buildingData.m_position);
            DistrictPolicies.Services policies = districtManager.m_districts.m_buffer[(int) district].m_servicePolicies;

            DistrictPolicies.Taxation taxationPolicies = districtManager.m_districts.m_buffer[(int) district].m_taxationPolicies;
            DistrictPolicies.CityPlanning cityPlanning = districtManager.m_districts.m_buffer[(int) district].m_cityPlanningPolicies;
            DistrictPolicies.Special special = districtManager.m_districts.m_buffer[(int) district].m_specialPolicies;

            districtManager.m_districts.m_buffer[(int) district].m_servicePoliciesEffect |= policies & (DistrictPolicies.Services.PowerSaving | DistrictPolicies.Services.WaterSaving | DistrictPolicies.Services.SmokeDetectors | DistrictPolicies.Services.PetBan | DistrictPolicies.Services.Recycling | DistrictPolicies.Services.SmokingBan | DistrictPolicies.Services.ExtraInsulation | DistrictPolicies.Services.NoElectricity | DistrictPolicies.Services.OnlyElectricity);

            this.GetConsumptionRates(new Randomizer((int) buildingID), 100, out int electricityConsumption, out int waterConsumption, out int sewageAccumulation, out int garbageAccumulation, out int incomeAccumulation);

            int modifiedElectricityConsumption = 1 + (electricityConsumption * behaviour.m_electricityConsumption + 9999) / 10000;
            waterConsumption = 1 + (waterConsumption * behaviour.m_waterConsumption + 9999) / 10000;
            int modifiedSewageAccumulation = 1 + (sewageAccumulation * behaviour.m_sewageAccumulation + 9999) / 10000;
            garbageAccumulation = (garbageAccumulation * behaviour.m_garbageAccumulation + 9999) / 10000;
            int modifiedIncomeAccumulation = 0;

            // Handle Heating
            int heatingConsumption = 0;
            if (modifiedElectricityConsumption != 0 && districtManager.IsPolicyLoaded(DistrictPolicies.Policies.ExtraInsulation)) 
            {
                if ((policies & DistrictPolicies.Services.ExtraInsulation) != DistrictPolicies.Services.None) 
                {
                    heatingConsumption = Mathf.Max(1, modifiedElectricityConsumption * 3 + 8 >> 4);
                } 
                else
                    heatingConsumption = Mathf.Max(1, modifiedElectricityConsumption + 2 >> 2);
            }

            // Handle Recylcing and Pets
            if (garbageAccumulation != 0) 
            {
                if ((policies & DistrictPolicies.Services.Recycling) != DistrictPolicies.Services.None) 
                {
                    garbageAccumulation = (policies & DistrictPolicies.Services.PetBan) == DistrictPolicies.Services.None ? Mathf.Max(1, garbageAccumulation * 85 / 100) : Mathf.Max(1, garbageAccumulation * 7650 / 10000);
                    modifiedIncomeAccumulation = modifiedIncomeAccumulation * 95 / 100;
                } 
                else if ((policies & DistrictPolicies.Services.PetBan) != DistrictPolicies.Services.None) 
                {
                    garbageAccumulation = Mathf.Max(1, garbageAccumulation * 90 / 100);
                }
            }

            if ((int) buildingData.m_fireIntensity == 0) 
            {
                int maxMail = 100;
                int mailAccumulation = 1;
                int commonConsumptionValue = this.HandleCommonConsumption(buildingID, ref buildingData, ref frameData, ref modifiedElectricityConsumption, ref heatingConsumption, ref waterConsumption, ref modifiedSewageAccumulation, ref garbageAccumulation, ref mailAccumulation, maxMail, policies);
                buildingData.m_flags |= Building.Flags.Active;
            } 
            else 
            {
                // Handle on fire
                modifiedElectricityConsumption = 0;
                heatingConsumption = 0;
                waterConsumption = 0;
                modifiedSewageAccumulation = 0;
                garbageAccumulation = 0;
                buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.Electricity | Notification.Problem1.Water | Notification.Problem1.Sewage | Notification.Problem1.Flood | Notification.Problem1.Heating);
                buildingData.m_flags &= ~Building.Flags.Active;
            }


            buildingData.m_customBuffer1 = (ushort)aliveCount;
            int health = 0;
            float radius = (float) (buildingData.Width + buildingData.Length) * 2.5f;
            if (behaviour.m_healthAccumulation != 0) 
            {
                if (aliveCount != 0) 
                {
                    health = (behaviour.m_healthAccumulation + (aliveCount >> 1)) / aliveCount;
                }
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.ChildCare, behaviour.m_healthAccumulation, buildingData.m_position, radius);
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Health, behaviour.m_healthAccumulation, buildingData.m_position, radius);
            }
            Logger.LogInfo(Logger.LOG_SIMULATION, "OrphanageAI.SimulationStepActive -- health: {0}", health);

            // Get the Wellbeing
            int wellbeing = 0;
            if (behaviour.m_wellbeingAccumulation != 0) 
            {
                if (aliveCount != 0) 
                {
                    wellbeing = (behaviour.m_wellbeingAccumulation + (aliveCount >> 1)) / aliveCount;
                }
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Wellbeing, behaviour.m_wellbeingAccumulation, buildingData.m_position, radius);
            }
            Logger.LogInfo(Logger.LOG_SIMULATION, "OrphanageAI.SimulationStepActive -- wellbeing: {0}", wellbeing);

            if (aliveCount != 0) 
            {
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Density, aliveCount, buildingData.m_position, radius);
            }

            // Calculate Happiness
            int happiness = Citizen.GetHappiness(health, wellbeing);
            if ((buildingData.m_problems & Notification.Problem1.MajorProblem) != Notification.Problem1.None) 
            {
                happiness -= happiness >> 1;
            } 
            else if (buildingData.m_problems != Notification.Problem1.None) 
            {
                happiness -= happiness >> 2;
            }
            Logger.LogInfo(Logger.LOG_SIMULATION, "OrphanageAI.SimulationStepActive -- happiness: {0}", happiness);

            buildingData.m_health = (byte) health;
            buildingData.m_happiness = (byte) happiness;
            buildingData.m_citizenCount = (byte) aliveCount;
            buildingData.m_education1 = (byte) behaviour.m_education1Count;
            buildingData.m_education2 = (byte) behaviour.m_education2Count;
            buildingData.m_education3 = (byte) behaviour.m_education3Count;
            buildingData.m_teens = (byte) behaviour.m_teenCount;
            buildingData.m_youngs = (byte) behaviour.m_youngCount;
            buildingData.m_adults = (byte) behaviour.m_adultCount;
            buildingData.m_seniors = (byte) behaviour.m_seniorCount;

            HandleSick(buildingID, ref buildingData, ref behaviour, totalWorkerCount + totalCount);
            HandleDead(buildingID, ref buildingData, ref behaviour, totalWorkerCount + totalCount);

            // Handle Crime and Fire Factors
            int crimeAccumulation = behaviour.m_crimeAccumulation / (3 * getModifiedCapacity());
            if ((policies & DistrictPolicies.Services.RecreationalUse) != DistrictPolicies.Services.None) 
            {
                crimeAccumulation = crimeAccumulation * 3 + 3 >> 2;
            }
            this.HandleCrime(buildingID, ref buildingData, crimeAccumulation, aliveCount);
            int crimeBuffer = (int) buildingData.m_crimeBuffer;
            int crimeRate;
            if (aliveCount != 0) 
            {
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Density, aliveCount, buildingData.m_position, radius);
                // num1
                int fireFactor = (behaviour.m_educated0Count * 30 + behaviour.m_educated1Count * 15 + behaviour.m_educated2Count * 10) / aliveCount + 50;
                if ((int) buildingData.m_crimeBuffer > aliveCount * 40) 
                {
                    fireFactor += 30;
                } 
                else if ((int) buildingData.m_crimeBuffer > aliveCount * 15) 
                {
                    fireFactor += 15;
                } 
                else if ((int) buildingData.m_crimeBuffer > aliveCount * 5) 
                {
                    fireFactor += 10;
                }
                buildingData.m_fireHazard = (byte) fireFactor;
                crimeRate = (crimeBuffer + (aliveCount >> 1)) / aliveCount;
            } 
            else 
            {
                buildingData.m_fireHazard = (byte) 0;
                crimeRate = 0;
            }

            districtManager.m_districts.m_buffer[(int) district].AddResidentialData(ref behaviour, aliveCount, health, happiness, crimeRate, homeCount, aliveHomeCount, emptyHomeCount, (int) this.m_info.m_class.m_level, modifiedElectricityConsumption, heatingConsumption, waterConsumption, modifiedSewageAccumulation, garbageAccumulation, modifiedIncomeAccumulation, Mathf.Min(100, (int) buildingData.m_garbageBuffer / 50), (int) buildingData.m_waterPollution * 100 / (int) byte.MaxValue, this.m_info.m_class.m_subService);

            // Handle custom maintenance in addition to the standard maintenance handled in the base class
            handleAdditionalMaintenanceCost(ref buildingData);
		    
            base.SimulationStepActive(buildingID, ref buildingData, ref frameData);
            HandleFire(buildingID, ref buildingData, ref frameData, policies);
	    }

        protected override void ProduceGoods(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount) 
        {
            base.ProduceGoods(buildingID, ref buildingData, ref frameData, productionRate, finalProductionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);    
            if (finalProductionRate == 0)
		    {
			    return;
		    }
            getOccupancyDetails(ref buildingData, out int numResidents, out int numRoomsOccupied);

            // Make sure there are no problems
            if ((buildingData.m_problems & (Notification.Problem1.MajorProblem | Notification.Problem1.Electricity | Notification.Problem1.ElectricityNotConnected | Notification.Problem1.Fire | Notification.Problem1.NoWorkers | Notification.Problem1.Water | Notification.Problem1.WaterNotConnected | Notification.Problem1.RoadNotConnected | Notification.Problem1.TurnedOff)) != Notification.Problem1.None) 
            {
                return;
            }

            // Fetch a Child
            OrphanageManager orphanageManager = OrphanageManager.getInstance();
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;

            uint[] familyWithChildren = orphanageManager.getFamilyWithChildren();
            if (familyWithChildren == null) 
            {
                // No Family Located
                return;
            }

            Logger.LogInfo(Logger.LOG_PRODUCTION, "------------------------------------------------------------");
            Logger.LogInfo(Logger.LOG_PRODUCTION, "OrphanageAI.ProduceGoods -- Family: {0}", string.Join(", ", Array.ConvertAll(familyWithChildren, item => item.ToString())));

            // Check move in chance
            NumWorkers numWorkers = getNumWorkers(ref behaviour);
            bool shouldMoveIn = MoveInProbabilityHelper.checkIfShouldMoveIn(familyWithChildren, ref buildingData, ref randomizer, operationRadius, quality, ref numWorkers);

            if(shouldMoveIn)
            {
                // get all the students in the family
                List<uint> childrenList = new();
                foreach (uint familyMember in familyWithChildren) 
                {
                    if (orphanageManager.isChild(familyMember))
                    {
                        Logger.LogInfo(Logger.LOG_PRODUCTION, "OrphanageAI.ProduceGoods -- familyMember: {0} is a child", familyMember);
                        childrenList.Add(familyMember);
                    }
                }

                // add a child to a room randomly
                for(int i = 0; i < childrenList.Count; i++)
                {
                    uint orphanageRoomId = getCitizenUnit(ref buildingData);
                    if (orphanageRoomId == 0) 
                    {
                        return;
                    }
                    uint childId = childrenList[i];
                    Logger.LogInfo(Logger.LOG_PRODUCTION, "OrphanageAI.ProduceGoods -- Moving In: {0}", childId);
                    citizenManager.m_citizens.m_buffer[childId].SetHome(childId, buildingID, orphanageRoomId);
                    orphanageManager.doneProcessingChild(childId);
                }
            }

            // Fetch orpahns who needs to move out of the orpahange
            uint[] OrphanesRoomOrphans = orphanageManager.getOrphanesRoom();
            if (OrphanesRoomOrphans != null) 
            {
                Logger.LogInfo(Logger.LOG_PRODUCTION, "------------------------------------------------------------");
                Logger.LogInfo(Logger.LOG_PRODUCTION, "OrphanageAI.ProduceGoods -- OrphanesRoomOrphans: {0}", string.Join(", ", Array.ConvertAll(OrphanesRoomOrphans, item => item.ToString())));

                foreach (uint orphanId in OrphanesRoomOrphans)
                {
                    Logger.LogInfo(Logger.LOG_PRODUCTION, "OrphanageAI.ProduceGoods -- Moving Out: {0}", orphanId);
                    if(orphanId != 0)
                    {
                        citizenManager.m_citizens.m_buffer[orphanId].SetHome(orphanId, 0, 0);
                        orphanageManager.doneProcessingChild(orphanId);
                    } 
                }
            }
        }

        public override string GetLocalizedTooltip()
	    {
		    return LocaleFormatter.FormatGeneric("AIINFO_WATER_CONSUMPTION", GetWaterConsumption() * 16) + Environment.NewLine + LocaleFormatter.FormatGeneric("AIINFO_ELECTRICITY_CONSUMPTION", GetElectricityConsumption() * 16);
	    }

        public override string GetLocalizedStats(ushort buildingID, ref Building data) 
        {
            getOccupancyDetails(ref data, out int numResidents, out int numRoomsOccupied);
            // Get Worker Data
            Citizen.BehaviourData workerBehaviourData = new Citizen.BehaviourData();
            int aliveWorkerCount = 0;
            int totalWorkerCount = 0;
            GetWorkBehaviour(buildingID, ref data, ref workerBehaviourData, ref aliveWorkerCount, ref totalWorkerCount);
            
            // Build Stats
            // TODO: Localize!!!
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(string.Format("Uneducated Workers: {0} of {1}", workerBehaviourData.m_educated0Count, numUneducatedWorkers));
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(string.Format("Educated Workers: {0} of {1}", workerBehaviourData.m_educated1Count, numEducatedWorkers));
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(string.Format("Well Educated Workers: {0} of {1}", workerBehaviourData.m_educated2Count, numWellEducatedWorkers));
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(string.Format("Highly Educated Workers: {0} of {1}", workerBehaviourData.m_educated3Count, numHighlyEducatedWorkers));
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(string.Format("Orphanage Quality: {0}", quality));
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(string.Format("Rooms Occupied: {0} of {1}", numRoomsOccupied, getModifiedCapacity()));
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(string.Format("Number of Children: {0}", numResidents));
            return stringBuilder.ToString();
        }

        private int getCustomMaintenanceCost(ref Building buildingData) 
        {
            int originalAmount = -(this.m_maintenanceCost * 100);

            CimCareMod mod = CimCareMod.getInstance();
            if (mod == null) 
            {
                return 0;
            }

            OptionsManager optionsManager = mod.getOptionsManager();
            if (optionsManager == null) 
            {
                return 0;
            }

            getOccupancyDetails(ref buildingData, out int numResidents, out int numRoomsOccupied);
            float capacityModifier = (float) numRoomsOccupied / getModifiedCapacity();
            int modifiedAmount = (int) (originalAmount * capacityModifier);

            int amount = 0;
            switch (optionsManager.getOrphanagesIncomeModifier()) 
            {
                case OptionsManager.IncomeValues.FULL_MAINTENANCE:
                    return 0;
                case OptionsManager.IncomeValues.HALF_MAINTENANCE:
                    amount = modifiedAmount / 2;
                    break;
                case OptionsManager.IncomeValues.NO_MAINTENANCE:
                    amount = modifiedAmount;
                    break;
                case OptionsManager.IncomeValues.NORMAL_PROFIT:
                    amount = modifiedAmount * 2;
                    break;
                case OptionsManager.IncomeValues.DOUBLE_DOUBLE:
                    amount = -originalAmount + (modifiedAmount * 4);
                    break;
                case OptionsManager.IncomeValues.DOUBLE_PROFIT:
                    amount = modifiedAmount * 3;
                    break;
            }

            if(amount == 0) 
            {
                return 0;
            }
            
            Singleton<EconomyManager>.instance.m_EconomyWrapper.OnGetMaintenanceCost(ref amount, this.m_info.m_class.m_service, this.m_info.m_class.m_subService, this.m_info.m_class.m_level);
            Logger.LogInfo(Logger.LOG_INCOME, "getCustomMaintenanceCost - building: {0} - calculated maintenance amount: {1}", buildingData.m_buildIndex, amount);

            return amount;
        }

        public void handleAdditionalMaintenanceCost(ref Building buildingData) 
        {
            int amount = getCustomMaintenanceCost(ref buildingData);
            if (amount == 0) 
            {
                return;
            }

            int productionRate = (int) buildingData.m_productionRate;
            int budget = Singleton<EconomyManager>.instance.GetBudget(this.m_info.m_class);
            amount = amount / 100;
            amount = productionRate * budget / 100 * amount / 100;
            Logger.LogInfo(Logger.LOG_INCOME, "getCustomMaintenanceCost - building: {0} - adjusted maintenance amount: {1}", buildingData.m_buildIndex, amount);

            if ((buildingData.m_flags & Building.Flags.Original) == Building.Flags.None && amount != 0) 
            {
                int result = Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, amount, this.m_info.m_class);
            }
        }
  
        private uint getCitizenUnit(ref Building data) 
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            uint citizenUnitIndex = data.m_citizenUnits;
            while ((int) citizenUnitIndex != 0) 
            {
                uint nextCitizenUnitIndex = citizenManager.m_units.m_buffer[citizenUnitIndex].m_nextUnit;
                if ((citizenManager.m_units.m_buffer[citizenUnitIndex].m_flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None) 
                {
                    for (int i = 0; i < 5; i++)
                    {
                        uint citizenId = citizenManager.m_units.m_buffer[citizenUnitIndex].GetCitizen(i);
                        if(citizenId == 0)
                        {
                            return citizenUnitIndex;
                        }
                    }
                }
                citizenUnitIndex = nextCitizenUnitIndex;
            }

            return 0;
        }

        private NumWorkers getNumWorkers(ref Citizen.BehaviourData workerBehaviourData) 
        {
            NumWorkers numWorkers = new()
            {
                maxNumUneducatedWorkers = numUneducatedWorkers,
                numUneducatedWorkers = workerBehaviourData.m_educated0Count,
                maxNumEducatedWorkers = numEducatedWorkers,
                numEducatedWorkers = workerBehaviourData.m_educated1Count,
                maxNumWellEducatedWorkers = numWellEducatedWorkers,
                numWellEducatedWorkers = workerBehaviourData.m_educated2Count,
                maxNumHighlyEducatedWorkers = numHighlyEducatedWorkers,
                numHighlyEducatedWorkers = workerBehaviourData.m_educated3Count
            };
            return numWorkers;
        }

        private int GetAverageResidentRequirement(ushort buildingID, ref Building data, ImmaterialResourceManager.Resource resource) 
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            uint citizenUnit = data.m_citizenUnits;
            uint numCitizenUnits = citizenManager.m_units.m_size;
            int counter = 0;
            int requirement1 = 0;
            int requirement2 = 0;
            while ((int) citizenUnit != 0) 
            {
                uint num5 = citizenManager.m_units.m_buffer[citizenUnit].m_nextUnit;
                if ((citizenManager.m_units.m_buffer[citizenUnit].m_flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None) 
                {
                    int residentRequirement1 = 0;
                    int residentRequirement2 = 0;
                    for (int index = 0; index < 5; ++index) 
                    {
                        uint citizen = citizenManager.m_units.m_buffer[citizenUnit].GetCitizen(index);
                        if ((int) citizen != 0 && !citizenManager.m_citizens.m_buffer[citizen].Dead) 
                        {
                            residentRequirement1 += GetResidentRequirement(resource, ref citizenManager.m_citizens.m_buffer[citizen]);
                            ++residentRequirement2;
                        }
                    }
                    if (residentRequirement2 == 0) 
                    {
                        requirement1 += 100;
                        ++requirement2;
                    } 
                    else 
                    {
                        requirement1 += residentRequirement1;
                        requirement2 += residentRequirement2;
                    }
                }
                citizenUnit = num5;
                if (++counter > numCitizenUnits) 
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }
            if (requirement2 != 0)
                return (requirement1 + (requirement2 >> 1)) / requirement2;
            return 0;
        }

        private int GetResidentRequirement(ImmaterialResourceManager.Resource resource, ref Citizen citizen) 
        {
            switch (resource) 
            {
                case ImmaterialResourceManager.Resource.HealthCare:
                    return Citizen.GetHealthCareRequirement(Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age));
                case ImmaterialResourceManager.Resource.FireDepartment:
                    return Citizen.GetFireDepartmentRequirement(Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age));
                case ImmaterialResourceManager.Resource.PoliceDepartment:
                    return Citizen.GetPoliceDepartmentRequirement(Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age));
                case ImmaterialResourceManager.Resource.EducationElementary:
                    Citizen.AgePhase agePhase1 = Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age);
                    if (agePhase1 < Citizen.AgePhase.Teen0)
                        return Citizen.GetEducationRequirement(agePhase1);
                    return 0;
                case ImmaterialResourceManager.Resource.EducationHighSchool:
                    Citizen.AgePhase agePhase2 = Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age);
                    if (agePhase2 >= Citizen.AgePhase.Teen0 && agePhase2 < Citizen.AgePhase.Young0)
                        return Citizen.GetEducationRequirement(agePhase2);
                    return 0;
                case ImmaterialResourceManager.Resource.EducationUniversity:
                    Citizen.AgePhase agePhase3 = Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age);
                    if (agePhase3 >= Citizen.AgePhase.Young0)
                        return Citizen.GetEducationRequirement(agePhase3);
                    return 0;
                case ImmaterialResourceManager.Resource.DeathCare:
                    return Citizen.GetDeathCareRequirement(Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age));
                case ImmaterialResourceManager.Resource.PublicTransport:
                    return Citizen.GetTransportRequirement(Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age));
                case ImmaterialResourceManager.Resource.Entertainment:
                    return Citizen.GetEntertainmentRequirement(Citizen.GetAgePhase(citizen.EducationLevel, citizen.Age));
                default:
                    return 100;
            }
        }

        public override float GetEventImpact(ushort buildingID, ref Building data, ImmaterialResourceManager.Resource resource, float amount) 
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.BurnedDown)) != Building.Flags.None)
                return 0.0f;
            switch (resource) 
            {
                case ImmaterialResourceManager.Resource.HealthCare:
                    int residentRequirement1 = GetAverageResidentRequirement(buildingID, ref data, resource);
                    int local1;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local1);
                    int num1 = ImmaterialResourceManager.CalculateResourceEffect(local1, residentRequirement1, 500, 20, 40);
                    return Mathf.Clamp((float) (ImmaterialResourceManager.CalculateResourceEffect(local1 + Mathf.RoundToInt(amount), residentRequirement1, 500, 20, 40) - num1) / 20f, -1f, 1f);
                case ImmaterialResourceManager.Resource.FireDepartment:
                    int residentRequirement2 = GetAverageResidentRequirement(buildingID, ref data, resource);
                    int local2;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local2);
                    int num2 = ImmaterialResourceManager.CalculateResourceEffect(local2, residentRequirement2, 500, 20, 40);
                    return Mathf.Clamp((float) (ImmaterialResourceManager.CalculateResourceEffect(local2 + Mathf.RoundToInt(amount), residentRequirement2, 500, 20, 40) - num2) / 20f, -1f, 1f);
                case ImmaterialResourceManager.Resource.PoliceDepartment:
                    int residentRequirement3 = GetAverageResidentRequirement(buildingID, ref data, resource);
                    int local3;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local3);
                    int num3 = ImmaterialResourceManager.CalculateResourceEffect(local3, residentRequirement3, 500, 20, 40);
                    return Mathf.Clamp((float) (ImmaterialResourceManager.CalculateResourceEffect(local3 + Mathf.RoundToInt(amount), residentRequirement3, 500, 20, 40) - num3) / 20f, -1f, 1f);
                case ImmaterialResourceManager.Resource.EducationElementary:
                case ImmaterialResourceManager.Resource.EducationHighSchool:
                case ImmaterialResourceManager.Resource.EducationUniversity:
                    int residentRequirement4 = GetAverageResidentRequirement(buildingID, ref data, resource);
                    int local4;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local4);
                    int num4 = ImmaterialResourceManager.CalculateResourceEffect(local4, residentRequirement4, 500, 20, 40);
                    return Mathf.Clamp((float) (ImmaterialResourceManager.CalculateResourceEffect(local4 + Mathf.RoundToInt(amount), residentRequirement4, 500, 20, 40) - num4) / 20f, -1f, 1f);
                case ImmaterialResourceManager.Resource.DeathCare:
                    int residentRequirement5 = GetAverageResidentRequirement(buildingID, ref data, resource);
                    int local5;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local5);
                    int num5 = ImmaterialResourceManager.CalculateResourceEffect(local5, residentRequirement5, 500, 10, 20);
                    return Mathf.Clamp((float) (ImmaterialResourceManager.CalculateResourceEffect(local5 + Mathf.RoundToInt(amount), residentRequirement5, 500, 10, 20) - num5) / 20f, -1f, 1f);
                case ImmaterialResourceManager.Resource.PublicTransport:
                    int residentRequirement6 = GetAverageResidentRequirement(buildingID, ref data, resource);
                    int local6;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local6);
                    int num6 = ImmaterialResourceManager.CalculateResourceEffect(local6, residentRequirement6, 500, 20, 40);
                    return Mathf.Clamp((float) (ImmaterialResourceManager.CalculateResourceEffect(local6 + Mathf.RoundToInt(amount), residentRequirement6, 500, 20, 40) - num6) / 20f, -1f, 1f);
                case ImmaterialResourceManager.Resource.NoisePollution:
                    int local7;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local7);
                    int num7 = local7 * 100 / (int) byte.MaxValue;
                    return Mathf.Clamp((float) (Mathf.Clamp(local7 + Mathf.RoundToInt(amount), 0, (int) byte.MaxValue) * 100 / (int) byte.MaxValue - num7) / 50f, -1f, 1f);
                case ImmaterialResourceManager.Resource.Entertainment:
                    int residentRequirement7 = GetAverageResidentRequirement(buildingID, ref data, resource);
                    int local8;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local8);
                    int num8 = ImmaterialResourceManager.CalculateResourceEffect(local8, residentRequirement7, 500, 30, 60);
                    return Mathf.Clamp((float) (ImmaterialResourceManager.CalculateResourceEffect(local8 + Mathf.RoundToInt(amount), residentRequirement7, 500, 30, 60) - num8) / 30f, -1f, 1f);
                case ImmaterialResourceManager.Resource.Abandonment:
                    int local9;
                    Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(resource, data.m_position, out local9);
                    int num9 = ImmaterialResourceManager.CalculateResourceEffect(local9, 15, 50, 10, 20);
                    return Mathf.Clamp((float) (ImmaterialResourceManager.CalculateResourceEffect(local9 + Mathf.RoundToInt(amount), 15, 50, 10, 20) - num9) / 50f, -1f, 1f);
                default:
                    return 0f;
            }
        }
        public override float GetEventImpact(ushort buildingID, ref Building data, NaturalResourceManager.Resource resource, float amount) 
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.BurnedDown)) != Building.Flags.None)
                return 0.0f;
            if (resource != NaturalResourceManager.Resource.Pollution)
                return 0f;
            byte groundPollution;
            Singleton<NaturalResourceManager>.instance.CheckPollution(data.m_position, out groundPollution);
            int num = (int) groundPollution * 100 / (int) byte.MaxValue;
            return Mathf.Clamp((float) (Mathf.Clamp((int) groundPollution + Mathf.RoundToInt(amount), 0, (int) byte.MaxValue) * 100 / (int) byte.MaxValue - num) / 50f, -1f, 1f);
        }

        public void GetConsumptionRates(Randomizer randomizer, int productionRate, out int electricityConsumption, out int waterConsumption, out int sewageAccumulation, out int garbageAccumulation, out int incomeAccumulation) 
        {
            electricityConsumption = 0;
            waterConsumption = 0;
            sewageAccumulation = 0;
            garbageAccumulation = 0;
            incomeAccumulation = 0;
            switch (quality) 
            {
                case 1:
                    electricityConsumption = 18;
                    waterConsumption = 40;
                    sewageAccumulation = 40;
                    garbageAccumulation = 40;
                    break;
                case 2:
                    electricityConsumption = 18;
                    waterConsumption = 40;
                    sewageAccumulation = 40;
                    garbageAccumulation = 30;
                    break;
                case 3:
                    electricityConsumption = 16;
                    waterConsumption = 35;
                    sewageAccumulation = 35;
                    garbageAccumulation = 20;
                    break;
                case 4:
                    electricityConsumption = 16;
                    waterConsumption = 35;
                    sewageAccumulation = 35;
                    garbageAccumulation = 20;
                    break;
                case 5:
                    electricityConsumption = 14;
                    waterConsumption = 30;
                    sewageAccumulation = 30;
                    garbageAccumulation = 15;
                    break;
            }

            if (electricityConsumption != 0)
                electricityConsumption = Mathf.Max(100, productionRate * electricityConsumption + randomizer.Int32(70U)) / 100;
            if (waterConsumption != 0) 
            {
                int waterAndSewageConsumptionModifier = randomizer.Int32(70U);
                waterConsumption = Mathf.Max(100, productionRate * waterConsumption + waterAndSewageConsumptionModifier) / 100;
                if (sewageAccumulation != 0)
                    sewageAccumulation = Mathf.Max(100, productionRate * sewageAccumulation + waterAndSewageConsumptionModifier) / 100;
            } 
            else if (sewageAccumulation != 0)
                sewageAccumulation = Mathf.Max(100, productionRate * sewageAccumulation + randomizer.Int32(70U)) / 100;
            if (garbageAccumulation != 0)
                garbageAccumulation = Mathf.Max(100, productionRate * garbageAccumulation + randomizer.Int32(70U)) / 100;
            if (incomeAccumulation == 0)
                return;
            incomeAccumulation = productionRate * incomeAccumulation;
        }

        private void getOccupancyDetails(ref Building data, out int numResidents, out int numRoomsOccupied) 
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            uint citizenUnitIndex = data.m_citizenUnits;
            uint numCitizenUnits = citizenManager.m_units.m_size;
            numResidents = 0;
            numRoomsOccupied = 0;
            int counter = 0;

            // Calculate number of occupied rooms and total number of residents
            while ((int) citizenUnitIndex != 0) 
            {
                uint nextCitizenUnitIndex = citizenManager.m_units.m_buffer[citizenUnitIndex].m_nextUnit;
                if ((citizenManager.m_units.m_buffer[citizenUnitIndex].m_flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None) 
                {
                    bool occupied = false;
                    for (int index = 0; index < 5; ++index) 
                    {
                        uint citizenId = citizenManager.m_units.m_buffer[citizenUnitIndex].GetCitizen(index);
                        if (citizenId != 0) 
                        {
                            occupied = true;
                            numResidents++;
                        }
                    }
                    if (occupied) {
                        numRoomsOccupied++;
                    }
                }
                citizenUnitIndex = nextCitizenUnitIndex;
                if (++counter > numCitizenUnits) {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public void updateCapacity(float newCapacityModifier) 
        {
            Logger.LogInfo(Logger.LOG_OPTIONS, "OrphanageAI.updateCapacity -- Updating capacity with modifier: {0}", newCapacityModifier);
            // Set the capcityModifier and check to see if the value actually changes
            if (Interlocked.Exchange(ref capacityModifier, newCapacityModifier) == newCapacityModifier) 
            {
                // Capcity has already been set to this value, nothing to do
                Logger.LogInfo(Logger.LOG_OPTIONS, "OrphanageAI.updateCapacity -- Skipping capacity change because the value was already set");
                return;
            }
        }

        private int getModifiedCapacity() 
        {
            return (capacityModifier > 0 ? (int) (numRooms * capacityModifier) : numRooms);
        }

        public void validateCapacity(ushort buildingId, ref Building data, bool shouldCreateRooms) 
        {
            int numRoomsExpected = getModifiedCapacity();
            
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            uint citizenUnitIndex = data.m_citizenUnits;
            uint lastCitizenUnitIndex = 0;
            int numRoomsFound = 0;

            // Count the number of rooms
            while ((int) citizenUnitIndex != 0) 
            {
                uint nextCitizenUnitIndex = citizenManager.m_units.m_buffer[citizenUnitIndex].m_nextUnit;
                if ((citizenManager.m_units.m_buffer[citizenUnitIndex].m_flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None) 
                {
                    numRoomsFound++;
                }
                lastCitizenUnitIndex = citizenUnitIndex;
                citizenUnitIndex = nextCitizenUnitIndex;
            }

            Logger.LogInfo(Logger.LOG_CAPACITY_MANAGEMENT, "OrphanageAI.validateCapacity -- Checking Expected Capacity {0} vs Current Capacity {1} for Building {2}", numRoomsExpected, numRoomsFound, buildingId);
            // Check to see if the correct amount of rooms are present, otherwise adjust accordingly
            if (numRoomsFound == numRoomsExpected) 
            {
                return;
            } 
            else if (numRoomsFound < numRoomsExpected) 
            {
                if (shouldCreateRooms) 
                {
                    // Only create rooms after a building is already loaded, otherwise let EnsureCitizenUnits to create them
                    createRooms((numRoomsExpected - numRoomsFound), buildingId, ref data, lastCitizenUnitIndex);
                }
            } 
            else 
            {
                deleteRooms((numRoomsFound - numRoomsExpected), buildingId, ref data);
            }
        }

        private void createRooms(int numRoomsToCreate, ushort buildingId, ref Building data, uint lastCitizenUnitIndex) 
        {
            Logger.LogInfo(Logger.LOG_CAPACITY_MANAGEMENT, "OrphanageAI.createRooms -- Creating {0} Rooms", numRoomsToCreate);
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            citizenManager.CreateUnits(out uint firstUnit, ref Singleton<SimulationManager>.instance.m_randomizer, buildingId, 0, numRoomsToCreate, 0, 0, 0, 0);
            citizenManager.m_units.m_buffer[lastCitizenUnitIndex].m_nextUnit = firstUnit;
        }

        private void deleteRooms(int numRoomsToDelete, ushort buildingId, ref Building data) 
        {
            Logger.LogInfo(Logger.LOG_CAPACITY_MANAGEMENT, "OrphanageAI.deleteRooms -- Deleting {0} Rooms", numRoomsToDelete);
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            
            // Always start with the second to avoid loss of pointer from the building to the first unit
            uint prevUnit = data.m_citizenUnits;
            uint citizenUnitIndex = citizenManager.m_units.m_buffer[data.m_citizenUnits].m_nextUnit;

            // First try to delete empty rooms
            while (numRoomsToDelete > 0 && (int) citizenUnitIndex != 0) 
            {
                bool deleted = false;
                uint nextCitizenUnitIndex = citizenManager.m_units.m_buffer[citizenUnitIndex].m_nextUnit;
                if ((citizenManager.m_units.m_buffer[citizenUnitIndex].m_flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None) 
                {
                    if (citizenManager.m_units.m_buffer[citizenUnitIndex].Empty()) 
                    {
                        deleteRoom(citizenUnitIndex, ref citizenManager.m_units.m_buffer[citizenUnitIndex], prevUnit);
                        numRoomsToDelete--;
                        deleted = true;
                    }
                }
                if(!deleted) {
                    prevUnit = citizenUnitIndex;
                }
                citizenUnitIndex = nextCitizenUnitIndex;
            }

            // Check to see if enough rooms were deleted
            if(numRoomsToDelete == 0) 
            {
                return;
            }

            Logger.LogInfo(Logger.LOG_CAPACITY_MANAGEMENT, "OrphanageAI.deleteRooms -- Deleting {0} Occupied Rooms", numRoomsToDelete);
            // Still need to delete more rooms so start deleting rooms with people in them...
            // Always start with the second to avoid loss of pointer from the building to the first unit
            prevUnit = data.m_citizenUnits;
            citizenUnitIndex = citizenManager.m_units.m_buffer[data.m_citizenUnits].m_nextUnit;

            // Delete any rooms still available until the correct number is acheived
            while (numRoomsToDelete > 0 && (int) citizenUnitIndex != 0) 
            {
                bool deleted = false;
                uint nextCitizenUnitIndex = citizenManager.m_units.m_buffer[citizenUnitIndex].m_nextUnit;
                if ((citizenManager.m_units.m_buffer[citizenUnitIndex].m_flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None) 
                {
                    deleteRoom(citizenUnitIndex, ref citizenManager.m_units.m_buffer[citizenUnitIndex], prevUnit);
                    numRoomsToDelete--;
                    deleted = true;
                }
                if (!deleted) 
                {
                    prevUnit = citizenUnitIndex;
                }
                citizenUnitIndex = nextCitizenUnitIndex;
            }
        }

        private void deleteRoom(uint unit, ref CitizenUnit data, uint prevUnit) 
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;

            // Update the pointer to bypass this unit
            citizenManager.m_units.m_buffer[prevUnit].m_nextUnit = data.m_nextUnit;

            // Release all the citizens
            releaseUnitCitizen(data.m_citizen0, ref data);
            releaseUnitCitizen(data.m_citizen1, ref data);
            releaseUnitCitizen(data.m_citizen2, ref data);
            releaseUnitCitizen(data.m_citizen3, ref data);
            releaseUnitCitizen(data.m_citizen4, ref data);

            // Release the Unit
            data = new CitizenUnit();
            citizenManager.m_units.ReleaseItem(unit);
        }

        private void releaseUnitCitizen(uint citizen, ref CitizenUnit data) 
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;

            if ((int) citizen == 0) 
            {
                return;
            }
            if ((data.m_flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None) 
            {
                citizenManager.m_citizens.m_buffer[citizen].m_homeBuilding = 0;
            }
            if ((data.m_flags & (CitizenUnit.Flags.Work | CitizenUnit.Flags.Student)) != CitizenUnit.Flags.None) 
            {
                citizenManager.m_citizens.m_buffer[citizen].m_workBuilding = 0;
            }
            if ((data.m_flags & CitizenUnit.Flags.Visit) != CitizenUnit.Flags.None) 
            {
                citizenManager.m_citizens.m_buffer[citizen].m_visitBuilding = 0;
            }
            if ((data.m_flags & CitizenUnit.Flags.Vehicle) == CitizenUnit.Flags.None) 
            {
                return;
            }
            citizenManager.m_citizens.m_buffer[citizen].m_vehicle = 0;
        }

    }
}