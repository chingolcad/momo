/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionSceneAdd.cs"
 * 
 *	This action adds or removes a scene without affecting any other open scenes.
 * 
 */

using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionSceneAdd : Action
	{

		public enum SceneAddRemove { Add, Remove };
		public SceneAddRemove sceneAddRemove = SceneAddRemove.Add;
		public bool runCutsceneOnStart;
		public bool runCutsceneIfAlreadyOpen;

		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		public int sceneNumber;
		public int sceneNumberParameterID = -1;
		public string sceneName;
		public int sceneNameParameterID = -1;

		protected bool waitedOneMoreFrame = false;


		public ActionSceneAdd ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Scene;
			title = "Add or remove";
			description = "Adds or removes a scene without affecting any other open scenes.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			sceneNumber = AssignInteger (parameters, sceneNumberParameterID, sceneNumber);
			sceneName = AssignString (parameters, sceneNameParameterID, sceneName);
		}
		
		
		public override float Run ()
		{
			SceneInfo sceneInfo = new SceneInfo (chooseSceneBy, AdvGame.ConvertTokens (sceneName), sceneNumber);

			if (!isRunning)
			{
				waitedOneMoreFrame = false;
				isRunning = true;

				if (KickStarter.sceneSettings.OverridesCameraPerspective ())
				{
					ACDebug.LogError ("The current scene overrides the default camera perspective - this feature should not be used in conjunction with multiple-open scenes.");
				}

				if (sceneAddRemove == SceneAddRemove.Add)
				{
					if (KickStarter.sceneChanger.AddSubScene (sceneInfo))
					{
						return defaultPauseTime;
					}

					if (runCutsceneIfAlreadyOpen && runCutsceneOnStart)
					{
						KickStarter.sceneSettings.cutsceneOnStart.Interact ();
					}
				}
				else if (sceneAddRemove == SceneAddRemove.Remove)
				{
					KickStarter.sceneChanger.RemoveScene (sceneInfo);
				}
			}
			else
			{
				if (!waitedOneMoreFrame)
				{
					waitedOneMoreFrame = true;
					return defaultPauseTime;
				}

				if (sceneAddRemove == SceneAddRemove.Add)
				{
					bool found = false;

					foreach (SubScene subScene in KickStarter.sceneChanger.GetSubScenes ())
					{
						if (subScene.SceneInfo.Matches (sceneInfo))
						{
							found = true;

							if (runCutsceneOnStart && subScene.SceneSettings != null && subScene.SceneSettings.cutsceneOnStart != null)
							{
								subScene.SceneSettings.cutsceneOnStart.Interact ();
							}
						}
					}

					if (!found)
					{
						LogWarning ("Adding a non-AC scene additively!  A GameEngine prefab must be placed in scene '" + sceneInfo.GetLabel () + "'.");
					}
				}

				isRunning = false;
			}

			return 0f;
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			sceneAddRemove = (SceneAddRemove) EditorGUILayout.EnumPopup ("Method:", sceneAddRemove);

			chooseSceneBy = (ChooseSceneBy) EditorGUILayout.EnumPopup ("Choose scene by:", chooseSceneBy);
			if (chooseSceneBy == ChooseSceneBy.Name)
			{
				sceneNameParameterID = Action.ChooseParameterGUI ("Scene name:", parameters, sceneNameParameterID, ParameterType.String);
				if (sceneNameParameterID < 0)
				{
					sceneName = EditorGUILayout.TextField ("Scene name:", sceneName);
				}
			}
			else
			{
				sceneNumberParameterID = Action.ChooseParameterGUI ("Scene number:", parameters, sceneNumberParameterID, ParameterType.Integer);
				if (sceneNumberParameterID < 0)
				{
					sceneNumber = EditorGUILayout.IntField ("Scene number:", sceneNumber);
				}
			}

			if (sceneAddRemove == SceneAddRemove.Add)
			{
				runCutsceneOnStart = EditorGUILayout.Toggle ("Run 'Cutscene on start'?", runCutsceneOnStart);
				if (runCutsceneOnStart)
				{
					runCutsceneIfAlreadyOpen = EditorGUILayout.Toggle ("Run if already open?", runCutsceneIfAlreadyOpen);
				}
			}

			AfterRunningOption ();
		}


		public override string SetLabel ()
		{
			if (chooseSceneBy == ChooseSceneBy.Name)
			{
				return sceneAddRemove.ToString () + " " + sceneName;
			}
			return sceneAddRemove.ToString () + " " + sceneNumber;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Scene: Add or remove' Action, set to add a new scene</summary>
		 * <param name = "newSceneInfo">Data about the scene to add</param>
		 * <param name = "runCutsceneOnStart">If True, the new scene's OnStart cutscene will be triggered</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneAdd CreateNew_Add (SceneInfo newSceneInfo, bool runCutsceneOnStart)
		{
			ActionSceneAdd newAction = (ActionSceneAdd) CreateInstance <ActionSceneAdd>();
			newAction.sceneAddRemove = SceneAddRemove.Add;
			newAction.sceneName = newSceneInfo.name;
			newAction.sceneNumber = newSceneInfo.number;
			newAction.chooseSceneBy = string.IsNullOrEmpty (newSceneInfo.name) ? ChooseSceneBy.Number : ChooseSceneBy.Name;
			newAction.runCutsceneOnStart = runCutsceneOnStart;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Scene: Add or remove' Action, set to remove an open scene</summary>
		 * <param name = "removeSceneInfo">Data about the scene to remove</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneAdd CreateNew_Remove (SceneInfo removeSceneInfo)
		{
			ActionSceneAdd newAction = (ActionSceneAdd) CreateInstance <ActionSceneAdd>();
			newAction.sceneAddRemove = SceneAddRemove.Remove;
			newAction.sceneName = removeSceneInfo.name;
			newAction.sceneNumber = removeSceneInfo.number;
			newAction.chooseSceneBy = string.IsNullOrEmpty (removeSceneInfo.name) ? ChooseSceneBy.Number : ChooseSceneBy.Name;
			return newAction;
		}
		
	}

}