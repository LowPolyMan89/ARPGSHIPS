using UnityEngine;

public class ShieldSectorVisual : MonoBehaviour
{
	[SerializeField] private Renderer rnd;

	private Material _mat;

	int idCharge;
	int idHitPos;
	int idHitStrength;
	int idHitTime;

	float hitStrength;
	float hitTime;

	public void Init()
	{
		_mat = Instantiate(rnd.material);
		rnd.material = _mat;

		idCharge = Shader.PropertyToID("_Charge");
		idHitPos = Shader.PropertyToID("_HitPos");
		idHitStrength = Shader.PropertyToID("_HitStrength");
		idHitTime = Shader.PropertyToID("_HitTime");
	}

	private void Update()
	{
		if (hitStrength > 0)
		{
			hitTime += Time.deltaTime;
			hitStrength = Mathf.MoveTowards(hitStrength, 0, Time.deltaTime * 2f);
		}

		_mat.SetFloat(idHitStrength, hitStrength);
		_mat.SetFloat(idHitTime, hitTime);
	}

	public void SetCharge(float t)
	{
		_mat.SetFloat(idCharge, t);
	}

	public void Hit(Vector3 worldPos)
	{
		_mat.SetVector(idHitPos, worldPos);
		hitStrength = 1f;
		hitTime = 0f;
	}
}