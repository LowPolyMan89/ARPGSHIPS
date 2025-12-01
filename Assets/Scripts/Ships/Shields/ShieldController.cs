using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            RegisterVisual(sector.Side);
        }

        public void RegisterVisual(ShieldSide side)
        {
            foreach (var sectorVisual in Visuals)
            {
                if (sectorVisual.Side == side)
                {
                    var s = Sectors.Find(x => x.Side == side);
                    sectorVisual.Init();
                    sectorVisual.SetSectorAngles(s.StartAngle, s.EndAngle);
                    sectorVisual.SetCharge(s.ShieldHP.Current / s.ShieldHP.Maximum);
                }
            }
            
        }
        

        public void OnShieldHit(ShieldSide side, Vector2 hitPoint, Projectile proj)
        {
            var sector = Sectors.FirstOrDefault(s => s.Side == side);

            // если сектора нет — прямой урон
            if (sector == null)
            {
                Ship.TakeDamage(proj.Damage, hitPoint, proj.SourceWeapon);
                return;
            }

            float dmg = proj.Damage;

            // Уменьшаем HP щита
            float leftover = sector.Absorb(dmg);

            // визуал
            var vis = Visuals.FirstOrDefault(v => v.Side == side);
            if (vis != null)
                vis.Hit(hitPoint);

            // остаток урона — в тело корабля
            if (leftover > 0)
                Ship.TakeDamage(leftover, hitPoint, proj.SourceWeapon);
        }
        

        private void UpdateVisuals()
        {
            for (int i = 0; i < Sectors.Count; i++)
            {
                if (Visuals[i] == null) continue;

                float t = Sectors[i].ShieldHP.Current / Sectors[i].ShieldHP.Maximum;
                Visuals[i].SetCharge(t);
            }
        }
    }
}
