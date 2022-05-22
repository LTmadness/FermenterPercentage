using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace FermenterPercentage
{

    [BepInPlugin(GUID, "Fermenter Percentage", "1.0.0")]
    public class FermenterPercentage : BaseUnityPlugin
    {
        private const string GUID = "org.ltmadness.valheim.fermenterpercentage";

        private static ConfigEntry<ProgressText> progressText;
        private static ConfigEntry<bool> color;
        private static ConfigEntry<ProgressBar> progressBar;

        private static HudData hudData;

        public void Awake()
        {
            progressText = Config.Bind<ProgressText>("General", "Progress Text", ProgressText.PERCENT, "Should the progress text be off/percent/time left");
            color = Config.Bind<bool>("Advanced", "Use color", false, "Adds smooth color change to %/time");
            progressBar = Config.Bind<ProgressBar>("Advanced", "Progress Bar", ProgressBar.OFF, "Should the progress bar be off/red/yellow");

            Config.Save();

            Harmony.CreateAndPatchAll(typeof(FermenterPercentage), GUID);
        }

        [HarmonyPatch(typeof(Fermenter), "GetHoverText")]
        [HarmonyPostfix]
        public static void GetHoverText_light(ref Fermenter __instance, ref string __result)
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

                    if (!timePassed.Equals(-1) && timePassed < (double)__instance.m_fermentationDuration)
                    {

                        if ((bool)AccessTools.Field(typeof(Fermenter), "m_exposed").GetValue(__instance))
                        {
                            if (hudData != null)
                            {
                                hudData.m_gui.SetActive(false);
                            }
                            return;
                        }

                        float percentage = (float)(timePassed / (double)__instance.m_fermentationDuration * 100);
                        int perc = (int)percentage;

                        if (hudData == null && !ProgressBar.OFF.Equals(progressBar.Value))
                        {
                            hudData = new HudData();

                            hudData.m_gui = UnityEngine.Object.Instantiate<GameObject>(EnemyHud.instance.m_baseHud, EnemyHud.instance.m_hudRoot.transform);
                            hudData.m_gui.SetActive(true);
                            hudData.m_gui.transform.position = Hud.instance.m_crosshair.transform.position;
                            hudData.m_healthRoot = hudData.m_gui.transform.Find("Health").gameObject;
                            hudData.m_healthFast = hudData.m_healthRoot.transform.Find("health_fast").GetComponent<GuiBar>();
                            hudData.m_healthSlow = hudData.m_healthRoot.transform.Find("health_slow").GetComponent<GuiBar>();
                            hudData.m_name = hudData.m_gui.transform.Find("Name").GetComponent<Text>();
                            hudData.m_name.text = "";
                            hudData.m_level2 = hudData.m_gui.transform.Find("level_2") as RectTransform;
                            hudData.m_level2.gameObject.SetActive(false);
                            hudData.m_level3 = hudData.m_gui.transform.Find("level_3") as RectTransform;
                            hudData.m_level3.gameObject.SetActive(false);
                            hudData.m_alerted = hudData.m_gui.transform.Find("Alerted") as RectTransform;
                            hudData.m_alerted.gameObject.SetActive(false);
                            hudData.m_aware = hudData.m_gui.transform.Find("Aware") as RectTransform;
                            hudData.m_aware.gameObject.SetActive(false);
                        }

                        if (hudData != null)
                        {
                            hudData.m_gui.transform.position = Hud.instance.m_crosshair.transform.position;
                            hudData.m_gui.SetActive(true);
                            if (ProgressBar.YELLOW.Equals(progressBar.Value))
                            {
                                hudData.m_healthSlow.SetValue(percentage);
                                hudData.m_healthSlow.SetMaxValue(100);
                                hudData.m_healthFast.SetValue(0);
                            }
                            else
                            {
                                hudData.m_healthFast.SetValue(percentage);
                                hudData.m_healthFast.SetMaxValue(100);
                                hudData.m_healthSlow.SetValue(0);
                            }
                        }

                        if (color.Value)
                        {
                            string colorHex = $"#{(255 / 100 * (100 - perc)):X2}{(255 / 100 * perc):X2}{0:X2}";

                            if (ProgressText.PERCENT.Equals(progressText.Value))
                            {
                                __result = __result.Replace(")", $"<color={colorHex}>{perc}%</color> )");
                            }
                            else if (ProgressText.TIME.Equals(progressText.Value))
                            {
                                __result = __result.Replace(")", getTimeResult(__instance, timePassed, colorHex));
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
                                __result = __result.Replace(")", getTimeResult(__instance, timePassed, null));

                            }
                            return;
                        }

                    }
                }
            }
            if (hudData != null)
            {
                hudData.m_gui.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
        [HarmonyPrefix]
        public static void UpdateCrosshair(Player player, float bowDrawPercentage)
        {
            GameObject hoverObject = player.GetHoverObject();
            object hoverable = (bool)(UnityEngine.Object)hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null;

            if (!(hoverable is Fermenter))
            {
                if (hudData != null)
                {
                    hudData.m_gui.SetActive(false);
                }
            }

        }

        public static string getTimeResult(Fermenter __instance, double timePassed, string colorHex)
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

        private class HudData
        {
            //public Character m_character;
            //public BaseAI m_ai;
            public GameObject m_gui;
            public GameObject m_healthRoot;
            public RectTransform m_level2;
            public RectTransform m_level3;
            public RectTransform m_alerted;
            public RectTransform m_aware;
            public GuiBar m_healthFast;
            public GuiBar m_healthSlow;
            public Text m_name;
            public float m_hoverTimer = 0f;
        }

        enum ProgressText
        {
            OFF,
            PERCENT,
            TIME
        }

        enum ProgressBar
        {
            OFF,
            RED,
            YELLOW
        }
    }
}