/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Button.cs"
 * 
 *	This script is a container class for interactions
 *	that are linked to Hotspots and NPCs.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A data container for Hotspot interactions.
	 */
	[System.Serializable]
	public class Button
	{

		#region Variables

		/** The Interaction ActionList to run, if the Hotspot's interactionSource = InteractionSource.InScene */
		public Interaction interaction = null;
		/** The ActionListAsset to run, if the Hotspots's interactionSource = InteractionSource.AssetFile */
		public ActionListAsset assetFile = null;

		/** The GameObject with the custom script to run, if the Hotspot's interactionSource = InteractionSource.CustomScript */
		public GameObject customScriptObject = null;
		/** The name of the function to run, if the Hotspot's interactionSource = InteractionSource.CustomScript */
		public string customScriptFunction = "";

		/** If True, then the interaction is disabled and cannot be displayed or triggered*/
		public bool isDisabled = false;
		/** The ID number of the inventory item (InvItem) this interaction is associated with, if this is an "Inventory" interaction */
		public int invID = 0;
		/** The ID number of the CursorIcon this interaction is associated with, if this is a "Use" interaction */
		public int iconID = -1;
		/** What kind of inventory interaction mode this responds to (Use, Give) */
		public SelectItemMode selectItemMode = SelectItemMode.Use;

		/** What the Player prefab does after clicking the Hotspot, but before the Interaction itself is run (DoNothing, TurnToFace, WalkTo, WalkToMarker) */
		public PlayerAction playerAction = PlayerAction.DoNothing;

		/** If True, and playerAction = PlayerAction.WalkTo, then the Interaction will be run once the Player is within a certain distance of the Hotspot */
		public bool setProximity = false;
		/** The proximity the Player must be within, if setProximity = True */
		public float proximity = 1f;
		/** If True, then the Player will face the Hotspot after reaching the Marker */
		public bool faceAfter = false;
		/** If True, and playerAction = PlayerAction.WalkTo / WalkToMarker, then gameplay will be blocked while the Player moves */
		public bool isBlocking = false;

		/** If >=0, The ID number of the GameObject ActionParameter in assetFile / interaction to set to the Hotspot that the Button is a part of */
		public int parameterID = -1;
		/** If >=0, The ID number of the InventoryItem ActionParameter in assetFile / interaction to set to the InvItem that was active when the Button was triggered */
		public int invParameterID = -1;

		#endregion


		#region Constructors

		/**
		 * The default Constructor.
		 */
		public Button ()
		{ }

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks if any of the Button's values have been modified from their defaults.</summary>
		 * <returns>True if any of the Button's values have been modified from their defaults.</returns>
		 */
		public bool IsButtonModified ()
		{
			if (interaction != null ||
			    assetFile != null ||
			    customScriptObject != null ||
			    customScriptFunction != "" ||
			    isDisabled != false ||
			    playerAction != PlayerAction.DoNothing ||
			    setProximity != false ||
			    !Mathf.Approximately (proximity, 1f) ||
			    faceAfter != false ||
			    isBlocking != false)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Copies the values of another Button onto itself.</summary>
		 * <param name = "_button">The Button to copies values from</param>
		 */
		public void CopyButton (Button _button)
		{
			interaction = _button.interaction;
			assetFile = _button.assetFile;
			customScriptObject = _button.customScriptObject;
			customScriptFunction = _button.customScriptFunction;
			isDisabled = _button.isDisabled;
			invID = _button.invID;
			iconID = _button.iconID;
			playerAction = _button.playerAction;
			setProximity = _button.setProximity;
			proximity = _button.proximity;
			faceAfter = _button.faceAfter;
			isBlocking = _button.isBlocking;
			parameterID = _button.parameterID;
			invParameterID = _button.invParameterID;
		}


		public string GetFullLabel (Hotspot _hotspot, int _language)
		{
			if (_hotspot == null) return string.Empty;

			if (_hotspot.lookButton == this)
			{
				string prefix = KickStarter.cursorManager.GetLabelFromID (KickStarter.cursorManager.lookCursor_ID, _language);
				return AdvGame.CombineLanguageString (prefix,
													  _hotspot.GetName (_language),
													  _language);
			}
			else if (_hotspot.useButtons.Contains (this))
			{
				string prefix = KickStarter.cursorManager.GetLabelFromID (iconID, _language);
				return AdvGame.CombineLanguageString (prefix,
													  _hotspot.GetName (_language),
													  _language);
			}
			else if (_hotspot.invButtons.Contains (this))
			{
				InvItem item = KickStarter.runtimeInventory.GetItem (invID);
				string prefix = KickStarter.runtimeInventory.GetHotspotPrefixLabel (item, item.GetLabel (_language), _language);
				return AdvGame.CombineLanguageString (prefix,
													  _hotspot.GetName (_language),
													  _language);
			}

			return string.Empty;
		}

		#endregion

	}

}