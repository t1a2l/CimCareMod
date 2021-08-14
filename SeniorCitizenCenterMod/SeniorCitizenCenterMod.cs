using ICities;
using CitiesHarmony.API;
using UnityEngine;
using SeniorCitizenCenterMod.Utils;

namespace SeniorCitizenCenterMod
{
    public class SeniorCitizenCenterMod : LoadingExtensionBase, IUserMod, ISerializableData  {
        private const bool LOG_BASE = true;

        private GameObject nursingHomeInitializerObj;
        private NursingHomeInitializer nursingHomeInitializer;
        private OptionsManager optionsManager = new OptionsManager();

        public new IManagers managers { get; }

        private static SeniorCitizenCenterMod instance;
        string IUserMod.Name => "Senior Citizen Center Mod";

        string IUserMod.Description => "Enables functionality for Nursing Home Assets to function as working Nursing Homes.";
        
        public void OnEnabled() {
             HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public static SeniorCitizenCenterMod getInstance() {
            return instance;
        }

        public NursingHomeInitializer getNursingHomeInitializer()
	    {
		    return nursingHomeInitializer;
	    }

        public OptionsManager getOptionsManager() {
            return this.optionsManager;
        }

        public void OnSettingsUI(UIHelperBase helper) {
            this.optionsManager.initialize(helper);
            this.optionsManager.loadOptions();
        }

        public override void OnCreated(ILoading loading) {
            Logger.logInfo(LOG_BASE, "SeniorCitizenCenterMod Created");
            instance = this;
            base.OnCreated(loading);
            if (!(this.nursingHomeInitializerObj != null)) {
                this.nursingHomeInitializerObj = new GameObject("SeniorCitizenCenterMod Nursing Homes");
                this.nursingHomeInitializer = this.nursingHomeInitializerObj.AddComponent<NursingHomeInitializer>();
            }
        }

        public override void OnLevelUnloading()
	    {
		    base.OnLevelUnloading();
		    nursingHomeInitializer?.OnLevelUnloading();
	    }

        public override void OnLevelLoaded(LoadMode mode) {
            Logger.logInfo(true, "SeniorCitizenCenterMod Level Loaded: {0}", mode);
		    base.OnLevelLoaded(mode);
		    switch (mode)
		    {
		        case LoadMode.NewGame:
		        case LoadMode.LoadGame:
			        nursingHomeInitializer?.OnLevelWasLoaded(6);
			    break;
		        case LoadMode.NewAsset:
		        case LoadMode.LoadAsset:
			        nursingHomeInitializer?.OnLevelWasLoaded(19);
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
