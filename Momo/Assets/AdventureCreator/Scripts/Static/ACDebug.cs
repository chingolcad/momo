using UnityEngine;

namespace AC
{

	public static class ACDebug
	{

		private static string hr = "\n\n -> AC debug logger";


		public static void Log (object message, UnityEngine.Object context = null)
		{
			if (CanDisplay (true))
			{
				Debug.Log (message + hr, context);
			}
		}


		public static void LogWarning (object message, UnityEngine.Object context = null)
		{
			if (CanDisplay ())
			{
				Debug.LogWarning (message + hr, context);
			}
		}


		public static void LogError (object message, UnityEngine.Object context = null)
		{
			if (CanDisplay ())
			{
				Debug.LogError (message + hr, context);
			}
		}


		public static void Log (object message, ActionList actionList, AC.Action action, UnityEngine.Object context = null)
		{
			if (CanDisplay (true))
			{
				if (context == null) context = actionList;
				Debug.Log (message + GetActionListSuffix (actionList, action) + hr, context);
			}
		}


		public static void LogWarning (object message, ActionList actionList, AC.Action action, UnityEngine.Object context = null)
		{
			if (CanDisplay ())
			{
				if (context == null) context = actionList;
				Debug.LogWarning (message + GetActionListSuffix (actionList, action) + hr, context);
			}
		}


		public static void LogError (object message, ActionList actionList, AC.Action action, UnityEngine.Object context = null)
		{
			if (CanDisplay ())
			{
				if (context == null) context = actionList;
				Debug.LogError (message + GetActionListSuffix (actionList, action) + hr, context);
			}
		}


		private static string GetActionListSuffix (ActionList actionList, AC.Action action)
		{
			if (actionList != null && actionList.actions.Contains (action))
			{
				return ("\n(From Action #" + actionList.actions.IndexOf (action) + " in ActionList '" + actionList.name + "')");
			}
			return string.Empty;
		}


		private static bool CanDisplay (bool isInfo = false)
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				return true;
			}
			#endif
			if (KickStarter.settingsManager != null)
			{
				switch (KickStarter.settingsManager.showDebugLogs)
				{
				case ShowDebugLogs.Always :
					return true;

				case ShowDebugLogs.Never :
					return false;

				case ShowDebugLogs.OnlyWarningsOrErrors :
					if (!isInfo)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			return true;
		}

	}

}