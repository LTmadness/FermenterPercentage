using BepInEx;
using HarmonyLib;
using System;

namespace FermenterPercentage
{

    [BepInPlugin("org.ltmadness.valheim.fermenterpercentage", "Fermenter Percentage", "0.0.2")]
    public class FermenterPercentage : BaseUnityPlugin
    {
        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(FermenterPercentage), null);
        }

        [HarmonyPatch(typeof(Fermenter), "GetHoverText")]
        [HarmonyPrefix]
        public static bool GetHoverText_modded(ref Fermenter __instance, ref string __result)
        {
            ZNetView m_nview = (ZNetView)AccessTools.Field(typeof(Fermenter), "m_nview").GetValue(__instance);
            if (!m_nview.GetZDO().GetString("Content", "").IsNullOrWhiteSpace())
            {
                DateTime d = new DateTime(m_nview.GetZDO().GetLong("StartTime", 0L));
                if (d.Ticks != 0L)
                {
                    double time = (ZNet.instance.GetTime() - d).TotalSeconds;
                    if (time < (double)__instance.m_fermentationDuration)
                    {
                        string contentName = (string)AccessTools.Method(typeof(Fermenter), "GetContentName").Invoke(__instance, new object[] { });
                        int percentage = (int)(time / (double)__instance.m_fermentationDuration * 100);
                        __result = Localization.instance.Localize(__instance.m_name + "( " + contentName + ", $piece_fermenter_fermenting " + percentage + "% )");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}