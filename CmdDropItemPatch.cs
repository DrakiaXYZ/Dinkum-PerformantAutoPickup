using HarmonyLib;
using System.Collections.Generic;

namespace DrakiaXYZ.PerformantAutoPickup
{
    [HarmonyPatch(typeof(CharMovement))]
    public class CmdDropItemPatch
    {
        public static List<int> LocalDroppedItems { get; private set; } = new List<int>();

        [HarmonyPatch("CmdDropItem")]
        [HarmonyPrefix]
        public static void CmdDropItemPrefix(CharMovement __instance, int itemId)
        {
            LocalDroppedItems.Add(itemId);
        }
    }
}
