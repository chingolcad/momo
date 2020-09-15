using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	public class ToolbarLinksDemo : EditorWindow
	{

		[MenuItem ("Adventure Creator/Getting started/Load 3D Demo", false, 6)]
		static void Demo3D ()
		{
			ManagerPackage package = AssetDatabase.LoadAssetAtPath ("Assets/AdventureCreator/Demo/ManagerPackage.asset", typeof (ManagerPackage)) as ManagerPackage;
			if (package != null)
			{
				package.AssignManagers ();
				AdventureCreator.RefreshActions ();

				if (!ACInstaller.IsInstalled ())
				{
					ACInstaller.DoInstall ();
				}

				if (UnityVersionHandler.GetCurrentSceneName () != "Basement")
				{
					#if UNITY_5_3 || UNITY_5_4 || UNITY_5_3_OR_NEWER
					bool canProceed = EditorUtility.DisplayDialog ("Open demo scene", "Would you like to open the 3D Demo scene, Basement, now?", "Yes", "No");
					if (canProceed)
					{
						if (UnityVersionHandler.SaveSceneIfUserWants ())
						{
							UnityEditor.SceneManagement.EditorSceneManager.OpenScene ("Assets/AdventureCreator/Demo/Scenes/Basement.unity");
						}
					}
					#else
					ACDebug.Log ("3D Demo managers loaded - you can now run the 3D Demo scene in 'Assets/AdventureCreator/Demo/Scenes/Basement.unity'");
					#endif
				}

				AdventureCreator.Init ();
			}
		}

	}

}