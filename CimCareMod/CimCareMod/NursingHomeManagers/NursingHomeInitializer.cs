using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ColossalFramework;
using UnityEngine;
using CimCareMod.AI;
using CimCareMod.Utils;

namespace CimCareMod.NursingHomeManagers 
{
    public class NursingHomeInitializer : MonoBehaviour 
    {
        private const bool LOG_INITIALIZER = true;

        public const int LOADED_LEVEL_GAME = 6;
        public const int LOADED_LEVEL_ASSET_EDITOR = 19;

        private const String ELDER_CARE_NAME = "Eldercare 01";

        private static readonly Queue<IEnumerator> ACTION_QUEUE = new Queue<IEnumerator>();
        private static readonly object QUEUE_LOCK = new object();

        private int attemptingInitialization;
        private int numTimesSearchedForElderCare = 0;

        private bool initialized;
        private int numAttempts = 0;
        private int loadedLevel = -1;

        private void Awake() 
        {
            // Specify that this object should not be destroyed
            // Without this statement this object would be cleaned up very quickly
            DontDestroyOnLoad(this);
        }

        private void Start() 
        {
            Logger.logInfo(LOG_INITIALIZER, "NursingHomeInitializer Starting");
        }

        public void OnLevelWasLoaded(int level) 
        {
            this.loadedLevel = level;
            Logger.logInfo(LOG_INITIALIZER, "NursingHomeInitializer.OnLevelWasLoaded: {0}", level);
        }

        public void OnLevelUnloading() 
        {
            this.loadedLevel = -1;
            Logger.logInfo(LOG_INITIALIZER, "NursingHomeInitializer.OnLevelUnloading: {0}", this.loadedLevel);
        }

        public int getLoadedLevel() 
        {
            return this.loadedLevel;
        }

        private void Update() 
        {
            if (!this.initialized && this.loadedLevel != -1) 
            {
                // Still need initilization, check to see if already attempting initilization
                // Note: Not sure if it's possible for this method to be called more than once at a time, but locking just in case
                if (Interlocked.CompareExchange(ref this.attemptingInitialization, 1, 0) == 0) 
                {
                    this.attemptInitialization();
                }
            }
        }

        private void attemptInitialization() 
        {
            // Make sure not attempting initilization too many times -- This means the mod may not function properly, but it won't waste resources continuing to try
            if (this.numAttempts++ >= 20) 
            {
                Logger.logError("NursingHomeInitializer.attemptInitialization -- *** NURSING HOMES FUNCTIONALITY DID NOT INITLIZIE PRIOR TO GAME LOADING -- THE SENIOR CITIZEN CENTER MOD MAY NOT FUNCTION PROPERLY ***");
                // Set initilized so it won't keep trying
                this.SetInitialized();
            }

            // Check to see if initilization can start
            if (PrefabCollection<BuildingInfo>.LoadedCount() <= 0) 
            {
                this.attemptingInitialization = 0;
                return;
            }

            // Wait for the Eldercare Facility or other EldercareAI Building to load since all new Nursing Homes will copy its values
            BuildingInfo elderCareBuildingInfo = this.findElderCareBuildingInfo();
            if (elderCareBuildingInfo == null) 
            {
                this.attemptingInitialization = 0;
                return;
            }

            // Start loading
            Logger.logInfo(LOG_INITIALIZER, "NursingHomeInitializer.attemptInitialization -- Attempting Initialization");
            Singleton<LoadingManager>.instance.QueueLoadingAction(ActionWrapper(() => 
            {
                try 
                {
                    if (this.loadedLevel == LOADED_LEVEL_GAME || this.loadedLevel == LOADED_LEVEL_ASSET_EDITOR) 
                    {
                        this.StartCoroutine(this.initNursingHomes());
                        AddQueuedActionsToLoadingQueue();
                    }
                } 
                catch (Exception e) 
                {
                    Logger.logError("Error loading prefabs: {0}", e.Message);
                }
            }));

            // Set initilized
            this.SetInitialized();
        }

        private void SetInitialized() 
        {
            this.initialized = true;
            this.attemptingInitialization = 0;
            this.numTimesSearchedForElderCare = 0;
        }
        
        private BuildingInfo findElderCareBuildingInfo() 
        {
            // First check for the known Eldercare Facility
            BuildingInfo eldercareBuildingInfo = PrefabCollection<BuildingInfo>.FindLoaded(ELDER_CARE_NAME);
            if (eldercareBuildingInfo != null) 
            {
                return eldercareBuildingInfo;
            }

            // Try 5 times to search for the Eldercare Facility before giving up
            if (++this.numTimesSearchedForElderCare < 5) 
            {
                return null;
            }

            // Attempt to find a suitable Eldercare building that can be used as a template
            Logger.logInfo(LOG_INITIALIZER, "NursingHomeInitializer.findMedicalBuildingInfo -- Couldn't find the Elder Care asset after {0} tries, attempting to search for any Building with a ElderCareAI", this.numTimesSearchedForElderCare);
            for (uint i=0; (long) PrefabCollection<BuildingInfo>.LoadedCount() > (long) i; ++i) 
            {
                BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (buildingInfo != null && buildingInfo.GetService() == ItemClass.Service.HealthCare && !buildingInfo.m_buildingAI.IsWonder() && buildingInfo.m_buildingAI is EldercareAI) 
                {
                    Logger.logInfo(LOG_INITIALIZER, "NursingHomeInitializer.findMedicalBuildingInfo -- Using the {0} as a template instead of the Elder Care", buildingInfo);
                    return buildingInfo;
                }
            }

            // Return null to try again next time
            return null;
        }

        private IEnumerator initNursingHomes() 
        {
            Logger.logInfo(LOG_INITIALIZER, "NursingHomeInitializer.initNursingHomes");
            float capcityModifier = CimCareMod.getInstance().getOptionsManager().getNursingHomesCapacityModifier();
            uint index = 0U;
            int i = 0;
            BuildingInfo elderCareBuildingInfo = this.findElderCareBuildingInfo();
            while (!Singleton<LoadingManager>.instance.m_loadingComplete || i++ < 2)
            {
                Logger.logInfo(LOG_INITIALIZER, "NursingHomeInitializer.initNursingHomes -- Iteration: {0}", i);
                for (; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index) 
                {
                    BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);

                    // Check for replacement of AI
                    if (buildingInfo != null && buildingInfo.name.EndsWith("_Data") && buildingInfo.name.Contains("NH123"))
                    {
                        buildingInfo.m_class = elderCareBuildingInfo.m_class;
                        AiReplacementHelper.ApplyNewAIToBuilding(buildingInfo, "NursingHome");
                    }

                    // Check for updating capacity - Existing NHs will be updated on-load, this will set the data used for placing new homes
                    if (this.loadedLevel == LOADED_LEVEL_GAME && buildingInfo != null && buildingInfo.m_buildingAI is NursingHomeAI nursingHomeAI) 
                    {
                        nursingHomeAI.updateCapacity(capcityModifier);  
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }
        
        private static IEnumerator ActionWrapper(Action a) 
        {
            a();
            yield break;
        }

        private static void AddQueuedActionsToLoadingQueue() 
        {
            LoadingManager instance = Singleton<LoadingManager>.instance;
            object obj = typeof(LoadingManager).GetFieldByName("m_loadingLock").GetValue(instance);

            while (!Monitor.TryEnter(obj, SimulationManager.SYNCHRONIZE_TIMEOUT)) 
            {
            }
            try 
            {
                FieldInfo fieldByName = typeof(LoadingManager).GetFieldByName("m_mainThreadQueue");
                Queue<IEnumerator> queue1 = (Queue<IEnumerator>) fieldByName.GetValue(instance);
                if (queue1 == null) 
                {
                    return;
                }
                Queue<IEnumerator> queue2 = new Queue<IEnumerator>(queue1.Count + 1);
                queue2.Enqueue(queue1.Dequeue());
                while (!Monitor.TryEnter(QUEUE_LOCK, SimulationManager.SYNCHRONIZE_TIMEOUT));
                try 
                {
                    while (ACTION_QUEUE.Count > 0) 
                    {
                        queue2.Enqueue(ACTION_QUEUE.Dequeue());
                    }
                } 
                finally 
                {
                    Monitor.Exit(QUEUE_LOCK);
                }
                while (queue1.Count > 0) 
                {
                    queue2.Enqueue(queue1.Dequeue());
                }
                fieldByName.SetValue(instance, queue2);
            } 
            finally 
            {
                Monitor.Exit(obj);
            }
        }
    }
}