using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	[CustomEditor (typeof (SetInventoryInteractionParameters))]
	public class SetInventoryInteractionParametersEditor : Editor
	{

		private SetInventoryInteractionParameters _target;


		public override void OnInspectorGUI ()
		{
			_target = (SetInventoryInteractionParameters) target;

			_target.ShowGUI ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
		
	}

}