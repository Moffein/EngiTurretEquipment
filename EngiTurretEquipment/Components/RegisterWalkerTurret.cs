using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace EngiTurretEquipment.Components
{
    public class RegisterWalkerTurret : MonoBehaviour
    {
        private void Start()
        {
            if (NetworkServer.active)
            {
                CharacterBody body = GetComponent<CharacterBody>();
                if (body.master && body.master.minionOwnership && body.master.minionOwnership.ownerMaster)
                {
                    MasterWalkerTurretCounter mst = body.master.minionOwnership.ownerMaster.gameObject.GetComponent<MasterWalkerTurretCounter>();
                    if (!mst) mst = body.master.minionOwnership.ownerMaster.gameObject.AddComponent<MasterWalkerTurretCounter>();

                    mst.AddTurretServer(body.master);
                }
            }
            Destroy(this);
        }
    }
}
