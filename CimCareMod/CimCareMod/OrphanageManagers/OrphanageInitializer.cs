using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ColossalFramework;
using UnityEngine;
using CimCareMod.Utils;
using CimCareMod.AI;

namespace CimCareMod.OrphanageManagers {
    public class OrphanageInitializer : MonoBehaviour {
        private const bool LOG_INITIALIZER = true;

        public const int LOADED_LEVEL_GAME = 6;
        public const int LOADED_LEVEL_ASSET_EDITOR = 19;

        private const String CHILD_CARE_NAME = "Child Health Center 01";

        private static readonly Queue<IEnumerator> ACTION_QUEUE = new Queue<IEnumerator>();
        private static readonly object QUEUE_LOCK = new();

        private int attemptingInitialization;
        private int numTimesSearchedForChildCare = 0;

        private bool initialized;
        private int numAttempts = 0;
        private int loadedLevel = -1;

        private void Awake() {
            // Specify that this object should not be destroyed
            // Without this statement this object would be cleaned up very quickly
            DontDestroyOnLoad(this);
        }

        private void Start() 
        {
            Logger.logInfo(LOG_INITIALIZER, "OrphanageInitializer Starting");
        }

        public void OnLevelWasLoaded(int level) 
        {
            this.loadedLevel = level;
            Logger.logInfo(LOG_INITIALIZER, "OrphanageInitializer.OnLevelWasLoaded: {0}", level);
        }

        public void OnLevelUnloading() 
        {
            this.loadedLevel = -1;
            Logger.logInfo(LOG_INITIALIZER, "OrphanageInitializer.OnLevelUnloading: {0}", this.loadedLevel);
        }

        public int getLoadedLevel() 
        {
            return this.loadedLevel;
        }

        private void Update() {
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
                Logger.logError("OrphanageInitializer.attemptInitialization -- *** ORPHANAGES FUNCTIONALITY DID NOT INITLIZIE PRIOR TO GAME LOADING -- THE ORPHANAGE CENTER MOD MAY NOT FUNCTION PROPERLY ***");
                // Set initilized so it won't keep trying
                this.SetInitialized();
            }

            // Check to see if initilization can start
            if (PrefabCollection<BuildingInfo>.LoadedCount() <= 0) 
            {
                this.attemptingInitialization = 0;
                return;
            }

            // Wait for the Childcare Facility or other ChildcareAI Building to load since all new Orphanages will copy its values
            BuildingInfo childCareBuildingInfo = this.findChildCareBuildingInfo();
            if (childCareBuildingInfo == null) 
            {
                this.attemptingInitialization = 0;
                return;
            }

            // Start loading
            Logger.logInfo(LOG_INITIALIZER, "OrphanageInitializer.attemptInitialization -- Attempting Initialization");
            Singleton<LoadingManager>.instance.QueueLoadingAction(ActionWrapper(() => 
            {
                try 
                {
                    if (this.loadedLevel == LOADED_LEVEL_GAME || this.loadedLevel == LOADED_LEVEL_ASSET_EDITOR) 
                    {
                        this.StartCoroutine(this.initOrphanages());
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
            this.numTimesSearchedForChildCare = 0;
        }
        
        private BuildingInfo findChildCareBuildingInfo() 
        {
            // First check for the known Childcare Facility
            BuildingInfo childcareBuildingInfo = PrefabCollection<BuildingInfo>.FindLoaded(CHILD_CARE_NAME);
            if (childcareBuildingInfo != null) 
            {
                return childcareBuildingInfo;
            }

            // Try 5 times to search for the Childcare Facility before giving up
            if (++this.numTimesSearchedForChildCare < 5) 
            {
                return null;
            }

            // Attempt to find a suitable childcare building that can be used as a template
            Logger.logInfo(LOG_INITIALIZER, "OrphanageInitializer.findChildCareBuildingInfo -- Couldn't find the Child Care asset after {0} tries, attempting to search for any Building with a ChildCareAI", this.numTimesSearchedForChildCare);
            for (uint i=0; (long) PrefabCollection<BuildingInfo>.LoadedCount() > (long) i; ++i) 
            {
                BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (buildingInfo != null && buildingInfo.GetService() == ItemClass.Service.HealthCare && !buildingInfo.m_buildingAI.IsWonder() && buildingInfo.m_buildingAI is ChildcareAI) 
                {
                    Logger.logInfo(LOG_INITIALIZER, "OrphanageInitializer.findChildCareBuildingInfo -- Using the {0} as a template instead of the Child Care", buildingInfo);
                    return buildingInfo;
                }
            }

            // Return null to try again next time
            return null;
        }

        private IEnumerator initOrphanages() 
        {
            Logger.logInfo(LOG_INITIALIZER, "OrphanageInitializer.initOrphanages");
            float capcityModifier = CimCareMod.getInstance().getOptionsManager().getOrphanagesCapacityModifier();
            uint index = 0U;
            int i = 0;
            BuildingInfo childCareBuildingInfo = this.findChildCareBuildingInfo();
            while (!Singleton<LoadingManager>.instance.m_loadingComplete || i++ < 2) 
            {
                Logger.logInfo(LOG_INITIALIZER, "OrphanageInitializer.initOrphanages -- Iteration: {0}", i);
                for (; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index) 
                {
                    BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);

                    // Check for replacement of AI
                    if (buildingInfo != null && buildingInfo.name.Contains("OR123"))
                    {
                        buildingInfo.m_class = childCareBuildingInfo.m_class;
                        AiReplacementHelper.ApplyNewAIToBuilding(buildingInfo, "Orphanage");
                    }

                    // Check for updating capacity - Existing Orphanages will be updated on-load, this will set the data used for placing new orphanages
                    if (this.loadedLevel == LOADED_LEVEL_GAME && buildingInfo != null && buildingInfo.m_buildingAI is OrphanageAI orphanagAI) 
                    {
                        orphanagAI.updateCapacity(capcityModifier);  
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
                if (queue1 == null) {
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