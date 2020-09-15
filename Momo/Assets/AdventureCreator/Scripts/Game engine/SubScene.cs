/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"SubScene.cs"
 * 
 *	This component keeps track of data regarding any scene added to the active scene during gameplay.
 *	It is generated automatically by the sub-scene's MultiSceneChecker, and should not be added manually.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 *	This component keeps track of data regarding any scene added to the active scene during gameplay.
	 *	It is generated automatically by the sub-scene's MultiSceneChecker, and should not be added manually.
	 */
	public class SubScene : MonoBehaviour
	{

		#region Variables

		protected SceneInfo sceneInfo;

		protected LocalVariables localVariables;
		protected SceneSettings sceneSettings;

		protected KickStarter kickStarter;
		protected MainCamera mainCamera;

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Syncs the component with the correct scene.</summary>
		 * <param name = "_multiSceneChecker">The MultiSceneChecker component in the scene for which this component is to sync with.</param>
		 */
		public void Initialise (MultiSceneChecker _multiSceneChecker)
		{
			UnityEngine.SceneManagement.Scene scene = _multiSceneChecker.gameObject.scene;
			gameObject.name = "SubScene " + scene.buildIndex;

			kickStarter = _multiSceneChecker.GetComponent <KickStarter>();

			sceneInfo = new SceneInfo (scene.name, scene.buildIndex);

			localVariables = _multiSceneChecker.GetComponent <LocalVariables>();
			sceneSettings = _multiSceneChecker.GetComponent <SceneSettings>();

			UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene (gameObject, scene);

			kickStarter = UnityVersionHandler.GetOwnSceneInstance <KickStarter> (gameObject);
			if (kickStarter != null)
			{
				kickStarter.gameObject.SetActive (false);
			}

			mainCamera = UnityVersionHandler.GetOwnSceneInstance <MainCamera> (gameObject);
			if (mainCamera != null)
			{
				mainCamera.gameObject.SetActive (false);
			}

			Player ownPlayer = UnityVersionHandler.GetOwnSceneInstance <Player> (gameObject);
			if (ownPlayer != null)
			{
				ownPlayer.gameObject.SetActive (false);
			}

			if (sceneSettings.OverridesCameraPerspective ())
			{
				ACDebug.LogError ("The added scene (" + scene.name + ", " + scene.buildIndex + ") overrides the default camera perspective - this feature should not be used in conjunction with multiple-open scenes.", gameObject);
			}

			KickStarter.sceneChanger.RegisterSubScene (this);
		}


		/**
		 * Prepares the sub-scene to become the new active scene, due to the active scene being removed.  The gameobject will be destroyed afterwards.
		 */
		public void MakeMain ()
		{
			if (mainCamera)
			{
				mainCamera.gameObject.SetActive (true);
				mainCamera.OnAwake (false);
				mainCamera.OnStart ();
			}
			if (kickStarter)
			{
				kickStarter.gameObject.SetActive (true);
			}

			KickStarter.SetGameEngine (gameObject);
			KickStarter.mainCamera = mainCamera;

			UnityEngine.SceneManagement.SceneManager.SetActiveScene (gameObject.scene);
			Destroy (gameObject);
		}

		#endregion


		#region GetSet

		/**
		 * Gets the SceneInfo for the scene that this component represents.
		 */
		public SceneInfo SceneInfo
		{
			get
			{
				return sceneInfo;
			}
		}


		/**
		 * Gets the LocalVariables for the scene that this component represents.
		 */
		public LocalVariables LocalVariables
		{
			get
			{
				return localVariables;
			}
		}


		/**
		 * Gets the SceneSettings for the scene that this component represents.
		 */
		public SceneSettings SceneSettings
		{
			get
			{
				return sceneSettings;
			}
		}

		#endregion

	}

}