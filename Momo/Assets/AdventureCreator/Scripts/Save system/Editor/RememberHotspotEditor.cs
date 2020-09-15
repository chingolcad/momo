using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberHotspot), true)]
	public class RememberHotspotEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI()
		{
			RememberHotspot _target = (RememberHotspot) target;

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Hotspot", EditorStyles.boldLabel);
			_target.startState = (AC_OnOff) CustomGUILayout.EnumPopup ("Hotspot state on start:", _target.startState, "The interactive state of the Hotspot when the game begins");
			EditorGUILayout.EndVertical ();

			if (_target.GetComponent <Hotspot>() == null)
			{
				EditorGUILayout.HelpBox ("This script expects a Hotspot component!", MessageType.Warning);
			}

			SharedGUI ();
		}

	}

}