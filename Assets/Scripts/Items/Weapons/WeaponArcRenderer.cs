using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ships
{
	/// <summary>
	/// Общая реализация отрисовки дуги для оружия.
	/// Работает как с CanvasRenderer, так и с MeshFilter/MeshRenderer.
	/// </summary>
	public class WeaponArcRenderer : MonoBehaviour
	{
		public enum ArcSpace
		{
			Canvas,
			WorldXY,
			WorldXZ
		}

		[SerializeField] protected Color _color = new Color(1f, 0.72f, 0.2f, 0.25f);
		[SerializeField] protected float _arcStepDegrees = 6f;
		[SerializeField] protected int _minSegments = 12;
		[SerializeField] protected int _maxSegments = 96;
		[SerializeField] protected Material _material;
		[SerializeField] protected bool _disableObjectOnHide = true;

		protected Mesh _mesh;
		protected CanvasRenderer _canvasRenderer;
		protected MeshFilter _meshFilter;
		protected MeshRenderer _meshRenderer;
		protected RectTransform _rectTransform;

		protected virtual void Awake()
		{
			CacheComponents();
		}

		protected virtual void OnDisable()
		{
			ClearMesh();
		}

		protected virtual void OnDestroy()
		{
			ClearMesh(true);
		}

		protected void CacheComponents()
		{
			if (_canvasRenderer == null)
				_canvasRenderer = GetComponent<CanvasRenderer>();
			if (_meshFilter == null)
				_meshFilter = GetComponent<MeshFilter>();
			if (_meshRenderer == null)
				_meshRenderer = GetComponent<MeshRenderer>();
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
		}

		protected void RenderArc(float arcDeg, float radius, Vector3 center, ArcSpace space, float innerRadius = 0f)
		{
			if (arcDeg <= 0f || radius <= 0f)
			{
				ClearAndHide();
				return;
			}

			CacheComponents();

			var segments = Mathf.Clamp(
				Mathf.CeilToInt(arcDeg / Mathf.Max(1f, _arcStepDegrees)),
				_minSegments,
				_maxSegments);

			if (_mesh == null)
			{
				_mesh = new Mesh { name = "WeaponArcMesh" };
			}
			else
			{
				_mesh.Clear();
			}

			BuildGeometry(segments, arcDeg, radius, innerRadius, center, space);

			ApplyMesh();
		}

		protected virtual void ClearAndHide()
		{
			ClearMesh();

			if (_disableObjectOnHide && gameObject.activeSelf)
				gameObject.SetActive(false);
		}

		protected void ClearMesh(bool destroying = false)
		{
			if (_mesh != null)
			{
				if (Application.isPlaying && !destroying)
					Destroy(_mesh);
				else
					DestroyImmediate(_mesh);

				_mesh = null;
			}

			if (_canvasRenderer != null)
				_canvasRenderer.SetMesh(null);
			if (_meshFilter != null)
				_meshFilter.sharedMesh = null;
		}

		private void ApplyMesh()
		{
			if (_mesh == null)
				return;

			if (_canvasRenderer != null)
			{
				_canvasRenderer.SetMesh(_mesh);
				_canvasRenderer.SetMaterial(_material != null ? _material : Graphic.defaultGraphicMaterial, null);
			}

			if (_meshFilter != null)
			{
				_meshFilter.sharedMesh = _mesh;

				if (_meshRenderer != null)
				{
					var mat = _material != null ? _material : _meshRenderer.sharedMaterial;
					if (mat != null && _meshRenderer.sharedMaterial != mat)
						_meshRenderer.sharedMaterial = mat;
				}
			}
		}

		private void BuildGeometry(int segments, float arcDeg, float radius, float innerRadius, Vector3 center, ArcSpace space)
		{
			var verts = new List<Vector3>(segments * 2 + 2);
			var cols = new List<Color>(segments * 2 + 2);
			var uvs = new List<Vector2>(segments * 2 + 2);
			var tris = new List<int>(segments * 6);

			var halfAngle = arcDeg * 0.5f;
			var stepDeg = arcDeg / segments;

			if (innerRadius > 0f)
			{
				for (var i = 0; i <= segments; i++)
				{
					var ang = (-halfAngle + stepDeg * i) * Mathf.Deg2Rad;
					var dir = DirFromAngle(ang, space);
					var inner = center + dir * innerRadius;
					var outer = center + dir * radius;

					verts.Add(inner);
					verts.Add(outer);
					cols.Add(_color);
					cols.Add(_color);
					uvs.Add(new Vector2(0f, i / (float)segments));
					uvs.Add(new Vector2(1f, i / (float)segments));
				}

				for (var i = 0; i < segments; i++)
				{
					var idx = i * 2;
					tris.Add(idx);
					tris.Add(idx + 1);
					tris.Add(idx + 2);

					tris.Add(idx + 1);
					tris.Add(idx + 3);
					tris.Add(idx + 2);
				}
			}
			else
			{
				verts.Add(center);
				cols.Add(_color);
				uvs.Add(new Vector2(0.5f, 0.5f));

				for (var i = 0; i <= segments; i++)
				{
					var ang = (-halfAngle + stepDeg * i) * Mathf.Deg2Rad;
					var dir = DirFromAngle(ang, space);
					verts.Add(center + dir * radius);
					cols.Add(_color);

					var uv = space == ArcSpace.WorldXZ
						? new Vector2(dir.x * 0.5f + 0.5f, dir.z * 0.5f + 0.5f)
						: new Vector2(dir.x * 0.5f + 0.5f, dir.y * 0.5f + 0.5f);
					uvs.Add(uv);
				}

				for (var i = 1; i < verts.Count - 1; i++)
				{
					tris.Add(0);
					tris.Add(i);
					tris.Add(i + 1);
				}
			}

			_mesh.SetVertices(verts);
			_mesh.SetColors(cols);
			_mesh.SetUVs(0, uvs);
			_mesh.SetTriangles(tris, 0);
			_mesh.RecalculateBounds();
			_mesh.RecalculateNormals();
		}

		private static Vector3 DirFromAngle(float angleRad, ArcSpace space)
		{
			return space == ArcSpace.WorldXZ
				? new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad))
				: new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad), 0f);
		}
	}
}
