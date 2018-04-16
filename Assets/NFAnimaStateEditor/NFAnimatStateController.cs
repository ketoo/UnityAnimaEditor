using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using DG.Tweening;

public class NFAnimatStateController : MonoBehaviour
{
	[SerializeField] public NFAnimaStateData mxSkillData;
	private Animator mAnimator;
	private NFAnimationEvent mxAnimationEvent = new NFAnimationEvent ();
	private NFAnimaStateType meLastPlayID  = NFAnimaStateType.Idle;
	private static int mnSkillIndex = 0;

	struct BulletTrace
	{
		public GameObject bullet;
		public GameObject target;
		public int index;
		public int gameID;
		public BulletStruct.MoveType movetype;
	}

	private List<BulletTrace> mxBulletTraceInfo = new List<BulletTrace>();


	public NFAnimationEvent GetAnimationEvent()
	{
		return mxAnimationEvent;
	}

	public GameObject[] EnemyList;

    void Start ()
    {
		mAnimator = transform.GetComponent<Animator> ();
    }

	//the use should pass a position when the bullet need a pos
	public int PlayAnimaState (NFAnimaStateType eAnimaType)
	{
		return PlayAnimaState (eAnimaType, Vector3.zero);
	}

	public int PlayAnimaState (NFAnimaStateType eAnimaType, Vector3 v)
    {
		if (meLastPlayID == eAnimaType)
		{
			return -1;
		}

		mxAnimationEvent.OnEndAnimaEvent (this.gameObject, meLastPlayID, mnSkillIndex);

		mnSkillIndex++;

		mxAnimationEvent.OnStartAnimaEvent (this.gameObject, eAnimaType, mnSkillIndex);

		for (int i = 0; i < mxSkillData.AnimationSkillList.Count; ++i)
		{
			AnimationSkillStruct xAnimationSkillStruct = mxSkillData.AnimationSkillList [i];
			if (xAnimationSkillStruct.Type == eAnimaType)
			{
				if (xAnimationSkillStruct.AnimationClip != null) 
				{
					mAnimator.Play (eAnimaType.ToString (), 0);
				} 
				else 
				{
					UnityEditor.EditorUtility.DisplayDialog ("Warning", "The AnimationClip is null!", "OK", "Cancel");
				}

				foreach (EffectStruct es in xAnimationSkillStruct.EffectStructList)
				{
					if (es.Effect != null)
					{
						es.index = mnSkillIndex;
						StartCoroutine (WaitPlayEffect (es));
					}
				}
				foreach (AudioStruct es in xAnimationSkillStruct.AudioStructList)
				{
					if (es.Audio != null)
					{
						es.index = mnSkillIndex;
						StartCoroutine (WaitPlayAudio (es));
					}
				}

				foreach (BulletStruct es in xAnimationSkillStruct.BulletStructList)
				{
					if (es.Bullet != null)
					{
						es.index = mnSkillIndex;
						StartCoroutine (WaitPlayBullet (es, v));
					}
				}
				foreach (MovementStruct es in xAnimationSkillStruct.MovementStructList)
				{
					es.index = mnSkillIndex;
					StartCoroutine (WaitPlayMovement (es));
				}

				if (xAnimationSkillStruct.BulletStructList.Count <= 0)
				{
					foreach (DamageStruct es in xAnimationSkillStruct.DamageStructList)
					{
						es.index = mnSkillIndex;
						StartCoroutine (WaitPlayDamage (es));
					}
				}

				foreach (CameraStruct es in xAnimationSkillStruct.CameraStructList)
				{
					es.index = mnSkillIndex;
					StartCoroutine (WaitPlayCamera (es));
				}

				meLastPlayID = eAnimaType;

				//get time
				if (eAnimaType != NFAnimaStateType.Idle)
				{
					StartCoroutine (WaitPlayNextAnim(xAnimationSkillStruct.fTime, xAnimationSkillStruct.NextType));
				}

			}
		}

		return mnSkillIndex;
    }

	IEnumerator WaitPlayNextAnim (float time, NFAnimaStateType nextType)
	{
		yield return new WaitForSeconds (time);

		PlayAnimaState (nextType);
	}

	IEnumerator WaitPlayEffect (EffectStruct es)
    {
        //如果特效没有启用则不进行播放
        if (es.isEnabled)
            yield return new WaitForSeconds (es.DelayTime);
        else
            yield break;

        Vector3 _pos = Vector3.zero;
        Quaternion _rotation = es.Effect.transform.rotation;
        Transform _parent = null;

        if (es.VirtualPointName != "None")
        {
            Transform _targetTrans = FindTransform (es.VirtualPointName);
            if (_targetTrans)
            {
                _pos = _targetTrans.position + es.Offset;

                if (es.IsFollow)
                    _parent = _targetTrans;
            }
            else
            {
                Debug.LogError ("The specified virtual point can not be found: " + es.VirtualPointName);
            }
        }
        else
        {
            _pos = transform.position + es.Offset;
        }

        _rotation.eulerAngles += es.Rotate;

		GameObject _eff = GameObject.Instantiate<GameObject> (es.Effect, _pos, _rotation, _parent);
		_eff.SetActive (true);
		Destroy (_eff, es.LifeTime);
    }
		
	IEnumerator WaitMoveBulletToLine (BulletStruct es, GameObject bullet, Vector3 v)
	{
		BulletTrace xBulletTrace = new BulletTrace();
		xBulletTrace.target = null;
		xBulletTrace.bullet = bullet;
		xBulletTrace.index = es.index;
		xBulletTrace.gameID = this.gameObject.GetInstanceID();
		xBulletTrace.movetype = es.moveType;
		mxBulletTraceInfo.Add (xBulletTrace);

		Vector3 vForward = this.gameObject.transform.forward;
		Vector3 vTargetPos = vForward * es.Distance + bullet.transform.position;
		if (v != Vector3.zero)
		{
			vTargetPos = v;
		}

		float fDis = Vector3.Distance (vTargetPos, bullet.transform.position);
		Tweener t = bullet.transform.DOMove (vTargetPos, fDis / es.Speed);
		t.OnComplete (delegate()
		{
				int arrayIndex = -1;

				for (int i = 0; i < mxBulletTraceInfo.Count; ++i)
				{
					if (mxBulletTraceInfo [i].bullet.GetInstanceID () == bullet.GetInstanceID ())
					{
						arrayIndex = i;
						break;
					}
				}

				mxAnimationEvent.OnBulletTouchPositionEvent(this.gameObject, vTargetPos, mxBulletTraceInfo [arrayIndex].index);

				mxBulletTraceInfo.RemoveAt (arrayIndex);


				Destroy (bullet);

				if (es.TouchEffect)
				{
					GameObject _startEff = GameObject.Instantiate<GameObject> (es.TouchEffect, vTargetPos, Quaternion.identity);
					_startEff.SetActive (true);
					Destroy (_startEff, es.TouchEffLifeTime);
				}
				if (es.TouchAudio)
				{
					AudioClip _startEff = GameObject.Instantiate<AudioClip> (es.TouchAudio, vTargetPos, Quaternion.identity);
					AudioSource.PlayClipAtPoint(_startEff, transform.position); 
					Destroy (_startEff, es.TouchEffLifeTime);
				}
		});

		yield return new WaitForEndOfFrame ();

		/*
		while (true)
		{
			float fDis = Vector3.Distance (vTargetPos, go.transform.position);
			if (fDis > fMinDis) 
			{
				Vector3 _vec = vTargetPos - go.transform.position;
				float fMoveDis = es.Speed * Time.deltaTime;
				if (fMoveDis > fDis)
				{
					go.transform.position = vTargetPos;
				} 
				else 
				{
					go.transform.Translate (_vec.normalized * es.Speed * Time.deltaTime);
				}
			}

			if (fDis < fMinDis)
			{
				Destroy (go);

				if (es.TouchEffect)
				{
					GameObject _startEff = Instantiate (es.TouchEffect, vTargetPos, Quaternion.identity) as GameObject;
					_startEff.SetActive (true);
					Destroy (_startEff, es.TouchEffLifeTime);
				}
				if (es.TouchAudio)
				{
					AudioClip _startEff = Instantiate (es.TouchAudio, vTargetPos, Quaternion.identity) as AudioClip;
					AudioSource.PlayClipAtPoint(_startEff, transform.position); 
					Destroy (_startEff, es.TouchEffLifeTime);
				}
				yield break;
			}

			yield return new WaitForEndOfFrame ();
		}
		*/
	}

	IEnumerator WaitMoveBulletToTarget (BulletStruct es, GameObject bullet, GameObject target)
    {
		BulletTrace xBulletTrace = new BulletTrace();
		xBulletTrace.target = target;
		xBulletTrace.bullet = bullet;
		xBulletTrace.index = es.index;
		xBulletTrace.gameID = this.gameObject.GetInstanceID();
		xBulletTrace.movetype = es.moveType;
		mxBulletTraceInfo.Add (xBulletTrace);

		float fMinDis = 0.1f;
		Vector3 vTargetPos = target.transform.position;
        while (true)
        {
			if (target == null)
			{
				Vector3 _vec = vTargetPos - bullet.transform.position;
				float fDis = Vector3.Distance (vTargetPos, bullet.transform.position);
				float fMoveDis = es.Speed * Time.deltaTime;
				if (fMoveDis > fDis)
				{
					bullet.transform.position = vTargetPos;
				}
				else
				{
					bullet.transform.Translate (_vec.normalized * es.Speed * Time.deltaTime);
				}
			} 
			else 
			{
				vTargetPos = target.transform.position;
				float fDis = Vector3.Distance (vTargetPos, bullet.transform.position);
				if (fDis > fMinDis) 
				{
					Vector3 _vec = vTargetPos - bullet.transform.position;
					float fMoveDis = es.Speed * Time.deltaTime;
					if (fMoveDis > fDis)
					{
						bullet.transform.position = vTargetPos;
					}
					else
					{
						bullet.transform.Translate (_vec.normalized * es.Speed * Time.deltaTime);
					}
				}
			}

			if (Vector3.Distance (bullet.transform.position, vTargetPos) < fMinDis)
			{
				if (target == null)
				{
					mxAnimationEvent.OnBulletTouchPositionEvent(bullet, vTargetPos, -1);
				}
				else
				{
					int arrayIndex = -1;

					for (int i = 0; i < mxBulletTraceInfo.Count; ++i)
					{
						if (mxBulletTraceInfo [i].bullet.GetInstanceID () == bullet.GetInstanceID ())
						{
							arrayIndex = i;
							break;
						}
					}

					mxAnimationEvent.OnBulletTouchTargetEvent (bullet, target, mxBulletTraceInfo [arrayIndex].index);
					mxBulletTraceInfo.RemoveAt (arrayIndex);
				}

				Destroy (bullet);

				if (es.TouchEffect)
				{
					GameObject _startEff = GameObject.Instantiate<GameObject> (es.TouchEffect, vTargetPos, Quaternion.identity);
					_startEff.SetActive (true);
					Destroy (_startEff, es.TouchEffLifeTime);
				}
				if (es.TouchAudio)
				{
					AudioClip _startEff = GameObject.Instantiate<AudioClip> (es.TouchAudio, vTargetPos, Quaternion.identity);
					AudioSource.PlayClipAtPoint(_startEff, transform.position); 
					Destroy (_startEff, es.TouchEffLifeTime);
				}
				yield break;
			}

            yield return new WaitForEndOfFrame ();
        }
    }

	IEnumerator WaitPlayAudio (AudioStruct es)
	{
		if (es.isEnabled)
			yield return new WaitForSeconds (es.DelayTime);
		else
			yield break;
		
		if (es.Audio != null)
		{
			AudioClip _startEff = GameObject.Instantiate<AudioClip> (es.Audio, this.transform.position, Quaternion.identity);
			AudioSource.PlayClipAtPoint(_startEff, transform.position); 
			Destroy (_startEff, es.LifeTime);
		}

		yield return new WaitForEndOfFrame ();
	}

	IEnumerator WaitPlayCamera(CameraStruct es)
	{
		//如果特效没有启用则不进行播放
		if (es.isEnabled)
			yield return new WaitForSeconds (es.DelayTime);
		else
			yield break;

		if (Camera.main != null)
		{
			Camera.main.transform.DOShakePosition (es.ShakeTime, es.Strength);
		}

		yield return new WaitForEndOfFrame ();

	}

	IEnumerator WaitPlayDamage (DamageStruct es)
	{
		//如果特效没有启用则不进行播放
		if (es.isEnabled)
			yield return new WaitForSeconds (es.DelayTime);
		else
			yield break;

		//if you have bullet, then you can not use damage event because bullet will triggers the callback function
		for (int i = 0; i < EnemyList.Length; ++i)
		{
			if (EnemyList[i] != null)
			{
				mxAnimationEvent.OnDamageEvent (this.gameObject, EnemyList[i], es.index);
			}
		}

		yield return new WaitForEndOfFrame ();

	}

	IEnumerator WaitPlayMovement (MovementStruct es)
	{
		//如果特效没有启用则不进行播放
		if (es.isEnabled)
			yield return new WaitForSeconds (es.DelayTime);
		else
			yield break;

		if (es.StartEffect)
		{
			GameObject _startEff = GameObject.Instantiate<GameObject> (es.StartEffect, this.gameObject.transform.position, Quaternion.identity);
			_startEff.SetActive (true);
			Destroy (_startEff, es.StartEffLifeTime);
		}
		if (es.StartAudio)
		{
			AudioClip _startEff = GameObject.Instantiate<AudioClip> (es.StartAudio, this.gameObject.transform.position, Quaternion.identity);
			AudioSource.PlayClipAtPoint(_startEff, transform.position);  

			Destroy (_startEff, es.StartEffLifeTime);
		}

		Vector3 vForward = new Vector3();
		Vector3 vTargetPos = new Vector3();
		switch (es.moveType) 
		{
		case MovementStruct.MoveType.Forward:
			{
				vForward = this.gameObject.transform.forward;
			}
			break;
		case MovementStruct.MoveType.Back:
			{
				vForward = -this.gameObject.transform.forward;
			}
			break;
		case MovementStruct.MoveType.Left:
			{
				vForward = -this.gameObject.transform.right;
			}
			break;
		case MovementStruct.MoveType.Right:
			{
				vForward = this.gameObject.transform.right;
			}
			break;
		default:
			break;
		}

		vTargetPos = vForward * es.Distance + this.transform.position;

		float fDis = Vector3.Distance (vTargetPos, this.transform.position);
		Tweener t = this.transform.DOMove (vTargetPos, fDis / es.Speed);
		t.OnComplete (delegate()
		{
			if (es.TouchEffect)
			{
					GameObject _startEff = GameObject.Instantiate<GameObject> (es.TouchEffect, vTargetPos, Quaternion.identity);
				_startEff.SetActive (true);
				Destroy (_startEff, es.TouchEffLifeTime);
			}
			if (es.TouchAudio)
			{
					AudioClip _startEff = GameObject.Instantiate<AudioClip> (es.TouchAudio, vTargetPos, Quaternion.identity);
				AudioSource.PlayClipAtPoint(_startEff, transform.position); 

				Destroy (_startEff, es.TouchEffLifeTime);
			}
		});

		yield return new WaitForEndOfFrame ();

	}

	IEnumerator WaitPlayBullet (BulletStruct es, Vector3 v)
	{
		//如果特效没有启用则不进行播放
		if (es.isEnabled)
			yield return new WaitForSeconds (es.DelayTime);
		else
			yield break;


		Vector3 _pos = Vector3.zero;
		if (es.FirePointName != "None")
		{
			Transform _targetTrans = FindTransform (es.FirePointName);
			if (_targetTrans)
			{
				_pos = _targetTrans.position + es.Offset;
			}
			else
			{
				Debug.LogError ("The specified virtual point can not be found: " + es.FirePointName);
			}
		}
		else
		{
			_pos = transform.position + es.Offset;
		}

		switch (es.moveType)
		{
		case BulletStruct.MoveType.TargetObject:
			{
				for (int i = 0; i < EnemyList.Length; ++i)
				{
					GameObject _bullet = GameObject.Instantiate<GameObject> (es.Bullet, _pos, Quaternion.identity);
					_bullet.SetActive (true);

					if (es.StartEffect)
					{
						GameObject _startEff = GameObject.Instantiate<GameObject> (es.StartEffect, _pos, Quaternion.identity);
						_startEff.SetActive (true);
						Destroy (_startEff, es.StartEffLifeTime);
					}
					if (es.StartAudio)
					{
						AudioClip _startEff = GameObject.Instantiate<AudioClip> (es.StartAudio, _pos, Quaternion.identity);
						AudioSource.PlayClipAtPoint(_startEff, transform.position); 

						Destroy (_startEff, es.StartEffLifeTime);
					}

					StartCoroutine (WaitMoveBulletToTarget (es, _bullet, EnemyList[i]));
				}
			}
			break;
		case BulletStruct.MoveType.Line:
			{
				GameObject _bullet = GameObject.Instantiate<GameObject>(es.Bullet, _pos, Quaternion.identity);
				_bullet.SetActive (true);

				if (es.StartEffect)
				{
					GameObject _startEff = GameObject.Instantiate<GameObject> (es.StartEffect, _pos, Quaternion.identity);
					_startEff.SetActive (true);
					Destroy (_startEff, es.StartEffLifeTime);
				}
				if (es.StartAudio)
				{
					AudioClip _startEff = GameObject.Instantiate<AudioClip> (es.StartAudio, _pos, Quaternion.identity);
					AudioSource.PlayClipAtPoint(_startEff, transform.position); 

					Destroy (_startEff, es.StartEffLifeTime);
				}

				StartCoroutine (WaitMoveBulletToLine (es, _bullet, v));
			}
			break;
		default:
				break;
		}
	}

    Transform FindTransform (string name)
    {
        foreach (Transform t in GetComponentsInChildren<Transform> (true))
        {
            if (t.name == name)
                return t;
        }
        return null;
    }
}