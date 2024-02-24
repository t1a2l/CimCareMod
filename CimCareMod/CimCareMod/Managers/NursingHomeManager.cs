using System;
using System.Collections.Generic;
using System.Threading;
using ColossalFramework;
using ColossalFramework.Math;
using ICities;
using CimCareMod.AI;

namespace CimCareMod.Managers 
{
    public class NursingHomeManager : ThreadingExtensionBase 
    {
        private const int DEFAULT_NUM_SEARCH_ATTEMPTS = 3;

        private static NursingHomeManager instance;

        private readonly BuildingManager buildingManager;
        private readonly CitizenManager citizenManager;

        private readonly List<uint> familiesWithSeniors;

        private readonly HashSet<uint> seniorCitizensBeingProcessed;

        private Randomizer randomizer;

        private int running;

        private const int StepMask = 0xFF;
        private const int BuildingStepSize = 192;
        private ushort seniorCheckStep;

        private int seniorCheckCounter;

        public NursingHomeManager() 
        {
            Logger.LogInfo(Logger.LOG_SENIORS, "NursingHomeManager Created");
            instance = this;

            this.randomizer = new Randomizer((uint) 73);
            this.citizenManager = Singleton<CitizenManager>.instance;
            this.buildingManager = Singleton<BuildingManager>.instance;

            this.familiesWithSeniors = [];

            this.seniorCitizensBeingProcessed = [];
        }

        public static NursingHomeManager getInstance() 
        {
            return instance;
        }

        public override void OnBeforeSimulationFrame()
        {
            uint currentFrame = SimulationManager.instance.m_currentFrameIndex;
            ProcessFrame(currentFrame);
        }

        public void ProcessFrame(uint frameIndex)
        {
            RefreshSeniorCitizens();

            if ((frameIndex & StepMask) != 0)
            {
                return;
            }
        }

        private void RefreshSeniorCitizens()
        {
            if (seniorCheckCounter > 0)
            {
                --seniorCheckCounter;
                return;
            }

            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 1)
            {
                return;
            }

            ushort step = seniorCheckStep;
            seniorCheckStep = (ushort)((step + 1) & StepMask);

            RefreshSeniorCitizens(step);

            this.running = 0;
        }

        private void RefreshSeniorCitizens(ushort step) 
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;

            ushort first = (ushort)(step * BuildingStepSize);
            ushort last = (ushort)((step + 1) * BuildingStepSize - 1);

            for (ushort i = first; i <= last; ++i)
            {
                var building = buildingManager.m_buildings.m_buffer[i];
                if (building.Info.GetAI() is not ResidentialBuildingAI && building.Info.GetAI() is not NursingHomeAI)
                {
                    continue;
                }
                if ((building.m_flags & Building.Flags.Created) == 0)
                {
                    continue;
                }

                uint num = building.m_citizenUnits;
                int num2 = 0;
                while (num != 0)
                {
                    var citizenUnit = instance.m_units.m_buffer[num];
                    uint nextUnit = citizenUnit.m_nextUnit;
                    if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Home) != 0 && !citizenUnit.Empty())
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            uint citizenId = citizenUnit.GetCitizen(j);
                            if (citizenManager.m_citizens.m_buffer[citizenId].m_flags.IsFlagSet(Citizen.Flags.Created) && this.isSenior(citizenId) && this.validateSeniorCitizen(citizenId))
                            {
                                this.familiesWithSeniors.Add(num);
                                break;
                            }
                        }
                    }
                    num = nextUnit;
                    if (++num2 > 524288)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }
        }

        public uint[] getFamilyWithSenior() 
        {
            return this.getFamilyWithSenior(DEFAULT_NUM_SEARCH_ATTEMPTS);
        }

        public uint[] getFamilyWithSenior(int numAttempts) 
        {
            Logger.LogInfo(Logger.LOG_SENIORS, "NursingHomeManager.getFamilyWithSenior -- Start");
            // Lock to prevent refreshing while running, otherwise bail
            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 1) 
            {
                return null;
            }

            // Get random family that contains at least one senior
            uint[] family = this.getFamilyWithSeniorInternal(numAttempts);
            if (family == null) 
            {
                Logger.LogInfo(Logger.LOG_SENIORS, "NursingHomeManager.getFamilyWithSenior -- No Family");
                this.running = 0;
                return null;
            }

            // Mark all seniors in the family as being processed
            foreach (uint familyMember in family) 
            {
                if (this.isSenior(familyMember)) 
                {
                    this.seniorCitizensBeingProcessed.Add(familyMember);
                }
            }


            Logger.LogInfo(Logger.LOG_SENIORS, "NursingHomeManager.getFamilyWithSenior -- Finished: {0}", string.Join(", ", Array.ConvertAll(family, item => item.ToString())));
            this.running = 0;
            return family;
        }

        public void doneProcessingSenior(uint seniorCitizenId) 
        {
            this.seniorCitizensBeingProcessed.Remove(seniorCitizenId);
        }

        private uint[] getFamilyWithSeniorInternal(int numAttempts) 
        {
            // Check to see if too many attempts already
            if (numAttempts <= 0) 
            {
                return null;
            }

            // Get a random senior citizen
            uint familyId = this.fetchRandomFamilyWithSeniorCitizen();
            Logger.LogInfo(Logger.LOG_SENIORS, "NursingHomeManager.getFamilyWithSeniorInternal -- Family Id: {0}", familyId);
            if (familyId == 0) 
            {
                // No Family with Senior Citizens to be located
                return null;
            }


            // Validate all seniors in the family and build an array of family members
            CitizenUnit familyWithSenior = this.citizenManager.m_units.m_buffer[familyId];
            uint[] family = new uint[5];
            bool seniorPresent = false;
            for (int i = 0; i < 5; i++) 
            {
                uint familyMember = familyWithSenior.GetCitizen(i);
                if (this.isSenior(familyMember)) 
                {
                    if (!this.validateSeniorCitizen(familyMember)) 
                    {
                        // This particular Senior Citizen is no longer valid for some reason, call recursively with one less attempt
                        return this.getFamilyWithSeniorInternal(--numAttempts);
                    }
                    seniorPresent = true;
                }
                Logger.LogInfo(Logger.LOG_SENIORS, "NursingHomeManager.getFamilyWithSeniorInternal -- Family Member: {0}", familyMember);
                family[i] = familyMember;
            }

            if (!seniorPresent) 
            {
                // No Senior was found in this family (which is a bit weird), try again
                return this.getFamilyWithSeniorInternal(--numAttempts);
            }

            return family;
        }

        private uint fetchRandomFamilyWithSeniorCitizen() 
        {
            if (this.familiesWithSeniors.Count == 0)
            {
                return 0;
            }

            int index = this.randomizer.Int32((uint)this.familiesWithSeniors.Count);
            var family = this.familiesWithSeniors[index];
            this.familiesWithSeniors.RemoveAt(index);
            return family;
        }

        public bool isSenior(uint seniorCitizenId) 
        {
            if (seniorCitizenId == 0) 
            {
                return false;
            }

            // Validate not dead
            if (this.citizenManager.m_citizens.m_buffer[seniorCitizenId].Dead) 
            {
                return false;
            }

            // Validate Age
            int age = this.citizenManager.m_citizens.m_buffer[seniorCitizenId].Age;
            if (age <= Citizen.AGE_LIMIT_ADULT || age >= Citizen.AGE_LIMIT_SENIOR) 
            {
                return false;
            }

            return true;
        }

        private bool validateSeniorCitizen(uint seniorCitizenId) 
        {
            // Validate this Senior is not already being processed
            if (this.seniorCitizensBeingProcessed.Contains(seniorCitizenId)) 
            {
                return false;
            }

            // Validate not homeless
            ushort homeBuildingId = this.citizenManager.m_citizens.m_buffer[seniorCitizenId].m_homeBuilding;
            if (homeBuildingId == 0) 
            {
                return false;
            }

            // Validate not already living in a nursing home
            if (this.buildingManager.m_buildings.m_buffer[homeBuildingId].Info.m_buildingAI is NursingHomeAI) 
            {
                return false;
            }

            return true;
        }
    }
}