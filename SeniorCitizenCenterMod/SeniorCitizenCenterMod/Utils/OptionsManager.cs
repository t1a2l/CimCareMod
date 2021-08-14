using ICities;
using ColossalFramework.UI;
using System.IO;
using System.Xml.Serialization;
using System;
using ColossalFramework;
using SeniorCitizenCenterMod.AI;

namespace SeniorCitizenCenterMod.Utils {
    public class OptionsManager {

        private static readonly string[] CAPACITY_LABELS = new string[] { "Give Em Room (x0.5)", "Realistic (x1.0)", "Just a bit More (x1.5)", "Gameplay over Realism (x2.0)", "Who needs Living Space? (x2.5)", "Pack em like Sardines! (x3.0)" };
        private static readonly float[] CAPACITY_VALUES = new float[] { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f };

        private static readonly string[] INCOME_LABELS = new string[] { "Communisim is Key (Full Maintenance)", "Seniors can Help a Little (Half Maintenance at Full Capacity)", "Make the Seniors Pay (No Maintenance at Full Capacity)", "Nursing Homes should be Profitable (Maintenance becomes Profit at Full Capacity)", "Twice the Pain, Twice the Gain (2x Maintenance, 2x Profit)", "Show me the Money! (Profit x2, Normal Maintenance)" };
        public enum IncomeValues {
            FULL_MAINTENANCE = 1,
            HALF_MAINTENANCE = 2,
            NO_MAINTENANCE = 3,
            NORMAL_PROFIT = 4,
            DOUBLE_DOUBLE = 5,
            DOUBLE_PROFIT = 6
        };

        private UIDropDown capacityDropDown;
        private float capacityModifier = -1.0f;

        private UIDropDown incomeDropDown;
        private IncomeValues incomeValue = IncomeValues.NO_MAINTENANCE;

        public void initialize(UIHelperBase helper) {
            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.initialize -- Initializing Menu Options");
            UIHelperBase group = helper.AddGroup("Nursing Home Settings");
            this.capacityDropDown = (UIDropDown) group.AddDropdown("Capacity Modifier", CAPACITY_LABELS, 1, handleCapacityChange);
            this.incomeDropDown = (UIDropDown) group.AddDropdown("Income Modifier", INCOME_LABELS, 2, handleIncomeChange);
            group.AddSpace(5);
            group.AddButton("Save", saveOptions);
            //group.AddSlider("Capacity Modifier", 0.5f, 5.0f, 0.5f, 1.0f, handleCapacityChange);
        }

        private void handleCapacityChange(int newSelection) {
            // Do nothing until Save is pressed
        }

        private void handleIncomeChange(int newSelection) {
            // Do nothing until Save is pressed
        }

        private void handleHideTabChange(bool newSelection) {
            // Do nothing until Save is pressed
        }

        public void updateCapacity() {
            this.updateCapacity(this.capacityModifier);
        }

        public float getCapacityModifier() {
            return this.capacityModifier;
        }

        public IncomeValues getIncomeModifier() {
            return this.incomeValue;
        }

        public void updateCapacity(float targetValue) {

            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.updateCapacity -- Updating capacity with modifier: {0}", targetValue);
            for (uint index = 0; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index) {
                BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);
                if (buildingInfo != null && buildingInfo.m_buildingAI is NursingHomeAI nursingHomeAI) {
                    nursingHomeAI.updateCapacity(targetValue);
                }
            }

            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            for (ushort i=0; i < buildingManager.m_buildings.m_buffer.Length; i++) {
                if (buildingManager.m_buildings.m_buffer[i].Info != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI is NursingHomeAI nursingHomeAI) {
                    nursingHomeAI.validateCapacity(i, ref buildingManager.m_buildings.m_buffer[i], true);
                }
            }
        }

        private void saveOptions() {
            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.saveOptions -- Saving Options");
            OptionsManager.Options options = new OptionsManager.Options();
            options.capacityModifierSelectedIndex = -1;
            
            if(this.capacityDropDown != null) {
                int capacitySelectedIndex = this.capacityDropDown.selectedIndex;
                options.capacityModifierSelectedIndex = capacitySelectedIndex;
                if (capacitySelectedIndex >= 0) {
                    Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.saveOptions -- Capacity Modifier Set to: {0}", CAPACITY_VALUES[capacitySelectedIndex]);
                    this.capacityModifier = CAPACITY_VALUES[capacitySelectedIndex];
                    this.updateCapacity(CAPACITY_VALUES[capacitySelectedIndex]);
                }
            }

            if (this.incomeDropDown != null) {
                int incomeSelectedIndex = this.incomeDropDown.selectedIndex + 1;
                options.incomeModifierSelectedIndex = incomeSelectedIndex;
                if (incomeSelectedIndex >= 0) {
                    Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.saveOptions -- Income Modifier Set to: {0}", (IncomeValues) incomeSelectedIndex);
                    this.incomeValue = (IncomeValues) incomeSelectedIndex;
                }
            }

            try {
                using (StreamWriter streamWriter = new StreamWriter("SeniorCitizenCenterModOptions.xml")) {
                    new XmlSerializer(typeof(OptionsManager.Options)).Serialize(streamWriter, options);
                }
            } catch (Exception e) {
                Logger.logError(Logger.LOG_OPTIONS, "Error saving options: {0} -- {1}", e.Message, e.StackTrace);
            }

        }

        public void loadOptions() {
            Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.loadOptions -- Loading Options");
            OptionsManager.Options options = new OptionsManager.Options();

            try {
                using (StreamReader streamReader = new StreamReader("SeniorCitizenCenterModOptions.xml")) {
                    options = (OptionsManager.Options) new XmlSerializer(typeof(OptionsManager.Options)).Deserialize(streamReader);
                }
            } catch (FileNotFoundException) {
                // Options probably not serialized yet, just return
                return;
            } catch (Exception e) {
                Logger.logError(Logger.LOG_OPTIONS, "Error loading options: {0} -- {1}", e.Message, e.StackTrace);
                return;
            }

            if (options.capacityModifierSelectedIndex != -1) {
                Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.loadOptions -- Loading Capacity Modifier to: x{0}", CAPACITY_VALUES[options.capacityModifierSelectedIndex]);
                this.capacityDropDown.selectedIndex = options.capacityModifierSelectedIndex;
                this.capacityModifier = CAPACITY_VALUES[options.capacityModifierSelectedIndex];
            }

            if (options.incomeModifierSelectedIndex > 0) {
                Logger.logInfo(Logger.LOG_OPTIONS, "OptionsManager.loadOptions -- Loading Income Modifier to: {0}", (IncomeValues) options.incomeModifierSelectedIndex);
                this.incomeDropDown.selectedIndex = options.incomeModifierSelectedIndex - 1;
                this.incomeValue = (IncomeValues) options.incomeModifierSelectedIndex;
            }
        }

        public struct Options {
            public int capacityModifierSelectedIndex;
            public int incomeModifierSelectedIndex;
            public bool? hideTabSelectedValue;
        }
    }
}
