/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"NavigationManager.cs"
 * 
 *	This script instantiates the chosen
 *	NavigationEngine subclass at runtime.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This component instantiates the scene's chosen NavigationEngine ScriptableObject when the game begins.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_navigation_manager.html")]
	public class NavigationManager : MonoBehaviour
	{

		#region Variables

		/** The NavigationEngine ScriptableObject that performs the scene's pathfinding algorithms. */
		[HideInInspector] public NavigationEngine navigationEngine = null;

		#endregion


		#region UnityStandards

		/**
		 * Initialises the Navigation Engine.  This is public so we have control over when it is called in relation to other Awake functions.
		 */
		public void OnAwake ()
		{
			navigationEngine = null;
			ResetEngine ();
		}

		#endregion


		#region PublicFunctions

		/**
		 * Sets up the scene's chosen NavigationEngine ScriptableObject if it is not already present.
		 */
		public void ResetEngine ()
		{
			string className = string.Empty;
			if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.Custom)
			{
				className = KickStarter.sceneSettings.customNavigationClass;
			}
			else
			{
				className = "NavigationEngine_" + KickStarter.sceneSettings.navigationMethod.ToString ();
			}

			if (string.IsNullOrEmpty (className) && Application.isPlaying)
			{
				ACDebug.LogWarning ("Could not initialise navigation - a custom script must be assigned if the Pathfinding method is set to Custom.");
			}
			else if (navigationEngine == null || !navigationEngine.ToString ().Contains (className))
			{
				navigationEngine = (NavigationEngine) ScriptableObject.CreateInstance (className);
				if (navigationEngine != null)
				{
					navigationEngine.OnReset (KickStarter.sceneSettings.navMesh);
				}
			}
		}


		/**
		 * <summary>Checks if the Navigation Engine is written to work with Unity 2D or not.</summary>
		 * <returns>True if the Navigation Engine is written to work with Unity 2D.</returns>
		 */
		public bool Is2D ()
		{
			if (navigationEngine != null)
			{
				return navigationEngine.is2D;
			}
			return false;
		}

		#endregion

	}

}