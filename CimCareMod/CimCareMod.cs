using ICities;
using CitiesHarmony.API;
using UnityEngine;
using CimCareMod.Utils;
using CimCareMod.NursingHomeManagers;
using CimCareMod.OrphanageManagers;

namespace CimCareMod
{
    public class CimCareMod : LoadingExtensionBase, IUserMod, ISerializableData  {
        private const bool LOG_BASE = true;

        private GameObject nursingHomeInitializerObj;
        private GameObject orphanageInitializerObj;

        private NursingHomeInitializer nursingHomeInitializer;
        private OrphanageInitializer orphanageInitializer;

        private OptionsManager optionsManager = new OptionsManager();

        public new IManagers managers { get; }

        private static CimCareMod instance;
        string IUserMod.Name => "Cim Care Mod";

        string IUserMod.Description => "Enables functionality for Nursing Home Assets and Orphanage Assets to function as working Nursing Homes and Orphanages";
        
        public void OnEnabled() {
             HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public static CimCareMod getInstance() {
            return instance;
        }

        public NursingHomeInitializer getNursingHomeInitializer()
	    {
		    return nursingHomeInitializer;
	    }

        public OrphanageInitializer getOrphanageInitializer()
	    {
		    return orphanageInitializer;
	    }

        public OptionsManager getOptionsManager() {
            return this.optionsManager;
        }

        public void OnSettingsUI(UIHelperBase helper) {
            this.optionsManager.initialize(helper);
            this.optionsManager.loadOptions();
        }

        public override void OnCreated(ILoading loading) {
            Logger.logInfo(LOG_BASE, "CimCareMod Created");
            instance = this;
            base.OnCreated(loading);
            if (!(this.nursingHomeInitializerObj != null)) {
                this.nursingHomeInitializerObj = new GameObject("CimCareMod Nursing Homes");
                this.nursingHomeInitializer = this.nursingHomeInitializerObj.AddComponent<NursingHomeInitializer>();
            }
            if (!(this.orphanageInitializerObj != null)) {
                this.orphanageInitializerObj = new GameObject("CimCareMod Nursing Homes");
                this.orphanageInitializer = this.orphanageInitializerObj.AddComponent<OrphanageInitializer>();
            }
        }

        public override void OnLevelUnloading()
	    {
		    base.OnLevelUnloading();
		    nursingHomeInitializer?.OnLevelUnloading();
            orphanageInitializer?.OnLevelUnloading();
	    }

        public override void OnLevelLoaded(LoadMode mode) {
            Logger.logInfo(true, "CimCareMod Level Loaded: {0}", mode);
		    base.OnLevelLoaded(mode);
		    switch (mode)
		    {
		        case LoadMode.NewGame:
		        case LoadMode.LoadGame:
			        nursingHomeInitializer?.OnLevelWasLoaded(6);
                    orphanageInitializer?.OnLevelWasLoaded(6);
			    break;
		        case LoadMode.NewAsset:
		        case LoadMode.LoadAsset:
			        nursingHomeInitializer?.OnLevelWasLoaded(19);
                    orphanageInitializer?.OnLevelWasLoaded(19);
			    break;
		    }
        }

        public override void OnReleased() {
            base.OnReleased();
            if (!HarmonyHelper.IsHarmonyInstalled)
            {
                return;
            }
            if (this.nursingHomeInitializerObj != null) {
                Object.Destroy(this.nursingHomeInitializerObj);
            }
            if (this.nursingHomeInitializerObj != null) {
                Object.Destroy(this.nursingHomeInitializerObj);
            }
        }

        public byte[] LoadData(string id) {
            Logger.logInfo(Logger.LOG_OPTIONS, "Load Data: {0}", id);
            return null;
        }

        public void SaveData(string id, byte[] data) {
            Logger.logInfo(Logger.LOG_OPTIONS, "Save Data: {0} -- {1}", id, data);
        }

        public string[] EnumerateData()
	    {
		    return null;
	    }

        public void EraseData(string id)
	    {
	    }

	    public bool LoadGame(string saveName)
	    {
		    return false;
	    }

	    public bool SaveGame(string saveName)
	    {
		    return false;
	    }
    }
}
