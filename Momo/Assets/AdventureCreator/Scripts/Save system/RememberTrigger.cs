/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"RememberTrigger.cs"
 * 
 *	This script is attached to Trigger objects in the scene
 *	whose on/off state we wish to save. 
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Attach this script to Trigger objects in the scene whose on/off state you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Trigger")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_trigger.html")]
	public class RememberTrigger : Remember
	{

		/** Whether the Trigger should be enabled or not when the game begins */
		public AC_OnOff startState = AC_OnOff.On;

		private bool loadedData = false;

		
		private void Awake ()
		{
			if (loadedData) return;

			if (GameIsPlaying () && GetComponent <AC_Trigger>())
			{
				if (startState == AC_OnOff.On)
				{
					GetComponent <AC_Trigger>().TurnOn ();
				}
				else
				{
					GetComponent <AC_Trigger>().TurnOff ();
				}
			}
		}
		

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			TriggerData triggerData = new TriggerData ();
			triggerData.objectID = constantID;
			triggerData.savePrevented = savePrevented;

			if (GetComponent <Collider>())
			{
				triggerData.isOn = GetComponent <Collider>().enabled;
			}
			else if (GetComponent <Collider2D>())
			{
				triggerData.isOn = GetComponent <Collider2D>().enabled;
			}
			else
			{
				triggerData.isOn = false;
			}

			return Serializer.SaveScriptData <TriggerData> (triggerData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			TriggerData data = Serializer.LoadScriptData <TriggerData> (stringData);
			if (data == null)
			{
				loadedData = false;
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (GetComponent <Collider>())
			{
				GetComponent <Collider>().enabled = data.isOn;
			}
			else if (GetComponent <Collider2D>())
			{
				GetComponent <Collider2D>().enabled = data.isOn;
			}

			loadedData = true;
		}

	}


	/**
	 * A data container used by the RememberTrigger script.
	 */
	[System.Serializable]
	public class TriggerData : RememberData
	{

		/** True if the Trigger is enabled */
		public bool isOn;


		/**
		 * The default Constructor.
		 */
		public TriggerData () { }

	}

}