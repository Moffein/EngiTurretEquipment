using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace EngiTurretEquipment.Modules
{
    public static class Assets
    {
        public static AssetBundle mainAssetBundle;
        internal static void LoadAssetBundle()
        {
            if (mainAssetBundle == null)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EngiTurretEquipment.engiturretequipmentbundle"))
                {
                    mainAssetBundle = AssetBundle.LoadFromStream(stream);
                }
            }
        }
    }
}
