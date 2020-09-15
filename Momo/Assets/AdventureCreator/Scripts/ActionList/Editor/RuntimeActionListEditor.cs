using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	[CustomEditor (typeof (RuntimeActionList))]
	[System.Serializable]
	public class RuntimeActionListEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			RuntimeActionList _target = (RuntimeActionList) target;

			if (Application.isPlaying)
			{
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.ObjectField ("Asset source:", _target.assetSource, typeof (ActionListAsset), false);

				if (_target.useParameters)
				{
					EditorGUILayout.EndVertical ();
					EditorGUILayout.BeginVertical ("Button");
					EditorGUILayout.LabelField ("Parameters", EditorStyles.boldLabel);
					ActionListEditor.ShowParametersGUI (_target, null, _target.parameters);
				}
				EditorGUILayout.EndVertical ();
			}
			else
			{
				EditorGUILayout.HelpBox ("This component should not be added manually - it is added automatically by AC at runtime.", MessageType.Warning);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}