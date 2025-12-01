using Ships;
using UnityEngine;

public class ShieldSectorVisual : MonoBehaviour
{
	[SerializeField] private SpriteRenderer sprite;
	public ShieldSide Side;
	private Material mat;

	int idCharge;
	int idStart;
	int idEnd;
	int idHitPoint;
	int idHitStrength;
	int idHitTime;

	float hitStrength;
	float hitTime;

	public void Init()
	{
		mat = Instantiate(sprite.material);
		sprite.material = mat;

		idCharge = Shader.PropertyToID("_Charge");
		idStart = Shader.PropertyToID("_SectorStart");
		idEnd = Shader.PropertyToID("_SectorEnd");
		idHitPoint = Shader.PropertyToID("_HitPoint");
		idHitStrength = Shader.PropertyToID("_HitStrength");
		idHitTime = Shader.PropertyToID("_HitTime");
	}

	private void Update()
	{
		if (hitStrength > 0)
		{
			hitTime += Time.deltaTime;
			hitStrength = Mathf.MoveTowards(hitStrength, 0, Time.deltaTime * 3f);
		}

		mat.SetFloat(idHitStrength, hitStrength);
		mat.SetFloat(idHitTime, hitTime);
	}

	public void SetSectorAngles(float start, float end)
	{
		mat.SetFloat(idStart, start);
		mat.SetFloat(idEnd, end);
	}

	public void SetCharge(float t)
	{
		mat.SetFloat(idCharge, t);
	}

	public void Hit(Vector2 worldPos)
	{
		Vector2 local = transform.InverseTransformPoint(worldPos);
		Vector2 uv = local * 0.5f + new Vector2(0.5f, 0.5f);

		mat.SetVector(idHitPoint, uv);
		hitStrength = 1f;
		hitTime = 0;
	}
}