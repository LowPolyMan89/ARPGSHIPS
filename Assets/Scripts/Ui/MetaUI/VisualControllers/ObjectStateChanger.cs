using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public class ObjectStateChanger : MonoBehaviour
	{
		public List<GameObject> ListObjectsToSwitchState = new List<GameObject>();
		public List<GameObject> ListObjectsToActivate = new List<GameObject>();
		public List<GameObject> ListObjectsToDeactivate= new List<GameObject>();
		public void SwitchState()
		{
			foreach (var obj in ListObjectsToSwitchState)
			{
				obj.SetActive(!obj.activeInHierarchy);
			}
			foreach (var obj in ListObjectsToActivate)
			{
				obj.SetActive(true);
			}
			foreach (var obj in ListObjectsToDeactivate)
			{
				obj.SetActive(false);
			}
		}
	}
}