using BepInEx;
using BepInEx.Configuration;
using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace DrakiaXYZ.PerformantAutoPickup
{
    [BepInPlugin("xyz.drakia.dinkum.autopickup", "DrakiaXYZ-PerformantAutoPickup", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<float> PickupDistance;
        private ConfigEntry<float> PickupInterval;
        private float timer = 0f;

        private void Awake()
        {
            PickupDistance = this.Config.Bind<float>("General", "Pickup Distance", 5f, "Range to auto pickup items");
            PickupInterval = this.Config.Bind<float>("General", "Pickup Interval", 0.1f, 
                new ConfigDescription(
                    "How often to search for items", 
                    new AcceptableValueRange<float>(0f, 1f)
                ));
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // Skip if gliding
            if (NetworkMapSharer.Instance.localChar.usingHangGlider)
            {
                timer = 0f;
                return;
            }

            // Skip if carrying an item or hitting interact key
            if (NetworkMapSharer.Instance.localChar.myPickUp.holdingPickUp || InputMaster.input.Interact() || InputMaster.input.InteractHeld())
            {
                timer = 0f;
                return;
            }

            // Don't run yet
            if (timer < PickupInterval.Value)
            {
                return;
            }

            List<DroppedItem> items = findDroppedItems(NetworkMapSharer.Instance.localChar.transform.position, PickupDistance.Value);
            foreach (DroppedItem item in items)
            {
                if (NetworkIdentity.spawned.ContainsKey(item.netId) && !item.HasBeenPickedUp())
                {
                    if (Inventory.Instance.checkIfItemCanFit(item.myItemId, item.stackAmount))
                    {
                        SoundManager.Instance.play2DSound(SoundManager.Instance.pickUpItem);
                        NetworkMapSharer.Instance.localChar.myPickUp.CmdPickUp(item.netId);
                        item.pickUpLocal();
                    }
                    else
                    {
                        NotificationManager.manage.turnOnPocketsFullNotification(true);
                    }
                }
            }

            timer = 0f;
        }

        public List<DroppedItem> findDroppedItems(Vector3 position, float distance)
        {
            List<DroppedItem> droppedItems = new List<DroppedItem>();
            if (Physics.CheckSphere(position, distance, WorldManager.Instance.pickUpLayer))
            {
                Collider[] items = Physics.OverlapSphere(position, distance, WorldManager.Instance.pickUpLayer);
                for (int i = 0; i < items.Length; i++)
                {
                    DroppedItem droppedItem = items[i].GetComponentInParent<DroppedItem>();
                    if (droppedItem != null)
                    {
                        if (Vector3.Distance(position, items[i].transform.position) < distance && WorldManager.Instance.isPositionInSameFencedArea(position, items[i].transform.position))
                        {
                            droppedItems.Add(droppedItem);
                        }
                    }
                }
            }

            return droppedItems;
        }
    }
}
