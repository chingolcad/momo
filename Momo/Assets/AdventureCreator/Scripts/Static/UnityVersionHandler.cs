/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"UnityVersionHandler.cs"
 * 
 *	This is a static class that contains commonly-used functions that vary depending on which version of Unity is being used.
 * 
 */

#if UNITY_2018_3_OR_NEWER
#define NEW_PREFABS
#endif

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


namespace AC
{

	/**
	 * This is a class that contains commonly-used functions that vary depending on which version of Unity is being used.
	 */
	public class UnityVersionHandler
	{

		/**
		 * <summary>Performs a Physics2D.Raycast on Triggers in the scene.</summary>
		 * <param name = "origin">The ray's origin</param>
		 * <param name = "direction">The ray's direction</param>
		 * <param name = "layerMask">The LayerMask to act upon</param>
		 * <returns>The result of the Physics2D.Raycast</returns>
		 */
		public static RaycastHit2D Perform2DRaycast (Vector2 origin, Vector2 direction, float length, LayerMask layerMask)
		{
			RaycastHit2D[] hits = new RaycastHit2D [1];
			ContactFilter2D filter = new ContactFilter2D();
			filter.useTriggers = true;
			filter.SetLayerMask (layerMask);
			filter.ClearDepth ();
			Physics2D.Raycast (origin, direction, filter, hits, length);
			return hits[0];
		}


		/**
		 * <summary>Performs a Physics2D.Raycast on Triggers in the scene.</summary>
		 * <param name = "origin">The ray's origin</param>
		 * <param name = "direction">The ray's direction</param>
		 * <param name = "length">The ray's length</param>
		 * <returns>The result of the Physics2D.Raycast</returns>
		 */
		public static RaycastHit2D Perform2DRaycast (Vector2 origin, Vector2 direction, float length)
		{
			RaycastHit2D[] hits = new RaycastHit2D [1];
			ContactFilter2D filter = new ContactFilter2D();
			filter.useTriggers = true;
			filter.ClearDepth ();
			Physics2D.Raycast (origin, direction, filter, hits, length);
			return hits[0];
		}


		/**
		 * The 'lock' state of the cursor.
		 */
		public static bool CursorLock
		{
			get
			{
				return (Cursor.lockState == CursorLockMode.Locked) ? true : false;
			}
			set
			{
				if (value)
				{
					if (KickStarter.cursorManager.cursorRendering == CursorRendering.Software && !KickStarter.cursorManager.lockSystemCursor)
					{
						return;
					}

					Cursor.lockState = CursorLockMode.Locked;
				}
				else
				{
					#if UNITY_STANDALONE_WIN
					if (KickStarter.cursorManager.confineSystemCursor)
					{
						Cursor.lockState = CursorLockMode.Confined;
						return;
					}
					#endif
					Cursor.lockState = CursorLockMode.None;
				}
			}
		}


		/**
		 * <summary>Gets the index number of the active scene, as listed in the Build Settings.</summary>
		 * <returns>The index number of the active scene, as listed in the Build Settings.</returns>
		 */
		public static int GetCurrentSceneNumber ()
		{
			return UnityEngine.SceneManagement.SceneManager.GetActiveScene ().buildIndex;
		}


		/**
		 * <summary>Gets a SceneInfo class for the scene that a given GameObject is in.</summary>
		 * <param name = "_gameObject">The GameObject in the scene</param>
		 * <returns>A SceneInfo class for the scene that a given GameObject is in.</returns>
		 */
		public static SceneInfo GetSceneInfoFromGameObject (GameObject _gameObject)
		{
			return new SceneInfo (_gameObject.scene.name, _gameObject.scene.buildIndex);
		}


		/**
		 * <summary>Gets the LocalVariables component that is in the same scene as a given GameObject.</summary>
		 * <param name = "_gameObject">The GameObject in the scene</param>
		 * <returns>The LocalVariables component that is in the same scene as the given GameObject</returns>
		 */
		public static LocalVariables GetLocalVariablesOfGameObject (GameObject _gameObject)
		{
			if (UnityVersionHandler.ObjectIsInActiveScene (_gameObject))
			{
				return KickStarter.localVariables;
			}

			UnityEngine.SceneManagement.Scene scene = _gameObject.scene;
			if (Application.isPlaying)
			{
				foreach (SubScene subScene in KickStarter.sceneChanger.GetSubScenes ())
				{
					if (subScene.gameObject.scene == scene)
					{
						return subScene.LocalVariables;
					}
				}
			}
			else
			{
				foreach (LocalVariables localVariables in GameObject.FindObjectsOfType <LocalVariables>())
				{
					if (localVariables.gameObject.scene == scene)
					{
						return localVariables;
					}
				}
			}

			return null;
		}


		/**
		 * <summary>Gets the SceneSettings component that is in the same scene as a given GameObject.</summary>
		 * <param name = "_gameObject">The GameObject in the scene</param>
		 * <returns>The SceneSettings component that is in the same scene as the given GameObject</returns>
		 */
		public static SceneSettings GetSceneSettingsOfGameObject (GameObject _gameObject)
		{
			if (UnityVersionHandler.ObjectIsInActiveScene (_gameObject))
			{
				return KickStarter.sceneSettings;
			}

			UnityEngine.SceneManagement.Scene scene = _gameObject.scene;
			if (Application.isPlaying)
			{
				foreach (SubScene subScene in KickStarter.sceneChanger.GetSubScenes ())
				{
					if (subScene.gameObject.scene == scene)
					{
						return subScene.SceneSettings;
					}
				}
			}
			else
			{
				foreach (SceneSettings sceneSettings in GameObject.FindObjectsOfType <SceneSettings>())
				{
					if (sceneSettings.gameObject.scene == scene)
					{
						return sceneSettings;
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the name of the active scene.</summary>
		 * <returns>The name of the active scene. If this is called in the Editor, the full filepath is also returned.</returns>
		 */
		public static string GetCurrentSceneName ()
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene ().name;
			}
			#endif
			return UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name;
		}


		public static SceneInfo GetCurrentSceneInfo ()
		{
			return new SceneInfo (GetCurrentSceneName (), GetCurrentSceneNumber ());
		}


		/**
		 * <summary>Loads a scene by name.</summary>
		 * <param name = "sceneName">The name of the scene to load</param>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 * <param name = "loadAdditively">If True, the scene will be loaded on top of the active scene (Unity 5.3 only)</param>
		 */
		public static void OpenScene (string sceneName, bool forceReload = false, bool loadAdditively = false)
		{
			if (string.IsNullOrEmpty (sceneName)) return;

			try
			{
				if (forceReload || GetCurrentSceneName () != sceneName)
				{
					#if UNITY_EDITOR
					if (!Application.isPlaying)
					{
						UnityEditor.SceneManagement.EditorSceneManager.OpenScene (sceneName);
						return;
					}
					#endif

					UnityEngine.SceneManagement.LoadSceneMode loadSceneMode = (loadAdditively) ? UnityEngine.SceneManagement.LoadSceneMode.Additive : UnityEngine.SceneManagement.LoadSceneMode.Single;
					UnityEngine.SceneManagement.SceneManager.LoadScene (sceneName, loadSceneMode);
				}
			}
			catch (System.Exception e)
 			{
				Debug.LogWarning ("Error when opening scene " + sceneName + ": " + e);
 			}
		}


		/**
		 * <summary>Loads a scene by index number, as listed in the Build Settings.</summary>
		 * <param name = "sceneNumber">The index number of the scene to load, as listed in the Build Settings</param>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 * <param name = "loadAdditively">If True, the scene will be loaded on top of the active scene (Unity 5.3 only)</param>
		 */
		public static void OpenScene (int sceneNumber, bool forceReload = false, bool loadAdditively = false)
		{
			if (sceneNumber < 0) return;

			if (KickStarter.settingsManager.reloadSceneWhenLoading)
			{
				forceReload = true;
			}

			try
			{
				if (forceReload || GetCurrentSceneNumber () != sceneNumber)
				{
					UnityEngine.SceneManagement.LoadSceneMode loadSceneMode = (loadAdditively) ? UnityEngine.SceneManagement.LoadSceneMode.Additive : UnityEngine.SceneManagement.LoadSceneMode.Single;
					UnityEngine.SceneManagement.SceneManager.LoadScene (sceneNumber, loadSceneMode);
				}
			}
			catch (System.Exception e)
 			{
				Debug.LogWarning ("Error when opening scene " + sceneNumber + ": " + e);
 			}
		}


		/**
		 * <summary>Closes a scene by name.</summary>
		 * <param name = "sceneName">The name of the scene to load</param>
		 * <returns>True if the close was successful</returns>
		 */
		public static bool CloseScene (string sceneName)
		{
			if (string.IsNullOrEmpty (sceneName)) return false;

			if (GetCurrentSceneName () != sceneName)
			{
				UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync (sceneName);
				return true;
			}
			return false;
		}


		/**
		 * <summary>Closes a scene by index number, as listed in the Build Settings.</summary>
		 * <param name = "sceneNumber">The index number of the scene to load, as listed in the Build Settings</param>
		 * <returns>True if the close was successful</returns>
		 */
		public static bool CloseScene (int sceneNumber)
		{
			if (sceneNumber < 0) return false;

			if (GetCurrentSceneNumber () != sceneNumber)
			{
				UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync (sceneNumber);
				return true;
			}
			return false;
		}


		/**
		 * <summary>Loads the scene asynchronously.</summary>
		 * <param name = "sceneNumber">The index number of the scene to load.</param>
		 * <param name = "sceneName">The name of the scene to load. If this is blank, sceneNumber will be used instead.</param>
		 * <returns>The generated AsyncOperation class</returns>
		 */
		public static AsyncOperation LoadLevelAsync (int sceneNumber, string sceneName = "")
		{
			if (!string.IsNullOrEmpty (sceneName))
			{
				return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync (sceneName);
			}

			return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync (sceneNumber);
		}


		/**
		 * <summary>Checks if Json serialization is supported by the current version of Unity.</summary>
		 * <returns>True if Json serialization is supported by the current version of Unity.</returns>
		 */
		public static bool CanUseJson ()
		{
			return true;
		}


		/**
		 * <summary>Places a supplied GameObject in a "folder" scene object, as generated by the Scene Manager.</summary>
		 * <param name = "ob">The GameObject to move into a folder</param>
		 * <param name = "folderName">The name of the folder scene object</param>
		 * <returns>True if a suitable folder object was found, and ob was successfully moved.</returns>
		 */
		public static bool PutInFolder (GameObject ob, string folderName)
		{
			if (ob == null || string.IsNullOrEmpty (folderName)) return false;

			UnityEngine.Object[] folders = Object.FindObjectsOfType (typeof (GameObject));
			foreach (GameObject folder in folders)
			{
				if (folder.name == folderName && folder.transform.position == Vector3.zero && folderName.Contains ("_") && folder.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene ())
				{
					ob.transform.parent = folder.transform;
					return true;
				}
			}
					
			return false;
		}


		#if UNITY_EDITOR

		public static void NewScene ()
		{
			EditorSceneManager.NewScene (NewSceneSetup.DefaultGameObjects);
		}


		public static void SaveScene ()
		{
			UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene ();
			EditorSceneManager.SaveScene (currentScene);
		}


		public static bool SaveSceneIfUserWants ()
		{
			return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
		}


		/**
		 * <summary>Gets the name of the active scene, if multiple scenes are being edited.</summary>
		 * <returns>The name of the active scene, if multiple scenes are being edited. Returns nothing otherwise.</returns>
		 */
		public static string GetActiveSceneName ()
		{
			if (UnityEngine.SceneManagement.SceneManager.sceneCount <= 1)
			{
				return string.Empty;
			}
			UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene ();
			if (!string.IsNullOrEmpty (activeScene.name))
			{
				return activeScene.name;
			}
			return "New scene";
		}


		/**
		 * <summary>Checks if a suppplied GameObject is present within the active scene.</summary>
		 * <param name = "gameObjectName">The name of the GameObject to check for</param>
		 * <param name = "persistentIsValid">If True, then objects marked as "DontDestroyOnLoad" will also be valid</param>
		 * <returns>True if the GameObject is present within the active scene.</returns>
		 */
		public static bool ObjectIsInActiveScene (string gameObjectName, bool persistentIsValid = true)
		{
			if (string.IsNullOrEmpty (gameObjectName) || !GameObject.Find (gameObjectName))
			{
				return false;
			}

			UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene ();

			UnityEngine.Object[] allObjects = Object.FindObjectsOfType (typeof (GameObject));
			foreach (GameObject _object in allObjects)
			{
				if (_object.name == gameObjectName)
				{
					if (_object.scene == activeScene)
					{
						return true;
					}
					else if (persistentIsValid && GameObjectIsPersistent (_object))
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Adds a component to a GameObject, which can be a prefab or a scene-based object</summary>
		 * <param name = "gameObject">The GameObject to amend</param>
		 * <returns>The GameObject's component</returns>
		 */
		public static T AddComponentToGameObject <T> (GameObject gameObject) where T : Component
		{
			T existingComponent = gameObject.GetComponent <T>();

			if (existingComponent != null)
			{
				return existingComponent;
			}

			#if NEW_PREFABS
			if (IsPrefabFile (gameObject) && !IsPrefabEditing (gameObject))
			{
				string assetPath = AssetDatabase.GetAssetPath (gameObject);
				GameObject instancedObject = PrefabUtility.LoadPrefabContents (assetPath);
				instancedObject.AddComponent <T>();
				PrefabUtility.SaveAsPrefabAsset (instancedObject, assetPath);
				PrefabUtility.UnloadPrefabContents (instancedObject);
				return gameObject.GetComponent <T>();
			}
			#endif

			T newComponent = gameObject.AddComponent <T>();
			CustomSetDirty (gameObject, true);
			if (IsPrefabFile (gameObject))
			{
				AssetDatabase.SaveAssets ();
			}
			return newComponent;
		}


		/**
		 * <summary>Adds a ConstantID component to a GameObject, which can be a prefab or a scene-based object</summary>
		 * <param name = "gameObject">The GameObject to amend</param>
		 * <returns>The GameObject's component</returns>
		 */
		public static T AddConstantIDToGameObject <T> (GameObject gameObject, bool forcePrefab = false) where T : ConstantID
		{
			T existingComponent = gameObject.GetComponent <T>();

			if (existingComponent != null)
			{
				if (existingComponent.constantID == 0)
				{
					#if NEW_PREFABS
					if (IsPrefabFile (gameObject) && !IsPrefabEditing (gameObject))
					{
						string assetPath = AssetDatabase.GetAssetPath (gameObject);
						GameObject instancedObject = PrefabUtility.LoadPrefabContents (assetPath);
						instancedObject.GetComponent <ConstantID>().AssignInitialValue (true);
						PrefabUtility.SaveAsPrefabAsset (instancedObject, assetPath);
						PrefabUtility.UnloadPrefabContents (instancedObject);
					}
					else
					{
						existingComponent.AssignInitialValue (forcePrefab);
					}
					#else
					existingComponent.AssignInitialValue (forcePrefab);
					#endif
				}

				CustomSetDirty (gameObject, true);
				if (IsPrefabFile (gameObject))
				{
					AssetDatabase.SaveAssets ();
				}

				return existingComponent;
			}

			#if NEW_PREFABS
			if (UnityVersionHandler.IsPrefabFile (gameObject) && !IsPrefabEditing (gameObject))
			{
				string assetPath = AssetDatabase.GetAssetPath (gameObject);
				GameObject instancedObject = PrefabUtility.LoadPrefabContents (assetPath);
				existingComponent = instancedObject.AddComponent <T>();
				existingComponent.AssignInitialValue (true);

				foreach (ConstantID constantIDScript in instancedObject.GetComponents <ConstantID>())
				{
					if (!(constantIDScript is Remember) && !(constantIDScript is RememberTransform) && constantIDScript != existingComponent)
					{
						GameObject.DestroyImmediate (constantIDScript, true);
						ACDebug.Log ("Replaced " + gameObject.name + "'s 'ConstantID' component with '" + existingComponent.GetType ().ToString () + "'", gameObject);
					}
				}

				PrefabUtility.SaveAsPrefabAsset (instancedObject, assetPath);
				PrefabUtility.UnloadPrefabContents (instancedObject);

				CustomSetDirty (gameObject, true);
				AssetDatabase.SaveAssets ();

				return existingComponent;
			}
			#endif

			existingComponent = gameObject.AddComponent <T>();
			existingComponent.AssignInitialValue (forcePrefab);

			foreach (ConstantID constantIDScript in gameObject.GetComponents <ConstantID>())
			{
				if (!(constantIDScript is Remember) && !(constantIDScript is RememberTransform) && constantIDScript != existingComponent)
				{
					GameObject.DestroyImmediate (constantIDScript, true);
					ACDebug.Log ("Replaced " + gameObject.name + "'s 'ConstantID' component with '" + existingComponent.GetType ().ToString () + "'", gameObject);
				}
			}

			CustomSetDirty (gameObject, true);
			if (IsPrefabFile (gameObject))
			{
				AssetDatabase.SaveAssets ();
			}

			return existingComponent;
		}


		public static void AssignIDsToTranslatable (ITranslatable translatable, int[] lineIDs, bool isInScene, bool isMonoBehaviour)
		{
			bool isModified = false;

			for (int i=0; i<lineIDs.Length; i++)
			{
				if (translatable.GetTranslationID (i) != lineIDs[i])
				{
					translatable.SetTranslationID (i, lineIDs[i]);
					isModified = true;
				}
			}

			if (isModified && isMonoBehaviour)
			{
				EditorUtility.SetDirty (translatable as MonoBehaviour);
			}
		}


		/**
		 * <summary>Checks if a given object is part of an original prefab (as opposed to an instance of one).</summary>
		 * <param name = "_target">The object being checked</param>
		 * <returns>True if the object is part of an original prefab</returns>
		 */
		public static bool IsPrefabFile (Object _target)
		{
			#if NEW_PREFABS
			bool isPartOfAnyPrefab = PrefabUtility.IsPartOfAnyPrefab (_target);
			bool isPartOfPrefabInstance = PrefabUtility.IsPartOfPrefabInstance (_target);
			bool isPartOfPrefabAsset = PrefabUtility.IsPartOfPrefabAsset (_target);
			if (isPartOfAnyPrefab && !isPartOfPrefabInstance && isPartOfPrefabAsset)
			{
				return true;
			}

			if (IsPrefabEditing (_target))
			{
				return true;
			}
			return false;
			#else
			return PrefabUtility.GetPrefabType (_target) == PrefabType.Prefab;
			#endif
		}


		public static bool IsPrefabEditing (Object _target)
		{
			#if NEW_PREFABS
			UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage ();
			if (prefabStage != null && _target is GameObject)
			{
				return prefabStage.IsPartOfPrefabContents (_target as GameObject);
			}
			#endif
			return false;
		}


		/**
		 * <Summary>Marks an object as dirty so that changes made will be saved.
		 * In Unity 5.3 and above, the scene itself is marked as dirty to ensure it is properly changed.</summary>
		 * <param name = "_target">The object to mark as dirty</param>
		 * <param name = "force">If True, then the object will be marked as dirty regardless of whether or not GUI.changed is true. This should not be set if called every frame.</param>
		 */
		public static void CustomSetDirty (Object _target, bool force = false)
		{
			if (_target != null && (force || GUI.changed))
			{
				if (!Application.isPlaying && 
					(!IsPrefabFile (_target) || IsPrefabEditing (_target)))
				{
					if (_target is MonoBehaviour)
					{
						MonoBehaviour monoBehaviour = (MonoBehaviour) _target;
						UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (monoBehaviour.gameObject.scene);
					}
					else if (_target is GameObject)
					{
						UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty ((_target as GameObject).scene);
					}
					else
					{
						UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty ();
					}
				}

				EditorUtility.SetDirty (_target);
			}
		}


		public static string GetCurrentSceneFilepath ()
		{
			string sceneName = GetCurrentSceneName ();

			if (!string.IsNullOrEmpty (sceneName))
			{
				foreach (UnityEditor.EditorBuildSettingsScene S in UnityEditor.EditorBuildSettings.scenes)
				{
					if (S.enabled)
					{
						if (S.path.Contains (sceneName))
						{
							return S.path;
						}
					}
				}
			}
			return string.Empty;
		}


		public static bool ShouldAssignPrefabConstantID (GameObject gameObject)
		{
			#if NEW_PREFABS
			if (IsPrefabFile (gameObject))
			{
				return true;
			}
			#elif UNITY_2018_2
			if (PrefabUtility.GetCorrespondingObjectFromSource (gameObject) == null && PrefabUtility.GetPrefabObject (gameObject) != null)
			{
				return true;
			}
			#else
			if (PrefabUtility.GetPrefabParent (gameObject) == null && PrefabUtility.GetPrefabObject (gameObject) != null)
			{
				return true;
			}
			#endif
			return false;
		}

		#endif


		/**
		 * <summary>Checks if a suppplied GameObject is present within the active scene. The GameObject must be in the Hierarchy at runtime.</summary>
		 * <param name = "gameObject">The GameObject to check for</param>
		 * <param name = "persistentIsValid">If True, then objects marked as "DontDestroyOnLoad" will also be valid</param>
		 * <returns>True if the GameObject is present within the active scene</returns>
		 */
		public static bool ObjectIsInActiveScene (GameObject gameObject, bool persistentIsValid = true)
		{
			if (gameObject == null)
			{
				return false;
			}
			#if UNITY_EDITOR
			if (IsPrefabFile (gameObject))
			{
				return false;
			}
			#endif

			UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene ();
			if (gameObject.scene == activeScene)
			{
				return true;
			}

			if (persistentIsValid && GameObjectIsPersistent (gameObject))
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if a given GameObject is set to be persistent, i.e. flagged as 'DontDestroyOnLoad'</summary>
		 * <param name = "gameObject">The GameObject to check for</param>
		 * <returns>True if the given GameObject is set to be persistent</returns>
		 */
		public static bool GameObjectIsPersistent (GameObject gameObject)
		{
			if (gameObject.scene.name == "DontDestroyOnLoad" ||
				(gameObject.scene.name == null && gameObject.scene.buildIndex == -1)) // Because on Android, DontDestroyOnLoad scene has no name
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if a suppplied GameObject is present within a given scene. The GameObject must be in the Hierarchy at runtime.</summary>
		 * <param name = "gameObject">The GameObject to check for</param>
		 * <param name = "sceneIndex">The build index of the scene</param>
		 * <param name = "persistentIsValid">If True, then objects marked as "DontDestroyOnLoad" will also be valid</param>
		 * <returns>True if the GameObject is present within the given scene.</returns>
		 */
		public static bool ObjectIsInScene (GameObject gameObject, int sceneIndex, bool persistentIsValid = true)
		{
			if (gameObject == null)
			{
				return false;
			}
			#if UNITY_EDITOR
			if (IsPrefabFile (gameObject))
			{
				return false;
			}
			#endif

			if (gameObject.scene.buildIndex == sceneIndex)
			{
				return true;
			}
			else if (persistentIsValid && GameObjectIsPersistent (gameObject))
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if a suppplied GameObject is present within a given scene. The GameObject must be in the Hierarchy at runtime.</summary>
		 * <param name = "gameObject">The GameObject to check for</param>
		 * <param name = "sceneName">The name of the scene</param>
		 * <param name = "persistentIsValid">If True, then objects marked as "DontDestroyOnLoad" will also be valid</param>
		 * <returns>True if the GameObject is present within the given scene.</returns>
		 */
		public static bool ObjectIsInScene (GameObject gameObject, string sceneName, bool persistentIsValid = true)
		{
			if (gameObject == null)
			{
				return false;
			}
			#if UNITY_EDITOR
			if (IsPrefabFile (gameObject))
			{
				return false;
			}
			#endif

			if (gameObject.scene.name == sceneName)
			{
				return true;
			}
			else if (persistentIsValid && GameObjectIsPersistent (gameObject))
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Finds the correct instance of a component required by the KickStarter script.</summary>
		 */
		public static T GetKickStarterComponent <T> () where T : Behaviour
		{
			#if UNITY_EDITOR
			if (Object.FindObjectsOfType <T>() != null)
			{
				UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene ();
				T[] instances = Object.FindObjectsOfType <T>() as T[];
				foreach (T instance in instances)
				{
					if (instance.gameObject.scene == activeScene || instance.gameObject.scene.name == "DontDestroyOnLoad")
					{
						return instance;
					}
				}
			}
			#else
			if (Object.FindObjectOfType <T>())
			{
				return Object.FindObjectOfType <T>();
			}
			#endif
			return null;
		}


		/**
		 * <summary>Gets a Behaviour that is in the same scene as a given GameObject.</summary>
		 * <param name = "_gameObject">The GameObject in the scene</param>
		 * <returns>The Behaviour that is in the same scene as the given GameObject</returns>
		 */
		public static T GetOwnSceneInstance <T> (GameObject gameObject) where T : Behaviour
		{
			UnityEngine.SceneManagement.Scene ownScene = gameObject.scene;

			T[] instances = Object.FindObjectsOfType (typeof (T)) as T[];
			foreach (T instance in instances)
			{
				if (instance != null && instance.gameObject.scene == ownScene)
				{
					return instance;
				}
			}

			return null;
		}


		/**
		 * <summary>Gets all instances of a Component that are in the same scene as a given GameObject.</summary>
		 * <param name = "_gameObject">The GameObject in the scene</param>
		 * <returns>All instances of a Component that are in the same scene as the given GameObject</returns>
		 */
		public static T[] GetOwnSceneComponents <T> (GameObject gameObject = null) where T : Component
		{
			T[] instances = Object.FindObjectsOfType (typeof (T)) as T[];
			bool gameObjectSupplied = (gameObject != null);

			List<T> instancesToSend = new List<T>();
			foreach (T instance in instances)
			{
				if (instance)
				{
					if (gameObjectSupplied)
					{
						if (instance.gameObject.scene == gameObject.scene)
						{
							instancesToSend.Add (instance);
						}
					}
					else if (ObjectIsInActiveScene (instance.gameObject))
					{
						instancesToSend.Add (instance);
					}
				}
			}
			return instancesToSend.ToArray ();
		}

	}

}