using ICities;
using ColossalFramework.UI;
using System.IO;
using System.Xml.Serialization;
using System;
using ColossalFramework;
using CimCareMod.AI;

namespace CimCareMod.Utils 
{
    public class OptionsManager 
    {
        private static readonly string[] CAPACITY_LABELS = new string[] { "Give Em Room (x0.5)", "Realistic (x1.0)", "Just a bit More (x1.5)", "Gameplay over Realism (x2.0)", "Who needs Living Space? (x2.5)", "Pack em like Sardines! (x3.0)" };
        private static readonly float[] CAPACITY_VALUES = new float[] { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f };

        private static readonly string[] NURSING_HOMES_INCOME_LABELS = new string[] { "Communisim is Key (Full Maintenance)", "Seniors can Help a Little (Half Maintenance at Full Capacity)", "Make the Seniors Pay (No Maintenance at Full Capacity)", "Nursing Homes should be Profitable (Maintenance becomes Profit at Full Capacity)", "Twice the Pain, Twice the Gain (2x Maintenance, 2x Profit)", "Show me the Money! (Profit x2, Normal Maintenance)" };
        private static readonly string[] ORPHANAGES_INCOME_LABELS = new string[] { "Communisim is Key (Full Maintenance)", "Orphans allowance (Half Maintenance at Full Capacity)", "Orphans will work on the street (No Maintenance at Full Capacity)", "Orphanages should be Profitable (Maintenance becomes Profit at Full Capacity)", "Twice the Pain, Twice the Gain (2x Maintenance, 2x Profit)", "Show me the Money! (Profit x2, Normal Maintenance)" };

        public enum IncomeValues 
        {
            FULL_MAINTENANCE = 1,
            HALF_MAINTENANCE = 2,
            NO_MAINTENANCE = 3,
            NORMAL_PROFIT = 4,
            DOUBLE_DOUBLE = 5,
            DOUBLE_PROFIT = 6
        };

        private UIDropDown nursingHomesCapacityDropDown;
        private UIDropDown orphanagesCapacityDropDown;
        private float nursingHomesCapacityModifier = -1.0f;
        private float orphanagesCapacityModifier = -1.0f;

        private UIDropDown nursingHomesIncomeDropDown;
        private UIDropDown orphanagesIncomeDropDown;

        private IncomeValues nursingHomesIncomeValue = IncomeValues.NO_MAINTENANCE;
        private IncomeValues orphanagesIncomeValue = IncomeValues.NO_MAINTENANCE;

        public void initialize(UIHelperBase helper) 
        {
            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.initialize -- Initializing Menu Options");
            UIHelperBase group = helper.AddGroup("Cim Care Settings");
            this.nursingHomesCapacityDropDown = (UIDropDown) group.AddDropdown("Nursing Homes Capacity Modifier", CAPACITY_LABELS, 1, handleCapacityChange);
            this.nursingHomesIncomeDropDown = (UIDropDown) group.AddDropdown("Nursing Homes Income Modifier", NURSING_HOMES_INCOME_LABELS, 2, handleIncomeChange);
            group.AddSpace(2);
            this.orphanagesCapacityDropDown = (UIDropDown) group.AddDropdown("Orphanages Capacity Modifier", CAPACITY_LABELS, 1, handleCapacityChange);
            this.orphanagesIncomeDropDown = (UIDropDown) group.AddDropdown("Orphanages Income Modifier", ORPHANAGES_INCOME_LABELS, 2, handleIncomeChange);
            group.AddSpace(5);
            group.AddButton("Save", saveOptions);
        }

        private void handleCapacityChange(int newSelection) 
        {
            // Do nothing until Save is pressed
        }

        private void handleIncomeChange(int newSelection) 
        {
            // Do nothing until Save is pressed
        }

        public void updateNursingHomesCapacity() 
        {
            this.updateNursingHomesCapacity(this.nursingHomesCapacityModifier);
        }

        public void updateOrphanagesCapacity() 
        {
            this.updateOrphanagesCapacity(this.orphanagesCapacityModifier);
        }

        public float getNursingHomesCapacityModifier() 
        {
            return this.nursingHomesCapacityModifier;
        }

        public float getOrphanagesCapacityModifier() 
        {
            return this.orphanagesCapacityModifier;
        }

        public IncomeValues getNursingHomesIncomeModifier() 
        {
            return this.nursingHomesIncomeValue;
        }

        public IncomeValues getOrphanagesIncomeModifier() 
        {
            return this.orphanagesIncomeValue;
        }

        public void updateNursingHomesCapacity(float targetValue) 
        {
            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.updateNursingHomesCapacity -- Updating capacity with modifier: {0}", targetValue);
            for (uint index = 0; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index) 
            {
                BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);
                if (buildingInfo != null && buildingInfo.m_buildingAI is NursingHomeAI nursingHomeAI) 
                {
                    nursingHomeAI.updateCapacity(targetValue);
                }
            }

            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            for (ushort i=0; i < buildingManager.m_buildings.m_buffer.Length; i++) 
            {
                if (buildingManager.m_buildings.m_buffer[i].Info != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI is NursingHomeAI nursingHomeAI) 
                {
                    nursingHomeAI.validateCapacity(i, ref buildingManager.m_buildings.m_buffer[i], true);
                }
            }
        }

        public void updateOrphanagesCapacity(float targetValue) 
        {
            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.updateOrphanagesCapacity -- Updating capacity with modifier: {0}", targetValue);
            for (uint index = 0; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index) 
            {
                BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);
                if (buildingInfo != null && buildingInfo.m_buildingAI is OrphanageAI orphanageAI) 
                {
                    orphanageAI.updateCapacity(targetValue);
                }
            }

            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            for (ushort i=0; i < buildingManager.m_buildings.m_buffer.Length; i++) 
            {
                if (buildingManager.m_buildings.m_buffer[i].Info != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI is OrphanageAI orphanageAI) 
                {
                    orphanageAI.validateCapacity(i, ref buildingManager.m_buildings.m_buffer[i], true);
                }
            }
        }

        private void saveOptions() 
        {
            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.saveOptions -- Saving Options");
            Options options = new();
            options.nursingHomesCapacityModifierSelectedIndex = -1;
            options.orphanagesCapacityModifierSelectedIndex = -1;
            
            if(this.nursingHomesCapacityDropDown != null) 
            {
                int nursingHomesCapacitySelectedIndex = this.nursingHomesCapacityDropDown.selectedIndex;
                options.nursingHomesCapacityModifierSelectedIndex = nursingHomesCapacitySelectedIndex;
                if (nursingHomesCapacitySelectedIndex >= 0) 
                {
                    Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.saveOptions -- Nursing Homes Capacity Modifier Set to: {0}", CAPACITY_VALUES[nursingHomesCapacitySelectedIndex]);
                    this.nursingHomesCapacityModifier = CAPACITY_VALUES[nursingHomesCapacitySelectedIndex];
                    this.updateNursingHomesCapacity(CAPACITY_VALUES[nursingHomesCapacitySelectedIndex]);
                }
            }

            if (this.nursingHomesIncomeDropDown != null) 
            {
                int nursingHomesIncomeSelectedIndex = this.nursingHomesIncomeDropDown.selectedIndex + 1;
                options.nursingHomesIncomeModifierSelectedIndex = nursingHomesIncomeSelectedIndex;
                if (nursingHomesIncomeSelectedIndex >= 0) 
                {
                    Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.saveOptions -- Nursing Homes Income Modifier Set to: {0}", (IncomeValues) nursingHomesIncomeSelectedIndex);
                    this.nursingHomesIncomeValue = (IncomeValues) nursingHomesIncomeSelectedIndex;
                }
            }

            if(this.orphanagesCapacityDropDown != null) 
            {
                int orphanagesCapacitySelectedIndex = this.orphanagesCapacityDropDown.selectedIndex;
                options.orphanagesCapacityModifierSelectedIndex = orphanagesCapacitySelectedIndex;
                if (orphanagesCapacitySelectedIndex >= 0) 
                {
                    Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.saveOptions -- Orphanages Capacity Modifier Set to: {0}", CAPACITY_VALUES[orphanagesCapacitySelectedIndex]);
                    this.orphanagesCapacityModifier = CAPACITY_VALUES[orphanagesCapacitySelectedIndex];
                    this.updateOrphanagesCapacity(CAPACITY_VALUES[orphanagesCapacitySelectedIndex]);
                }
            }

            if (this.orphanagesIncomeDropDown != null) 
            {
                int orphanagesIncomeSelectedIndex = this.orphanagesIncomeDropDown.selectedIndex + 1;
                options.orphanagesIncomeModifierSelectedIndex = orphanagesIncomeSelectedIndex;
                if (orphanagesIncomeSelectedIndex >= 0) 
                {
                    Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.saveOptions -- Orphanages Income Modifier Set to: {0}", (IncomeValues) orphanagesIncomeSelectedIndex);
                    this.orphanagesIncomeValue = (IncomeValues) orphanagesIncomeSelectedIndex;
                }
            }

            try 
            {
                using (StreamWriter streamWriter = new StreamWriter("CimCareModOptions.xml")) 
                {
                    new XmlSerializer(typeof(OptionsManager.Options)).Serialize(streamWriter, options);
                }
            } 
            catch (Exception e) 
            {
                Logger.logError(Logger.LOG_OPTIONS, "Error saving options: {0} -- {1}", e.Message, e.StackTrace);
            }

        }

        public void loadOptions() 
        {
            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.loadOptions -- Loading Options");
            OptionsManager.Options options = new OptionsManager.Options();

            try 
            {
                using (StreamReader streamReader = new StreamReader("CimCareModOptions.xml")) 
                {
                    options = (OptionsManager.Options) new XmlSerializer(typeof(OptionsManager.Options)).Deserialize(streamReader);
                }
            } 
            catch (FileNotFoundException) 
            {
                // Options probably not serialized yet, just return
                return;
            } 
            catch (Exception e) 
            {
                Logger.logError(Logger.LOG_OPTIONS, "Error loading options: {0} -- {1}", e.Message, e.StackTrace);
                return;
            }

            if (options.nursingHomesCapacityModifierSelectedIndex != -1) 
            {
                Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.loadOptions -- Loading Nursing Homes Capacity Modifier to: x{0}", CAPACITY_VALUES[options.nursingHomesCapacityModifierSelectedIndex]);
                this.nursingHomesCapacityDropDown.selectedIndex = options.nursingHomesCapacityModifierSelectedIndex;
                this.nursingHomesCapacityModifier = CAPACITY_VALUES[options.nursingHomesCapacityModifierSelectedIndex];
            }

            if (options.nursingHomesIncomeModifierSelectedIndex > 0) 
            {
                Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.loadOptions -- Loading Nursing Homes Income Modifier to: {0}", (IncomeValues) options.nursingHomesIncomeModifierSelectedIndex);
                this.nursingHomesIncomeDropDown.selectedIndex = options.nursingHomesIncomeModifierSelectedIndex - 1;
                this.nursingHomesIncomeValue = (IncomeValues) options.nursingHomesIncomeModifierSelectedIndex;
            }

            if (options.orphanagesCapacityModifierSelectedIndex != -1) 
            {
                Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.loadOptions -- Loading Orphanages Capacity Modifier to: x{0}", CAPACITY_VALUES[options.orphanagesCapacityModifierSelectedIndex]);
                this.orphanagesCapacityDropDown.selectedIndex = options.orphanagesCapacityModifierSelectedIndex;
                this.orphanagesCapacityModifier = CAPACITY_VALUES[options.orphanagesCapacityModifierSelectedIndex];
            }

            if (options.orphanagesIncomeModifierSelectedIndex > 0) 
            {
                Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.loadOptions -- Loading Orphanages Income Modifier to: {0}", (IncomeValues) options.orphanagesIncomeModifierSelectedIndex);
                this.orphanagesIncomeDropDown.selectedIndex = options.orphanagesIncomeModifierSelectedIndex - 1;
                this.orphanagesIncomeValue = (IncomeValues) options.orphanagesIncomeModifierSelectedIndex;
            }
        }

        public struct Options 
        {
            public int nursingHomesCapacityModifierSelectedIndex;
            public int nursingHomesIncomeModifierSelectedIndex;
            public int orphanagesCapacityModifierSelectedIndex;
            public int orphanagesIncomeModifierSelectedIndex;
        }
    }
}
