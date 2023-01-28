// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;
using PhantomBrigade.Combat.Systems;
using PhantomBrigade.Data;

namespace EchKode.PBMods.TimelinePopups
{
	static class CIViewCombatAction
	{
		internal static void OnActionHoverEnd(object actionDataArg)
		{
			if (!(actionDataArg is DataContainerAction dataContainerAction))
			{
				return;
			}

			AudioUtility.CreateAudioEvent("ui_unit_action_hover_off");
			CIViewBaseCustomizationInfo.ins.UnpinEverything();
			CIViewBaseCustomizationInfo.ins.TryExit(null);
			if (dataContainerAction.dataEquipment == null)
			{
				return;
			}
			if (!dataContainerAction.dataEquipment.partUsed)
			{
				return;
			}
			if (dataContainerAction.dataEquipment.partSocket == "core")
			{
				return;
			}

			var selectedCombatUnitID = Contexts.sharedInstance.combat.hasUnitSelected
				? Contexts.sharedInstance.combat.unitSelected.id
				: -99;
			var combatUnit = IDUtility.GetCombatEntity(selectedCombatUnitID);
			var unit = IDUtility.GetLinkedPersistentEntity(combatUnit);
			var partInUnit = EquipmentUtility.GetPartInUnit(unit, dataContainerAction.dataEquipment.partSocket);
			if (partInUnit != null)
			{
				WorldUICombat.OnRangeEnd();
				ActionProjectionSystem.ForceNextUpdate();
			}
		}

		internal static void TryEntry()
		{
			CIViewBaseCustomizationInfo.ins.RemoveInventoryBinding();
		}
	}
}
