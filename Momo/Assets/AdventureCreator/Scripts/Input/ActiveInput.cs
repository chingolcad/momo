/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActiveInput.cs"
 * 
 *	A data container for active inputs, which map ActionListAssets to input buttons.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A data container for active inputs, which map ActionListAssets to input buttons.
	 */
	[System.Serializable]
	public class ActiveInput
	{

		#region Variables

		/** An Editor-friendly name */
		public string label;
		/** A unique identifier */
		public int ID;
		/** The name of the Input button, as defined in the Input Manager */
		public string inputName;
		/** If True, the active input is enabled when the game begins */
		public bool enabledOnStart = true;
		/** What state the game must be in for the actionListAsset to run (Normal, Cutscene, Paused, DialogOptions) */
		public GameState gameState;
		/** The ActionListAsset to run when the input button is pressed */
		public ActionListAsset actionListAsset;
		/** What type of input is expected (Button, Axis) */
		public SimulateInputType inputType = SimulateInputType.Button;
		/** If inputType = SimulateInputType.Axis, the threshold value for the axis to trigger the ActionListAsset */
		public float axisThreshold = 0.2f;

		[SerializeField] protected ActiveInputButtonType buttonType = ActiveInputButtonType.OnButtonDown;
		protected enum ActiveInputButtonType { OnButtonDown, OnButtonUp };

		protected bool isEnabled;

		#endregion


		#region Constructors

		/**
		 * The default Constructor.
		 */
		public ActiveInput (int[] idArray)
		{
			inputName = string.Empty;
			gameState = GameState.Normal;
			actionListAsset = null;
			enabledOnStart = true;
			ID = 1;
			inputType = SimulateInputType.Button;
			buttonType = ActiveInputButtonType.OnButtonDown;
			axisThreshold = 0.2f;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (ID == _id)
					ID ++;
			}
		}


		/**
		 * <summary>A Constructor in which the unique identifier is explicitly set.</summary>
		 * <param name = "_ID">The unique identifier</param>
		 */
		public ActiveInput (int _ID)
		{
			inputName = string.Empty;
			gameState = GameState.Normal;
			actionListAsset = null;
			enabledOnStart = true;
			ID = _ID;
			inputType = SimulateInputType.Button;
			buttonType = ActiveInputButtonType.OnButtonDown;
			axisThreshold = 0.2f;
		}

		#endregion


		#region PublicFunctions

		/**
		 * Sets the enabled state to the default value.
		 */
		public void SetDefaultState ()
		{
			isEnabled = enabledOnStart;
		}


		/**
		 * <summary>Tests if the associated input button is being pressed at the right time, and runs its associated ActionListAsset if it is</summary>
		 */
		public bool TestForInput ()
		{
			if (IsEnabled)
			{
				switch (inputType)
				{
					case SimulateInputType.Button:
						if (buttonType == ActiveInputButtonType.OnButtonDown)
						{
							if (KickStarter.playerInput.InputGetButtonDown (inputName))
							{
								return TriggerIfStateMatches ();
							}
						}
						else if (buttonType == ActiveInputButtonType.OnButtonUp)
						{
							if (KickStarter.playerInput.InputGetButtonUp (inputName))
							{
								return TriggerIfStateMatches ();
							}
						}
						break;

					case SimulateInputType.Axis:
						float axisValue = KickStarter.playerInput.InputGetAxis (inputName);
						if ((axisThreshold >= 0f && axisValue > axisThreshold) || (axisThreshold < 0f && axisValue < axisThreshold))
						{
							return TriggerIfStateMatches ();
						}
						break;
				}
			}
			return false;
		}

		#endregion


		#region ProtectedFunctions

		protected bool TriggerIfStateMatches ()
		{
			if (KickStarter.stateHandler.gameState == gameState && actionListAsset != null && !KickStarter.actionListAssetManager.IsListRunning (actionListAsset))
			{
				AdvGame.RunActionListAsset (actionListAsset);
				return true;
			}
			return false;
		}

		#endregion


		#region GetSet

		/**
		 * The runtime enabled state of the active input
		 */
		public bool IsEnabled
		{
			get
			{
				return isEnabled;
			}
			set
			{
				if (value && string.IsNullOrEmpty (inputName))
				{
					ACDebug.LogWarning ("Active input " + ID + " has no input name!");
					value = false;
				}

				isEnabled = value;
			}
		}


		public string Label
		{
			get
			{
				if (string.IsNullOrEmpty (label))
				{
					if (!string.IsNullOrEmpty (inputName))
					{
						label = inputName;
					}
					else
					{
						label = "(Untitled)";	
					}
				}
				return label;
			}
		}

		#endregion


		#region StaticFunctions

		/**
		 * Upgrades all active inputs from pre-v1.58
		 */
		public static void Upgrade ()
		{
			// Set IDs as index + 1 (because default is 0 when not upgraded)
			if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.activeInputs != null)
			{
				if (AdvGame.GetReferences ().settingsManager.activeInputs.Count > 0 && AdvGame.GetReferences ().settingsManager.activeInputs[0].ID == 0)
				{
					for (int i=0; i<AdvGame.GetReferences ().settingsManager.activeInputs.Count; i++)
					{
						AdvGame.GetReferences ().settingsManager.activeInputs[i].ID = i+1;
						AdvGame.GetReferences ().settingsManager.activeInputs[i].enabledOnStart = true;
					}
				}
			}
		}


		/**
		 * <summary>Creates a save string containing the enabled state of a list of active inputs</summary>
		 * <param name = "activeInputs">The active inputs to save</param>
		 * <returns>The save string</returns>
		 */
		public static string CreateSaveData (List<ActiveInput> activeInputs)
		{
			if (activeInputs != null && activeInputs.Count > 0)
			{
				System.Text.StringBuilder dataString = new System.Text.StringBuilder ();
				
				foreach (ActiveInput activeInput in activeInputs)
				{
					if (activeInput != null)
					{
						dataString.Append (activeInput.ID.ToString ());
						dataString.Append (SaveSystem.colon);
						dataString.Append ((activeInput.IsEnabled) ? "1" : "0");
						dataString.Append (SaveSystem.pipe);
					}
				}
				dataString.Remove (dataString.Length-1, 1);
				return dataString.ToString ();		
			}
			return "";
		}


		/**
		 * <summary>Restores the enabled states of the game's active inputs from a saved data string</summary>
		 * <param name = "dataString">The data string to load</param>
		 */
		public static void LoadSaveData (string dataString)
		{
			if (!string.IsNullOrEmpty (dataString) && KickStarter.settingsManager.activeInputs != null && KickStarter.settingsManager.activeInputs.Count > 0)
			{
				string[] dataArray = dataString.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in dataArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
		
					int _enabled = 0;
					int.TryParse (chunkData[1], out _enabled);

					foreach (ActiveInput activeInput in KickStarter.settingsManager.activeInputs)
					{
						if (activeInput.ID == _id)
						{
							activeInput.isEnabled = (_enabled == 1) ? true : false;
						}
					}
				}
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			string defaultName = "ActiveInput_" + Label;

			label = CustomGUILayout.TextField ("Label:", label, string.Empty, "An Editor-friendly name");
			inputName = CustomGUILayout.TextField ("Input button:", inputName, string.Empty, "The name of the Input button, as defined in the Input Manager");
			inputType = (SimulateInputType) CustomGUILayout.EnumPopup ("Input type:", inputType, string.Empty, "What type of input is expected");
			if (inputType == SimulateInputType.Axis)
			{
				axisThreshold = CustomGUILayout.Slider ("Axis threshold:", axisThreshold, -1f, 1f, string.Empty, "The threshold value for the axis to trigger the ActionListAsset");
			}
			else if (inputType == SimulateInputType.Button)
			{
				buttonType = (ActiveInputButtonType) CustomGUILayout.EnumPopup ("Responds to:", buttonType, string.Empty, "What type of button press this responds to");
			}
			enabledOnStart = CustomGUILayout.Toggle ("Enabled by default?", enabledOnStart, string.Empty, "If True, the active input is enabled when the game begins");
			gameState = (GameState) CustomGUILayout.EnumPopup ("Available when game is:", gameState, string.Empty, "What state the game must be in for the actionListAsset to run");
			actionListAsset = ActionListAssetMenu.AssetGUI ("ActionList when triggered:", actionListAsset, defaultName, string.Empty, "The ActionListAsset to run when the input button is pressed");
		}

		#endif

	}

}