﻿using System.Linq;
using CrowdedRoles.Components;
using CrowdedRoles.UI;
using HarmonyLib;
using Reactor.Extensions;
using UnityEngine;

namespace CrowdedRoles.Patches
{
    internal static class UIPatches
    {
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class HudManager_Start
        {
            public static void Postfix(HudManager __instance)
            {
                int i = 0;
                foreach (var button in ButtonManager.RegisteredButtons)
                {
                    GameObject copiedObject = Object.Instantiate(__instance.KillButton.gameObject, __instance.transform);
                    copiedObject.name = "CustomButton_" + i++;
                    var customManager = copiedObject.AddComponent<CustomButtonManager>();
                    var killButtonManager = copiedObject.GetComponent<KillButtonManager>();
                    customManager.TimerText = killButtonManager.TimerText; // It works very weird, but works
                    killButtonManager.Destroy();
                    customManager.Button = button;
                    button.CustomButtonManager = customManager;
                }
            }
        }

        [HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
        public static class IntroCutscene_CoBegin
        {
            public static void Postfix(bool __result)
            {
                if (!__result)
                {
                    ButtonManager.ActiveButtons.Clear();
                    foreach (CooldownButton button in ButtonManager.RegisteredButtons.Where(button => button.CanUse()))
                    {
                        ButtonManager.ActiveButtons.Add(button);
                        button.CustomButtonManager.gameObject.SetActive(true);
                    }
                }
            }
        }

        [HarmonyPatch]
        public static class SetActivePatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
            public static void HudManager_SetHudActive([HarmonyArgument(0)] bool isActive)
            {
                foreach (CooldownButton button in ButtonManager.ActiveButtons)
                {
                    button.Visible = button.ShouldSetActive(isActive && !PlayerControl.LocalPlayer.Data.IsDead, SetActiveReason.Hud);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
            public static void PlayerControl_Die(PlayerControl __instance)
            {
                if (!__instance.AmOwner) return;
                
                foreach (CooldownButton button in ButtonManager.ActiveButtons)
                {
                    button.Visible = button.ShouldSetActive(false, SetActiveReason.Death);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Revive))]
            public static void PlayerControl_Revive(PlayerControl __instance)
            {
                if (!__instance.AmOwner) return;
                
                foreach (CooldownButton button in ButtonManager.ActiveButtons)
                {
                    button.Visible = button.ShouldSetActive(true, SetActiveReason.Revival);
                }
            }
        }
    }
}