using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (Player))]
	public class PlayerEditor : CharEditor
	{

		public override void OnInspectorGUI ()
		{
			Player _target = (Player) target;
			
			SharedGUIOne (_target);
			SharedGUITwo (_target);

			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			if (settingsManager && (settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity || settingsManager.playerSwitching == PlayerSwitching.Allow))
			{
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.LabelField ("Player settings", EditorStyles.boldLabel);

				if (settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity)
				{
					_target.hotspotDetector = (DetectHotspots) CustomGUILayout.ObjectField <DetectHotspots> ("Hotspot detector child:", _target.hotspotDetector, true, "", "The DetectHotspots component to rely on for hotspot detection. This should be a child object of the Player.");
				}

				if (settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					_target.associatedNPCPrefab = (NPC) CustomGUILayout.ObjectField <NPC> ("Associated NPC prefab:", _target.associatedNPCPrefab, false, "", "The NPC counterpart of the Player, used as a stand-in when switching the active Player prefab");
				}

				EditorGUILayout.EndVertical ();
			}

			if (Application.isPlaying && _target.gameObject.activeInHierarchy)
			{
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.LabelField ("Current inventory", EditorStyles.boldLabel);

				bool isCarrying = false;

				if (KickStarter.runtimeInventory != null && KickStarter.runtimeInventory.localItems != null)
				{
					for (int i=0; i<KickStarter.runtimeInventory.localItems.Count; i++)
					{
						InvItem invItem = KickStarter.runtimeInventory.localItems[i];

						if (invItem != null)
						{
							isCarrying = true;

							EditorGUILayout.BeginHorizontal ();
							EditorGUILayout.LabelField ("Item:", GUILayout.Width (80f));
							if (invItem.canCarryMultiple)
							{
								EditorGUILayout.LabelField (invItem.label, EditorStyles.boldLabel, GUILayout.Width (135f));
								EditorGUILayout.LabelField ("Count:", GUILayout.Width (50f));
								EditorGUILayout.LabelField (invItem.count.ToString (), GUILayout.Width (44f));
							}
							else
							{
								EditorGUILayout.LabelField (invItem.label, EditorStyles.boldLabel);
							}
							EditorGUILayout.EndHorizontal ();
						}
					}
				}

				if (KickStarter.inventoryManager != null && KickStarter.runtimeDocuments != null && KickStarter.runtimeDocuments.GetCollectedDocumentIDs () != null)
				{
					for (int i=0; i<KickStarter.runtimeDocuments.GetCollectedDocumentIDs ().Length; i++)
					{
						Document document = KickStarter.inventoryManager.GetDocument (KickStarter.runtimeDocuments.GetCollectedDocumentIDs ()[i]);

						if (document != null)
						{
							isCarrying = true;

							EditorGUILayout.BeginHorizontal ();
							EditorGUILayout.LabelField ("Document:", GUILayout.Width (80f));
							EditorGUILayout.LabelField (document.Title, EditorStyles.boldLabel);
							EditorGUILayout.EndHorizontal ();
						}
					}
				}

				if (!isCarrying)
				{
					EditorGUILayout.HelpBox ("This Player is not carrying any items.", MessageType.Info);
				}

				EditorGUILayout.EndVertical ();
			}
			
			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}