// Disabe Error Log For Now Till I Fix False Errors
// Disabe Error Log For Now Till I Fix False Errors
// Disabe Error Log For Now Till I Fix False Errors

using HarmonyLib;



[HarmonyPatch(typeof(ErrorMessagePopup), nameof(ErrorMessagePopup.ShowErrorPopup))]
internal static class ErrorMessagePopup_Patch
{ 
    static bool Prefix()
    {
        return false;
    }
}


[HarmonyPatch(typeof(ErrorMessagePopup), nameof(ErrorMessagePopup.ShowErrorPopup))]
internal static class ErrorMessagePopup_Show_Patch
{
    static bool Prefix() => false;
}


[HarmonyPatch(typeof(ErrorMessagePopup), "OnEnable")]
internal static class ErrorMessagePopup_OnEnable_Patch
{
    static void Postfix(ErrorMessagePopup __instance)
    {
        if (__instance != null)
        {
            __instance.gameObject.SetActive(false);
        }
    }
}

[HarmonyPatch(typeof(DebugManager), "ShowError")]
internal static class DebugManager_ShowError_Patch
{
    static bool Prefix()
    {
        return false;
    }
}
