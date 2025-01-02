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
        private static readonly string[] CAPACITY_LABELS = ["Give Em Room (x0.5)", "Realistic (x1.0)", "Just a bit More (x1.5)", "Gameplay over Realism (x2.0)", "Who needs Living Space? (x2.5)", "Pack em like Sardines! (x3.0)"];
        private static readonly float[] CAPACITY_VALUES = [0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f];

        private static readonly string[] NURSING_HOMES_INCOME_LABELS = ["Communisim is Key (Full Maintenance)", "Seniors can Help a Little (Half Maintenance at Full Capacity)", "Make the Seniors Pay (No Maintenance at Full Capacity)", "Nursing Homes should be Profitable (Maintenance becomes Profit at Full Capacity)", "Twice the Pain, Twice the Gain (2x Maintenance, 2x Profit)", "Show me the Money! (Profit x2, Normal Maintenance)"];
        private static readonly string[] ORPHANAGES_INCOME_LABELS = ["Communisim is Key (Full Maintenance)", "Orphans allowance (Half Maintenance at Full Capacity)", "Orphans will work on the street (No Maintenance at Full Capacity)", "Orphanages should be Profitable (Maintenance becomes Profit at Full Capacity)", "Twice the Pain, Twice the Gain (2x Maintenance, 2x Profit)", "Show me the Money! (Profit x2, Normal Maintenance)"];

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

        public void Initialize(UIHelperBase helper)
        {
            Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.Initialize -- Initializing Menu Options");
            UIHelperBase group = helper.AddGroup("Cim Care Settings");
            nursingHomesCapacityDropDown = (UIDropDown)group.AddDropdown("Nursing Homes Capacity Modifier", CAPACITY_LABELS, 1, HandleCapacityChange);
            nursingHomesIncomeDropDown = (UIDropDown)group.AddDropdown("Nursing Homes Income Modifier", NURSING_HOMES_INCOME_LABELS, 2, HandleIncomeChange);
            group.AddSpace(2);
            orphanagesCapacityDropDown = (UIDropDown)group.AddDropdown("Orphanages Capacity Modifier", CAPACITY_LABELS, 1, HandleCapacityChange);
            orphanagesIncomeDropDown = (UIDropDown)group.AddDropdown("Orphanages Income Modifier", ORPHANAGES_INCOME_LABELS, 2, HandleIncomeChange);
            group.AddSpace(5);
            group.AddButton("Save", SaveOptions);
        }

        private void HandleCapacityChange(int newSelection)
        {
            // Do nothing until Save is pressed
        }

        private void HandleIncomeChange(int newSelection)
        {
            // Do nothing until Save is pressed
        }

        public void UpdateNursingHomesCapacity()
        {
            UpdateNursingHomesCapacity(nursingHomesCapacityModifier);
        }

        public void UpdateOrphanagesCapacity()
        {
            UpdateOrphanagesCapacity(orphanagesCapacityModifier);
        }

        public float GetNursingHomesCapacityModifier()
        {
            return nursingHomesCapacityModifier;
        }

        public float GetOrphanagesCapacityModifier()
        {
            return orphanagesCapacityModifier;
        }

        public IncomeValues GetNursingHomesIncomeModifier()
        {
            return nursingHomesIncomeValue;
        }

        public IncomeValues GetOrphanagesIncomeModifier()
        {
            return orphanagesIncomeValue;
        }

        public void UpdateNursingHomesCapacity(float targetValue)
        {
            Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.UpdateNursingHomesCapacity -- Updating capacity with modifier: {0}", targetValue);
            for (uint index = 0; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index)
            {
                BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);
                if (buildingInfo != null && buildingInfo.m_buildingAI is NursingHomeAI nursingHomeAI)
                {
                    nursingHomeAI.UpdateCapacity(targetValue);
                }
            }

            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            for (ushort i = 0; i < buildingManager.m_buildings.m_buffer.Length; i++)
            {
                if (buildingManager.m_buildings.m_buffer[i].Info != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI is NursingHomeAI nursingHomeAI)
                {
                    nursingHomeAI.ValidateCapacity(i, ref buildingManager.m_buildings.m_buffer[i], true);
                }
            }
        }

        public void UpdateOrphanagesCapacity(float targetValue)
        {
            Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.UpdateOrphanagesCapacity -- Updating capacity with modifier: {0}", targetValue);
            for (uint index = 0; PrefabCollection<BuildingInfo>.LoadedCount() > index; ++index)
            {
                BuildingInfo buildingInfo = PrefabCollection<BuildingInfo>.GetLoaded(index);
                if (buildingInfo != null && buildingInfo.m_buildingAI is OrphanageAI orphanageAI)
                {
                    orphanageAI.UpdateCapacity(targetValue);
                }
            }

            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            for (ushort i = 0; i < buildingManager.m_buildings.m_buffer.Length; i++)
            {
                if (buildingManager.m_buildings.m_buffer[i].Info != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI != null && buildingManager.m_buildings.m_buffer[i].Info.m_buildingAI is OrphanageAI orphanageAI)
                {
                    orphanageAI.ValidateCapacity(i, ref buildingManager.m_buildings.m_buffer[i], true);
                }
            }
        }

        private void SaveOptions()
        {
            Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.SaveOptions -- Saving Options");
            Options options = new()
            {
                nursingHomesCapacityModifierSelectedIndex = -1,
                orphanagesCapacityModifierSelectedIndex = -1
            };

            if (nursingHomesCapacityDropDown != null)
            {
                int nursingHomesCapacitySelectedIndex = nursingHomesCapacityDropDown.selectedIndex;
                options.nursingHomesCapacityModifierSelectedIndex = nursingHomesCapacitySelectedIndex;
                if (nursingHomesCapacitySelectedIndex >= 0)
                {
                    Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.SaveOptions -- Nursing Homes Capacity Modifier Set to: {0}", CAPACITY_VALUES[nursingHomesCapacitySelectedIndex]);
                    nursingHomesCapacityModifier = CAPACITY_VALUES[nursingHomesCapacitySelectedIndex];
                    UpdateNursingHomesCapacity(CAPACITY_VALUES[nursingHomesCapacitySelectedIndex]);
                }
            }

            if (nursingHomesIncomeDropDown != null)
            {
                int nursingHomesIncomeSelectedIndex = nursingHomesIncomeDropDown.selectedIndex + 1;
                options.nursingHomesIncomeModifierSelectedIndex = nursingHomesIncomeSelectedIndex;
                if (nursingHomesIncomeSelectedIndex >= 0)
                {
                    Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.SaveOptions -- Nursing Homes Income Modifier Set to: {0}", (IncomeValues)nursingHomesIncomeSelectedIndex);
                    nursingHomesIncomeValue = (IncomeValues)nursingHomesIncomeSelectedIndex;
                }
            }

            if (orphanagesCapacityDropDown != null)
            {
                int orphanagesCapacitySelectedIndex = orphanagesCapacityDropDown.selectedIndex;
                options.orphanagesCapacityModifierSelectedIndex = orphanagesCapacitySelectedIndex;
                if (orphanagesCapacitySelectedIndex >= 0)
                {
                    Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.SaveOptions -- Orphanages Capacity Modifier Set to: {0}", CAPACITY_VALUES[orphanagesCapacitySelectedIndex]);
                    orphanagesCapacityModifier = CAPACITY_VALUES[orphanagesCapacitySelectedIndex];
                    UpdateOrphanagesCapacity(CAPACITY_VALUES[orphanagesCapacitySelectedIndex]);
                }
            }

            if (orphanagesIncomeDropDown != null)
            {
                int orphanagesIncomeSelectedIndex = orphanagesIncomeDropDown.selectedIndex + 1;
                options.orphanagesIncomeModifierSelectedIndex = orphanagesIncomeSelectedIndex;
                if (orphanagesIncomeSelectedIndex >= 0)
                {
                    Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.SaveOptions -- Orphanages Income Modifier Set to: {0}", (IncomeValues)orphanagesIncomeSelectedIndex);
                    orphanagesIncomeValue = (IncomeValues)orphanagesIncomeSelectedIndex;
                }
            }

            try
            {
                using StreamWriter streamWriter = new("CimCareModOptions.xml");
                new XmlSerializer(typeof(Options)).Serialize(streamWriter, options);
            }
            catch (Exception e)
            {
                Logger.LogError(Logger.LOG_OPTIONS, "Error saving options: {0} -- {1}", e.Message, e.StackTrace);
            }

        }

        public void LoadOptions()
        {
            Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.LoadOptions -- Loading Options");
            Options options = new();

            string old_file_path = "SeniorCitizenCenterMod.xml";
            string new_file_path = "CimCareModOptions.xml";

            try
            {
                if (File.Exists(old_file_path))
                {
                    using StreamReader streamReader = new(old_file_path);
                    options = (Options)new XmlSerializer(typeof(Options)).Deserialize(streamReader);
                    File.Delete(old_file_path);
                }
                else
                {
                    using StreamReader streamReader = new(new_file_path);
                    options = (Options)new XmlSerializer(typeof(Options)).Deserialize(streamReader);
                }


            }
            catch (FileNotFoundException)
            {
                // Options probably not serialized yet, just return
                return;
            }
            catch (Exception e)
            {
                Logger.LogError(Logger.LOG_OPTIONS, "Error loading options: {0} -- {1}", e.Message, e.StackTrace);
                return;
            }

            if (options.nursingHomesCapacityModifierSelectedIndex != -1)
            {
                Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.LoadOptions -- Loading Nursing Homes Capacity Modifier to: x{0}", CAPACITY_VALUES[options.nursingHomesCapacityModifierSelectedIndex]);
                nursingHomesCapacityDropDown.selectedIndex = options.nursingHomesCapacityModifierSelectedIndex;
                nursingHomesCapacityModifier = CAPACITY_VALUES[options.nursingHomesCapacityModifierSelectedIndex];
            }

            if (options.nursingHomesIncomeModifierSelectedIndex > 0)
            {
                Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.LoadOptions -- Loading Nursing Homes Income Modifier to: {0}", (IncomeValues)options.nursingHomesIncomeModifierSelectedIndex);
                nursingHomesIncomeDropDown.selectedIndex = options.nursingHomesIncomeModifierSelectedIndex - 1;
                nursingHomesIncomeValue = (IncomeValues)options.nursingHomesIncomeModifierSelectedIndex;
            }

            if (options.orphanagesCapacityModifierSelectedIndex != -1)
            {
                Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.LoadOptions -- Loading Orphanages Capacity Modifier to: x{0}", CAPACITY_VALUES[options.orphanagesCapacityModifierSelectedIndex]);
                orphanagesCapacityDropDown.selectedIndex = options.orphanagesCapacityModifierSelectedIndex;
                orphanagesCapacityModifier = CAPACITY_VALUES[options.orphanagesCapacityModifierSelectedIndex];
            }

            if (options.orphanagesIncomeModifierSelectedIndex > 0)
            {
                Logger.LogInfo(Logger.LOG_OPTIONS, "OptionsManager.LoadOptions -- Loading Orphanages Income Modifier to: {0}", (IncomeValues)options.orphanagesIncomeModifierSelectedIndex);
                orphanagesIncomeDropDown.selectedIndex = options.orphanagesIncomeModifierSelectedIndex - 1;
                orphanagesIncomeValue = (IncomeValues)options.orphanagesIncomeModifierSelectedIndex;
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
