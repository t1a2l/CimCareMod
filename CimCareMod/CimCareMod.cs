using ICities;
using CitiesHarmony.API;
using CimCareMod.Utils;

namespace CimCareMod
{
    public class CimCareMod : LoadingExtensionBase, IUserMod, ISerializableData  
    {
        private const bool LOG_BASE = true;

        private OptionsManager optionsManager = new OptionsManager();

        public new IManagers managers { get; }

        private static CimCareMod instance;
        string IUserMod.Name => "Cim Care Mod";

        string IUserMod.Description => "Enables functionality for Nursing Home Assets and Orphanage Assets to function as working Nursing Homes and Orphanages";
        
        public void OnEnabled() 
        {
             HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled() 
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public static CimCareMod getInstance() 
        {
            return instance;
        }

        public OptionsManager getOptionsManager() 
        {
            return this.optionsManager;
        }

        public void OnSettingsUI(UIHelperBase helper) 
        {
            this.optionsManager.initialize(helper);
            this.optionsManager.loadOptions();
        }

        public override void OnCreated(ILoading loading) 
        {
            Logger.logInfo(LOG_BASE, "CimCareMod Created");
            instance = this;
        }

        public byte[] LoadData(string id) 
        {
            Logger.logInfo(Logger.LOG_OPTIONS, "Load Data: {0}", id);
            return null;
        }

        public void SaveData(string id, byte[] data) 
        {
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
