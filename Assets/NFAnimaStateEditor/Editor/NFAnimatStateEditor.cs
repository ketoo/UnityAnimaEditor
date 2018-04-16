#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEditor.Animations;


[CustomEditor (typeof (NFAnimatStateController))]
public class NFAnimatStateEditor : Editor
{
    #region 数据成员
    private NFAnimatStateController mASD;
	private NFAnimaStateData mData;

    private bool isFoldoutVirtualPoint;
    private string virtualPointName;
	#endregion

    #region 界面重写
    /// <summary>
    /// 界面重写
    /// </summary>
    public override void OnInspectorGUI ()
    {
        base.OnInspectorGUI ();

        mASD = target as NFAnimatStateController;
        mData = mASD.mxSkillData;

        if (mData)
        {
			if (GUILayout.Button ("Fix All anims"))
			{
				FixAllAnims ();
			}

			mData.DefaultAnimationType = (NFAnimaStateType) EditorGUILayout.EnumPopup ("DefaultAnima:", mData.DefaultAnimationType);


            DrawVirtualPoint ();
			DrawAnimation ();
        }
        else
        {
            EditorGUILayout.HelpBox ("Please specify a \"AnimationSkillData\" data .", MessageType.Error);
        }

        EditorUtility.SetDirty (mASD);
        if (mData != null)
            EditorUtility.SetDirty (mData);
    }
    #endregion

    #region 虚拟体
    void DrawVirtualPoint ()
    {
        isFoldoutVirtualPoint = EditorGUILayout.Foldout (isFoldoutVirtualPoint, "Virtual Point");
        if (!isFoldoutVirtualPoint)
        {
            EditorGUILayout.HelpBox ("Add Your Custom Virtual Point Filter...", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical (EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal (EditorStyles.helpBox);
        virtualPointName = EditorGUILayout.TextField ("Add Point:", virtualPointName);

        if (GUILayout.Button ("Add"))
        {
            AddVirtualPoint (virtualPointName);
        }
        EditorGUILayout.EndHorizontal ();

        DrawVirtualPointList ();
        EditorGUILayout.EndVertical ();

    }

    void AddVirtualPoint (string name)
    {
        if (!string.IsNullOrEmpty (name))
        {
            if (!mData.VirtualPointList.Contains (name))
                mData.VirtualPointList.Add (name);
        }
    }

    void DrawVirtualPointList ()
    {
        for (int i = 0; i < mData.VirtualPointList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal ();
            EditorGUILayout.LabelField ("ID: " + i, "name: " + mData.VirtualPointList[i]);
            if (i > 0)
                if (GUILayout.Button ("Delete"))
                    mData.VirtualPointList.Remove (mData.VirtualPointList[i]);
            EditorGUILayout.EndHorizontal ();
        }
    }
    #endregion

	void FixAllAnims()
	{

		foreach (NFAnimaStateType  myCode in Enum.GetValues(typeof(NFAnimaStateType))) 
		{
			bool b = false;
			for (int i = 0; i < mData.AnimationSkillList.Count; i++) 
			{
				if (mData.AnimationSkillList[i].Type == myCode)
				{
					b = true;
				}
			}

			if (!b)
			{
				AnimationSkillStruct _ass = new AnimationSkillStruct ();
				_ass.Type = myCode;
				_ass.IsFoldout = true;

				_ass.EffectStructList = new List<EffectStruct> ();
				_ass.AudioStructList = new List<AudioStruct> ();
				_ass.BulletStructList = new List<BulletStruct> ();
				_ass.DamageStructList = new List<DamageStruct> ();
				_ass.MovementStructList = new List<MovementStruct> ();
				_ass.CameraStructList = new List<CameraStruct> ();
				mData.AnimationSkillList.Add (_ass);
			}
		}

		for (int i = mData.AnimationSkillList.Count-1; i >= 0; i--) 
		{
			bool b = false;
			foreach (NFAnimaStateType  myCode in Enum.GetValues(typeof(NFAnimaStateType))) 
			{
				if (mData.AnimationSkillList[i].Type == myCode)
				{
					b = true;
				}
			}

			if (!b)
			{
				mData.AnimationSkillList.RemoveAt (i);
			}
		}

		String strPath = AssetDatabase.GetAssetPath (mASD.GetComponent<Animator> ().avatar);
		AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(strPath + ".controller");
		AnimatorStateMachine state_machine = ctrl.layers[0].stateMachine;

		mASD.GetComponent<Animator> ().runtimeAnimatorController = ctrl;

		foreach (NFAnimaStateType  myCode in Enum.GetValues(typeof(NFAnimaStateType)))
		{
			AnimatorState state = state_machine.AddState(myCode.ToString());
		}
	}

    void DrawAnimation ()
    {
		if (mData.AnimationSkillList == null)
			return;

		for (int i = 0; i < mData.AnimationSkillList.Count; i++)
        {
			EditorGUILayout.Space ();

            AnimationSkillStruct _ass = mData.AnimationSkillList[i];

            if (_ass.IsFoldout)
            {
				EditorGUILayout.BeginVertical (EditorStyles.helpBox); 

				EditorGUILayout.LabelField (_ass.Type.ToString());
				if (GUILayout.Button ("Preview"))
				{
					mASD.PlayAnimaState (_ass.Type);
				}

				_ass.AnimationClip = (AnimationClip) EditorGUILayout.ObjectField ("AnimaitonClip:", _ass.AnimationClip, typeof (AnimationClip), true);

				EditorGUILayout.BeginHorizontal ();
				if (_ass.AnimationClip == null) 
				{
					_ass.fTime = 1f;
				} 
				else 
				{
					_ass.fTime = _ass.AnimationClip.length;
				}
				EditorGUILayout.LabelField ("NextAnima After:", _ass.fTime.ToString());
				_ass.NextType = (NFAnimaStateType) EditorGUILayout.EnumPopup (_ass.NextType);

				EditorGUILayout.EndHorizontal ();

				AnimatorController ctl = (AnimatorController)mASD.GetComponent<Animator> ().runtimeAnimatorController;
				if (ctl != null)
				{
					AnimatorStateMachine state_machine = ctl.layers[0].stateMachine;
					for (int j = 0; j < state_machine.states.Length; ++j)
					{
						if (state_machine.states [j].state.name == _ass.Type.ToString())
						{
							String strPath = AssetDatabase.GetAssetPath (_ass.AnimationClip);
							AnimationClip anim = AssetDatabase.LoadAssetAtPath(strPath, typeof(AnimationClip)) as AnimationClip;
							state_machine.states [j].state.motion = anim;
							break;
						}
					}
				}

				mData.AnimationSkillList[i] = _ass;

                //添加特效与删除片断
                EditorGUILayout.BeginHorizontal ();
                if (GUILayout.Button ("ADD EFFECT"))
                {
                    EffectStruct _es = new EffectStruct ();
                    _es.LifeTime = 3;
                    _es.isFoldout = true;
                    mData.AnimationSkillList[i].EffectStructList.Add (_es);
                }
				if (GUILayout.Button ("ADD Audio"))
				{
					AudioStruct _es = new AudioStruct ();
					_es.LifeTime = 3;
					_es.isFoldout = true;
					mData.AnimationSkillList[i].AudioStructList.Add (_es);
				}
				if (GUILayout.Button ("ADD Bullet"))
				{
					BulletStruct _es = new BulletStruct ();
					_es.isFoldout = true;
					mData.AnimationSkillList[i].BulletStructList.Add (_es);
				}

			

				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("ADD Damage"))
				{
					DamageStruct _es = new DamageStruct ();
					_es.isFoldout = true;
					mData.AnimationSkillList[i].DamageStructList.Add (_es);
				}

				if (GUILayout.Button ("ADD Movement"))
				{
					MovementStruct _es = new MovementStruct ();
					_es.isFoldout = true;
					mData.AnimationSkillList[i].MovementStructList.Add (_es);
				}

				if (GUILayout.Button ("ADD Camera"))
				{
					CameraStruct _es = new CameraStruct ();
					_es.isFoldout = true;
					mData.AnimationSkillList[i].CameraStructList.Add (_es);
				}
			
                EditorGUILayout.EndHorizontal ();

				if (mData.AnimationSkillList.Count > 0) 
				{
					DrawEffect (i);
					DrawAudio (i);
					DrawMovement (i);
					DrawDamage (i);
					DrawBullet (i);
					DrawCamera (i);
				}
                EditorGUILayout.EndVertical ();
            }
        }
    }


    void DrawEffect (int id)
    {
        if (!(id < mData.AnimationSkillList.Count))
            return;

        for (int i = 0; i < mData.AnimationSkillList[id].EffectStructList.Count; i++)
        {
            EffectStruct _eff = mData.AnimationSkillList[id].EffectStructList[i];

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical (EditorStyles.helpBox);

            string _titleName = _eff.Effect ? _eff.Effect.name : "Effect" + (i + 1).ToString ();
            EditorGUILayout.BeginHorizontal ();
            //此子特效的界面折叠
            _eff.isFoldout = EditorGUILayout.Foldout (_eff.isFoldout, _titleName);
            GUILayout.FlexibleSpace ();
            //此子特效是否可用
            _eff.isEnabled = EditorGUILayout.Toggle ("", _eff.isEnabled);

			if (GUILayout.Button ("DELETE"))
			{
				mData.AnimationSkillList[id].EffectStructList.Remove (_eff);
				return;
			}

            EditorGUILayout.EndHorizontal ();

            mData.AnimationSkillList[id].EffectStructList[i] = _eff;

            if (_eff.isFoldout)
            {
                EditorGUI.BeginDisabledGroup (!_eff.isEnabled);
                _eff.Effect = (GameObject) EditorGUILayout.ObjectField ("Effect", _eff.Effect, typeof (GameObject), true);

                string[] _nameArry = mData.VirtualPointList.ToArray ();
                _eff.VirtualPointID = EditorGUILayout.Popup ("Virtual Point", _eff.VirtualPointID, _nameArry);
                _eff.VirtualPointName = _nameArry[_eff.VirtualPointID];

                _eff.Offset = EditorGUILayout.Vector3Field ("Offset", _eff.Offset);
                _eff.Rotate = EditorGUILayout.Vector3Field ("Rotate", _eff.Rotate);
                _eff.IsFollow = EditorGUILayout.Toggle ("Is Follow", _eff.IsFollow);
                _eff.DelayTime = EditorGUILayout.FloatField ("Delay Time", _eff.DelayTime);
                _eff.LifeTime = EditorGUILayout.FloatField ("Life Time", _eff.LifeTime);

				if (_eff.DelayTime > mData.AnimationSkillList[id].fTime)
				{
					_eff.DelayTime = mData.AnimationSkillList [id].fTime;
				}

                mData.AnimationSkillList[id].EffectStructList[i] = _eff;
            }
            EditorGUI.EndDisabledGroup ();


            EditorGUILayout.EndVertical ();
            EditorGUI.indentLevel--;
        }
    }

	void DrawAudio (int id)
	{
		if (!(id < mData.AnimationSkillList.Count))
			return;

		for (int i = 0; i < mData.AnimationSkillList[id].AudioStructList.Count; i++)
		{
			AudioStruct _eff = mData.AnimationSkillList[id].AudioStructList[i];

			EditorGUI.indentLevel++;
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);

			string _titleName = _eff.Audio ? _eff.Audio.name : "Audio" + (i + 1).ToString ();
			EditorGUILayout.BeginHorizontal ();
			//此子特效的界面折叠
			_eff.isFoldout = EditorGUILayout.Foldout (_eff.isFoldout, _titleName);
			GUILayout.FlexibleSpace ();
			//此子特效是否可用
			_eff.isEnabled = EditorGUILayout.Toggle ("", _eff.isEnabled);

			if (GUILayout.Button ("DELETE"))
			{
				mData.AnimationSkillList[id].AudioStructList.Remove (_eff);
				return;
			}

			EditorGUILayout.EndHorizontal ();

			mData.AnimationSkillList[id].AudioStructList[i] = _eff;

			if (_eff.isFoldout)
			{
				EditorGUI.BeginDisabledGroup (!_eff.isEnabled);
				_eff.Audio = (AudioClip) EditorGUILayout.ObjectField ("Audio", _eff.Audio, typeof (AudioClip), true);

				_eff.DelayTime = EditorGUILayout.FloatField ("Delay Time", _eff.DelayTime);
				if (_eff.DelayTime > mData.AnimationSkillList[id].fTime)
				{
					_eff.DelayTime = mData.AnimationSkillList [id].fTime;
				}

				_eff.LifeTime = EditorGUILayout.FloatField ("Life Time", _eff.LifeTime);

				mData.AnimationSkillList[id].AudioStructList[i] = _eff;
			}
			EditorGUI.EndDisabledGroup ();


			EditorGUILayout.EndVertical ();
			EditorGUI.indentLevel--;
		}
	}

	void DrawMovement (int id)
	{
		if (!(id < mData.AnimationSkillList.Count))
			return;

		for (int i = 0; i < mData.AnimationSkillList[id].MovementStructList.Count; i++)
		{
			MovementStruct _eff = mData.AnimationSkillList[id].MovementStructList[i];

			EditorGUI.indentLevel++;
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);

			string _titleName = "Movement" + (i + 1).ToString ();
			EditorGUILayout.BeginHorizontal ();
			//此子特效的界面折叠
			_eff.isFoldout = EditorGUILayout.Foldout (_eff.isFoldout, _titleName);
			GUILayout.FlexibleSpace ();
			//此子特效是否可用
			_eff.isEnabled = EditorGUILayout.Toggle ("", _eff.isEnabled);

			if (GUILayout.Button ("DELETE"))
			{
				mData.AnimationSkillList[id].MovementStructList.Remove (_eff);
				return;
			}

			EditorGUILayout.EndHorizontal ();

			mData.AnimationSkillList[id].MovementStructList[i] = _eff;

			if (_eff.isFoldout)
			{
				EditorGUI.BeginDisabledGroup (!_eff.isEnabled);
				_eff.DelayTime = EditorGUILayout.FloatField ("Delay Time", _eff.DelayTime);
				if (_eff.DelayTime > mData.AnimationSkillList[id].fTime)
				{
					_eff.DelayTime = mData.AnimationSkillList [id].fTime;
				}

				string[] _nameArry = mData.VirtualPointList.ToArray ();

				_eff.StartAudio = (AudioClip) EditorGUILayout.ObjectField ("StartAudio", _eff.StartAudio, typeof (AudioClip), true);
				_eff.StartEffect = (GameObject) EditorGUILayout.ObjectField ("StartEffect", _eff.StartEffect, typeof (GameObject), true);
				_eff.StartEffLifeTime = EditorGUILayout.FloatField ("StartEffLifeTime", _eff.StartEffLifeTime);

				//运动方式
				_eff.moveType = (MovementStruct.MoveType) EditorGUILayout.EnumPopup ("Move Type", _eff.moveType);
				_eff.Distance = EditorGUILayout.FloatField ("Distance", _eff.Distance);
				_eff.Speed = EditorGUILayout.FloatField ("Speed", _eff.Speed);

				_eff.TouchAudio = (AudioClip) EditorGUILayout.ObjectField ("TouchAudio", _eff.TouchAudio, typeof (AudioClip), true);
				_eff.TouchEffect = (GameObject) EditorGUILayout.ObjectField ("TouchEffect", _eff.TouchEffect, typeof (GameObject), true);

				_eff.TouchEffLifeTime = EditorGUILayout.FloatField ("TouchEffLifeTime", _eff.TouchEffLifeTime);

				mData.AnimationSkillList[id].MovementStructList[i] = _eff;
			}
			EditorGUI.EndDisabledGroup ();


			EditorGUILayout.EndVertical ();
			EditorGUI.indentLevel--;
		}
	}

	void DrawBullet (int id)
	{
		if (!(id < mData.AnimationSkillList.Count))
			return;

		for (int i = 0; i < mData.AnimationSkillList[id].BulletStructList.Count; i++)
		{
			BulletStruct _eff = mData.AnimationSkillList[id].BulletStructList[i];

			EditorGUI.indentLevel++;
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);

			string _titleName = _eff.Bullet ? _eff.Bullet.name : "Bullet" + (i + 1).ToString ();
			EditorGUILayout.BeginHorizontal ();
			//此子特效的界面折叠
			_eff.isFoldout = EditorGUILayout.Foldout (_eff.isFoldout, _titleName);
			GUILayout.FlexibleSpace ();
			//此子特效是否可用
			_eff.isEnabled = EditorGUILayout.Toggle ("", _eff.isEnabled);

			if (GUILayout.Button ("DELETE"))
			{
				mData.AnimationSkillList[id].BulletStructList.Remove (_eff);
				return;
			}

			EditorGUILayout.EndHorizontal ();

			mData.AnimationSkillList[id].BulletStructList[i] = _eff;

			if (_eff.isFoldout)
			{
				EditorGUI.BeginDisabledGroup (!_eff.isEnabled);
				_eff.Bullet = (GameObject) EditorGUILayout.ObjectField ("Bullet", _eff.Bullet, typeof (GameObject), true);
				_eff.DelayTime = EditorGUILayout.FloatField ("Delay Time", _eff.DelayTime);
				if (_eff.DelayTime > mData.AnimationSkillList[id].fTime)
				{
					_eff.DelayTime = mData.AnimationSkillList [id].fTime;
				}

				string[] _nameArry = mData.VirtualPointList.ToArray ();
				_eff.FirePointID = EditorGUILayout.Popup ("Fire Point", _eff.FirePointID, _nameArry);
				_eff.FirePointName = _nameArry[_eff.FirePointID];
				_eff.Offset = EditorGUILayout.Vector3Field ("Fire Offset", _eff.Offset);

				_eff.StartAudio = (AudioClip) EditorGUILayout.ObjectField ("StartAudio", _eff.StartAudio, typeof (AudioClip), true);
				_eff.StartEffect = (GameObject) EditorGUILayout.ObjectField ("StartEffect", _eff.StartEffect, typeof (GameObject), true);
				_eff.StartEffLifeTime = EditorGUILayout.FloatField ("StartEffLifeTime", _eff.StartEffLifeTime);

				//特效运动方式
				_eff.moveType = (BulletStruct.MoveType) EditorGUILayout.EnumPopup ("Move Type", _eff.moveType);
				switch (_eff.moveType)
				{
				case BulletStruct.MoveType.Line:
					{
						_eff.Distance = EditorGUILayout.FloatField ("Distance", _eff.Distance);
					}
					break;
				case BulletStruct.MoveType.TargetObject:
					{	
					}
					break;
				}

				_eff.Speed = EditorGUILayout.FloatField ("Speed", _eff.Speed);

				_eff.TouchAudio = (AudioClip) EditorGUILayout.ObjectField ("TouchAudio", _eff.TouchAudio, typeof (AudioClip), true);
				_eff.TouchEffect = (GameObject) EditorGUILayout.ObjectField ("TouchEffect", _eff.TouchEffect, typeof (GameObject), true);

				_eff.TouchEffLifeTime = EditorGUILayout.FloatField ("TouchEffLifeTime", _eff.TouchEffLifeTime);

				mData.AnimationSkillList[id].BulletStructList[i] = _eff;
			}
			EditorGUI.EndDisabledGroup ();


			EditorGUILayout.EndVertical ();
			EditorGUI.indentLevel--;
		}
	}
	void DrawDamage (int id)
	{
		if (!(id < mData.AnimationSkillList.Count))
			return;

		for (int i = 0; i < mData.AnimationSkillList[id].DamageStructList.Count; i++)
		{
			DamageStruct _eff = mData.AnimationSkillList[id].DamageStructList[i];

			EditorGUI.indentLevel++;
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);

			string _titleName = "Damage" + (i + 1).ToString ();
			EditorGUILayout.BeginHorizontal ();
			//此子特效的界面折叠
			_eff.isFoldout = EditorGUILayout.Foldout (_eff.isFoldout, _titleName);
			GUILayout.FlexibleSpace ();
			//此子特效是否可用
			_eff.isEnabled = EditorGUILayout.Toggle ("", _eff.isEnabled);

			if (GUILayout.Button ("DELETE"))
			{
				mData.AnimationSkillList[id].DamageStructList.Remove (_eff);
				return;
			}

			EditorGUILayout.EndHorizontal ();

			mData.AnimationSkillList[id].DamageStructList[i] = _eff;

			if (_eff.isFoldout)
			{
				EditorGUI.BeginDisabledGroup (!_eff.isEnabled);
				_eff.DelayTime = EditorGUILayout.FloatField ("Delay Time", _eff.DelayTime);
				if (_eff.DelayTime > mData.AnimationSkillList[id].fTime)
				{
					_eff.DelayTime = mData.AnimationSkillList [id].fTime;
				}

				string[] _nameArry = mData.VirtualPointList.ToArray ();

				mData.AnimationSkillList[id].DamageStructList[i] = _eff;
			}
			EditorGUI.EndDisabledGroup ();


			EditorGUILayout.EndVertical ();
			EditorGUI.indentLevel--;
		}
	}
	void DrawCamera (int id)
	{
		if (!(id < mData.AnimationSkillList.Count))
			return;

		for (int i = 0; i < mData.AnimationSkillList[id].CameraStructList.Count; i++)
		{
			CameraStruct _eff = mData.AnimationSkillList[id].CameraStructList[i];

			EditorGUI.indentLevel++;
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);

			string _titleName = "Camera" + (i + 1).ToString ();
			EditorGUILayout.BeginHorizontal ();
			//此子特效的界面折叠
			_eff.isFoldout = EditorGUILayout.Foldout (_eff.isFoldout, _titleName);
			GUILayout.FlexibleSpace ();
			//此子特效是否可用
			_eff.isEnabled = EditorGUILayout.Toggle ("", _eff.isEnabled);

			if (GUILayout.Button ("DELETE"))
			{
				mData.AnimationSkillList[id].CameraStructList.Remove (_eff);
				return;
			}

			EditorGUILayout.EndHorizontal ();

			mData.AnimationSkillList[id].CameraStructList[i] = _eff;

			if (_eff.isFoldout)
			{
				EditorGUI.BeginDisabledGroup (!_eff.isEnabled);
				_eff.DelayTime = EditorGUILayout.FloatField ("Delay Time", _eff.DelayTime);
				if (_eff.DelayTime > mData.AnimationSkillList[id].fTime)
				{
					_eff.DelayTime = mData.AnimationSkillList [id].fTime;
				}

				_eff.ShakeTime = EditorGUILayout.FloatField ("Shake Time", _eff.ShakeTime);
				_eff.Strength = EditorGUILayout.FloatField ("Strength", _eff.Strength);
				_eff.Vibrato = EditorGUILayout.IntField ("Vibrato", _eff.Vibrato);

				string[] _nameArry = mData.VirtualPointList.ToArray ();

				mData.AnimationSkillList[id].CameraStructList[i] = _eff;
			}
			EditorGUI.EndDisabledGroup ();


			EditorGUILayout.EndVertical ();
			EditorGUI.indentLevel--;
		}
	}

}
#endif