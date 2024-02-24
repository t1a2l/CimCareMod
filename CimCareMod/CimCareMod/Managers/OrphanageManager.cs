using System;
using System.Collections.Generic;
using System.Threading;
using ColossalFramework;
using ColossalFramework.Math;
using ICities;
using CimCareMod.AI;

namespace CimCareMod.Managers
{
    public class OrphanageManager : ThreadingExtensionBase
    {
        private const int DEFAULT_NUM_SEARCH_ATTEMPTS = 3;

        private static OrphanageManager instance;

        private readonly BuildingManager buildingManager;
        private readonly CitizenManager citizenManager;

        private readonly List<uint> familiesWithChildren;
        private readonly List<uint> orphanesMovingOut;

        private readonly HashSet<uint> childrenBeingProcessed;

        private Randomizer randomizer;

        private int running;

        private const int StepMask = 0xFF;
        private const int BuildingStepSize = 192;
        private ushort orphanCheckStep;

        private int orphanCheckCounter;

        public OrphanageManager()
        {
            Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager Created");
            instance = this;

            this.randomizer = new Randomizer((uint)73);
            this.citizenManager = Singleton<CitizenManager>.instance;
            this.buildingManager = Singleton<BuildingManager>.instance;

            this.familiesWithChildren = [];
            this.orphanesMovingOut = [];

            this.childrenBeingProcessed = [];
        }

        public static OrphanageManager getInstance()
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
            RefreshChildren();

            if ((frameIndex & StepMask) != 0)
            {
                return;
            }
        }

        private void RefreshChildren()
        {
            if (orphanCheckCounter > 0)
            {
                --orphanCheckCounter;
                return;
            }

            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 1)
            {
                return;
            }

            ushort step = orphanCheckStep;
            orphanCheckStep = (ushort)((step + 1) & StepMask);

            RefreshChildren(step);

            this.running = 0;
        }

        private void RefreshChildren(ushort step)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;

            ushort first = (ushort)(step * BuildingStepSize);
            ushort last = (ushort)((step + 1) * BuildingStepSize - 1);

            for (ushort i = first; i <= last; ++i)
            {
                var building = buildingManager.m_buildings.m_buffer[i];
                if (building.Info.GetAI() is not ResidentialBuildingAI && building.Info.GetAI() is not OrphanageAI)
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
                            Citizen citizen = citizenManager.m_citizens.m_buffer[citizenId];
                            if (citizen.m_flags.IsFlagSet(Citizen.Flags.Created) && this.validateChild(citizenId))
                            {
                                if (this.isMovingIn(citizenId))
                                {
                                    this.familiesWithChildren.Add(num);
                                    break;
                                }
                                else if (this.isMovingOut(citizenId))
                                {
                                    this.orphanesMovingOut.Add(num);
                                    break;
                                }
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

        public uint[] getFamilyWithChildren()
        {
            return this.getFamilyWithChildren(DEFAULT_NUM_SEARCH_ATTEMPTS);
        }

        public uint[] getOrphanesRoom()
        {
            return this.getOrphanesRoom(DEFAULT_NUM_SEARCH_ATTEMPTS);
        }

        public uint[] getFamilyWithChildren(int numAttempts)
        {
            Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getFamilyWithChildren -- Start");
            // Lock to prevent refreshing while running, otherwise bail
            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 1)
            {
                return null;
            }

            // Get random family that contains at least one child
            uint[] family = this.getFamilyWithChildrenInternal(numAttempts);
            if (family == null)
            {
                Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getFamilyWithChildren -- No Family");
                this.running = 0;
                return null;
            }

            // Mark all children in the family as being processed
            foreach (uint familyMember in family)
            {
                if (this.isChild(familyMember))
                {
                    this.childrenBeingProcessed.Add(familyMember);
                }
            }

            Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getFamilyWithChildren -- Finished: {0}", string.Join(", ", Array.ConvertAll(family, item => item.ToString())));
            this.running = 0;
            return family;
        }

        public uint[] getOrphanesRoom(int numAttempts)
        {
            Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getOrphanesRoom -- Start");
            // Lock to prevent refreshing while running, otherwise bail
            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 1)
            {
                return null;
            }

            // Get random room from the orphanage
            uint[] orphanage_room = this.getOrphanesRoomInternal(numAttempts);
            if (orphanage_room == null)
            {
                Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getOrphanesRoomInternal -- No orphans in this room");
                this.running = 0;
                return null;
            }

            // Mark orphan as being processed
            foreach (uint orphan in orphanage_room)
            {
                if (this.isChild(orphan))
                {
                    this.childrenBeingProcessed.Add(orphan);
                }
            }

            Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getOrphanesRoomInternal -- Finished: {0}", string.Join(", ", Array.ConvertAll(orphanage_room, item => item.ToString())));
            this.running = 0;
            return orphanage_room;
        }

        public void doneProcessingChild(uint childId)
        {
            this.childrenBeingProcessed.Remove(childId);
        }

        private uint[] getFamilyWithChildrenInternal(int numAttempts)
        {
            // Check to see if too many attempts already
            if (numAttempts <= 0)
            {
                return null;
            }

            // Get a random child
            uint familyId = this.fetchRandomFamilyWithChildren();
            Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getFamilyWithChildrenInternal -- Family Id: {0}", familyId);
            if (familyId == 0)
            {
                // No Family with Children to be located
                return null;
            }


            // Validate all children in the family and build an array of family members
            CitizenUnit familyWithChildren = this.citizenManager.m_units.m_buffer[familyId];
            uint[] family = new uint[5];
            bool childrenPresent = false;
            for (int i = 0; i < 5; i++)
            {
                uint familyMember = familyWithChildren.GetCitizen(i);
                if (this.isChild(familyMember))
                {
                    if (!this.validateChild(familyMember))
                    {
                        // This particular Child is no longer valid for some reason, call recursively with one less attempt
                        return this.getFamilyWithChildrenInternal(--numAttempts);
                    }
                    childrenPresent = true;
                }
                Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getFamilyWithChildrenInternal -- Family Member: {0}", familyMember);
                family[i] = familyMember;
            }

            if (!childrenPresent)
            {
                // No Children was found in this family (which is a bit weird), try again
                return this.getFamilyWithChildrenInternal(--numAttempts);
            }

            return family;
        }

        private uint[] getOrphanesRoomInternal(int numAttempts)
        {
            // Check to see if too many attempts already
            if (numAttempts <= 0)
            {
                return null;
            }

            // Get a random orphanage room
            uint orphanageRoomId = this.fetchRandomOrphanageRoom();

            Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getOrphanesRoomInternal -- Family Id: {0}", orphanageRoomId);
            if (orphanageRoomId == 0)
            {
                // No dorm apartment to be located
                return null;
            }

            // create an array of orphans to move out of the orphanage
            CitizenUnit orphanageRoom = this.citizenManager.m_units.m_buffer[orphanageRoomId];
            uint[] orphanage_room = new uint[] { 0, 0, 0, 0, 0 };
            for (int i = 0; i < 5; i++)
            {
                uint orphanId = orphanageRoom.GetCitizen(i);
                Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getOrphanesRoomInternal -- Family Member: {0}", orphanId);
                // not a child anymore -> move out
                if (orphanId != 0 && !this.isChild(orphanId))
                {
                    if (!this.validateChild(orphanId))
                    {
                        // This particular student is already being processed
                        return this.getOrphanesRoomInternal(--numAttempts);
                    }
                    Logger.LogInfo(Logger.LOG_CHILDREN, "OrphanageManager.getOrphanesRoomInternal -- Family Member: {0}, is not an orphan", orphanId);
                    orphanage_room[i] = orphanId;
                }
            }

            return orphanage_room;

        }

        private uint fetchRandomFamilyWithChildren()
        {
            if (this.familiesWithChildren.Count == 0)
            {
                return 0;
            }

            int index = this.randomizer.Int32((uint)this.familiesWithChildren.Count);
            var family = this.familiesWithChildren[index];
            this.familiesWithChildren.RemoveAt(index);
            return family;
        }

        private uint fetchRandomOrphanageRoom()
        {
            if (this.orphanesMovingOut.Count == 0)
            {
                return 0;
            }

            int index = this.randomizer.Int32((uint)this.orphanesMovingOut.Count);
            var family = this.orphanesMovingOut[index];
            this.orphanesMovingOut.RemoveAt(index);
            return family;
        }

        private bool validateChild(uint childId)
        {
            // Validate this Child is not already being processed
            if (this.childrenBeingProcessed.Contains(childId))
            {
                return false; // being processed 
            }

            return true; // not being processed
        }

        public bool isChild(uint childId)
        {
            if (childId == 0)
            {
                return false;
            }

            // Validate not dead
            if (this.citizenManager.m_citizens.m_buffer[childId].Dead)
            {
                return false;
            }

            // Validate is child or teenager
            Citizen.AgeGroup age_group = Citizen.GetAgeGroup(this.citizenManager.m_citizens.m_buffer[childId].Age);
            if (age_group != Citizen.AgeGroup.Child && age_group != Citizen.AgeGroup.Teen)
            {
                return false;
            }

            return true;
        }

        private bool isMovingIn(uint citizenId)
        {
            if (!isChild(citizenId))
            {
                return false;
            }

            ushort homeBuildingId = this.citizenManager.m_citizens.m_buffer[citizenId].m_homeBuilding;

            // no home move to orphanage
            if (homeBuildingId == 0)
            {
                return true;
            }

            Building homeBuilding = buildingManager.m_buildings.m_buffer[homeBuildingId];

            // if already living in an orphanage
            if (homeBuilding.Info.m_buildingAI is OrphanageAI)
            {
                return false;
            }

            return true;
        }

        private bool isMovingOut(uint citizenId)
        {
            // if this child is living in an orphanage we should check the entire room
            ushort homeBuildingId = this.citizenManager.m_citizens.m_buffer[citizenId].m_homeBuilding;
            Building homeBuilding = buildingManager.m_buildings.m_buffer[homeBuildingId];
            if (homeBuilding.Info.m_buildingAI is OrphanageAI)
            {
                return true;
            }

            return false;
        }


    }
}