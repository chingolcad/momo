using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(PlayerStart))]
	public class PlayerStartEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			PlayerStart _target = (PlayerStart) target;

			if (KickStarter.sceneSettings != null && KickStarter.sceneSettings.defaultPlayerStart == _target)
			{
				EditorGUILayout.HelpBox ("This PlayerStart is the scene's default, and will be used if a more appropriate one is not found.", MessageType.Info);
			}

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Previous scene that activates", EditorStyles.boldLabel);
			_target.chooseSceneBy = (ChooseSceneBy) CustomGUILayout.EnumPopup ("Choose scene by:", _target.chooseSceneBy, "", "The way in which the previous scene is identified by");
			if (_target.chooseSceneBy == ChooseSceneBy.Name)
			{
				_target.previousSceneName = CustomGUILayout.TextField ("Previous scene:", _target.previousSceneName, "", "The name of the previous scene to check for");
			}
			else
			{
				_target.previousScene = CustomGUILayout.IntField ("Previous scene:", _target.previousScene, "", "The build-index number of the previous scene to check for");
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Camera settings", EditorStyles.boldLabel);
			_target.cameraOnStart = (_Camera) CustomGUILayout.ObjectField <_Camera> ("Camera on start:", _target.cameraOnStart, true, "", "The AC _Camera that should be made active when the Player starts the scene from this point");
			_target.fadeInOnStart = CustomGUILayout.Toggle ("Fade in on start?", _target.fadeInOnStart, "", "If True, then the MainCamera will fade in when the Player starts the scene from this point");
			if (_target.fadeInOnStart)
			{
				_target.fadeSpeed = CustomGUILayout.FloatField ("Fade speed:", _target.fadeSpeed, "", "The speed of the fade");
			}
			EditorGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}