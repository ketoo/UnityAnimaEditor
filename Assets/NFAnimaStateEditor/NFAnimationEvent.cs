using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NFAnimationEvent
{
	private Dictionary<int, int> mnDamageMap = new Dictionary<int, int> ();

	public void SetDamageNumber (int index, int number)
	{
		mnDamageMap.Add (index, number);
	}

	public void OnStartAnimaEvent(GameObject self, NFAnimaStateType eAnimaType, int index)
	{
		Debug.Log("Start Anima " + eAnimaType.ToString() + " " + index.ToString());
	}

	public void OnEndAnimaEvent(GameObject self, NFAnimaStateType eAnimaType, int index)
	{
		Debug.Log("End Anima " + eAnimaType.ToString() + " " + index.ToString());
	}

	public void OnDamageEvent(GameObject self, GameObject target, int index)
	{
		Debug.Log("On Damage " + self.ToString() + " " + target.ToString() + " " + index.ToString());
	}

	public void OnBulletTouchPositionEvent(GameObject self, Vector3 position, int index)
	{
		Debug.Log("TouchPosition " + index);
	}
	public void OnBulletTouchTargetEvent(GameObject self, GameObject target, int index)
	{
		Debug.Log("TouchTarget " + index);
	}
}
