using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace FermenterPercentage
{

    [BepInPlugin(GUID, "Fermenter Percentage", "0.0.3")]
    public class FermenterPercentage : BaseUnityPlugin
    {
        private const string GUID = "org.ltmadness.valheim.fermenterpercentage";

        private static ConfigEntry<bool> useTime;

        public void Awake()
        {
            useTime = Config.Bind<bool>("General", "Show time left until done", false, "instead of percentage it shows timer in minutes how much left until fermentig finished");

            Harmony.CreateAndPatchAll(typeof(FermenterPercentage), GUID);
        }


        [HarmonyPatch(typeof(Fermenter), "GetHoverText")]
        [HarmonyPrefix]
        public static bool GetHoverText_light(ref Fermenter __instance, ref string __result)
        {
            ZNetView m_nview = (ZNetView)AccessTools.Field(typeof(Fermenter), "m_nview").GetValue(__instance);
            if (!m_nview.GetZDO().GetString("Content", "").IsNullOrWhiteSpace())
            {
                DateTime d = new DateTime(m_nview.GetZDO().GetLong("StartTime", 0L));
                if (d.Ticks != 0L)
                {
                    double time = (ZNet.instance.GetTime() - d).TotalSeconds;
                    if (!time.Equals(-1) && time < (double)__instance.m_fermentationDuration)
                    {
                        string contentName = (string)AccessTools.Method(typeof(Fermenter), "GetContentName").Invoke(__instance, new object[] { });
                        if ((bool)AccessTools.Field(typeof(Fermenter), "m_exposed").GetValue(__instance))
                        {
                            __result =  Localization.instance.Localize(__instance.m_name + " ( " + contentName + ", $piece_fermenter_exposed )");
                        }

                        if (!useTime.Value)
                        {
                            int percentage = (int)(time / (double)__instance.m_fermentationDuration * 100);
                            __result = Localization.instance.Localize(__instance.m_name + "( " + contentName + ", $piece_fermenter_fermenting " + percentage + "% )");
                        }
                        else
                        {
                            double left = ((double)__instance.m_fermentationDuration) - time;
                            int min = (int)Math.Floor(left / 60);
                            int sec = ((int)left) % 60;

                            __result = Localization.instance.Localize(__instance.m_name + "( " + contentName + ", $piece_fermenter_fermenting " + min + "m " + sec + "s )");
                        }
                        return false;
                    }
                }
            }
            return true;
        }
    }
}