/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionPlatformCheck.cs"
 * 
 *	This action checks which device the game is currently running on,
 *	for platform-dependent gameplay.
 * 
 */

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionPlatformCheck : ActionCheck
	{
		
		public PlatformType platformType = PlatformType.Desktop;


		public ActionPlatformCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Engine;
			title = "Check platform";
			description = "Queries either the plaform the game is running on.";
		}

		
		public override bool CheckCondition ()
		{
			switch (platformType)
			{

			case PlatformType.Desktop:
				#if UNITY_STANDALONE
				return true;
				#else
				return false;
				#endif

			case PlatformType.TouchScreen:
				#if UNITY_ANDROID || UNITY_IOS
				return true;
				#else
				return false;
				#endif

			case PlatformType.WebPlayer:
				#if UNITY_WEBPLAYER
				return true;
				#else
				return false;
				#endif

			case PlatformType.Windows:
				#if UNITY_STANDALONE_WIN
				return true;
				#else
				return false;
				#endif

			case PlatformType.Mac:
				#if UNITY_STANDALONE_OSX
				return true;
				#else
				return false;
				#endif

			case PlatformType.Linux:
				#if UNITY_STANDALONE_LINUX
				return true;
				#else
				return false;
				#endif

			case PlatformType.iOS:
				#if UNITY_IOS
				return true;
				#else
				return false;
				#endif

			case PlatformType.Android:
				#if UNITY_ANDROID
				return true;
				#else
				return false;
				#endif
			}

			return false;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI ()
		{
			platformType = (PlatformType) EditorGUILayout.EnumPopup ("Platform is:", platformType);
		}


		public override string SetLabel ()
		{
			return platformType.ToString ();
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Engine: Check platform' Action</summary>
		 * <param name = "platformToCheck">The platform to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlatformCheck CreateNew (PlatformType platformToCheck)
		{
			ActionPlatformCheck newAction = (ActionPlatformCheck) CreateInstance <ActionPlatformCheck>();
			newAction.platformType = platformToCheck;
			return newAction;
		}

	}

}