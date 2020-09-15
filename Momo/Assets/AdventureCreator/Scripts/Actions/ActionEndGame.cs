/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionEndGame.cs"
 * 
 *	This Action will force the game to either
 *	restart an autosave, or quit.
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
	public class ActionEndGame : Action
	{
		
		public enum AC_EndGameType { QuitGame, LoadAutosave, ResetScene, RestartGame };
		public AC_EndGameType endGameType;
		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		public int sceneNumber;
		public string sceneName;
		public bool resetMenus;
		
		
		public ActionEndGame ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Engine;
			title = "End game";
			description = "Ends the current game, either by loading an autosave, restarting or quitting the game executable.";
			numSockets = 0;
		}
		
		
		public override float Run ()
		{
			if (endGameType == AC_EndGameType.QuitGame)
			{
				#if UNITY_EDITOR
					UnityEditor.EditorApplication.isPlaying = false;
				#else
					Application.Quit ();
				#endif
			}
			else if (endGameType == AC_EndGameType.LoadAutosave)
			{
				SaveSystem.LoadAutoSave ();
			}
			else
			{
				KickStarter.runtimeInventory.SetNull ();
				KickStarter.runtimeInventory.RemoveRecipes ();

				if (KickStarter.player)
				{
					DestroyImmediate (KickStarter.player.gameObject);
				}

				if (endGameType == AC_EndGameType.RestartGame)
				{
					KickStarter.ResetPlayer (KickStarter.settingsManager.GetDefaultPlayer (), KickStarter.settingsManager.GetDefaultPlayerID (), false, Quaternion.identity);

					KickStarter.saveSystem.ClearAllData ();
					KickStarter.levelStorage.ClearAllLevelData ();
					KickStarter.runtimeInventory.OnStart ();
					KickStarter.runtimeDocuments.OnStart ();
					KickStarter.runtimeVariables.OnStart ();

					if (resetMenus)
					{
						KickStarter.playerMenus.RebuildMenus ();
					}

					KickStarter.eventManager.Call_OnRestartGame ();

					KickStarter.stateHandler.CanGlobalOnStart ();
					KickStarter.sceneChanger.ChangeScene (new SceneInfo (chooseSceneBy, sceneName, sceneNumber), false, true);
				}
				else if (endGameType == AC_EndGameType.ResetScene)
				{
					sceneNumber = UnityVersionHandler.GetCurrentSceneNumber ();
					KickStarter.levelStorage.ClearCurrentLevelData ();
					KickStarter.sceneChanger.ChangeScene (new SceneInfo ("", sceneNumber), false, true);
				}
			}

			return 0f;
		}
		
		
		public override ActionEnd End (List<Action> actions)
		{
			return GenerateStopActionEnd ();
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI ()
		{
			endGameType = (AC_EndGameType) EditorGUILayout.EnumPopup ("Command:", endGameType);

			if (endGameType == AC_EndGameType.RestartGame)
			{
				chooseSceneBy = (ChooseSceneBy) EditorGUILayout.EnumPopup ("Choose scene by:", chooseSceneBy);
				if (chooseSceneBy == ChooseSceneBy.Name)
				{
					sceneName = EditorGUILayout.TextField ("Scene to restart to:", sceneName);
				}
				else
				{
					sceneNumber = EditorGUILayout.IntField ("Scene to restart to:", sceneNumber);
				}

				resetMenus = EditorGUILayout.Toggle ("Reset Menus too?", resetMenus);
			}
		}
		

		public override string SetLabel ()
		{
			return endGameType.ToString ();
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Engine: End game' Action, set to quit the game</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionEndGame CreateNew_QuitGame ()
		{
			ActionEndGame newAction = (ActionEndGame) CreateInstance <ActionEndGame>();
			newAction.endGameType = AC_EndGameType.QuitGame;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: End game' Action, set to reset the current scene</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionEndGame CreateNew_ResetScene ()
		{
			ActionEndGame newAction = (ActionEndGame) CreateInstance <ActionEndGame>();
			newAction.endGameType = AC_EndGameType.ResetScene;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: End game' Action, set to load the autosave</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionEndGame CreateNew_LoadAutosave ()
		{
			ActionEndGame newAction = (ActionEndGame) CreateInstance <ActionEndGame>();
			newAction.endGameType = AC_EndGameType.LoadAutosave;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: End game' Action, set to restart the game</summary>
		 * <param name = "newSceneBuildIndex">The build index number of the scene to load</param>
		 * <param name = "resetMenus">If True, then the state of all menus (e.g. visibility) will be reset</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionEndGame CreateNew_RestartGame (int newSceneBuildIndex, bool resetMenus = true)
		{
			ActionEndGame newAction = (ActionEndGame) CreateInstance <ActionEndGame>();
			newAction.endGameType = AC_EndGameType.RestartGame;
			newAction.chooseSceneBy = ChooseSceneBy.Number;
			newAction.resetMenus = resetMenus;
			return newAction;
		}
		
	}

}