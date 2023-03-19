using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Text.RegularExpressions;

namespace FermenterPercentage
{

    [BepInPlugin(GUID, "Fermenter Percentage", "1.1.0")]
    public class FermenterPercentage : BaseUnityPlugin
    {
        private const string GUID = "org.ltmadness.valheim.fermenterpercentage";
        private const string colorRegexPatern = "#(([0-9a-fA-F]{2}){3,4}|([0-9a-fA-F]){3,4})";

        private static ConfigEntry<ProgressText> progressText;
        private static ConfigEntry<bool> color;
        private static ConfigEntry<string> customColor;

        private static Regex colorRegex;

        public void Awake()
        {
            progressText = Config.Bind("General", "Progress Text", ProgressText.PERCENT, "Should the progress text be off/percent/time left");
            color = Config.Bind("Advanced", "Use color", false, "Adds smooth color change to %/time");
            customColor = Config.Bind("Advanced", "Custom Static Color", "", "If left black uses default, format of color #RRGGBB");

            Config.Save();

            System.Console.WriteLine($"Progress Text: {progressText.Value}, Use color: {color.Value}, Custom Static Color: {customColor.Value}");

            colorRegex = new Regex(colorRegexPatern);

            Harmony.CreateAndPatchAll(typeof(FermenterPercentage), GUID);
        }

        [HarmonyPatch(typeof(Fermenter), "GetHoverText")]
        [HarmonyPostfix]
        public static void GetHoverText(ref Fermenter __instance, ref string __result)
        {
            if (__result.IsNullOrWhiteSpace())
            {
                return;
            }

            ZNetView m_nview = (ZNetView)AccessTools.Field(typeof(Fermenter), "m_nview").GetValue(__instance);
            if (!m_nview.GetZDO().GetString("Content", "").IsNullOrWhiteSpace())
            {
                DateTime d = new DateTime(m_nview.GetZDO().GetLong("StartTime", 0L));
                if (d.Ticks != 0L)
                {
                    double timePassed = (ZNet.instance.GetTime() - d).TotalSeconds;

                    if (!timePassed.Equals(-1) && timePassed < __instance.m_fermentationDuration)
                    {
                        float percentage = (float)(timePassed / __instance.m_fermentationDuration * 100);
                        int perc = (int)percentage;

                        if (color.Value)
                        {
                            string colorHex = GetColor(perc);

                            if (ProgressText.PERCENT.Equals(progressText.Value))
                            {
                                __result = __result.Replace(")", $"<color={colorHex}>{perc}%</color> )");
                            }
                            else if (ProgressText.TIME.Equals(progressText.Value))
                            {
                                __result = __result.Replace(")", GetTimeResult(__instance, timePassed, colorHex));
                            }
                            return;
                        }
                        else
                        {
                            if (ProgressText.PERCENT.Equals(progressText.Value))
                            {
                                __result = __result.Replace(")", $"{perc}% )");
                            }
                            else if (ProgressText.TIME.Equals(progressText.Value))
                            {
                                __result = __result.Replace(")", GetTimeResult(__instance, timePassed, null));
                            }
                            return;
                        }
                    }
                }
            }
        }

        public static string GetTimeResult(Fermenter __instance, double timePassed, string colorHex)
        {
            double left = ((double)__instance.m_fermentationDuration) - timePassed;
            int min = (int)Math.Floor(left / 60);
            int sec = ((int)left) % 60;

            if (colorHex != null)
            {
                return $"<color={colorHex}>{min}m {sec}s</color> )";
            }

            return String.Format($"{min}m {sec}s )");
        }

        public static string GetColor(int percentage)
        {
            var csc = customColor.Value.Trim();
            if (!csc.IsNullOrWhiteSpace() && colorRegex.IsMatch(csc))
            {
                return customColor.Value;
            }

            return $"#{255 / 100 * (100 - percentage):X2}{255 / 100 * percentage:X2}{0:X2}";
        }

        enum ProgressText
        {
            OFF,
            PERCENT,
            TIME
        }
    }
}