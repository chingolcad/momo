using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (Parallax2D))]
	public class Parallax2DEditor : Editor
	{

		private Parallax2D _target;


		private void OnEnable ()
		{
			_target = (Parallax2D) target;
		}


		public override void OnInspectorGUI ()
		{
			EditorGUILayout.BeginVertical ("Button");
			_target.reactsTo = (ParallaxReactsTo) CustomGUILayout.EnumPopup ("Reacts to:", _target.reactsTo, "", "What entity affects the parallax behaviour");
			_target.depth = CustomGUILayout.FloatField ("Depth:", _target.depth, "", "The intensity of the depth effect. Positive values will make the GameObject appear further away (i.e. in the background), negative values will make it appear closer to the camera (i.e. in the foreground).");
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			_target.xScroll = CustomGUILayout.Toggle ("Scroll in X direction?", _target.xScroll, "", "If True, then the GameObject will scroll in the X-axis");
			if (_target.xScroll)
			{
				_target.xOffset = CustomGUILayout.FloatField ("Offset:", _target.xOffset, "", "An offset for the GameObject's initial position along the X-axis");
				_target.limitX = CustomGUILayout.Toggle ("Constrain?", _target.limitX, "If True, scrolling in the X-axis will be constrained");
				if (_target.limitX)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Minimum constraint:", GUILayout.Width (70f));
					_target.minX = EditorGUILayout.FloatField (_target.minX);
					EditorGUILayout.LabelField ("Maximum constraint:", GUILayout.Width (70f));
					_target.maxX = EditorGUILayout.FloatField (_target.maxX);
					EditorGUILayout.EndHorizontal ();
				}
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			_target.yScroll = CustomGUILayout.Toggle ("Scroll in Y direction?", _target.yScroll, "", "If True, then the GameObject will scroll in the Y-axis");
			if (_target.yScroll)
			{
				_target.yOffset = CustomGUILayout.FloatField ("Offset:", _target.yOffset, "", "An offset for the GameObject's initial position along the Y-axis");
				_target.limitY = CustomGUILayout.Toggle ("Constrain?", _target.limitY, "", "If True, scrolling in the Y-axis will be constrained");
				if (_target.limitY)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Minimum constraint:", GUILayout.Width (70f));
					_target.minY = EditorGUILayout.FloatField (_target.minY);
					EditorGUILayout.LabelField ("Maximum constraint:", GUILayout.Width (70f));
					_target.maxY = EditorGUILayout.FloatField (_target.maxY);
					EditorGUILayout.EndHorizontal ();
				}
			}
			EditorGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
	}

}