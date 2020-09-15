using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberTrigger), true)]
	public class RememberTriggerEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI ()
		{
			RememberTrigger _target = (RememberTrigger) target;
			
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Trigger", EditorStyles.boldLabel);
			_target.startState = (AC_OnOff) CustomGUILayout.EnumPopup ("Trigger state on start:", _target.startState, "", "The enabled state of the Trigger when the game begins");
			EditorGUILayout.EndVertical ();
			
			if (_target.GetComponent <AC_Trigger>() == null)
			{
				EditorGUILayout.HelpBox ("This script expects a Trigger component!", MessageType.Warning);
			}
			
			SharedGUI ();
		}
		
	}

}