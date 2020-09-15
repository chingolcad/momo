using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (FirstPersonCamera))]
	public class FirstPersonCameraEditor : Editor
	{
		
		private static GUILayoutOption
			labelWidth = GUILayout.MaxWidth (60),
			intWidth = GUILayout.MaxWidth (130);
		
		
		public override void OnInspectorGUI ()
		{
			FirstPersonCamera _target = (FirstPersonCamera) target;
			
			EditorGUILayout.BeginVertical ("Button");
			_target.headBob = EditorGUILayout.BeginToggleGroup ("Bob head when moving?", _target.headBob);
			_target.headBobMethod = (FirstPersonHeadBobMethod) EditorGUILayout.EnumPopup ("Head-bob method:", _target.headBobMethod);
			if (_target.headBobMethod == FirstPersonHeadBobMethod.BuiltIn)
			{
				_target.builtInSpeedFactor = EditorGUILayout.FloatField ("Speed factor:", _target.builtInSpeedFactor);
				_target.bobbingAmount = EditorGUILayout.FloatField ("Height change factor:", _target.bobbingAmount);
			}
			else if (_target.headBobMethod == FirstPersonHeadBobMethod.CustomAnimation)
			{
				_target.headBobSpeedParameter = EditorGUILayout.TextField ("Speed float parameter:", _target.headBobSpeedParameter);
				if (_target.GetComponent <Animator>() == null)
				{
					EditorGUILayout.HelpBox ("This GameObject must have an Animator component attached.", MessageType.Warning);
				}
			}
			else if (_target.headBobMethod == FirstPersonHeadBobMethod.CustomScript)
			{
				EditorGUILayout.HelpBox ("The component's public method 'GetHeadBobSpeed' will return the desired head-bobbing speed.", MessageType.Info);
			}
			EditorGUILayout.EndToggleGroup ();
			EditorGUILayout.EndVertical ();
			
			EditorGUILayout.BeginVertical ("Button");
				_target.allowMouseWheelZooming = EditorGUILayout.BeginToggleGroup ("Allow mouse-wheel zooming?", _target.allowMouseWheelZooming);
					EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.LabelField ("Min FOV:", labelWidth);
						_target.minimumZoom = EditorGUILayout.FloatField (_target.minimumZoom, intWidth);
						EditorGUILayout.LabelField ("Max FOV:", labelWidth);
						_target.maximumZoom = EditorGUILayout.FloatField (_target.maximumZoom, intWidth);
					EditorGUILayout.EndHorizontal ();
				EditorGUILayout.EndToggleGroup ();
			EditorGUILayout.EndVertical ();
			
			EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.LabelField ("Constrain pitch-rotation (degrees)");
				EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Min:", labelWidth);
					_target.minY = EditorGUILayout.FloatField (_target.minY, intWidth);
					EditorGUILayout.LabelField ("Max:", labelWidth);
					_target.maxY = EditorGUILayout.FloatField (_target.maxY, intWidth);
				EditorGUILayout.EndHorizontal ();
			EditorGUILayout.EndVertical ();
			
			EditorGUILayout.BeginVertical ("Button");
				_target.sensitivity = EditorGUILayout.Vector2Field ("Freelook sensitivity:", _target.sensitivity);
			EditorGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
		
	}

}