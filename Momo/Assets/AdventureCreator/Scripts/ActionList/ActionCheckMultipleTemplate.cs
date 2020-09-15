/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCheckMultipleTemplate.cs"
 * 
 *	This is a blank action template, which has any number of outputs.
 * 
 */

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace AC
{

	[System.Serializable]
	public class ActionCheckMultipleTemplate : ActionCheckMultiple
	{
		
		// Declare variables here
		
		
		public ActionCheckMultipleTemplate ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Custom;
			title = "Check multiple template";
			description = "This is a blank 'Check multiple' Action template.";
		}


		public override ActionEnd End (List<Action> actions)
		{
			// Here, we decide which output socket to follow (starting from 0)
			int outputSocketIndex = 0;

			// Then, we pass this index number onto ProcessResult and return the result
			return ProcessResult (outputSocketIndex, actions);
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI ()
		{
			// Action-specific Inspector GUI code here.

			// Set the number of output sockets here if dynamic, or in the constructor if fixed
			numSockets = 3;
		}
		

		public override string SetLabel ()
		{
			// (Optional) Return a string used to describe the specific action's job.

			return string.Empty;
		}


		protected override string GetSocketLabel (int i)
		{
			// (Optional) Return an output socket's label, given the index number (starting from 1).

			return "Option " + i.ToString () + ":";
		}

		#endif
		
	}

}