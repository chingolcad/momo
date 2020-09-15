/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionCharPortrait.cs"
 * 
 *	This action picks a new portrait for the chosen Character.
 *	Written for the AC community by Guran.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionCharPortrait : Action
	{

		public int parameterID = -1;
		public int constantID = 0;
		public bool isPlayer;
		public Char _char;
		protected Char runtimeChar;
		public Texture newPortraitGraphic;


		public ActionCharPortrait ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Character;
			title = "Switch Portrait";
			description = "Changes the 'speaking' graphic used by Characters. To display this graphic in a Menu, place a Graphic element of type Dialogue Portrait in a Menu of Appear type: When Speech Plays. If the new graphic is placed in a Resources folder, it will be stored in saved game files.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeChar = AssignFile <Char> (parameters, parameterID, constantID, _char);

			if (isPlayer)
			{
				runtimeChar = KickStarter.player;
			}
		}

		
		public override float Run ()
		{
			if (runtimeChar)
			{
				runtimeChar.portraitIcon.ReplaceTexture (newPortraitGraphic);
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			// Action-specific Inspector GUI code here
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (isPlayer)
			{
				if (Application.isPlaying)
				{
					_char = KickStarter.player;
				}
				else
				{
					_char = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
				}
			}
			else
			{
				parameterID = Action.ChooseParameterGUI ("Character:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					_char = null;
				}
				else
				{
					_char = (Char) EditorGUILayout.ObjectField ("Character:", _char, typeof (Char), true);
					
					constantID = FieldToID <Char> (_char, constantID);
					_char = IDToField <Char> (_char, constantID, false);
				}
			}
			
			newPortraitGraphic = (Texture) EditorGUILayout.ObjectField ("New Portrait graphic:", newPortraitGraphic, typeof (Texture), true);
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				if (saveScriptsToo)
				{
					if (_char != null && _char.GetComponent <NPC>())
					{
						AddSaveScript <RememberNPC> (_char);
					}
				}

				AssignConstantID <Char> (_char, constantID, parameterID);
			}
		}


		public override string SetLabel ()
		{
			if (isPlayer)
			{
				return "Player";
			}
			else if (_char != null)
			{
				return _char.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (_char != null && _char.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && _gameObject.GetComponent <Player>() != null) return true;
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Switch portrait' Action with key variables already set.</summary>
		 * <param name = "characterToUpdate">The character to update</param>
		 * <param name = "newPortraitGraphic">The texture to assign as the character's portrait graphic</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharPortrait CreateNew (Char characterToUpdate, Texture newPortraitGraphic)
		{
			ActionCharPortrait newAction = (ActionCharPortrait) CreateInstance <ActionCharPortrait>();
			newAction._char = characterToUpdate;
			newAction.newPortraitGraphic = newPortraitGraphic;
			return newAction;
		}
		
	}

}