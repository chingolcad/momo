using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (ConstantID), true)]
	public class ConstantIDEditor : Editor
	{

		public override void OnInspectorGUI()
	    {
			SharedGUI ();
		}
		
		
		protected void SharedGUI()
		{
			ConstantID _target = (ConstantID) target;

			EditorGUILayout.BeginVertical ("Button");

			EditorGUILayout.LabelField ("Constant ID number", EditorStyles.boldLabel);

			_target.autoManual = (AutoManual) CustomGUILayout.EnumPopup ("Set:", _target.autoManual, "", "Is the Constant ID set automatically or manually?");

			_target.retainInPrefab = CustomGUILayout.Toggle ("Retain in prefab?", _target.retainInPrefab, "", "If True, prefabs will share the same Constant ID as their scene-based counterparts");

			bool ignoreDirty = false;
			if (UnityVersionHandler.IsPrefabFile (_target.gameObject))
			{
				// Prefab
				if (!_target.retainInPrefab && _target.constantID != 0)
				{
					_target.constantID = 0;
					// Don't flag as dirty, otherwise get problems with scene instances
					ignoreDirty = true;
				}
				else if (_target.retainInPrefab && _target.constantID == 0)
				{
					_target.SetNewID_Prefab ();
				}
			}

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("ID:", "The recorded Constant ID number"), GUILayout.Width (50f));
			if (_target.autoManual == AutoManual.Automatic)
			{
				EditorGUILayout.LabelField (_target.constantID.ToString ());
			}
			else
			{
				_target.constantID = EditorGUILayout.DelayedIntField (_target.constantID);
			}
			if (GUILayout.Button ("Copy number"))
			{
				EditorGUIUtility.systemCopyBuffer = _target.constantID.ToString ();
			}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.EndVertical ();

			if (!ignoreDirty)
			{
				UnityVersionHandler.CustomSetDirty (_target);
			}
		}

	}

}