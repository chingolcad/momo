/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionSpeech.cs"
 * 
 *	This action handles the displaying of messages, and talking of characters.
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
	public class ActionSpeech : Action, ITranslatable
	{
		
		public int constantID = 0;
		public int parameterID = -1;
		
		public int messageParameterID = -1;
		
		public bool isPlayer;
		public Char speaker;
		public string messageText;
		public int lineID;
		public int[] multiLineIDs;
		public bool isBackground = false;
		public bool noAnimation = false;
		public AnimationClip headClip;
		public AnimationClip mouthClip;
		
		public bool play2DHeadAnim = false;
		public string headClip2D = "";
		public int headLayer;
		public bool play2DMouthAnim = false;
		public string mouthClip2D = "";
		public int mouthLayer;
		
		public float waitTimeOffset = 0f;
		protected bool stopAction = false;
		
		protected int splitIndex = 0;
		protected bool splitDelay = false;

		protected Char runtimeSpeaker;
		protected Speech speech;
		protected LocalVariables localVariables;
		protected bool runActionListInBackground;
		protected List<ActionParameter> ownParameters = new List<ActionParameter>();

		public static string[] stringSeparators = new string[] {"\n", "\\n"};


		public ActionSpeech ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Dialogue;
			title = "Play speech";
			description = "Makes a Character talk, or – if no Character is specified – displays a message. Subtitles only appear if they are enabled from the Options menu. A 'thinking' effect can be produced by opting to not play any animation.";
			lineID = -1;
		}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (parameters != null) ownParameters = parameters;

			runtimeSpeaker = AssignFile <Char> (parameters, parameterID, constantID, speaker);

			// Special case: Use associated NPC
			if (runtimeSpeaker != null &&
				runtimeSpeaker is Player &&
				KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow &&
				KickStarter.player != null)
			{
				// Make sure not the active Player
				ConstantID speakerID = speaker.GetComponent <ConstantID>();
				ConstantID playerID = KickStarter.player.GetComponent <ConstantID>();
				if ((speakerID == null && playerID != null) ||
					(speakerID != null && playerID == null) ||
					(speakerID != null && playerID != null && speakerID.constantID != playerID.constantID))
				{
					Player speakerPlayer = runtimeSpeaker as Player;
					foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
					{
						if (playerPrefab != null && playerPrefab.playerOb == speakerPlayer)
						{
							if (speakerPlayer.associatedNPCPrefab != null)
							{
								ConstantID npcConstantID = speakerPlayer.associatedNPCPrefab.GetComponent <ConstantID>();
								if (npcConstantID != null)
								{
									runtimeSpeaker = AssignFile <Char> (parameters, parameterID, npcConstantID.constantID, runtimeSpeaker);
								}
							}
							break;
						}
					}
				}
			}

			messageText = AssignString (parameters, messageParameterID, messageText);
			
			if (isPlayer)
			{
				runtimeSpeaker = KickStarter.player;
			}
		}


		public override void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
				runActionListInBackground = (actionList.actionListType == ActionListType.RunInBackground);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}
		
		
		public override float Run ()
		{
			if (KickStarter.speechManager == null)
			{
				Log ("No Speech Manager present");
				return 0f;
			}

			if (KickStarter.dialog && KickStarter.stateHandler)
			{
				if (!isRunning)
				{
					stopAction = false;
					isRunning = true;
					splitDelay = false;
					splitIndex = 0;

					StartSpeech ();

					if (isBackground)
					{
						if (KickStarter.speechManager.separateLines)
						{
							string[] textArray = messageText.Split (stringSeparators, System.StringSplitOptions.None);
							if (textArray != null && textArray.Length > 1)
							{
								LogWarning ("Cannot separate multiple speech lines when 'Is Background?' is checked - will only play '" + textArray[0] + "'");
							}
						}

						isRunning = false;
						return 0f;
					}
					return defaultPauseTime;
				}
				else
				{
					if (stopAction || (speech != null && speech.continueFromSpeech))
					{
						if (speech != null)
						{
							speech.continueFromSpeech = false;
						}
						isRunning = false;

						return 0;
					}

					if (speech == null || !speech.isAlive)
					{
						if (KickStarter.speechManager.separateLines)
						{
							if (!splitDelay)
							{
								// Begin pause if more lines are present
								splitIndex ++;
								string[] textArray = messageText.Split (stringSeparators, System.StringSplitOptions.None);
								
								if (textArray.Length > splitIndex)
								{
									if (KickStarter.speechManager.separateLinePause > 0f)
									{
										// Still got more to go
										splitDelay = true;
										return KickStarter.speechManager.separateLinePause;
									}
									else
									{
										// Show next line
										splitDelay = false;
										StartSpeech ();
										return defaultPauseTime;
									}
								}
								// else finished
							}
							else
							{
								// Show next line
								splitDelay = false;
								StartSpeech ();
								return defaultPauseTime;
							}
						}
						
						if (waitTimeOffset <= 0f)
						{
							isRunning = false;
							return 0f;
						}
						else
						{
							stopAction = true;
							return waitTimeOffset;
						}
					}
					else
					{
						return defaultPauseTime;
					}
				}
			}

			return 0f;
		}
		
		
		public override void Skip ()
		{
			KickStarter.dialog.KillDialog (true, true);

			SpeechLog log = new SpeechLog ();
			log.lineID = lineID;
			log.fullText = messageText;

			if (runtimeSpeaker)
			{
				log.speakerName = runtimeSpeaker.name;
				if (!noAnimation)
				{
					runtimeSpeaker.isTalking = false;
					
					if (runtimeSpeaker.GetAnimEngine () != null)
					{
						runtimeSpeaker.GetAnimEngine ().ActionSpeechSkip (this);
					}
				}
			}

			KickStarter.runtimeVariables.AddToSpeechLog (log);
		}


		public AC.Char Speaker
		{
			get
			{
				if (Application.isPlaying)
				{
					return runtimeSpeaker;
				}
				return speaker;
			}
		}
		
		
		#if UNITY_EDITOR

		public override void ClearIDs ()
		{
			lineID = -1;
		}

		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (lineID > -1)
			{
				if (multiLineIDs != null && multiLineIDs.Length > 0 && AdvGame.GetReferences ().speechManager != null && AdvGame.GetReferences ().speechManager.separateLines)
				{
					string IDs = lineID.ToString ();
					foreach (int multiLineID in multiLineIDs)
					{
						IDs += ", " + multiLineID;
					}

					EditorGUILayout.LabelField ("Speech Manager IDs:", IDs);

				}
				else
				{
					EditorGUILayout.LabelField ("Speech Manager ID:", lineID.ToString ());
				}
			}

			if (Application.isPlaying && runtimeSpeaker == null)
			{
				AssignValues (parameters);
			}

			isPlayer = EditorGUILayout.Toggle ("Player line?", isPlayer);
			if (!isPlayer)
			{
				if (Application.isPlaying)
				{
					if (runtimeSpeaker)
					{
						EditorGUILayout.LabelField ("Speaker: " + runtimeSpeaker.name);
					}
					else
					{
						EditorGUILayout.HelpBox ("The speaker cannot be assigned while the game is running.", MessageType.Info);
					}
				}
				else
				{
					parameterID = Action.ChooseParameterGUI ("Speaker:", parameters, parameterID, ParameterType.GameObject);
					if (parameterID >= 0)
					{
						constantID = 0;
						speaker = null;
					}
					else
					{
						speaker = (Char) EditorGUILayout.ObjectField ("Speaker:", speaker, typeof(Char), true);
						
						constantID = FieldToID <Char> (speaker, constantID);
						speaker = IDToField <Char> (speaker, constantID, false);
					}
				}
			}
			
			messageParameterID = Action.ChooseParameterGUI ("Line text:", parameters, messageParameterID, ParameterType.String);
			if (messageParameterID < 0)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Line text:", GUILayout.Width (65f));
				EditorStyles.textField.wordWrap = true;
				messageText = EditorGUILayout.TextArea (messageText, GUILayout.MaxWidth (400f));
				EditorGUILayout.EndHorizontal ();
			}

			Char _speaker = null;
			if (isPlayer)
			{
				if (Application.isPlaying)
				{
					_speaker = KickStarter.player;
				}
				else if (AdvGame.GetReferences ().settingsManager)
				{
					_speaker = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
				}
			}
			else
			{
				_speaker = (Application.isPlaying) ? runtimeSpeaker : speaker;
			}

			if (_speaker != null)
			{
				noAnimation = EditorGUILayout.Toggle ("Don't animate speaker?", noAnimation);
				if (!noAnimation)
				{
					if (_speaker.GetAnimEngine ())
					{
						_speaker.GetAnimEngine ().ActionSpeechGUI (this, _speaker);
					}
				}
			}
			else if (!isPlayer && parameterID < 0)
			{
				EditorGUILayout.HelpBox ("If no Character is set, this line\nwill be considered to be a Narration.", MessageType.Info);
			}
			
			isBackground = EditorGUILayout.Toggle ("Play in background?", isBackground);
			if (!isBackground)
			{
				waitTimeOffset = EditorGUILayout.Slider ("Wait time offset (s):", waitTimeOffset, 0f, 4f);
			}

			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID <Char> (speaker, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (parameterID == -1)
			{
				if (isPlayer)
				{
					return "Player";
				}
				else if (speaker != null)
				{
					return speaker.gameObject.name;
				}
				else
				{
					return "Narrator";
				}
			}
			return string.Empty;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			string newMessageText = AdvGame.ConvertLocalVariableTokenToGlobal (messageText, oldLocalID, newGlobalID);
			if (messageText != newMessageText && messageParameterID < 0)
			{
				wasAmended = true;
				messageText = newMessageText;
			}
			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool isAffected = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			string newMessageText = AdvGame.ConvertGlobalVariableTokenToLocal (messageText, oldGlobalID, newLocalID);
			if (messageText != newMessageText && messageParameterID < 0)
			{
				isAffected = true;
				if (isCorrectScene)
				{
					messageText = newMessageText;
				}
			}
			return isAffected;
		}


		public override int GetVariableReferences (List<ActionParameter> parameters, VariableLocation location, int varID, Variables _variables)
		{
			int thisCount = 0;

			string tokenText = AdvGame.GetVariableTokenText (location, varID);
			if (!string.IsNullOrEmpty (tokenText) && !string.IsNullOrEmpty (messageText) && messageText.Contains (tokenText) && messageParameterID < 0)
			{
				thisCount ++;
			}
			thisCount += base.GetVariableReferences (parameters, location, varID, _variables);
			return thisCount;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (speaker != null && speaker.gameObject == gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && gameObject.GetComponent <Player>()) return true;
			return false;
		}

		#endif


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			if (KickStarter.speechManager.separateLines)
			{
				return GetSpeechArray () [index];
			}
			return messageText;
		}


		public int GetTranslationID (int index)
		{
			if (index == 0)
			{
				return lineID;
			}
			else
			{
				return multiLineIDs[index-1];
			}
		}

		#if UNITY_EDITOR

		public int GetNumTranslatables ()
		{
			if (KickStarter.speechManager.separateLines)
			{
				string[] messages = GetSpeechArray ();

				if (messages.Length > 1)
				{
					List<int> lineIDs = new List<int>();
					for (int i=1; i<messages.Length; i++)
					{
						if (multiLineIDs != null && multiLineIDs.Length > (i-1))
						{
							lineIDs.Add (multiLineIDs[i-1]);
						}
						else
						{
							lineIDs.Add (-1);
						}
					}
					multiLineIDs = lineIDs.ToArray ();
				}
				else
				{
					multiLineIDs = null;
				}

				return messages.Length;
			}

			multiLineIDs = null;
			return 1;
		}


		public bool HasExistingTranslation (int index)
		{
			if (index == 0)
			{
				return (lineID > -1);
			}
			else
			{
				return (multiLineIDs[index-1] > -1);
			}
		}


		public void SetTranslationID (int index, int _lineID)
		{
			if (index == 0)
			{
				lineID = _lineID;
			}
			else
			{
				multiLineIDs[index-1] = _lineID;
			}
		}


		public string GetOwner (int index)
		{
			string _speaker = "";
			bool _isPlayer = isPlayer;
			if (!_isPlayer && speaker != null && speaker is Player)
			{
				_isPlayer = true;
			}
			
			if (_isPlayer)
			{
				_speaker = "Player";

				if (isPlayer && KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow && KickStarter.settingsManager.player)
				{
					_speaker = KickStarter.settingsManager.player.name;
				}
				else if (!isPlayer && speaker != null)
				{
					_speaker = speaker.name;
				}
			}
			else
			{
				if (isAssetFile)
				{
					if (!isPlayer && parameterID == -1)
					{
						speaker = IDToField <Char> (speaker, constantID, false);
					}
				}

				if (speaker)
				{
					_speaker = speaker.name;
				}
				else
				{
					_speaker = "Narrator";
				}
			}

			return _speaker;
		}


		public bool OwnerIsPlayer (int index)
		{
			if (isPlayer)
			{
				return true;
			}

			if (speaker != null && speaker is Player)
			{
				return true;
			}

			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Speech;
		}


		public bool CanTranslate (int index)
		{
			if (!string.IsNullOrEmpty (messageText) && messageParameterID < 0)
			{
				return true;
			}
			return false;
		}

		#endif

		#endregion


		protected string[] GetSpeechArray ()
		{
			string _text = messageText.Replace ("\\n", "\n");
			string[] textArray = _text.Split (stringSeparators, System.StringSplitOptions.None);
			return textArray;
		}


		protected void StartSpeech ()
		{
			string _text = messageText;
			int _lineID = lineID;
			
			int languageNumber = Options.GetLanguage ();
			if (languageNumber > 0)
			{
				// Not in original language, so pull translation in from Speech Manager
				_text = KickStarter.runtimeLanguages.GetTranslation (_text, lineID, languageNumber, AC_TextType.Speech);
			}
			
			_text = _text.Replace ("\\n", "\n");

			if (KickStarter.speechManager.separateLines)
			{
				string[] textArray = messageText.Replace ("\\n", "\n").Split (stringSeparators, System.StringSplitOptions.None);
				if (textArray.Length > 1)
				{
					_text = textArray [splitIndex];

					if (splitIndex > 0)
					{
						if (multiLineIDs != null && multiLineIDs.Length > (splitIndex-1))
						{
							_lineID = multiLineIDs[splitIndex-1];
						}
						else
						{
							_lineID = -1;
						}
					}

					if (languageNumber > 0)
					{
						_text = KickStarter.runtimeLanguages.GetTranslation (_text, _lineID, languageNumber, AC_TextType.Speech);
					}
				}
			}
			
			if (!string.IsNullOrEmpty (_text))
			{
				_text = AdvGame.ConvertTokens (_text, languageNumber, localVariables, ownParameters);
			
				speech = KickStarter.dialog.StartDialog (runtimeSpeaker, _text, (isBackground || runActionListInBackground), _lineID, noAnimation);

				if (runtimeSpeaker != null && !noAnimation && speech != null)
				{
					if (runtimeSpeaker.GetAnimEngine () != null)
					{
						runtimeSpeaker.GetAnimEngine ().ActionSpeechRun (this);
					}
				}
			}
		}


		/**
		 * <summary>Creates a new instance of the 'Dialogue: Play speech' Action with key variables already set.</summary>
		 * <param name = "charToSpeak">The character to speak</param>
		 * <param name = "subtitleText">What the character says</param>
		 * <param name = "translationID">The line's translation ID number, as generated by the Speech Manager</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the character has finished speaking</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSpeech CreateNew (Char charToSpeak, string subtitleText, int translationID = -1, bool waitUntilFinish = true)
		{
			ActionSpeech newAction = (ActionSpeech) CreateInstance <ActionSpeech>();
			newAction.speaker = charToSpeak;
			newAction.messageText = subtitleText;
			newAction.isBackground = !waitUntilFinish;
			newAction.lineID = translationID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Dialogue: Play speech' Action with key variables already set.</summary>
		 * <param name = "characterToSpeak">The character to speak</param>
		 * <param name = "subtitleText">What the character says</param>
		 * <param name = "translationIDs">The line's translation ID numbers, as generated by the Speech Manager</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the character has finished speaking</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSpeech CreateNew (Char characterToSpeak, string subtitleText, int[] translationIDs, bool waitUntilFinish = true)
		{
			ActionSpeech newAction = (ActionSpeech) CreateInstance <ActionSpeech>();
			newAction.speaker = characterToSpeak;
			newAction.messageText = subtitleText;
			newAction.isBackground = !waitUntilFinish;

			newAction.lineID = -1;
			if (translationIDs != null && translationIDs.Length > 0)
			{
				newAction.multiLineIDs = new int[translationIDs.Length-1];
				for (int i=0; i<translationIDs.Length; i++)
				{
					if (i == 0) newAction.lineID = translationIDs[i];
					else newAction.multiLineIDs[i-1] = translationIDs[i];
				}
			}
			return newAction;
		}

	}
	
}