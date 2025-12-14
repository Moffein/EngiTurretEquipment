using RoR2;
using RoR2.ContentManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EngiTurretEquipment.Modules
{
    public class PluginContentPack : IContentPackProvider
    {
        public static ContentPack content = new ContentPack();
        public static List<GameObject> networkedObjectPrefabs = new List<GameObject>();
        public static List<GameObject> bodyPrefabs = new List<GameObject>();
        public static List<GameObject> masterPrefabs = new List<GameObject>();
        public static List<GameObject> projectilePrefabs = new List<GameObject>();
        public static List<EquipmentDef> equipmentDefs = new List<EquipmentDef>();

        public string identifier => "EngiTurretEquipment.content";

        internal void Initialize()
        {
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(this);
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(content, args.output);
            yield break;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            content.networkedObjectPrefabs.Add(networkedObjectPrefabs.ToArray());
            content.bodyPrefabs.Add(bodyPrefabs.ToArray());
            content.masterPrefabs.Add(masterPrefabs.ToArray());
            content.equipmentDefs.Add(equipmentDefs.ToArray());
            content.projectilePrefabs.Add(projectilePrefabs.ToArray());
            yield break;
        }
    }
}
