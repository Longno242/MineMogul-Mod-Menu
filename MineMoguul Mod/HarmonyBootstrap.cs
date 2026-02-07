using UnityEngine;
using HarmonyLib;

namespace MineMoguul_Mod
{
    public class HarmonyBootstrap : MonoBehaviour
    {
        private static Harmony harmony;
        private const string HarmonyId = "com.minemoguul.errorpopup.blocker";

        void Start()
        {
            if (harmony != null) return;

            harmony = new Harmony(HarmonyId);
            harmony.PatchAll();

            Debug.Log("[MineMoguul] Harmony patches applied");
        }

        void OnDestroy()
        {
            if (harmony != null)
            {
                harmony.UnpatchAll(HarmonyId);
                harmony = null;
            }
        }
    }
}
