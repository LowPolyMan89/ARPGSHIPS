using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tanks
{
    public class ShieldController : MonoBehaviour
    {

        public Shield TankShield;
        public TankBase _tank;

        private void Awake()
        {
            _tank = GetComponent<TankBase>();
            StartCoroutine(Update1Sec());
        }

        private IEnumerator Update1Sec()
        {
            while (gameObject.activeSelf)
            {
                yield return new WaitForSeconds(1f);
                TankShield.Tick();
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

            float dmg = calculatedDamage.FinalDamage;

            // Уменьшаем HP щита
            float leftover = TankShield.Absorb(dmg);

            // визуал
            var vis = TankShield.Visual;
            vis.Hit(calculatedDamage.HitPoint);
            GameEvent.UiUpdate();
        }
        

        private void UpdateVisuals()
        {
            var t = TankShield.ShieldHP.Current / TankShield.ShieldHP.Maximum;
            TankShield.Visual.SetCharge(t);
        }
    }
}