// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Combat.Systems;
using PhantomBrigade.Data;
using PBCIViewCombatTimeline = CIViewCombatTimeline;

using UnityEngine;

namespace EchKode.PBMods.TimelinePopups
{
	static class CIViewCombatTimeline
	{
		private sealed class DisplayChange
		{
			internal bool Change;
			internal bool Hover;
			internal bool Drag;
		}

		[System.Flags]
		private enum DisplayElement
		{
			None = 0,
			Target = 1,
			EquipmentInfo = 2,
			Range = 4,
		}

		private static DisplayElement visibleDisplayElements;
		private static DisplayElement suppressedDisplayElements;
		private static Dictionary<int, CIHelperTimelineAction> helpersActionsPlanned;

		internal static void Initialize()
		{
			helpersActionsPlanned = Traverse.Create(PBCIViewCombatTimeline.ins).Field<Dictionary<int, CIHelperTimelineAction>>(nameof(helpersActionsPlanned)).Value;
		}

		internal static void ConfigureActionPlanned(CIHelperTimelineAction helper, int actionID)
		{
			if (ModLink.Settings.hoverDisplayElements == ModLink.ModSettings.DisplayElement.None)
			{
				return;
			}

			var action = IDUtility.GetActionEntity(actionID);
			if (action.isMovementExtrapolated)
			{
				return;
			}
			if (!action.hasStartTime)
			{
				return;
			}
			if (!action.hasDuration)
			{
				return;
			}
			if (!action.hasActiveEquipmentPart)
			{
				return;
			}

			var activePart = IDUtility.GetEquipmentEntity(action.activeEquipmentPart.equipmentID);
			if (activePart == null)
			{
				return;
			}
			if (!activePart.hasPrimaryActivationSubsystem)
			{
				return;
			}

			if (!ModLink.Settings.showPopupsForShields && IsShieldAction(action))
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) Installing hover callbacks on action | popups disabled for shields | action ID: {2}",
						ModLink.modIndex,
						ModLink.modId,
						action.id.id);
				}
				return;
			}

			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) Installing hover callbacks on action | action ID: {2} | owner combat ID: {3} | active part ID: {4}",
					ModLink.modIndex,
					ModLink.modId,
					actionID,
					action.hasActionOwner ? action.actionOwner.combatID : -99,
					activePart.id.id);
			}

			UIHelper.ReplaceCallbackObject(ref helper.button.callbackOnHoverStart, OnActionHoverStart, action);
			UIHelper.ReplaceCallbackObject(ref helper.button.callbackOnHoverEnd, OnActionHoverEnd, action);
		}

		internal static void OnActionDrag(object callbackAsObject)
		{
			if (visibleDisplayElements == DisplayElement.None)
			{
				return;
			}

			if (!CombatUIUtility.IsCombatUISafe())
			{
				return;
			}
			if (!(callbackAsObject is UICallback uiCallback))
			{
				return;
			}

			int argumentInt = uiCallback.argumentInt;
			if (!helpersActionsPlanned.ContainsKey(argumentInt))
			{
				return;
			}
			var action = IDUtility.GetActionEntity(argumentInt);
			if (action == null)
			{
				return;
			}

			suppressedDisplayElements = DisplayElement.None;

			var (target, info, range) = GetDisplayChanges();
			if (target.Change && target.Hover)
			{
				suppressedDisplayElements |= DisplayElement.Target;
				HideTargetedUnit(ModLink.ModSettings.DisplayElement.None);
			}
			if (info.Change && info.Hover)
			{
				suppressedDisplayElements |= DisplayElement.EquipmentInfo;
				HideEquipmentInfo(ModLink.ModSettings.DisplayElement.None);
			}
			if (range.Change && range.Hover)
			{
				suppressedDisplayElements |= DisplayElement.Range;
				HideRange(action, ModLink.ModSettings.DisplayElement.None);
			}

			visibleDisplayElements = DisplayElement.None;

			if (target.Change && target.Drag)
			{
				ShowTargetedUnit(action, ModLink.ModSettings.DisplayElement.None);
			}

			var infoShow = info.Change && info.Drag;
			var rangeShow = range.Change && range.Drag;
			if (!infoShow && !rangeShow)
			{
				return;
			}

			var (ok, combatEntity, partInUnit) = GetComponentsFromAction(action);
			if (!ok)
			{
				return;
			}

			if (infoShow)
			{
				ShowEquipmentInfo(partInUnit, ModLink.ModSettings.DisplayElement.None);
			}
			if (rangeShow)
			{
				ShowRange(combatEntity, partInUnit, ModLink.ModSettings.DisplayElement.None);
			}
		}

		internal static void OnActionDragEnd(object callbackAsObject)
		{
			if (suppressedDisplayElements == DisplayElement.None)
			{
				return;
			}

			suppressedDisplayElements = DisplayElement.None;

			var action = IDUtility.GetActionEntity(((UICallback)callbackAsObject).argumentInt);
			if (action == null)
			{
				return;
			}

			var (target, info, range) = GetDisplayChanges();
			if (target.Change && target.Drag)
			{
				HideTargetedUnit(ModLink.ModSettings.DisplayElement.None);
			}
			if (info.Change && info.Drag)
			{
				HideEquipmentInfo(ModLink.ModSettings.DisplayElement.None);
			}
			if (range.Change && range.Drag)
			{
				HideRange(action, ModLink.ModSettings.DisplayElement.None);
			}

			if (target.Change && target.Hover)
			{
				ShowTargetedUnit(action, ModLink.ModSettings.DisplayElement.None);
			}

			var infoShow = info.Change && info.Hover;
			var rangeShow = range.Change && range.Hover;
			if (!infoShow && !rangeShow)
			{
				return;
			}

			var (ok, combatEntity, partInUnit) = GetComponentsFromAction(action);
			if (!ok)
			{
				return;
			}

			if (infoShow)
			{
				ShowEquipmentInfo(partInUnit, ModLink.ModSettings.DisplayElement.None);
			}
			if (rangeShow)
			{
				ShowRange(combatEntity, partInUnit, ModLink.ModSettings.DisplayElement.None);
			}
		}

		static void OnActionHoverStart(object arg)
		{
			var action = (ActionEntity)arg;
			var isSimulating = Contexts.sharedInstance.combat.Simulating;
			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) Hover start on action | action ID: {2} | is simulating: {3}",
					ModLink.modIndex,
					ModLink.modId,
					action.id.id,
					isSimulating);
			}
			if (isSimulating && !ModLink.Settings.enableTargetPopupInSimulation)
			{
				return;
			}

			ShowTargetedUnit(action);

			if (isSimulating)
			{
				return;
			}

			AudioUtility.CreateAudioEvent("ui_unit_action_hover_on");

			var (ok, combatEntity, partInUnit) = GetComponentsFromAction(action);
			if (!ok)
			{
				return;
			}

			ShowEquipmentInfo(partInUnit);
			ShowRange(combatEntity, partInUnit);
		}

		static void OnActionHoverEnd(object arg)
		{
			var action = (ActionEntity)arg;
			var isSimulating = Contexts.sharedInstance.combat.Simulating;
			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) Hover end on action | action ID: {2} | is simulating: {3}",
					ModLink.modIndex,
					ModLink.modId,
					action.id.id,
					isSimulating);
			}
			if (isSimulating && !ModLink.Settings.enableTargetPopupInSimulation)
			{
				return;
			}

			HideTargetedUnit();

			if (isSimulating)
			{
				return;
			}

			AudioUtility.CreateAudioEvent("ui_unit_action_hover_off");
			HideEquipmentInfo();
			HideRange(action);
		}

		static bool IsShieldAction(ActionEntity action)
		{
			var (dataOK, actionData) = GetActionData(action);
			if (!dataOK)
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) is shield action | action data not OK | action ID: {2}",
						ModLink.modIndex,
						ModLink.modId,
						action.id.id);
				}
				return false;
			}
			var (partOK, _, part) = GetPartInUnit(actionData);
			if (!partOK)
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) is shield action | part not found on unit | action ID: {2}",
						ModLink.modIndex,
						ModLink.modId,
						action.id.id);
				}
				return false;
			}

			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) is shield action | action ID: {2} | tags: {3}",
					ModLink.modIndex,
					ModLink.modId,
					action.id.id,
					string.Join(",", part.tagCache.tags));
			}

			return part.tagCache.tags.Contains("type_defensive");
		}

		static void ShowTargetedUnit(
			ActionEntity action,
			ModLink.ModSettings.DisplayElement checkElement = ModLink.ModSettings.DisplayElement.TargetPopup)
		{
			if ((ModLink.Settings.hoverDisplayElements & checkElement) != checkElement)
			{
				return;
			}

			if (!action.hasTargetedEntity)
			{
				return;
			}

			var targetedUnit = IDUtility.GetCombatEntity(action.targetedEntity.combatID);
			if (targetedUnit == null)
			{
				return;
			}
			if (!targetedUnit.hasPosition)
			{
				return;
			}

			CombatUITargeting.OnTimelineUI(targetedUnit.id.id);
			visibleDisplayElements |= DisplayElement.Target;
		}

		static void ShowEquipmentInfo(
			EquipmentEntity part,
			ModLink.ModSettings.DisplayElement checkElement = ModLink.ModSettings.DisplayElement.EquipmentInfoPopup)
		{
			if ((ModLink.Settings.hoverDisplayElements & checkElement) != checkElement)
			{
				return;
			}

			CIViewBaseCustomizationInfo.ins.RemoveInventoryBinding();
			CIViewBaseCustomizationInfo.ins.SetLocation(CIViewBaseCustomizationInfo.Location.CombatAction);
			CIViewBaseCustomizationInfo.ins.OnPartRefresh(part.id.id);
			visibleDisplayElements |= DisplayElement.EquipmentInfo;
		}

		static void ShowRange(
			CombatEntity combatEntity,
			EquipmentEntity partInUnit,
			ModLink.ModSettings.DisplayElement checkElement = ModLink.ModSettings.DisplayElement.Range)
		{
			if ((ModLink.Settings.hoverDisplayElements & checkElement) != checkElement)
			{
				return;
			}

			WorldUICombat.OnRangeEnd();
			WorldUICombat.OnRangeDisplay(-1, 0, combatEntity.projectedPosition.v, partInUnit);
			visibleDisplayElements |= DisplayElement.Range;
		}

		static void HideTargetedUnit(ModLink.ModSettings.DisplayElement checkElement = ModLink.ModSettings.DisplayElement.TargetPopup)
		{
			if ((ModLink.Settings.hoverDisplayElements & checkElement) != checkElement)
			{
				return;
			}

			CombatUITargeting.OnTimelineUI(-99);
			visibleDisplayElements &= ~DisplayElement.Target;
		}

		static void HideEquipmentInfo(ModLink.ModSettings.DisplayElement checkElement = ModLink.ModSettings.DisplayElement.EquipmentInfoPopup)
		{
			if ((ModLink.Settings.hoverDisplayElements & checkElement) != checkElement)
			{
				return;
			}

			CIViewBaseCustomizationInfo.ins.UnpinEverything();
			CIViewBaseCustomizationInfo.ins.TryExit(null);
			visibleDisplayElements &= ~DisplayElement.EquipmentInfo;
		}

		static void HideRange(
			ActionEntity action,
			ModLink.ModSettings.DisplayElement checkElement = ModLink.ModSettings.DisplayElement.Range)
		{
			if ((ModLink.Settings.hoverDisplayElements & checkElement) != checkElement)
			{
				return;
			}

			var (ok, _, _) = GetComponentsFromAction(action);
			if (!ok)
			{
				return;
			}

			WorldUICombat.OnRangeEnd();
			ActionProjectionSystem.ForceNextUpdate();
			visibleDisplayElements &= ~DisplayElement.Range;
		}

		static (DisplayChange Target, DisplayChange Hover, DisplayChange Range)
			GetDisplayChanges()
		{
			var (targetHover, targetDrag) = IsDisplayElementEnabled(ModLink.ModSettings.DisplayElement.TargetPopup);
			var (infoHover, infoDrag) = IsDisplayElementEnabled(ModLink.ModSettings.DisplayElement.EquipmentInfoPopup);
			var (rangeHover, rangeDrag) = IsDisplayElementEnabled(ModLink.ModSettings.DisplayElement.Range);

			var targetChange = targetHover ^ targetDrag;
			var infoChange = infoHover ^ infoDrag;
			var rangeChange = rangeHover ^ rangeDrag;

			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) display changes | target: {2} | equipment info: {3} | range: {4}",
					ModLink.modIndex,
					ModLink.modId,
					targetChange,
					infoChange,
					rangeChange);
			}

			return (
				new DisplayChange()
				{
					Change = targetChange,
					Hover = targetHover,
					Drag = targetDrag,
				},
				new DisplayChange()
				{
					Change = infoChange,
					Hover = infoHover,
					Drag = infoDrag,
				},
				new DisplayChange()
				{
					Change = rangeChange,
					Hover = rangeHover,
					Drag = rangeDrag,
				});
		}

		static (bool Hover, bool Drag) IsDisplayElementEnabled(ModLink.ModSettings.DisplayElement element)
		{
			var hover = (ModLink.Settings.hoverDisplayElements & element) == element;
			var drag = (ModLink.Settings.dragDisplayElements & element) == element;
			return (hover, drag);
		}

		static (bool, CombatEntity, EquipmentEntity)
			GetComponentsFromAction(ActionEntity action)
		{
			var (dataOK, actionData) = GetActionData(action);
			if (!dataOK)
			{
				return (false, null, null);
			}

			var (partOK, combatEntity, partInUnit) = GetPartInUnit(actionData);
			if (!partOK)
			{
				return (false, null, null);
			}

			return (true, combatEntity, partInUnit);
		}

		static (bool, DataContainerAction) GetActionData(ActionEntity action)
		{
			if (!action.hasDataLinkAction)
			{
				return (false, null);
			}

			var actionData = action.dataLinkAction.data;
			if (actionData == null)
			{
				return (false, null);
			}
			if (actionData.dataEquipment == null)
			{
				return (false, null);
			}
			if (!actionData.dataEquipment.partUsed)
			{
				return (false, null);
			}
			if (actionData.dataEquipment.partSocket == "core")
			{
				return (false, null);
			}

			return (true, actionData);
		}

		static (bool, CombatEntity, EquipmentEntity) GetPartInUnit(DataContainerAction actionData)
		{
			var selectedCombatUnitID = Contexts.sharedInstance.combat.hasUnitSelected
				? Contexts.sharedInstance.combat.unitSelected.id
				: -99;
			var combatEntity = IDUtility.GetCombatEntity(selectedCombatUnitID);
			if (combatEntity == null)
			{
				return (false, null, null);
			}

			var unit = IDUtility.GetLinkedPersistentEntity(combatEntity);
			if (unit == null)
			{
				return (false, null, null);
			}

			var partInUnit = EquipmentUtility.GetPartInUnit(unit, actionData.dataEquipment.partSocket);
			if (partInUnit == null)
			{
				return (false, null, null);
			}

			return (true, combatEntity, partInUnit);
		}
	}
}
