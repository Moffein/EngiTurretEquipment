using BepInEx.Configuration;
using EngiTurretEquipment.Components;
using EngiTurretEquipment.Modules;
using HarmonyLib;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace EngiTurretEquipment.Equipment
{
    public class WalkerTurret : EquipmentBase<WalkerTurret>
    {
        public override string EquipmentName => "Mobile Turret";

        public override string EquipmentLangTokenName => "WALKER";

        public override GameObject EquipmentModel => GetEquipmentModel();

        public override Sprite EquipmentIcon => Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texEngiWalkerTurretEquipmentIcon.png");

        public override float Cooldown => _cooldown;

        private static GameObject _equipmentModel;
        public static GameObject bodyPrefab;
        public static GameObject masterPrefab;
        public static GameObject projectilePrefab;
        public static CharacterSpawnCard spawnCard;

        public static float moveSpeed;
        public static float _cooldown;
        public static float baseHealth;
        public static float baseDamage;
        public static float baseRegen;
        public static bool increaseRange;
        public static bool alwaysSprint;
        public static int maxActive;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            var dict = new ItemDisplayRuleDict();

            //Don't set up displays if 3d model isn't available
            GameObject display = EquipmentModel;
            if (!display) return dict;

            dict.Add("EquipmentDroneBody", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    followerPrefab = EquipmentModel,
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    childName = "GunBarrelBase",
                    localPos = new Vector3(0F, 0F, 2.6F),
                    localAngles = new Vector3(270F, 0F, 0F),
                    localScale = new Vector3(0.5F, 0.5F, 0.5F)
                }
            });

            return dict;
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            CharacterBody characterBody = slot.characterBody;
            CharacterMaster characterMaster = (characterBody != null) ? characterBody.master : null;
            if (!characterMaster || characterMaster.IsDeployableLimited(DeployableSlot.GummyClone))
            {
                return false;
            }
            Ray aimRay = slot.GetAimRay();
            Quaternion rotation = Quaternion.LookRotation(aimRay.direction);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = projectilePrefab,
                crit = slot.characterBody.RollCrit(),
                damage = 0f,
                damageColorIndex = DamageColorIndex.Item,
                force = 0f,
                owner = slot.gameObject,
                position = aimRay.origin,
                rotation = rotation
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            return true;
        }

        protected override void CreateConfig(ConfigFile config)
        {
            base.CreateConfig(config);
            baseHealth = config.Bind("Mobile Turret", "Base Health", 65f).Value;
            baseDamage = config.Bind("Mobile Turret", "Base Damage", 8f).Value;
            baseRegen = config.Bind("Mobile Turret", "Base Regen", 1f).Value;
            _cooldown = config.Bind("Mobile Turret", "Cooldown", 90f).Value;
            moveSpeed = config.Bind("Mobile Turret", "Move Speed", 7f).Value;
            increaseRange = config.Bind("Mobile Turret", "Increase Range", true).Value;
            alwaysSprint = config.Bind("Mobile Turret", "Always Sprint", true).Value;
            maxActive = config.Bind("Mobile Turret", "Max Active with Extra Equip Slots", -1).Value;
        }


        public override void CreateAssets(ConfigFile config)
        {
            base.CreateAssets(config);
            CreateBodyPrefab();
            CreateMasterPrefab();
            CreateSpawnCard();
            CreateEquipmentModel();
            CreateProjectilePrefab();
        }

        private void CreateSpawnCard()
        {
            if (!masterPrefab) CreateMasterPrefab();
            if (spawnCard) return;

            spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            spawnCard.directorCreditCost = 35;
            spawnCard.hullSize = HullClassification.Human;
            spawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            spawnCard.prefab = masterPrefab;
            spawnCard.sendOverNetwork = true;
            spawnCard.occupyPosition = false;
            spawnCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn;
            spawnCard.inventoryItemCopyFilter = Inventory.DefaultItemCopyFilter;
            (spawnCard as ScriptableObject).name = "cscMoffeinETEWalkerTurret";
        }

        private void CreateProjectilePrefab()
        {
            if (!spawnCard) CreateSpawnCard();
            if (projectilePrefab) return;
            GameObject prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MinorConstructOnKill/MinorConstructOnKillProjectile.prefab").WaitForCompletion().InstantiateClone("MoffeinETE_WalkerTurretProjectile", true);
            prefab.GetComponent<ProjectileController>().ghostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drifter/DrifterToolbotCrateGhost.prefab").WaitForCompletion();

            ProjectileSpawnMaster psm = prefab.GetComponent<ProjectileSpawnMaster>();
            psm.spawnCard = spawnCard;
            psm.deployableSlot = DeployableSlot.None;

            PluginContentPack.projectilePrefabs.Add(prefab);
            projectilePrefab = prefab;

            IL.RoR2.Projectile.ProjectileSpawnMaster.SpawnMaster += ProjectileSpawnMaster_SpawnMaster;
        }

        private void ProjectileSpawnMaster_SpawnMaster(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(x => x.MatchCallvirt<DirectorCore>("TrySpawnObject")))
            {
                c.EmitDelegate<Func<DirectorSpawnRequest, DirectorSpawnRequest>>(req =>
                {
                    if (req.spawnCard == spawnCard && req.summonerBodyObject != null)
                    {
                        CharacterBody summonerBody = req.summonerBodyObject.GetComponent<CharacterBody>();
                        if (summonerBody && summonerBody.inventory)
                        {
                            CharacterSpawnCard baseline = req.spawnCard as CharacterSpawnCard;
                            CharacterSpawnCard dynamicSpawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();

                            dynamicSpawnCard.prefab = spawnCard.prefab;
                            dynamicSpawnCard.occupyPosition = spawnCard.occupyPosition;
                            dynamicSpawnCard.sendOverNetwork = spawnCard.sendOverNetwork;
                            dynamicSpawnCard.hullSize = spawnCard.hullSize;
                            dynamicSpawnCard.nodeGraphType = spawnCard.nodeGraphType;
                            dynamicSpawnCard.forbiddenFlags = spawnCard.forbiddenFlags;
                            dynamicSpawnCard.inventoryItemCopyFilter = spawnCard.inventoryItemCopyFilter;

                            dynamicSpawnCard.inventoryToCopy = summonerBody.inventory;

                            req.spawnCard = dynamicSpawnCard;
                        }
                        req.ignoreTeamMemberLimit = true;
                    }
                    return req;
                });
            }
            else
            {
                Debug.LogError("EngiTurretEquipment: Walker Turret ProjectileSpawnMaster_SpawnMaster IL hook failed.");
            }
        }

        private void CreateBodyPrefab()
        {
            if (bodyPrefab) return;

            GameObject prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiWalkerTurretBody.prefab").WaitForCompletion().InstantiateClone("MoffeinETE_WalkerTurretBody", true);
            CharacterBody cb = prefab.GetComponent<CharacterBody>();
            cb.baseMaxHealth = baseHealth;
            cb.levelMaxHealth = baseHealth * 0.3f;

            cb.baseDamage = baseDamage;
            cb.levelDamage = baseDamage * 0.2f;

            cb.baseRegen = baseRegen;
            cb.levelRegen = baseRegen * 0.2f;

            cb.portraitIcon = Modules.Assets.mainAssetBundle.LoadAsset<Texture2D>("texEngiWalkerTurretBodyIcon.png");

            prefab.AddComponent<RegisterWalkerTurret>();

            PluginContentPack.bodyPrefabs.Add(prefab);
            bodyPrefab = prefab;
        }

        private void CreateMasterPrefab()
        {
            if (masterPrefab) return;
            if (!bodyPrefab) CreateBodyPrefab();
            GameObject prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiWalkerTurretMaster.prefab").WaitForCompletion().InstantiateClone("MoffeinETE_WalkerTurretMaster", true);
            CharacterMaster cm = prefab.GetComponent<CharacterMaster>();
            cm.bodyPrefab = bodyPrefab;
            PluginContentPack.masterPrefabs.Add(prefab);
            masterPrefab = prefab;

            AISkillDriver[] drivers = prefab.GetComponents<AISkillDriver>();
            foreach (AISkillDriver asd in drivers)
            {
                if (alwaysSprint && asd.customName != "Rest")
                {
                    asd.shouldSprint = true;
                }
                if (increaseRange && asd.customName == "ChaseAndFireAtEnemy")
                {
                    asd.maxDistance = 45f;
                }
            }

            if (increaseRange)
            {
                PluginUtils.SetAddressableEntityStateField("RoR2/Base/Engi/EntityStates.EngiTurret.EngiTurretWeapon.FireBeam.asset", "maxDistance", "45");
            }
        }

        private static void CreateEquipmentModel()
        {
            if (_equipmentModel) return;
            GameObject prefab = Modules.Assets.mainAssetBundle.LoadAsset<GameObject>("mdlEngiTurretWalker");
            var renderer = prefab.GetComponentInChildren<Renderer>();
            renderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Engi/matEngiTurret.mat").WaitForCompletion();

            ItemDisplay itemDisplay = prefab.AddComponent<ItemDisplay>();
            var renderers = new List<Renderer> { renderer };
            List<CharacterModel.RendererInfo> rendererInfos = new List<CharacterModel.RendererInfo>();
            foreach (Renderer r in renderers)
            {
                var rendererInfo = new CharacterModel.RendererInfo();
                rendererInfo.renderer = r;
                rendererInfo.defaultMaterial = r.material;
                rendererInfo.defaultShadowCastingMode = r.shadowCastingMode;
                rendererInfo.ignoreOverlays = false;
                rendererInfo.hideOnDeath = false;
                rendererInfos.Add(rendererInfo);
            }
            itemDisplay.rendererInfos = rendererInfos.ToArray();
            _equipmentModel = prefab;
        }

        private static GameObject GetEquipmentModel()
        {
            if (!_equipmentModel) CreateEquipmentModel();
            return _equipmentModel;
        }
    }
}
