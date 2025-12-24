using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Ships
{
    public class ShieldController : MonoBehaviour
    {

        [FormerlySerializedAs("TankShield")] public Shield ShipShield;
        [FormerlySerializedAs("_tank")] public ShipBase _ship;

        private void Awake()
        {
            _ship = GetComponent<ShipBase>();
            ShipShield.Init();
            StartCoroutine(Update1Sec());
        }

        private IEnumerator Update1Sec()
        {
            while (gameObject.activeSelf)
            {
                yield return new WaitForSeconds(1f);
                ShipShield.Tick();
            }
        }
        private void Update()
        {
            UpdateVisuals();
        }
        
        

        public void OnShieldHit(CalculatedDamage calculatedDamage)
        {
   
            // если сектора нет — прямой урон
            /*
            if (sector == null)
            {
                Ship.TakeDamage(proj.Damage, hitPoint, proj.SourceWeapon);
                return;
            }
            */

            var dmg = calculatedDamage.FinalDamage;

            // Уменьшаем HP щита
            var leftover = ShipShield.Absorb(dmg);

            // визуал
            var vis = ShipShield.Visual;
            vis.Hit(calculatedDamage.HitPoint);
            GameEvent.UiUpdate();
        }
        

        private void UpdateVisuals()
        {
            var t = ShipShield.ShieldHP.Current / ShipShield.ShieldHP.Maximum;
            ShipShield.Visual.SetCharge(t);
        }
    }
}
