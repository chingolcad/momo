using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor (typeof (Variables))]
	public class VariablesEditor : Editor
	{

		private bool showVariablesList = true;
		private bool showSettings = true;
		private bool showProperties = true;

		private VariableType typeFilter;
		private VarFilter varFilter;

		private GVar selectedVar = null;
		private Variables _target;


		public override void OnInspectorGUI ()
		{
			_target = (Variables) target;

			ShowSettings ();

			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showVariablesList = CustomGUILayout.ToggleHeader (showVariablesList, "Component variables");
			if (showVariablesList)
			{
				selectedVar = VariablesManager.ShowVarList (selectedVar, _target.vars, VariableLocation.Component, varFilter, _target.filter, typeFilter, !Application.isPlaying, _target);
			}
			EditorGUILayout.EndVertical ();

			if (selectedVar != null)
			{
				EditorGUILayout.Space ();
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				showProperties = CustomGUILayout.ToggleHeader (showProperties, "Variable '" + selectedVar.label + "' properties");
				if (showProperties)
				{
					VariablesManager.ShowVarGUI (selectedVar, VariableLocation.Component, !Application.isPlaying, null, string.Empty, _target);
				}
				EditorGUILayout.EndVertical ();
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void ShowSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSettings = CustomGUILayout.ToggleHeader (showSettings, "Editor settings");
			if (showSettings)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Filter by:", GUILayout.Width (65f));
				varFilter = (VarFilter) EditorGUILayout.EnumPopup (varFilter, GUILayout.MaxWidth (100f));
				if (varFilter == VarFilter.Type)
				{
					typeFilter = (VariableType) EditorGUILayout.EnumPopup (typeFilter);
				}
				else
				{
					_target.filter = EditorGUILayout.TextField (_target.filter);
				}
				EditorGUILayout.EndHorizontal ();
			}
		
			EditorGUILayout.EndVertical ();
		}

	}

}