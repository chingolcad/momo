using UnityEngine;
using UnityEditor;

namespace AC
{
	
	[CustomEditor (typeof (DetectHotspots))]
	public class DetectHotspotsEditor : Editor
	{
		
		private DetectHotspots _target;

		
		private void OnEnable ()
		{
			_target = (DetectHotspots) target;
		}

		
		public override void OnInspectorGUI ()
		{
			if (_target == null)
			{
				return;
			}

			if (SceneSettings.IsUnity2D ())
			{
				if (_target.GetComponent <Collider2D>() == null)
				{
					EditorGUILayout.HelpBox ("A 2D Collider component must be placed on this object.", MessageType.Warning);
				}
				else if (_target.GetComponent <Collider2D>() != null && !_target.GetComponent <Collider2D>().isTrigger)
				{
					EditorGUILayout.HelpBox ("This object's 2D Collider component must have 'Is Trigger?' set.", MessageType.Warning);
				}
				
				if (_target.GetComponent <Rigidbody2D>() == null && _target.GetComponentInParent <Rigidbody2D>() == null)
				{
					EditorGUILayout.HelpBox ("A 2D Rigidbody component must be placed on this object.", MessageType.Warning);
				}
			}
			else
			{
				if (_target.GetComponent <Collider>() == null)
				{
					EditorGUILayout.HelpBox ("A Collider component must be placed on this object.", MessageType.Warning);
				}
				else if (_target.GetComponent <Collider>() != null && !_target.GetComponent <Collider>().isTrigger)
				{
					EditorGUILayout.HelpBox ("This object's Collider component must have 'Is Trigger?' set.", MessageType.Warning);
				}

				if (_target.GetComponent <Rigidbody>() == null && _target.GetComponentInParent <Rigidbody>() == null)
				{
					EditorGUILayout.HelpBox ("A Rigidbody component must be placed on this object.", MessageType.Warning);
				}
			}
		}
	
	}
}
