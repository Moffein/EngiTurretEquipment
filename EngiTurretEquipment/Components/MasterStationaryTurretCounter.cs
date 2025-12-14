using EngiTurretEquipment.Equipment;
using System.Collections.Generic;
using RoR2;
using UnityEngine.Networking;
using UnityEngine;

namespace EngiTurretEquipment.Components
{
    public class MasterStationaryTurretCounter : MonoBehaviour
    {
        private Inventory inventory;

        public Queue<CharacterMaster> activeTurrets = new Queue<CharacterMaster>();

        private void Start()
        {
            if (!inventory) inventory = GetComponent<Inventory>();
        }

        private void UpdateActiveTurrets()
        {
            Queue<CharacterMaster> newActiveTurrets = new Queue<CharacterMaster>();
            foreach (CharacterMaster cm in activeTurrets)
            {
                if (cm && !newActiveTurrets.Contains(cm))
                {
                    CharacterBody body = cm.GetBody();
                    if (body && body.healthComponent && body.healthComponent.alive)
                    {
                        newActiveTurrets.Enqueue(cm);
                    }
                }
            }
            activeTurrets.Clear();
            activeTurrets = newActiveTurrets;
        }

        public bool CanSpawnTurret()
        {
            UpdateActiveTurrets();
            int maxTurrets = GetMaxTurrets();
            return activeTurrets.Count < maxTurrets;
        }

        protected EquipmentDef GetEquipmentDef()
        {
            return StationaryTurret.Instance != null ? StationaryTurret.Instance.EquipmentDef : null;
        }

        public int GetMaxTurrets()
        {
            int hardCap = StationaryTurret.maxActive;
            EquipmentDef ed = GetEquipmentDef();
            if (inventory && ed)
            {
                int count = 0;
                foreach (var eq in inventory._equipmentStateSlots)
                {
                    if (eq == null) continue;
                    foreach (var eqState in eq)
                    {
                        if (eqState.equipmentDef == ed) count++;
                    }
                }

                int max = Mathf.Max(1, count);
                if (hardCap > 0)
                {
                    max = Mathf.Min(max, hardCap);
                }
                return max;
            }
            else
            {
                return 1;
            }
        }

        public void AddTurretServer(CharacterMaster newTurret)
        {
            if (!NetworkServer.active) return;
            UpdateActiveTurrets();

            int maxTurrets = GetMaxTurrets();
            int diff = activeTurrets.Count + 1 - maxTurrets;
            if (diff > 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    if (activeTurrets.TryDequeue(out CharacterMaster master))
                    {
                        master.TrueKill();
                    }
                }
            }

            activeTurrets.Enqueue(newTurret);
        }
    }
}
