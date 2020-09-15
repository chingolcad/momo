/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"SetTriggerParameters.cs"
 * 
 *	A component used to set all of an Interaction's parameters when run as the result of interacting with a Hotspot.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A component used to set all of an Trigger's parameter values */
	[RequireComponent (typeof (AC_Trigger))]
	public class SetTriggerParameters : SetParametersBase
	{

		#region Variables

		private AC_Trigger ownTrigger;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			ownTrigger = GetComponent <AC_Trigger>();
			EventManager.OnRunTrigger += OnRunTrigger;
		}


		protected void OnDisable ()
		{
			EventManager.OnRunTrigger -= OnRunTrigger;
		}

		#endregion


		#region CustomEvents

		protected void OnRunTrigger (AC_Trigger trigger, GameObject collidingObject)
		{
			if (trigger == ownTrigger && trigger.source == ActionListSource.AssetFile)
			{
				AssignParameterValues (trigger);
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			AC_Trigger trigger = GetComponent <AC_Trigger>();
		
			if (trigger.source == ActionListSource.InScene)
			{
				EditorGUILayout.HelpBox ("This component requires that the Trigger's Source field is set to Asset File", MessageType.Warning);
			}
			else if (trigger.source == ActionListSource.AssetFile && trigger.assetFile != null && trigger.assetFile.NumParameters > 0)
			{
				ShowParametersGUI (trigger.assetFile.DefaultParameters, trigger.syncParamValues);
			}
			else
			{
				EditorGUILayout.HelpBox ("No parameters defined for Trigger '" + trigger.gameObject.name + "'.", MessageType.Warning);
			}
		}

		#endif

	}

}
