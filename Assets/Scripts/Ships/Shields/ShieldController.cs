using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ships
{
    public class ShieldController : MonoBehaviour
    {
        public List<ShieldSector> Sectors = new();
        public List<ShieldSectorVisual> Visuals = new();
        public ShipBase Ship;

        private void Awake()
        {
            Ship = GetComponent<ShipBase>();
            StartCoroutine(Update1Sec());
        }

        private IEnumerator Update1Sec()
        {
            while (gameObject.activeSelf)
            {
                yield return new WaitForSeconds(1f);
                foreach (var s in Sectors)
                    s.Tick();
            }
        }
        private void Update()
        {
            UpdateVisuals();
        }

        public void AddSector(ShieldSector sector)
        {
            Sectors.Add(sector);
            RegisterVisual(sector);
        }

        public void RegisterVisual(ShieldSector sector)
        {
            sector.Visual.Init();
            sector.Visual.SetSectorAngles(sector.StartAngle, sector.EndAngle);
            sector.Visual.SetCharge(sector.ShieldHP.Current / sector.ShieldHP.Maximum);
        }
        

        public void OnShieldHit(ShieldSide side, CalculatedDamage calculatedDamage)
        {
            var sector = Sectors.FirstOrDefault(s => s.Side == side);

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
            float leftover = sector.Absorb(dmg);

            // визуал
            var vis = sector.Visual;
            vis.Hit(calculatedDamage.HitPoint);
            GameEvent.UiUpdate();
        }
        

        private void UpdateVisuals()
        {
            foreach (var se in Sectors)
            {
                var t = se.ShieldHP.Current / se.ShieldHP.Maximum;
                se.Visual.SetCharge(t);
            }
        }
    }
}
