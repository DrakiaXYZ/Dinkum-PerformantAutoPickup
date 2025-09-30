using HarmonyLib;
using Mirror;

namespace DrakiaXYZ.PerformantAutoPickup
{
    [HarmonyPatch(typeof(CharPickUp))]
    internal class CmdPickUpPatch
    {
        [HarmonyPatch("CmdPickUp")]
        [HarmonyPrefix]
        public static void CmdPickUpPrefix(uint pickUpId)
        {
            if (NetworkIdentity.spawned.ContainsKey(pickUpId))
            {
                DroppedItem droppedItem = NetworkIdentity.spawned[pickUpId].GetComponent<DroppedItem>();
                if (droppedItem != null)
                {
                    CmdDropItemPatch.LocalDroppedItems.Remove(droppedItem.myItemId);
                }
            }
        }
    }
}
