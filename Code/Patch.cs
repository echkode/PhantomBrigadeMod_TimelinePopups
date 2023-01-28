// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;

using PBCIViewCombatAction = CIViewCombatAction;
using PBCIViewCombatTimeline = CIViewCombatTimeline;

namespace EchKode.PBMods.TimelinePopups
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBCIViewCombatAction), "OnActionHoverEnd")]
		[HarmonyPrefix]
		static bool Civca_OnActionHoverEndPrefix(object actionDataArg)
		{
			CIViewCombatAction.OnActionHoverEnd(actionDataArg);
			return false;
		}

		[HarmonyPatch(typeof(PBCIViewCombatAction), "TryEntry")]
		[HarmonyPostfix]
		static void Civca_TryEntryPostfix()
		{
			CIViewCombatAction.TryEntry();
		}

		[HarmonyPatch(typeof(PBCIViewCombatTimeline), "ConfigureActionPlanned")]
		[HarmonyPostfix]
		static void Civct_ConfigureActionPlannedPostfix(CIHelperTimelineAction helper, int actionID)
		{
			CIViewCombatTimeline.ConfigureActionPlanned(helper, actionID);
		}

		[HarmonyPatch(typeof(PBCIViewCombatTimeline), "OnActionDrag")]
		[HarmonyPostfix]
		static void Civct_OnActionDragPostfix(object callbackAsObject)
		{
			CIViewCombatTimeline.OnActionDrag(callbackAsObject);
		}

		[HarmonyPatch(typeof(PBCIViewCombatTimeline), "OnActionDragEnd")]
		[HarmonyPostfix]
		static void Civct_OnActionDragEndPostfix(object callbackAsObject)
		{
			CIViewCombatTimeline.OnActionDragEnd(callbackAsObject);
		}
	}
}
