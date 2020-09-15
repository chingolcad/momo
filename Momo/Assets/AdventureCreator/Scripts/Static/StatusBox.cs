/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"StatusBox.cs"
 * 
 *	This script handles the display of the 'AC Status' box, which is a debug window available from the Settings Manager.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	public static class StatusBox
	{

		private static Rect debugWindowRect = new Rect (0, 0, 260, 500);
		private static GUISkin sceneManagerSkin = null;


		/**
		 * <summary>Draws the debug window in the top-left corner of the Game window</summar>
		 */
		public static void DrawDebugWindow ()
		{
			if (KickStarter.settingsManager.showActiveActionLists != DebugWindowDisplays.Never)
			{
				#if !UNITY_EDITOR
				if (KickStarter.settingsManager.showActiveActionLists == DebugWindowDisplays.EditorOnly)
				{
					return;
				}
				#endif
				debugWindowRect.height = 21f;
				debugWindowRect = GUILayout.Window (10, debugWindowRect, StatusWindow, "AC status", GUILayout.Width (260));
			}
		}


		private static void StatusWindow (int windowID)
		{
			if (sceneManagerSkin == null)
			{
				sceneManagerSkin = (GUISkin) Resources.Load ("SceneManagerSkin");
			}
			GUI.skin = sceneManagerSkin;

			GUILayout.Label ("Current game state: " + KickStarter.stateHandler.gameState.ToString ());

			if (KickStarter.settingsManager.useProfiles)
			{
				GUILayout.Label ("Current profile ID: " + Options.GetActiveProfileID ());
			}

			if (KickStarter.player != null)
			{
				if (GUILayout.Button ("Current player: " + KickStarter.player.gameObject.name))
				{
					#if UNITY_EDITOR
					UnityEditor.EditorGUIUtility.PingObject (KickStarter.player.gameObject);
					#endif
				}
			}

			if (KickStarter.mainCamera != null)
			{
				KickStarter.mainCamera.DrawStatus ();
			}

			if (KickStarter.stateHandler.gameState == GameState.DialogOptions && KickStarter.playerInput.IsInConversation ())
			{
				if (GUILayout.Button ("Conversation: " + KickStarter.playerInput.activeConversation.gameObject.name))
				{
					#if UNITY_EDITOR
					UnityEditor.EditorGUIUtility.PingObject (KickStarter.playerInput.activeConversation.gameObject);
					#endif
				}
			}
			
			GUILayout.Space (4f);

			bool anyRunning = false;
			foreach (ActiveList activeList in KickStarter.actionListManager.activeLists)
			{
				if (activeList.IsRunning ())
				{
					anyRunning = true;
					break;
				}
			}

			if (anyRunning)
			{
				GUILayout.Label ("ActionLists running:");

				for (int i=0; i<KickStarter.actionListManager.activeLists.Count; i++)
				{
					KickStarter.actionListManager.activeLists[i].ShowGUI ();
				}
			}

			anyRunning = false;
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.IsRunning ())
				{
					anyRunning = true;
					break;
				}
			}

			if (anyRunning)
			{
				GUILayout.Label ("ActionList Assets running:");
				
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
				{
					activeList.ShowGUI ();
				}
			}

			if (KickStarter.actionListManager.IsGameplayBlocked ())
			{
				GUILayout.Space (4f);
				GUILayout.Label ("Gameplay is blocked");
			}

			GUI.DragWindow ();
		}

	}

}