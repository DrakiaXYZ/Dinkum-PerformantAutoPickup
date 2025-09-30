using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mirror;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DrakiaXYZ.PerformantAutoPickup
{
    [BepInPlugin("xyz.drakia.dinkum.autopickup", "DrakiaXYZ-PerformantAutoPickup", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<float> PickupDistance;
        private ConfigEntry<bool> PocketFullNotification;
        private ConfigEntry<KeyCode> ToggleKey;

        private float pickupInterval = 0.1f;
        private float timer = 0f;
        private bool pickupEnabled = true;

        public void Start()
        {
            PickupDistance = Config.Bind<float>("General", "Pickup Distance", 5f, "Range to auto pickup items");
            PocketFullNotification = Config.Bind<bool>("General", "Pocket Full Notification", true, "Enable or disable the pocket full notification when a pickup fails");
            ToggleKey = Config.Bind<KeyCode>("General", "Toggle Key", KeyCode.P, "Key used to toggle auto pickup on/off");

            var harmony = new Harmony("xyz.drakia.dinkum.autopickup");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void Update()
        {
            if (NetworkMapSharer.Instance.localChar == null)
            {
                return;
            }

            // Allow toggling
            if (ToggleKey.Value != KeyCode.None && !Inventory.Instance.isMenuOpen() && Input.GetKeyDown(ToggleKey.Value))
            {
                pickupEnabled = !pickupEnabled;

                NotificationManager.manage.createChatNotification($"Auto Pickup " + (pickupEnabled ? "Enabled" : "Disabled"), false);
            }
            if (!pickupEnabled)
            {
                timer = 0f;
                return;
            }

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

            timer += Time.deltaTime;
            if (timer < pickupInterval)
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
                        if (PocketFullNotification.Value)
                        {
                            NotificationManager.manage.turnOnPocketsFullNotification(true);
                        }
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
                    if (droppedItem != null && !CmdDropItemPatch.LocalDroppedItems.Contains(droppedItem.myItemId))
                    {
                        if (Vector3.Distance(position, items[i].transform.position) < distance)
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
