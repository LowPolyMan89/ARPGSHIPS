using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ships
{
   public class BattleUiWeaponSlot : MonoBehaviour
   {
      public WeaponBase CurrentWeapon;
      public Image WeaponImage;
      public Toggle EnableToggle;
      public ElementsProgressBar AmmoCountBar;
      public Image ReloadingProgressFillImage;
      private int _maxAmmo;

      public void Init(WeaponBase weapon)
      {
         CurrentWeapon = weapon;
         SetupToggle();
         SetupAmmoBar();
         SetupIcon();
      }
      private void OnDisable()
      {
         if (WeaponImage != null)
            WeaponImage.sprite = null;
      }
      private void Update()
      {
         OnUpdate();
      }
      public void OnUpdate()
      {
         if(!CurrentWeapon)
            return;

         UpdateAmmo();
         UpdateReloading();
      }

      private void SetupToggle()
      {
         if (!EnableToggle)
            return;

         EnableToggle.onValueChanged.RemoveAllListeners();
         EnableToggle.isOn = CurrentWeapon != null && CurrentWeapon.IsActive;
         EnableToggle.onValueChanged.AddListener(OnToggleChanged);
      }

      private void OnToggleChanged(bool state)
      {
         if(CurrentWeapon == null)
            return;
         CurrentWeapon.IsActive = state;
      }

      private void SetupAmmoBar()
      {
         if (AmmoCountBar == null || CurrentWeapon?.Model?.Stats == null)
            return;

         var ammoStat = CurrentWeapon.Model.Stats.GetStat(StatType.AmmoCount);
         _maxAmmo = ammoStat != null ? Mathf.RoundToInt(ammoStat.Maximum) : 0;
         if (_maxAmmo > 0)
            AmmoCountBar.Init(_maxAmmo);
      }

      private void UpdateAmmo()
      {
         if (AmmoCountBar == null || _maxAmmo <= 0)
            return;

         AmmoCountBar.CalculateAmount(CurrentWeapon.Ammo, _maxAmmo);
      }

      private void UpdateReloading()
      {
         if (!ReloadingProgressFillImage)
            return;

         var fill = 0f;
         if (CurrentWeapon.IsReloading)
         {
            var reloadStat = CurrentWeapon.Model?.Stats?.GetStat(StatType.ReloadTime);
            var duration = reloadStat != null ? reloadStat.Maximum : 0f;
            if (duration > 0f)
            {
               var remaining = CurrentWeapon.ReloadFinishTime - Time.time;
               fill = 1f - Mathf.Clamp01(remaining / duration);
            }
         }

         ReloadingProgressFillImage.fillAmount = fill;
      }

      private void SetupIcon()
      {
         if (WeaponImage != null)
            WeaponImage.enabled = false;

         if (CurrentWeapon == null)
            return;

         if (string.IsNullOrEmpty(CurrentWeapon.WeaponTemplateId))
            return;

         var item = new InventoryItem { TemplateId = CurrentWeapon.WeaponTemplateId };
         var sprite = ResourceLoader.LoadItemIcon(item, ItemIconContext.Inventory);
         if (WeaponImage == null)
            return;

         WeaponImage.sprite = sprite;
         WeaponImage.enabled = sprite != null;
      }
   }
}
