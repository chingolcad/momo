/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"PlayerStart.cs"
 * 
 *	This script defines a possible starting position for the
 *	player when the scene loads, based on what the previous
 *	scene was.  If no appropriate PlayerStart is found, the
 *	one define in SceneSettings is used as the default.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Defines a possible starting position for the Player when the scene loads, based on what the previous scene was
	 * If no appropriate PlayerStart is found, then the defaultPlayerStart defined in SceneSettings will be used instead.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_start.html")]
	public class PlayerStart : Marker
	{

		#region Variables

		/** The way in which the previous scene is identified by (Number, Name) */
		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		/** The number of the previous scene to check for */
		public int previousScene;
		/** The name of the previous scene to check for */
		public string previousSceneName;
		/** If True, then the MainCamera will fade in when the Player starts the scene from this point */
		public bool fadeInOnStart;
		/** The speed of the fade, if the MainCamera fades in when the Player starts the scene from this point */
		public float fadeSpeed = 0.5f;
		/** The _Camera that should be made active when the Player starts the scene from this point */
		public _Camera cameraOnStart;
		
		protected GameObject playerOb;

		#endregion


		#region PublicFunctions

		/**
		 * Places the Player at the GameObject's position, and activates the assigned cameraOnStart.
		 */
		public void SetPlayerStart ()
		{
			if (KickStarter.mainCamera)
			{
				if (fadeInOnStart)
				{
					KickStarter.mainCamera.FadeIn (fadeSpeed);
				}
				
				if (KickStarter.settingsManager)
				{
					if (KickStarter.player)
					{
						KickStarter.player.SetLookDirection (this.transform.forward, true);
						KickStarter.player.Teleport (KickStarter.sceneChanger.GetStartPosition (this.transform.position));

						if (SceneSettings.ActInScreenSpace ())
						{
							KickStarter.player.transform.position = AdvGame.GetScreenNavMesh (KickStarter.player.transform.position);
						}
					}
				
					if (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
					{
						KickStarter.mainCamera.SetFirstPerson ();
					}
					else if (cameraOnStart != null)
					{
						SetCameraOnStart ();
					}
					else
					{
						if (!KickStarter.settingsManager.IsInFirstPerson ())
						{
							ACDebug.LogWarning ("PlayerStart '" + this.name + "' has no Camera On Start", this);

							if (KickStarter.sceneSettings != null &&
								this != KickStarter.sceneSettings.defaultPlayerStart)
							{
								KickStarter.sceneSettings.defaultPlayerStart.SetCameraOnStart ();
							}
						}
					}

					KickStarter.eventManager.Call_OnOccupyPlayerStart (KickStarter.player, this);
				}
			}
		}


		/**
		 * Makes the assigned cameraOnStart the active _Camera.
		 */
		public void SetCameraOnStart ()
		{
			if (cameraOnStart != null)
			{
				KickStarter.mainCamera.SetGameCamera (cameraOnStart);
				KickStarter.mainCamera.lastNavCamera = cameraOnStart;
				cameraOnStart.MoveCameraInstant ();
				KickStarter.mainCamera.SetGameCamera (cameraOnStart);
			}
		}

		#endregion
		
	}

}