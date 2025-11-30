using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ships
{
    public class ShieldController : MonoBehaviour
    {
        public List<ShieldSector> Sectors = new();
        public List<ShieldSectorVisual> Visuals = new();

        private ShipBase ship;

        private void Awake()
        {
            ship = GetComponent<ShipBase>();
        }

        private void Update()
        {
            foreach (var s in Sectors)
                s.Tick();

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
                    sectorVisual.SetSectorAngles(s.StartAngle, s.EndAngle);
                    sectorVisual.SetCharge(s.ShieldHP.Current / s.ShieldHP.Maximum);
                }
            }
            
        }

        public int FindSectorIndex(Vector2 hitDir)
        {
            Vector2 forward = ship.transform.up;
            float angle = Vector2.SignedAngle(forward, hitDir);

            for (int i = 0; i < Sectors.Count; i++)
            {
                if (Sectors[i].ContainsAngle(angle))
                    return i;
            }

            return -1;
        }

        public float ApplyDamage(int index, float damage)
        {
            if (index < 0) return damage;

            float leftover = Sectors[index].Absorb(damage);

            return leftover;
        }

        public void OnSectorHit(int index, Vector2 worldPos)
        {
            if (index >= 0 && Visuals[index] != null)
                Visuals[index].Hit(worldPos);
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
