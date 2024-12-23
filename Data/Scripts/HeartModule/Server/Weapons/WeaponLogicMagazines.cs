﻿using System;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Weapons
{
    internal class WeaponLogicMagazines
    {
        Loading Definition;
        Audio DefinitionAudio;
        private readonly Func<IMyInventory> GetInventoryFunc;

        private int _selectedAmmoIndex = 0;
        private int shotsPerMag = 0;

        public ProjectileDefinitionBase CurrentAmmo => DefinitionManager.ProjectileDefinitions[Definition.Ammos[SelectedAmmoIndex]];

        public int SelectedAmmoIndex
        {
            get
            {
                return _selectedAmmoIndex;
            }
            set
            {
                if (Definition.Ammos.Length <= value || value < 0)
                    return;
                _selectedAmmoIndex = value;
                shotsPerMag = CurrentAmmo.UngroupedDef.ShotsPerMagazine;

                if (value == _selectedAmmoIndex)
                    return;
                EmptyMagazines();
            }
        }

        public WeaponLogicMagazines(SorterWeaponLogic weapon, Func<IMyInventory> getInventoryFunc, bool startLoaded = false)
        {
            Definition = weapon.Definition.Loading;
            DefinitionAudio = weapon.Definition.Audio;
            GetInventoryFunc = getInventoryFunc;
            RemainingReloads = Definition.MaxReloads;
            NextReloadTime = Definition.ReloadTime;
            SelectedAmmoIndex = 0;
            if (startLoaded)
            {
                MagazinesLoaded = Definition.MagazinesToLoad;
                ShotsInMag = CurrentAmmo.UngroupedDef.ShotsPerMagazine;
            }
        }

        public int MagazinesLoaded = 0;
        public int ShotsInMag = 0;
        public float NextReloadTime = -1; // In seconds
        public int RemainingReloads;

        public void UpdateReload(float delta = 1 / 60f)
        {
            if (RemainingReloads == 0)
                return;

            if (MagazinesLoaded >= Definition.MagazinesToLoad) // Don't load mags if already at capacity
                return;

            if (NextReloadTime == -1)
                return;

            NextReloadTime -= delta;

            if (NextReloadTime <= 0)
            {
                var inventory = GetInventoryFunc?.Invoke();
                string magazineItem = CurrentAmmo.UngroupedDef.MagazineItemToConsume;

                // Check and remove the specified item from the inventory
                if (!string.IsNullOrWhiteSpace(magazineItem) && inventory != null)
                {
                    var itemToConsume = new MyDefinitionId(typeof(MyObjectBuilder_Component), magazineItem);
                    if (inventory.ContainItems(1, itemToConsume))
                    {
                        inventory.RemoveItemsOfType(1, itemToConsume);

                        // Notify item consumption
                        MyVisualScriptLogicProvider.ShowNotification($"Consumed 1 {magazineItem} for reloading.", 1000 / 60, "White");

                        // Reload logic
                        MagazinesLoaded++;
                        RemainingReloads--;
                        NextReloadTime = Definition.ReloadTime;
                        ShotsInMag += shotsPerMag;

                        if (!string.IsNullOrEmpty(DefinitionAudio.ReloadSound))
                        {
                            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(DefinitionAudio.ReloadSound, Vector3D.Zero); // Assuming Vector3D.Zero as placeholder
                        }
                    }
                    else
                    {
                        // Notify item not available
                        //MyVisualScriptLogicProvider.ShowNotification($"Unable to reload - {magazineItem} not found in inventory.", 1000 / 60, "Red");
                        return;
                    }
                }
                else
                {
                    // Notify when MagazineItemToConsume is not specified
                    // TODO: Note in debug log
                    //MyVisualScriptLogicProvider.ShowNotification("MagazineItemToConsume not specified, proceeding with default reload behavior.", 1000 / 60, "Blue");
                }

                MagazinesLoaded++;
                RemainingReloads--;
                NextReloadTime = Definition.ReloadTime;
                ShotsInMag += shotsPerMag;
            }
        }

        public bool IsLoaded => ShotsInMag > 0;

        /// <summary>
        /// Mark a bullet as fired.
        /// </summary>
        public void UseShot()
        {
            ShotsInMag--;
            if (ShotsInMag % shotsPerMag == 0)
            {
                MagazinesLoaded--;
            }
        }

        public void EmptyMagazines()
        {
            ShotsInMag = 0;
            MagazinesLoaded = 0;
            NextReloadTime = Definition.ReloadTime;
        }
    }
}
