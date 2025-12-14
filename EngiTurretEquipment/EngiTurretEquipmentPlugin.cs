using BepInEx;
using EngiTurretEquipment.Equipment;
using EngiTurretEquipment.Modules;
using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Security;
using System.Security.Permissions;

//Allows you to access private methods/fields/etc from the stubbed Assembly-CSharp that is included.

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace EngiTurretEquipment
{
    [BepInDependency(R2API.ItemAPI.PluginGUID)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.LanguageAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin("com.Moffein.EngiTurretEquipment", "EngiTurretEquipment", "1.0.0")]
    public class EngiTurretEquipmentPlugin : BaseUnityPlugin
    {
        public static PluginInfo PInfo;
        public static List<EquipmentBase> Equipments = new List<EquipmentBase>();
        internal void Awake()
        {
            PInfo = Info;
            new PluginContentPack().Initialize();
            Modules.Assets.LoadAssetBundle();
            LanguageOverrides.Initialize();

            AddToAssembly();
        }

        private void AddToAssembly()
        {
            //this section automatically scans the project for all equipment
            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EquipmentBase)));
            var loadedEquipmentNames = new List<string>();
            var childEquipmentTypes = new List<EquipmentBase>();

            foreach (var equipmentType in EquipmentTypes)
            {
                EquipmentBase equipment = (EquipmentBase)System.Activator.CreateInstance(equipmentType);
                if (equipment.ParentEquipmentName != null)
                {
                    childEquipmentTypes.Add(equipment);
                    continue;
                }

                if (ValidateEquipment(equipment, Equipments))
                {
                    equipment.Init(Config);
                    loadedEquipmentNames.Add(equipment.EquipmentName);
                }
            }

            foreach (var childEquip in childEquipmentTypes)
            {
                if (loadedEquipmentNames.Contains(childEquip.ParentEquipmentName))
                    childEquip.Init(Config);
            }

            onFinishScanning?.Invoke();
        }
        public static Action onFinishScanning;


        /// <summary>
        /// A helper to easily set up and initialize an equipment from your equipment classes if the user has it enabled in their configuration files.
        /// </summary>
        /// <param name="equipment">A new instance of an EquipmentBase class."</param>
        /// <param name="equipmentList">The list you would like to add this to if it passes the config check.</param>
        public bool ValidateEquipment(EquipmentBase equipment, List<EquipmentBase> equipmentList)
        {
            var enabledDescription = "Should this equipment appear in runs?";
            if (equipment.Unfinished)
            {
                enabledDescription = "UNFINISHED! " + enabledDescription;
            }
            var enabled = Config.Bind(equipment.ConfigCategory, "Enable Equipment?", true, enabledDescription).Value;
            if (enabled)
            {
                equipmentList.Add(equipment);

                return true;
            }
            return false;
        }
    }
}
