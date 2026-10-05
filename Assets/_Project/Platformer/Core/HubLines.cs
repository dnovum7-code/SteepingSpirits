using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Picks what the spirits on the clearing say, from the player's progress.
    /// NPCs with the line key "hub" talk about the trips, "hub_shelf" about the
    /// rare finds. Other keys are returned unchanged.
    /// </summary>
    public static class HubLines
    {
        public static string Pick(string key, JumpNRunSave save, int levelCount, int rareTotal)
        {
            if (save == null)
            {
                return key;
            }

            switch (key)
            {
                case "hub":
                {
                    int done = 0;
                    foreach (string _ in save.Completed) done++;
                    if (done == 0) return "hub_welcome";
                    if (levelCount > 0 && done >= levelCount) return save.ChallengeCount >= levelCount ? "hub_all_lanterns" : "hub_all";
                    if (done == 1) return "hub_first";
                    return "hub_more";
                }
                case "hub_shelf":
                {
                    int rares = save.RareCount;
                    if (rares == 0) return "shelf_empty";
                    if (rares >= rareTotal) return "shelf_full";
                    return rares * 2 >= rareTotal ? "shelf_half" : "shelf_some";
                }
                default:
                    return key;
            }
        }
    }
}
