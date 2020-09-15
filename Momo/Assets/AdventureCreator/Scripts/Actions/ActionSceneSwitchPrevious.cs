/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionScene.cs"
 * 
 *	This action loads a new scene.
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
	public class ActionSceneSwitchPrevious : Action
	{
		
		public bool assignScreenOverlay;
		public bool onlyPreload = false;

		public bool relativePosition = false;
		public Marker relativeMarker;
		protected Marker runtimeRelativeMarker;
		public int relativeMarkerID;
		public int relativeMarkerParameterID = -1;


		public ActionSceneSwitchPrevious ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Scene;
			title = "Switch previous";
			description = "Moves the Player to the previously-loaded scene. The scene must be listed in Unity's Build Settings. By default, the screen will cut to black during the transition, but the last frame of the current scene can instead be overlayed. This allows for cinematic effects: if the next scene fades in, it will cause a crossfade effect; if the next scene doesn't fade, it will cause a straight cut.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeRelativeMarker = AssignFile <Marker> (parameters, relativeMarkerParameterID, relativeMarkerID, relativeMarker);
		}
		
		
		public override float Run ()
		{
			if (!assignScreenOverlay || (!relativePosition && onlyPreload))
			{
				ChangeScene ();
				return 0f;
			}

			if (!isRunning)
			{
				if (KickStarter.sceneChanger.GetPreviousSceneInfo () == null || KickStarter.sceneChanger.GetPreviousSceneInfo ().IsNull)
				{
					LogWarning ("Cannot load previous scene as there is no data stored - is this the first scene in the game?");
					return 0f;
				}

				isRunning = true;
				KickStarter.mainCamera._ExitSceneWithOverlay ();
				return defaultPauseTime;
			}
			else
			{
				ChangeScene ();
				isRunning = false;
				return 0f;
			}
		}


		public override void Skip ()
		{
			ChangeScene ();
		}


		protected void ChangeScene ()
		{
			SceneInfo sceneInfo = KickStarter.sceneChanger.GetPreviousSceneInfo ();
			if (sceneInfo == null || sceneInfo.IsNull)
			{
				LogWarning ("Cannot load previous scene as there is no data stored - is this the first scene in the game?");
				return;
			}

			if (!onlyPreload && relativePosition && runtimeRelativeMarker != null)
			{
				KickStarter.sceneChanger.SetRelativePosition (runtimeRelativeMarker.transform);
			}

			if (onlyPreload && !relativePosition)
			{
				if (AdvGame.GetReferences ().settingsManager.useAsyncLoading)
				{
					KickStarter.sceneChanger.PreloadScene (sceneInfo);
				}
				else
				{
					LogWarning ("To pre-load scenes, 'Load scenes asynchronously?' must be enabled in the Settings Manager.");
				}
			}
			else
			{
				KickStarter.sceneChanger.ChangeScene (sceneInfo, true);
			}
		}


		public override ActionEnd End (List<Action> actions)
		{
			if (onlyPreload && !relativePosition)
			{
				return base.End (actions);
			}
			return GenerateStopActionEnd ();
		}
		

		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			onlyPreload = EditorGUILayout.ToggleLeft ("Don't change scene, just preload data?", onlyPreload);

			if (!onlyPreload)
			{
				relativePosition = EditorGUILayout.ToggleLeft ("Position Player relative to Marker?", relativePosition);
				if (relativePosition)
				{
					relativeMarkerParameterID = Action.ChooseParameterGUI ("Relative Marker:", parameters, relativeMarkerParameterID, ParameterType.GameObject);
					if (relativeMarkerParameterID >= 0)
					{
						relativeMarkerID = 0;
						relativeMarker = null;
					}
					else
					{
						relativeMarker = (Marker) EditorGUILayout.ObjectField ("Relative Marker:", relativeMarker, typeof(Marker), true);
						
						relativeMarkerID = FieldToID (relativeMarker, relativeMarkerID);
						relativeMarker = IDToField (relativeMarker, relativeMarkerID, false);
					}
				}
			}

			if (onlyPreload && !relativePosition)
			{
				if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.useAsyncLoading)
				{}
				else
				{
					EditorGUILayout.HelpBox ("To pre-load scenes, 'Load scenes asynchronously?' must be enabled in the Settings Manager.", MessageType.Warning);
				}

				numSockets = 1;
				AfterRunningOption ();
			}
			else
			{
				numSockets = 0;
				assignScreenOverlay = EditorGUILayout.ToggleLeft ("Overlay current screen during switch?", assignScreenOverlay);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (relativeMarker, relativeMarkerID, relativeMarkerParameterID);
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (relativePosition && relativeMarkerParameterID < 0)
			{
				if (relativeMarker != null && relativeMarker.gameObject == gameObject) return true;
				if (relativeMarkerID == id) return true;
			}
			return false;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Scene: Switch previous' Action</summary>
		 * <param name = "overlayCurrentScreen">If True, the original scene will be displayed during the switch to mask the transition</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneSwitchPrevious CreateNew (bool overlayCurrentScreen)
		{
			ActionSceneSwitchPrevious newAction = (ActionSceneSwitchPrevious) CreateInstance <ActionSceneSwitchPrevious>();
			newAction.assignScreenOverlay = overlayCurrentScreen;
			return newAction;
		}
		
	}

}