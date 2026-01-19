using UnityEngine;

public class ShieldSectorVisual : MonoBehaviour
{
	private const int MaxHits = 6;

	[SerializeField] private Renderer rnd;
	[SerializeField] private Transform centerOverride;
	[SerializeField] private float radiusOverride;
	[SerializeField] private Collider hitSurface;
	[SerializeField] private float appearDuration = 0.6f;
	[SerializeField] private float breakDuration = 0.5f;
	[SerializeField] private float breakFlickerSpeed = 24f;

	private Material _mat;
	private bool _isSpriteRenderer;
	private float _lastCharge;
	private float _appearTimeLeft;
	private float _breakTimeLeft;
	private int _nextHitIndex;

	int idCharge;
	int idHitPos;
	int idHitStrength;
	int idHitTime;
	int idAppear;
	int idBreak;

	private Vector4[] _hitPos;
	private float[] _hitStrengths;
	private float[] _hitTimes;

	public void Init()
	{
		_mat = Instantiate(rnd.material);
		rnd.material = _mat;
		_isSpriteRenderer = rnd is SpriteRenderer;

		idCharge = Shader.PropertyToID("_Charge");
		idHitPos = Shader.PropertyToID("_HitPos");
		idHitStrength = Shader.PropertyToID("_HitStrength");
		idHitTime = Shader.PropertyToID("_HitTime");
		idAppear = Shader.PropertyToID("_Appear");
		idBreak = Shader.PropertyToID("_Break");

		_lastCharge = 0f;
		_nextHitIndex = 0;

		_hitPos = new Vector4[MaxHits];
		_hitStrengths = new float[MaxHits];
		_hitTimes = new float[MaxHits];
	}

	private void Update()
	{
		for (int i = 0; i < MaxHits; i++)
		{
			if (_hitStrengths[i] <= 0f)
				continue;

			_hitTimes[i] += Time.deltaTime;
			_hitStrengths[i] = Mathf.MoveTowards(_hitStrengths[i], 0, Time.deltaTime * 2f);
		}

		_mat.SetVectorArray(idHitPos, _hitPos);
		_mat.SetFloatArray(idHitStrength, _hitStrengths);
		_mat.SetFloatArray(idHitTime, _hitTimes);

		if (_appearTimeLeft > 0f)
			_appearTimeLeft = Mathf.Max(0f, _appearTimeLeft - Time.deltaTime);

		if (_breakTimeLeft > 0f)
			_breakTimeLeft = Mathf.Max(0f, _breakTimeLeft - Time.deltaTime);

		var appearT = appearDuration > 0f ? _appearTimeLeft / appearDuration : 0f;
		var breakT = breakDuration > 0f ? _breakTimeLeft / breakDuration : 0f;
		var breakFlicker = breakT * Mathf.Abs(Mathf.Sin(Time.time * breakFlickerSpeed));

		_mat.SetFloat(idAppear, appearT);
		_mat.SetFloat(idBreak, breakFlicker);
	}

	public void SetCharge(float t)
	{
		_mat.SetFloat(idCharge, t);
		if (_lastCharge <= 0.001f && t > 0.001f)
			_appearTimeLeft = appearDuration;
		else if (_lastCharge > 0.001f && t <= 0.001f)
			_breakTimeLeft = breakDuration;

		_lastCharge = t;
	}

	public void Hit(Vector3 worldPos)
	{
		var hitPos = worldPos;
		if (hitSurface != null)
		{
			hitPos = hitSurface.ClosestPoint(worldPos);
		}
		else
		{
		if (!_isSpriteRenderer)
		{
			var center = centerOverride != null ? centerOverride.position : rnd.bounds.center;
			var radius = radiusOverride > 0f
				? radiusOverride
				: Mathf.Max(rnd.bounds.extents.x, rnd.bounds.extents.y, rnd.bounds.extents.z);

			if (radius > 0.0001f)
			{
				var dir = worldPos - center;
				if (dir.sqrMagnitude < 0.0001f)
					dir = rnd.transform.forward;

				hitPos = center + dir.normalized * radius;
			}
		}
		}

		_hitPos[_nextHitIndex] = hitPos;
		_hitStrengths[_nextHitIndex] = 1f;
		_hitTimes[_nextHitIndex] = 0f;
		_nextHitIndex = (_nextHitIndex + 1) % MaxHits;
	}
}
