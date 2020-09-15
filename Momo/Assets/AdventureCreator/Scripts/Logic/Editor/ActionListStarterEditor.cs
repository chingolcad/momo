using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	[CustomEditor (typeof (ActionListStarter))]
	public class ActionListStarterEditor : Editor
	{

		private ActionListStarter _target;


		public override void OnInspectorGUI ()
		{
			_target = (ActionListStarter) target;

			_target.ShowGUI ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
		
	}

}