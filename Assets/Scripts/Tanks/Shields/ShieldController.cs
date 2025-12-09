using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tanks
{
    public class ShieldController : MonoBehaviour
    {
        public List<ShieldSector> Sectors = new();
        public List<ShieldSectorVisual> Visuals = new();
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