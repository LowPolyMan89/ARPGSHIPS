using Unity.VisualScripting;
using UnityEngine;

namespace Tanks
{
	using UnityEngine;

	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class WeaponSectorVisual : MonoBehaviour
	{
		public WeaponSlot slot;

		[Header("Visual")] public Color color = new Color(1f, 1f, 1f, 1f);
		public int segments = 48;

		[Header("Arc settings")] public float innerOffset = 3f; // отступ внутренней дуги

		private Mesh mesh;

		private void Awake()
		{
			mesh = new Mesh();
			mesh.name = "WeaponSectorMesh";
			GetComponent<MeshFilter>().mesh = mesh;

			if (!slot)
				slot = GetComponentInParent<WeaponSlot>();
		}

		private void LateUpdate()
		{
			if (!slot || slot.MountedWeapon == null || slot.MountedWeapon.Model == null)
				return;

			// позиция/поворот слота
			transform.position = slot.transform.position;
			transform.rotation = slot.transform.rotation;

			// игнорируем scale родителей
			transform.localScale = Vector3.one;

			float outerRadius = slot.MountedWeapon.Model.Stats.GetStat(StatType.FireRange).Current;
			float innerRadius = Mathf.Max(0, outerRadius - innerOffset);

			GenerateArc(innerRadius, outerRadius, slot.AllowedAngle);
		}

		private void GenerateArc(float innerR, float outerR, float angle)
		{
			mesh.Clear();

			int steps = Mathf.Max(4, segments);
			int vertCount = (steps + 1) * 2;

			Vector3[] verts = new Vector3[vertCount];
			Vector2[] uvs = new Vector2[vertCount];
			int[] tris = new int[steps * 6];

			float parentScale = slot.transform.lossyScale.x;

			innerR /= parentScale;
			outerR /= parentScale;

			int v = 0;

			for (int i = 0; i <= steps; i++)
			{
				float t = i / (float)steps;
				float rad = Mathf.Lerp(-angle, angle, t) * Mathf.Deg2Rad;

				Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);

				// внутренняя дуга
				verts[v] = dir * innerR;
				uvs[v] = new Vector2(t, 0f);

				// внешняя дуга
				verts[v + 1] = dir * outerR;
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

			mesh.vertices = verts;
			mesh.uv = uvs;
			mesh.triangles = tris;

			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
		}
	}
}