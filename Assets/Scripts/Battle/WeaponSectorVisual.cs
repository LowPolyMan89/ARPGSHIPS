using UnityEngine;

namespace Tanks
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WeaponSectorVisual : MonoBehaviour
    {
        [Header("Manual Settings (not from WeaponSlot)")]
        public float Range = 20f;        // дальность сектора
        public float Angle = 60f;        // угол сектора (в градусах)
        public float InnerRadius = 0f;   // внутренняя дуга (обычно 0)

        [Header("Quality")]
        public int segments = 48;

        private Mesh _mesh;

        private void Awake()
        {
            _mesh = new Mesh();
            _mesh.name = "WeaponSectorMesh3D";
            GetComponent<MeshFilter>().mesh = _mesh;
        }

        private void LateUpdate()
        {
            GenerateArc(InnerRadius, Range, Angle);
        }

        private void GenerateArc(float innerR, float outerR, float angleDeg)
        {
            _mesh.Clear();

            int steps = Mathf.Max(4, segments);
            int vertCount = (steps + 1) * 2;

            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs   = new Vector2[vertCount];
            int[] tris      = new int[steps * 6];

            float halfAngle = angleDeg * 0.5f;
            int v = 0;

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;

                // направление в XZ-плоскости
                Vector3 dir = new Vector3(
                    Mathf.Sin(angle),   // X
                    0f,
                    Mathf.Cos(angle)    // Z
                );

                verts[v]     = dir * innerR;
                verts[v + 1] = dir * outerR;

                uvs[v] = new Vector2(t, 0f);
                uvs[v + 1] = new Vector2(t, 1f);

                v += 2;
            }

            int ti = 0;
            for (int i = 0; i < steps; i++)
            {
                int i0 = i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;

                tris[ti++] = i1;
                tris[ti++] = i0;
                tris[ti++] = i2;

                tris[ti++] = i1;
                tris[ti++] = i2;
                tris[ti++] = i3;
            }

            _mesh.vertices = verts;
            _mesh.uv = uvs;
            _mesh.triangles = tris;

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}
