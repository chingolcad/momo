/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionFootstepSounds.cs"
 * 
 *	This Action changes the sounds listed in a FootstepSounds component.
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
	public class ActionFootstepSounds : Action
	{

		public int constantID = 0;
		public int parameterID = -1;
		public FootstepSounds footstepSounds;
		protected FootstepSounds runtimeFootstepSounds;

		public bool isPlayer;

		public enum FootstepSoundType { Walk, Run };
		public FootstepSoundType footstepSoundType = FootstepSoundType.Walk;

		public AudioClip[] newSounds;


		public ActionFootstepSounds ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Sound;
			title = "Change footsteps";
			description = "Changes the sounds used by a FootstepSounds component.";
		}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				if (KickStarter.player != null)
				{
					runtimeFootstepSounds = KickStarter.player.GetComponentInChildren <FootstepSounds>();
				}
			}
			else
			{
				runtimeFootstepSounds = AssignFile <FootstepSounds> (parameters, parameterID, constantID, footstepSounds);
			}
		}


		public override float Run ()
		{
			if (runtimeFootstepSounds == null)
			{
				LogWarning ("No FootstepSounds component set.");
			}
			else
			{
				if (footstepSoundType == FootstepSoundType.Walk)
				{
					runtimeFootstepSounds.footstepSounds = newSounds;
				}
				else if (footstepSoundType == FootstepSoundType.Run)
				{
					runtimeFootstepSounds.runSounds = newSounds;
				}
			}

			return 0f;
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Change Player's?", isPlayer);
			if (!isPlayer)
			{
				parameterID = Action.ChooseParameterGUI ("FootstepSounds:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					footstepSounds = null;
				}
				else
				{
					footstepSounds = (FootstepSounds) EditorGUILayout.ObjectField ("FootstepSounds:", footstepSounds, typeof (FootstepSounds), true);
					
					constantID = FieldToID <FootstepSounds> (footstepSounds, constantID);
					footstepSounds = IDToField <FootstepSounds> (footstepSounds, constantID, false);
				}
			}

			footstepSoundType = (FootstepSoundType) EditorGUILayout.EnumPopup ("Clips to change:", footstepSoundType);
			newSounds = ShowClipsGUI (newSounds, (footstepSoundType == FootstepSoundType.Walk) ? "New walk sounds:" : "New run sounds:");

			AfterRunningOption ();
		}


		private AudioClip[] ShowClipsGUI (AudioClip[] clips, string headerLabel)
		{
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField (headerLabel, EditorStyles.boldLabel);
			List<AudioClip> clipsList = new List<AudioClip>();

			if (clips != null)
			{
				foreach (AudioClip clip in clips)
				{
					clipsList.Add (clip);
				}
			}

			int numParameters = clipsList.Count;
			numParameters = EditorGUILayout.IntField ("# of footstep sounds:", numParameters);

			if (numParameters < clipsList.Count)
			{
				clipsList.RemoveRange (numParameters, clipsList.Count - numParameters);
			}
			else if (numParameters > clipsList.Count)
			{
				if (numParameters > clipsList.Capacity)
				{
					clipsList.Capacity = numParameters;
				}
				for (int i=clipsList.Count; i<numParameters; i++)
				{
					clipsList.Add (null);
				}
			}

			for (int i=0; i<clipsList.Count; i++)
			{
				clipsList[i] = (AudioClip) EditorGUILayout.ObjectField ("Sound #" + (i+1).ToString (), clipsList[i], typeof (AudioClip), false);
			}
			if (clipsList.Count > 1)
			{
				EditorGUILayout.HelpBox ("Sounds will be chosen at random.", MessageType.Info);
			}
			EditorGUILayout.EndVertical ();

			return clipsList.ToArray ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			FootstepSounds obToUpdate = footstepSounds;
			if (isPlayer)
			{
				if (!fromAssetFile && GameObject.FindObjectOfType <Player>() != null)
				{
					obToUpdate = GameObject.FindObjectOfType <Player>().GetComponentInChildren <FootstepSounds>();
				}

				if (obToUpdate == null && AdvGame.GetReferences ().settingsManager != null)
				{
					Player player = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
					obToUpdate = player.GetComponentInChildren <FootstepSounds>();
				}
			}

			if (saveScriptsToo)
			{
				AddSaveScript <RememberFootstepSounds> (obToUpdate);
			}
			AssignConstantID <FootstepSounds> (obToUpdate, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (parameterID == -1)
			{
				if (isPlayer)
				{
					return "Player";
				}
				else if (footstepSounds)
				{
					return footstepSounds.gameObject.name;
				}
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (footstepSounds != null && footstepSounds.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && _gameObject.GetComponent <Player>() != null) return true;
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Sound: Change footsteps' Action</summary>
		 * <param name = "footstepSoundsToModify">The FootstepSounds component to affect</param>
		 * <param name = "footstepSoundType">The type of footsteps (Walk / Run) to change</param>
		 * <param name = "newSounds">An array of sounds to set as the new sounds</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionFootstepSounds CreateNew (FootstepSounds footstepSoundsToModify, FootstepSoundType footstepSoundType, AudioClip[] newSounds)
		{
			ActionFootstepSounds newAction = (ActionFootstepSounds) CreateInstance <ActionFootstepSounds>();
			newAction.footstepSounds = footstepSoundsToModify;
			newAction.footstepSoundType = footstepSoundType;
			newAction.newSounds = newSounds;
			return newAction;
		}
		
	}
	
}