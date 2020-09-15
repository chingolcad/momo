using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberNPC), true)]
	public class RememberNPCEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI()
		{
			RememberNPC _target = (RememberNPC) target;

			if (_target.GetComponent <Hotspot>() != null && _target.GetComponent <RememberHotspot>() == null)
			{
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.LabelField ("NPC", EditorStyles.boldLabel);
				_target.startState = (AC_OnOff) CustomGUILayout.EnumPopup ("Hotspot state on start:", _target.startState, "", "The state of the NPC's Hotspot component when the game begins");
				EditorGUILayout.EndVertical ();
			}

			if (_target.GetComponent <NPC>() == null)
			{
				EditorGUILayout.HelpBox ("This script expects an NPC component!", MessageType.Warning);
			}


			SharedGUI ();
		}
		
	}

}