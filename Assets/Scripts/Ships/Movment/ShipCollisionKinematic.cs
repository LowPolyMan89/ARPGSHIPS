using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ships
{
	public class ShipCollisionKinematic : MonoBehaviour
	{
		[Header("Box Collision")]
		public Vector3 halfSize = new Vector3(1f, 0.5f, 2f);

		[Header("Layers")]
		public LayerMask staticObstacleMask; // стены, здания
		[UnityEngine.Serialization.FormerlySerializedAs("tankMask")]
		public LayerMask shipMask;           // другие корабли

		[HideInInspector] public Vector3 debugNextPos;
		[HideInInspector] public bool debugHasCollision;

		private Transform _tr;
		[SerializeField] private float _castOffset = 0.1f;

		private void Awake()
		{
			_tr = transform;
		}

		/// <summary>
		/// Пытаемся переместиться из currentPos в desiredPos.
		/// Возвращает true, если было столкновение.
		/// </summary>
		public bool Resolve(Vector3 currentPos, Vector3 desiredPos, out Vector3 resolvedPos)
		{
			debugNextPos = desiredPos;

			var move = desiredPos - currentPos;
			var distance = move.magnitude;

			if (distance < 0.0001f)
			{
				resolvedPos = currentPos;
				debugHasCollision = false;
				return false;
			}

			var dir = move.normalized;
			var rot = _tr.rotation;

			var mask = staticObstacleMask | shipMask;

			// ==================================================
			// 1) ОСНОВНОЙ BoxCast
			// ==================================================
			if (Physics.BoxCast(
				    currentPos,
				    halfSize,
				    dir,
				    out var hit,
				    rot,
				    distance + _castOffset,
				    mask))
			{
				if (!IsSelf(hit.collider))
				{
					var hitShip = IsShip(hit.collider);

					// ==========================================
					// ТАНК ↔ ТАНК (мягкое столкновение)
					// ==========================================
					if (hitShip)
					{
						// Разрыв симметрии: уступает тот, у кого InstanceID меньше
						if (GetInstanceID() < hit.collider.GetInstanceID())
						{
							resolvedPos = currentPos;
							debugHasCollision = true;
							return true;
						}

						// Пробуем скользить
						var slideDir = Vector3.ProjectOnPlane(dir, hit.normal).normalized;
						var slidePos = currentPos + slideDir * distance;

						resolvedPos = slidePos;
						debugHasCollision = true;
						return true;
					}

					// ==========================================
					// СТАТИЧЕСКОЕ ПРЕПЯТСТВИЕ (жёсткое)
					// ==========================================
					var hardSlideDir = Vector3.ProjectOnPlane(dir, hit.normal).normalized;
					var hardSlidePos = currentPos + hardSlideDir * distance;

					if (Physics.BoxCast(
						    currentPos,
						    halfSize,
						    hardSlideDir,
						    out var slideHit,
						    rot,
						    distance + _castOffset,
						    staticObstacleMask))
					{
						if (!IsSelf(slideHit.collider))
						{
							// даже скользить нельзя
							resolvedPos = currentPos;
							debugHasCollision = true;
							return true;
						}
					}

					resolvedPos = hardSlidePos;
					debugHasCollision = true;
					return true;
				}
			}

			// ==================================================
			// 2) OverlapBox — ТОЛЬКО статические препятствия
			// ==================================================
			var cols = Physics.OverlapBox(desiredPos, halfSize, rot, staticObstacleMask);
			for (var i = 0; i < cols.Length; i++)
			{
				if (!IsSelf(cols[i]))
				{
					resolvedPos = currentPos;
					debugHasCollision = true;
					return true;
				}
			}

			// чистое движение
			resolvedPos = desiredPos;
			debugHasCollision = false;
			return false;
		}

		// ==================================================
		// ROTATION
		// ==================================================
		public bool RotationBlocked(Vector3 pos, Quaternion newRot)
		{
			var mask = staticObstacleMask | shipMask;

			var cols = Physics.OverlapBox(pos, halfSize, newRot, mask);
			foreach (var col in cols)
			{
				if (!IsSelf(col))
					return true; // вращение приведёт к проникновению
			}

			return false;
		}
		public void PrepareForRotation()
		{
			var hits = Physics.OverlapBox(
				transform.position,
				halfSize,
				transform.rotation,
				shipMask
			);

			foreach (var col in hits)
			{
				if (IsSelf(col))
					continue;

				var delta = transform.position - col.transform.position;
				delta.y = 0f;

				if (delta.sqrMagnitude < 0.0001f)
					delta = transform.right;

				// микро-раздвижение
				transform.position += delta.normalized * 0.02f;
			}
		}
		public void ResolveShipOverlap()
		{
			var hits = Physics.OverlapBox(
				transform.position,
				halfSize,
				transform.rotation,
				shipMask
			);

			foreach (var col in hits)
			{
				if (IsSelf(col))
					continue;

				// вектор от другого корабля
				var otherTr = col.transform;
				var delta = transform.position - otherTr.position;
				delta.y = 0f;

				if (delta.sqrMagnitude < 0.0001f)
					delta = transform.right;

				var dir = delta.normalized;

				// минимальная дистанция между центрами
				var minDist =
					halfSize.z + otherTr.GetComponent<ShipCollisionKinematic>().halfSize.z;

				var currentDist = delta.magnitude;

				var push = minDist - currentDist;
				if (push > 0f)
				{
					// ВАЖНО: раздвигаем оба корабля
					transform.position += dir * (push * 0.5f);
					otherTr.position -= dir * (push * 0.5f);
				}
			}
		}

		// ==================================================
		// HELPERS
		// ==================================================
		private bool IsSelf(Collider col)
		{
			var t = col.transform;
			return t == _tr || t.IsChildOf(_tr);
		}

		private bool IsShip(Collider col)
		{
			return (shipMask.value & (1 << col.gameObject.layer)) != 0;
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			var pos = Application.isPlaying && _tr ? _tr.position : transform.position;
			var rot = Application.isPlaying && _tr ? _tr.rotation : transform.rotation;

			var color = debugHasCollision ? Color.red : Color.green;
			DrawBox(pos, rot, halfSize, color);

			if (Application.isPlaying)
			{
				DrawBox(debugNextPos, rot, halfSize, Color.yellow);
			}
		}

		private void DrawBox(Vector3 pos, Quaternion rot, Vector3 sizeHalf, Color color)
		{
			Handles.color = color;
			var m = Matrix4x4.TRS(pos, rot, Vector3.one);
			using (new Handles.DrawingScope(m))
			{
				Handles.DrawWireCube(Vector3.zero, sizeHalf * 2f);
			}
		}
#endif
	}
}
