using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace FermenterPercentage
{

    [BepInPlugin(GUID, "Fermenter Percentage", "0.0.4")]
    public class FermenterPercentage : BaseUnityPlugin
    {
        private const string GUID = "org.ltmadness.valheim.fermenterpercentage";

        private static ConfigEntry<bool> useTime;
        private static ConfigEntry<bool> color;

        public void Awake()
        {
            useTime = Config.Bind<bool>("General", "Show time left until done", false, "instead of percentage it shows timer in minutes how much left until fermentig finished");
            color = Config.Bind<bool>("Advanced", "Use color", false, "adds smooth color change to %/time");

            Config.Save();

            Harmony.CreateAndPatchAll(typeof(FermenterPercentage), GUID);
        }


        [HarmonyPatch(typeof(Fermenter), "GetHoverText")]
        [HarmonyPostfix]
        public static void GetHoverText_light(ref Fermenter __instance, ref string __result)
        {
            if(__result.IsNullOrWhiteSpace())
            {
                return;
            }

            ZNetView m_nview = (ZNetView)AccessTools.Field(typeof(Fermenter), "m_nview").GetValue(__instance);
            if (!m_nview.GetZDO().GetString("Content", "").IsNullOrWhiteSpace())
            {
                DateTime d = new DateTime(m_nview.GetZDO().GetLong("StartTime", 0L));
                if (d.Ticks != 0L)
                {
                    double time = (ZNet.instance.GetTime() - d).TotalSeconds;
                    if (!time.Equals(-1) && time < (double)__instance.m_fermentationDuration)
                    {

                        if ((bool)AccessTools.Field(typeof(Fermenter), "m_exposed").GetValue(__instance))
                        {
                            return;
                        }

                        int percentage = (int)(time / (double)__instance.m_fermentationDuration * 100);

                        if (color.Value)
                        {
                            string colorHex = $"#{(255 / 100 * (100 - percentage)):X2}{(255 / 100 * percentage):X2}{0:X2}";

                            if (!useTime.Value)
                            {
                                __result = __result.Replace(")", $"<color={colorHex}>{percentage}%</color> )");
                            }
                            else
                            {
                                double left = ((double)__instance.m_fermentationDuration) - time;
                                int min = (int)Math.Floor(left / 60);
                                int sec = ((int)left) % 60;

                                __result = __result.Replace(")", $"<color={colorHex}>{min}m {sec}s</color> )");
                            }
                            ZLog.Log(__result);
                            return;
                        }


                        if (!useTime.Value)
                        {

                            __result = __result.Replace(")", $"{percentage}% )");
                        }
                        else
                        {
                            double left = ((double)__instance.m_fermentationDuration) - time;
                            int min = (int)Math.Floor(left / 60);
                            int sec = ((int)left) % 60;

                            __result = __result.Replace(")", $"{min}m {sec}s )");
                        }
                    }
                }
            }
        }
    }
}