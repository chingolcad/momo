/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"InventoryManager.cs"
 * 
 *	This script handles the "Inventory" tab of the main wizard.
 *	Inventory items are defined with this.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	/**
	 * Handles the "Inventory" tab of the Game Editor window.
	 * All inventory items, inventory categories and recipes are defined here.
	 */
	[System.Serializable]
	public class InventoryManager : ScriptableObject
	{
		
		/** The game's full list of inventory items */
		public List<InvItem> items = new List<InvItem>();
		/** The game's full list of inventory item categories */
		public List<InvBin> bins = new List<InvBin>();
		/** The game's full list of inventory item properties */
		public List<InvVar> invVars = new List<InvVar>();
		/** The default ActionListAsset to run if an inventory combination is unhandled */
		public ActionListAsset unhandledCombine;
		/** The default ActionListAsset to run if using an inventory item on a Hotspot is unhandled */
		public ActionListAsset unhandledHotspot;
		/** If True, the Hotspot clicked on to initiate unhandledHotspot will be sent as a parameter to the ActionListAsset */
		public bool passUnhandledHotspotAsParameter;
		/** The default ActionListAsset to run if giving an inventory item to an NPC is unhandled */
		public ActionListAsset unhandledGive;
		/** The game's full list of available recipes */
		public List<Recipe> recipes = new List<Recipe>();
		/** The game's full list of documents */
		public List<Document> documents = new List<Document>();
		/** The game's full list of objectives */
		public List<Objective> objectives = new List<Objective>();
	
		
		#if UNITY_EDITOR
		
		private SettingsManager settingsManager;
		private CursorManager cursorManager;
		
		private string nameFilter = "";
		private int categoryFilter = 0;
		private bool filterOnStart = false;
		
		private InvItem selectedItem;
		private InvVar selectedInvVar;
		private Recipe selectedRecipe;
		private int sideItem = -1;
		private int sideInteraction = -1;
		private int invNumber = 0;
		private int binNumber = -1;

		private Vector2 scrollPos;
		private bool showItemsTab = true;
		private bool showBinsTab = false;
		private bool showCraftingTab = false;
		private bool showPropertiesTab = false;
		private bool showDocumentsTab = false;
		private bool showObjectivesTab = false;

		private bool showUnhandledEvents = true;
		private bool showItemList = true;
		private bool showItemProperties = true;

		private bool showCraftingList = true;
		private bool showCraftingProperties = true;
		private bool showPropertiesList = true;
		private bool showPropertiesProperties = true;

		private string[] boolType = {"False", "True"};
		
		private static GUILayoutOption
			buttonWidth = GUILayout.MaxWidth (20f);
		
		private static GUIContent
			deleteContent = new GUIContent("-", "Delete item");
		
		
		/**
		 * Shows the GUI.
		 */
		public void ShowGUI (Rect windowRect)
		{
			if (AdvGame.GetReferences ())
			{
				if (AdvGame.GetReferences ().settingsManager)
				{
					settingsManager = AdvGame.GetReferences ().settingsManager;
				}
				if (AdvGame.GetReferences ().cursorManager)
				{
					cursorManager = AdvGame.GetReferences ().cursorManager;
				}
			}
			
			EditorGUILayout.Space ();
			GUILayout.BeginHorizontal ();

			GUILayoutOption tabWidth = GUILayout.Width (windowRect.width / 3f - 5f); //GUILayout.MinWidth (60f);

			string label = (items.Count > 0) ? ("Items (" + items.Count + ")") : "Items";
			if (GUILayout.Toggle (showItemsTab, label, "toolbarbutton", tabWidth))
			{
				SetTab (0);
			}

			label = (bins.Count > 0) ? ("Categories (" + bins.Count + ")") : "Categories";
			if (GUILayout.Toggle (showBinsTab,  label, "toolbarbutton", tabWidth))
			{
				SetTab (1);
			}

			label = (invVars.Count > 0) ? ("Properties (" + invVars.Count + ")") : "Properties";
			if (GUILayout.Toggle (showPropertiesTab, label, "toolbarbutton", tabWidth))
			{
				SetTab (3);
			}

			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();

			label = (recipes.Count > 0) ? ("Crafting (" + recipes.Count + ")") : "Crafting";
			if (GUILayout.Toggle (showCraftingTab, label, "toolbarbutton", tabWidth))
			{
				SetTab (2);
			}

			label = (documents.Count > 0) ? ("Documents (" + documents.Count + ")") : "Documents";
			if (GUILayout.Toggle (showDocumentsTab, label, "toolbarbutton", tabWidth))
			{
				SetTab (4);
			}

			label = (objectives.Count > 0) ? ("Objectives (" + objectives.Count + ")") : "Objectives";
			if (GUILayout.Toggle (showObjectivesTab, label, "toolbarbutton", tabWidth))
			{
				SetTab (5);
			}

			GUILayout.EndHorizontal ();
			EditorGUILayout.Space ();
			
			if (showBinsTab)
			{
				BinsGUI ();
			}
			else if (showCraftingTab)
			{
				CraftingGUI ();
			}
			else if (showItemsTab)
			{
				ItemsGUI ();
			}
			else if (showPropertiesTab)
			{
				PropertiesGUI ();
			}
			else if (showDocumentsTab)
			{
				DocumentsGUI ();
			}
			else if (showObjectivesTab)
			{
				ObjectivesGUI ();
			}
			
			if (GUI.changed)
			{
				EditorUtility.SetDirty (this);
			}
		}
		
		
		private void ItemsGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showUnhandledEvents = CustomGUILayout.ToggleHeader (showUnhandledEvents, "Global unhandled events");
			if (showUnhandledEvents)
			{
				unhandledCombine = ActionListAssetMenu.AssetGUI ("Combine:", unhandledCombine, "Inventory_Unhandled_Combine", "AC.KickStarter.runtimeInventory.unhandledCombine", "The default ActionList asset to run if an inventory combination is unhandled");
				unhandledHotspot = ActionListAssetMenu.AssetGUI ("Use on hotspot:", unhandledHotspot, "Inventory_Unhandled_Hotspot", "AC.KickStarter.runtimeInventory.unhandledHotspot", "The default ActionList asset to run if using an inventory item on a Hotspot is unhandled");
				if (settingsManager != null && settingsManager.CanGiveItems ())
				{
					unhandledGive = ActionListAssetMenu.AssetGUI ("Give:", unhandledGive, "Inventory_Unhandled_Give", "AC.KickStarter.runtimeInventory.unhandledGive", "The default ActionList asset to run if giving an inventory item to an NPC is unhandled ");
				}

				if (unhandledHotspot != null)
				{
					passUnhandledHotspotAsParameter = CustomGUILayout.ToggleLeft ("Pass Hotspot as GameObject parameter?", passUnhandledHotspotAsParameter, "AC.KickStarter.inventoryManager.passUnhandledHotspotAsParameter", "If True, the Hotspot clicked on to initiate unhandledHotspot will be sent as a parameter to the ActionList asset");
					if (passUnhandledHotspotAsParameter)
					{
						EditorGUILayout.HelpBox ("The Hotspot will be set as " + unhandledHotspot.name + "'s first parameter, which must be set to type 'GameObject'.", MessageType.Info);
					}
				}
			}
			EditorGUILayout.EndVertical ();

			List<string> binList = new List<string>();
			foreach (InvBin bin in bins)
			{
				binList.Add (bin.label);
			}

			EditorGUILayout.Space ();
			CreateItemsGUI (binList);
			EditorGUILayout.Space ();
			
			if (selectedItem != null && items.Contains (selectedItem))
			{
				string apiPrefix = "AC.KickStarter.runtimeInventory.GetItem (" + selectedItem.id + ")";

				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				showItemProperties = CustomGUILayout.ToggleHeader (showItemProperties, "Inventory item '" + selectedItem.label + "' settings");
				if (showItemProperties)
				{
					selectedItem.label = CustomGUILayout.TextField ("Name:", selectedItem.label, apiPrefix + ".label", "The item's Editor name");
					selectedItem.altLabel = CustomGUILayout.TextField ("Label (if not name):", selectedItem.altLabel, apiPrefix + ".altLabel", "The item's in-game name, if not label");
					
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent ("Category:", "The category that the item belongs to"), GUILayout.Width (146f));
					if (bins.Count > 0)
					{
						binNumber = GetBinSlot (selectedItem.binID);
						binNumber = CustomGUILayout.Popup (binNumber, binList.ToArray(), apiPrefix + ".binID");
						selectedItem.binID = bins[binNumber].id;
					}
					else
					{
						selectedItem.binID = -1;
						EditorGUILayout.LabelField ("No categories defined!", EditorStyles.miniLabel, GUILayout.Width (146f));
					}
					EditorGUILayout.EndHorizontal ();

					selectedItem.carryOnStart = CustomGUILayout.Toggle ("Carry on start?", selectedItem.carryOnStart, apiPrefix + ".carryOnStart", "If True, the Player carries the item when the game begins");
					if (selectedItem.carryOnStart && AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.playerSwitching == PlayerSwitching.Allow && !AdvGame.GetReferences ().settingsManager.shareInventory)
					{
						selectedItem.carryOnStartNotDefault = CustomGUILayout.Toggle ("Give to non-default player?", selectedItem.carryOnStartNotDefault, apiPrefix + ".carryOnStartNotDefault", "If True, then a Player prefab that is not the default carries the item when the game begins");
						if (selectedItem.carryOnStartNotDefault)
						{
							selectedItem.carryOnStartID = ChoosePlayerGUI (selectedItem.carryOnStartID, apiPrefix + ".carryOnStartID", "The Player prefab that carries the item when the game begins");
						}
					}
					
					selectedItem.canCarryMultiple = CustomGUILayout.Toggle ("Can carry multiple?", selectedItem.canCarryMultiple, apiPrefix + ".canCarryMultiple", "If True, then multiple instances of the item can be carried at once");

					if (selectedItem.carryOnStart && selectedItem.canCarryMultiple)
					{
						selectedItem.count = CustomGUILayout.IntField ("Quantity on start:", selectedItem.count, apiPrefix + ".count", "The number of instances that the player is carrying when the game begins");
					}
					else
					{
						selectedItem.count = 1;
					}

					if (selectedItem.canCarryMultiple)
					{
						selectedItem.useSeparateSlots = CustomGUILayout.Toggle ("Place in separate slots?", selectedItem.useSeparateSlots, apiPrefix + ".useSeparateSlots", "If True, then multiple instances of the same item will be listed in separate InventoryBox menu element slots");

						if (!selectedItem.useSeparateSlots)
						{
							selectedItem.selectSingle = CustomGUILayout.Toggle ("Select one-at-a-time?", selectedItem.selectSingle, apiPrefix + ".selectSingle", "If True, then only one instance of the item will be selectable at a time");
						}
					}

					selectedItem.overrideUseSyntax = CustomGUILayout.Toggle ("Override 'Use' syntax?", selectedItem.overrideUseSyntax, apiPrefix + ".overrideUseSyntax", "If True, then the item has its own 'Use X on Y' syntax when selected");
					if (selectedItem.overrideUseSyntax)
					{
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.LabelField ("Use syntax:", GUILayout.Width (100f));
						selectedItem.hotspotPrefix1.label = EditorGUILayout.TextField (selectedItem.hotspotPrefix1.label, GUILayout.MaxWidth (80f));
						EditorGUILayout.LabelField ("(item)", GUILayout.MaxWidth (40f));
						selectedItem.hotspotPrefix2.label = EditorGUILayout.TextField (selectedItem.hotspotPrefix2.label, GUILayout.MaxWidth (80f));
						EditorGUILayout.LabelField ("(hotspot)", GUILayout.MaxWidth (55f));
						EditorGUILayout.EndHorizontal ();
					}

					selectedItem.linkedPrefab = (GameObject) CustomGUILayout.ObjectField <GameObject> ("Linked prefab:", selectedItem.linkedPrefab, false, apiPrefix + ".linkedPrefab", "A GameObject that can be associated with the item, for the creation of e.g. 3D inventory items (through scripting only)");
					if (selectedItem.linkedPrefab != null)
					{
						EditorGUILayout.HelpBox ("This reference is only accessible through scripting.", MessageType.Info);
					}

					GUILayout.Box ("", GUILayout.ExpandWidth (true), GUILayout.Height (1));

					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent ("Main graphic:", "The item's main graphic"), GUILayout.Width (145));
					selectedItem.tex = (Texture) CustomGUILayout.ObjectField <Texture> (selectedItem.tex, false, GUILayout.Width (70), GUILayout.Height (70), apiPrefix + ".tex");
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent ("Active graphic:", "The item's 'highlighted' graphic"), GUILayout.Width (145));
					selectedItem.activeTex = (Texture) CustomGUILayout.ObjectField <Texture> (selectedItem.activeTex, false, GUILayout.Width (70), GUILayout.Height (70), apiPrefix + ".activeTex");
					EditorGUILayout.EndHorizontal ();

					if (AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.selectInventoryDisplay == SelectInventoryDisplay.ShowSelectedGraphic)
					{
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.LabelField (new GUIContent ("Selected graphic:", "The item's 'selected' graphic"), GUILayout.Width (145));
						selectedItem.selectedTex = (Texture) CustomGUILayout.ObjectField<Texture> (selectedItem.selectedTex, false, GUILayout.Width (70), GUILayout.Height (70), apiPrefix + ".selectedTex");
						EditorGUILayout.EndHorizontal ();
					}
					if (AdvGame.GetReferences ().cursorManager != null)
					{
						CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
						if (cursorManager.inventoryHandling == InventoryHandling.ChangeCursor || cursorManager.inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel)
						{
							GUILayout.Box ("", GUILayout.ExpandWidth (true), GUILayout.Height (1));
							selectedItem.cursorIcon.ShowGUI (true, true, "Cursor (optional):", cursorManager.cursorRendering, apiPrefix + ".cursorIcon", "A Cursor that, if assigned, will be used in place of the 'tex' Texture when the item is selected on the cursor");
							GUILayout.Box ("", GUILayout.ExpandWidth (true), GUILayout.Height (1));
						}
					}

					EditorGUILayout.Space ();
					EditorGUILayout.LabelField ("Standard interactions",  CustomStyles.subHeader);
					if (settingsManager && settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive && settingsManager.inventoryInteractions == InventoryInteractions.Multiple && AdvGame.GetReferences ().cursorManager)
					{
						CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
						
						List<string> iconList = new List<string>();
						foreach (CursorIcon icon in cursorManager.cursorIcons)
						{
							iconList.Add (icon.label);
						}
						
						if (cursorManager.cursorIcons.Count > 0)
						{
							foreach (InvInteraction interaction in selectedItem.interactions)
							{
								EditorGUILayout.BeginHorizontal ();
								invNumber = GetIconSlot (interaction.icon.id);
								invNumber = EditorGUILayout.Popup (invNumber, iconList.ToArray());
								interaction.icon = cursorManager.cursorIcons[invNumber];

								int i = selectedItem.interactions.IndexOf (interaction);
								string autoName = selectedItem.label + "_" + interaction.icon.label;
								interaction.actionList = ActionListAssetMenu.AssetGUI ("", interaction.actionList, autoName, apiPrefix + ".interactions[" + i + "].actionList", "The ActionList to run when the interaction is triggered");

								if (GUILayout.Button ("", CustomStyles.IconCog))
								{
									SideInteractionMenu (selectedItem, selectedItem.interactions.IndexOf (interaction));
								}

								EditorGUILayout.EndHorizontal ();
							}
						}
						else
						{
							EditorGUILayout.HelpBox ("No interaction icons defined - please use the Cursor Manager", MessageType.Warning);
						}
						if (GUILayout.Button ("Add interaction"))
						{
							Undo.RecordObject (this, "Add new interaction");
							selectedItem.interactions.Add (new InvInteraction (cursorManager.cursorIcons[0]));
						}
					}
					else
					{
						string autoName = selectedItem.label + "_Use";
						selectedItem.useActionList = ActionListAssetMenu.AssetGUI ("Use:", selectedItem.useActionList, autoName, apiPrefix + ".useActionList", "The ActionList asset to run when using the item is used");
						if (cursorManager && cursorManager.allowInteractionCursorForInventory && cursorManager.cursorIcons.Count > 0)
						{
							int useCursor_int = cursorManager.GetIntFromID (selectedItem.useIconID) + 1;
							if (selectedItem.useIconID == -1) useCursor_int = 0;
							useCursor_int = CustomGUILayout.Popup ("Use cursor icon:", useCursor_int, cursorManager.GetLabelsArray (true), apiPrefix + ".useIconID", "The Cursor to show when hovering over the item");

							if (useCursor_int == 0)
							{
								selectedItem.useIconID = -1;
							}
							else if (cursorManager.cursorIcons.Count > (useCursor_int - 1))
							{
								selectedItem.useIconID = cursorManager.cursorIcons[useCursor_int-1].id;
							}
						}
						else
						{
							selectedItem.useIconID = 0;
						}
						autoName = selectedItem.label + "_Examine";
						selectedItem.lookActionList = ActionListAssetMenu.AssetGUI ("Examine:", selectedItem.lookActionList, autoName, apiPrefix + ".lookActionList", "The ActionListAsset to run when the item is examined");
					}
					
					if (settingsManager != null && settingsManager.CanSelectItems (false))
					{
						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("Unhandled interactions",  CustomStyles.subHeader);
						string autoName = selectedItem.label + "_Unhandled_Hotspot";
						selectedItem.unhandledActionList = ActionListAssetMenu.AssetGUI ("Unhandled use on Hotspot:", selectedItem.unhandledActionList, autoName, apiPrefix + ".unhandledActionList", "The ActionList asset to run when using the item on a Hotspot is unhandled");
						autoName = selectedItem.label + "_Unhandled_Combine";
						selectedItem.unhandledCombineActionList = ActionListAssetMenu.AssetGUI ("Unhandled combine:", selectedItem.unhandledCombineActionList, autoName, apiPrefix + ".unhandledCombineActionList", "The ActionListAsset to run when using the item on another InvItem is unhandled");
					}
					
					EditorGUILayout.Space ();
					EditorGUILayout.LabelField ("Combine interactions",  CustomStyles.subHeader);
					for (int i=0; i<selectedItem.combineActionList.Count; i++)
					{
						EditorGUILayout.BeginHorizontal ();
						invNumber = GetArraySlot (selectedItem.combineID[i]);
						invNumber = EditorGUILayout.Popup (invNumber, GetLabelList ());
						selectedItem.combineID[i] = items[invNumber].id;

						string autoName = selectedItem.label + "_Combine_" + GetLabelList () [invNumber];
						selectedItem.combineActionList[i] = ActionListAssetMenu.AssetGUI ("", selectedItem.combineActionList[i], autoName, apiPrefix + ".combineActionList[" + i + "]", "A List of all 'Combine' InvInteraction objects associated with the item");
						
						if (GUILayout.Button (deleteContent, EditorStyles.miniButtonRight, buttonWidth))
						{
							Undo.RecordObject (this, "Delete combine event");
							selectedItem.combineActionList.RemoveAt (i);
							selectedItem.combineID.RemoveAt (i);
							break;
						}
						EditorGUILayout.EndHorizontal ();
					}
					if (GUILayout.Button ("Add combine event"))
					{
						Undo.RecordObject (this, "Add new combine event");
						selectedItem.combineActionList.Add (null);
						selectedItem.combineID.Add (0);
					}
					
					// List all "reverse" inventory combinations
					string reverseCombinations = "";
					foreach (InvItem otherItem in items)
					{
						if (otherItem != selectedItem)
						{
							if (otherItem.combineID.Contains (selectedItem.id))
							{
								reverseCombinations += "- " + otherItem.label + "\n";
								continue;
							}
						}
					}
					if (reverseCombinations.Length > 0)
					{
						EditorGUILayout.Space ();
						EditorGUILayout.HelpBox ("The following inventory items have combine interactions that reference this item:\n" + reverseCombinations, MessageType.Info);
					}
					
					if (invVars.Count > 0)
					{
						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("Properties",  CustomStyles.subHeader);
						
						RebuildProperties (selectedItem);
						
						// UI for setting property values
						if (selectedItem.vars.Count > 0)
						{
							foreach (InvVar invVar in selectedItem.vars)
							{
								string label = invVar.label + ":";
								if (string.IsNullOrEmpty (invVar.label))
								{
									label = "Property " + invVar.id.ToString () + ":";
								}
								
								if (invVar.type == VariableType.Boolean)
								{
									if (invVar.val != 1)
									{
										invVar.val = 0;
									}
									invVar.val = CustomGUILayout.Popup (label, invVar.val, boolType, apiPrefix + ".GetProperty (" + invVar.id + ").val", "The property's value for this item");
								}
								else if (invVar.type == VariableType.Integer)
								{
									invVar.val = CustomGUILayout.IntField (label, invVar.val, apiPrefix + ".GetProperty (" + invVar.id + ").val", "The property's value for this item");
								}
								else if (invVar.type == VariableType.PopUp)
								{
									invVar.val = CustomGUILayout.Popup (label, invVar.val, invVar.GenerateEditorPopUpLabels (), apiPrefix + ".GetProperty (" + invVar.id + ").val", "The property's value for this item");
								}
								else if (invVar.type == VariableType.String)
								{
									invVar.textVal = CustomGUILayout.TextField (label, invVar.textVal, apiPrefix + ".GetProperty (" + invVar.id + ").textVal", "The property's value for this item");
								}
								else if (invVar.type == VariableType.Float)
								{
									invVar.floatVal = CustomGUILayout.FloatField (label, invVar.floatVal, apiPrefix + ".GetProperty (" + invVar.id + ").floatVal", "The property's value for this item");
								}
								else if (invVar.type == VariableType.Vector3)
								{
									invVar.vector3Val = CustomGUILayout.Vector3Field (label, invVar.vector3Val, apiPrefix + ".GetProperty (" + invVar.id + ").vector3Val", "The property's value for this item");
								}
							}
						}
						else
						{
							EditorGUILayout.HelpBox ("No properties have been defined that this inventory item can use.", MessageType.Info);
						}
					}
				}
				EditorGUILayout.EndVertical ();
			}
		}


		// Categories

		private bool showCategoriesList = true;
		private bool showSelectedCategory = true;
		private InvBin selectedCategory;
		private int sideCategory = -1;


		private void BinsGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCategoriesList = CustomGUILayout.ToggleHeader (showCategoriesList, "Categories");
			if (showCategoriesList)
			{
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (Mathf.Min (bins.Count * 21, ACEditorPrefs.MenuItemsBeforeScroll * 21) +5));
				foreach (InvBin bin in bins)
				{
					EditorGUILayout.BeginHorizontal ();

					if (GUILayout.Toggle (selectedCategory == bin, bin.EditorLabel, "Button"))
					{
						if (selectedCategory != bin)
						{
							DeactivateAllCategories ();
							ActivateCategory (bin);
						}
					}

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						SideMenu (bin);
					}
					
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();

				EditorGUILayout.Space ();
				if (GUILayout.Button ("Create new category"))
				{
					Undo.RecordObject (this, "Create new category");
					List<int> idArray = new List<int>();
					foreach (InvBin bin in bins)
					{
						idArray.Add (bin.id);
					}
					idArray.Sort ();

					InvBin newBin = new InvBin (idArray.ToArray ());
					bins.Add (newBin);

					DeactivateAllCategories ();
					ActivateCategory (newBin);
				}
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			if (selectedCategory != null && bins.Contains (selectedCategory))
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);

				showSelectedCategory = CustomGUILayout.ToggleHeader (showSelectedCategory, "Category #" + selectedCategory.EditorLabel);
				if (showSelectedCategory)
				{
					selectedCategory.label = CustomGUILayout.TextField ("Category name:", selectedCategory.label, "AC.KickStarter.inventoryManager.GetCategory (" + selectedCategory.id + ").label", "The category's editor name");
				}
				EditorGUILayout.EndVertical ();
			}
		}


		private void SideMenu (InvBin category)
		{
			GenericMenu menu = new GenericMenu ();
			sideCategory = bins.IndexOf (category);
			
			menu.AddItem (new GUIContent ("Delete"), false, CategoryCallback, "Delete");
			menu.ShowAsContext ();
		}


		private void CategoryCallback (object obj)
		{
			if (sideCategory >= 0)
			{
				InvBin tempCategory = bins[sideCategory];

				switch (obj.ToString ())
				{
					case "Delete":
						Undo.RecordObject (this, "Delete category");
						if (tempCategory == selectedCategory)
						{
							DeactivateAllCategories ();
						}
						bins.RemoveAt (sideCategory);
						break;
				}
			}
			
			EditorUtility.SetDirty (this);
			AssetDatabase.SaveAssets ();
			
			sideCategory = -1;
		}


		private void DeactivateAllCategories ()
		{
			selectedCategory = null;
		}


		private void ActivateCategory (InvBin category)
		{
			selectedCategory = category;
			EditorGUIUtility.editingTextField = false;
		}

		
		// Properties
		
		private void PropertiesGUI ()
		{
			List<string> binList = new List<string>();
			foreach (InvBin bin in bins)
			{
				binList.Add (bin.EditorLabel);
			}
			
			CreatePropertiesGUI ();
			EditorGUILayout.Space ();
			
			if (selectedInvVar != null && invVars.Contains (selectedInvVar))
			{
				string apiPrefix = "AC.KickStarter.variablesManager.GetProperty (" + selectedInvVar.id + ")";
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);

				showPropertiesProperties = CustomGUILayout.ToggleHeader (showPropertiesProperties, "Inventory property '" + selectedInvVar.label + "' properties");
				if (showPropertiesProperties)
				{
					selectedInvVar.label = CustomGUILayout.TextField ("Name:", selectedInvVar.label, apiPrefix + ".label", "Its editor name");
					selectedInvVar.type = (VariableType) CustomGUILayout.EnumPopup ("Type:", selectedInvVar.type, apiPrefix + ".type", "Its variable type");

					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Internal description:", GUILayout.MaxWidth (146f));
					selectedInvVar.description = EditorGUILayout.TextArea (selectedInvVar.description);
					EditorGUILayout.EndHorizontal ();

					if (selectedInvVar.type == VariableType.PopUp)
					{
						//selectedInvVar.popUps = VariablesManager.PopupsGUI (selectedInvVar.popUps);
						VariablesManager.ShowPopUpLabelsGUI (selectedInvVar, true);
					}
					
					selectedInvVar.limitToCategories = EditorGUILayout.BeginToggleGroup (new GUIContent ("Limit to set categories?", "If True, then the property will be limited to inventory items within certain categories"), selectedInvVar.limitToCategories);

					if (bins.Count > 0)
					{
						List<int> newCategoryIDs = new List<int>();
						foreach (InvBin bin in bins)
						{
							bool usesCategory = false;
							if (selectedInvVar.categoryIDs.Contains (bin.id))
							{
								usesCategory = true;
							}
							usesCategory = CustomGUILayout.Toggle ("Use in '" + bin.label + "'?", usesCategory, apiPrefix + ".categoryIDs");
							
							if (usesCategory)
							{
								newCategoryIDs.Add (bin.id);
							}
						}
						selectedInvVar.categoryIDs = newCategoryIDs;
					}
					else if (selectedInvVar.limitToCategories)
					{
						EditorGUILayout.HelpBox ("No categories are defined!", MessageType.Warning);
					}
					EditorGUILayout.EndToggleGroup ();
				}
				EditorGUILayout.EndVertical ();
			}
			
			if (GUI.changed)
			{
				foreach (InvItem item in items)
				{
					RebuildProperties (item);
				}
			}
		}
		
		
		private void RebuildProperties (InvItem item)
		{
			// Which properties are available?
			List<int> availableVarIDs = new List<int>();
			foreach (InvVar invVar in invVars)
			{
				if (!invVar.limitToCategories || bins.Count == 0 || invVar.categoryIDs.Contains (item.binID))
				{
					availableVarIDs.Add (invVar.id);
				}
			}
			
			// Create new properties / transfer existing values
			List<InvVar> newInvVars = new List<InvVar>();
			foreach (InvVar invVar in invVars)
			{
				if (availableVarIDs.Contains (invVar.id))
				{
					InvVar newInvVar = new InvVar (invVar);
					InvVar oldInvVar = item.GetProperty (invVar.id);
					if (oldInvVar != null)
					{
						newInvVar.TransferValues (oldInvVar);
					}
					newInvVar.popUpID = invVar.popUpID;
					newInvVars.Add (newInvVar);
				}
			}
			
			item.vars = newInvVars;
		}


		private void ResetFilter ()
		{
			nameFilter = string.Empty;
			categoryFilter = 0;
			filterOnStart = false;
		}


		// Documents

		private bool showDocumentsList = true;
		private bool showSelectedDocument = true;
		private Document selectedDocument;
		private int sideDocument = -1;

		private void DocumentsGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showDocumentsList = CustomGUILayout.ToggleHeader (showDocumentsList, "Documents");
			if (showDocumentsList)
			{
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (Mathf.Min (documents.Count * 21, ACEditorPrefs.MenuItemsBeforeScroll * 21) +5));
				foreach (Document document in documents)
				{
					EditorGUILayout.BeginHorizontal ();

					if (GUILayout.Toggle (selectedDocument == document, document.ID + ": " + document.title, "Button"))
					{
						if (selectedDocument != document)
						{
							DeactivateAllDocuments ();
							ActivateDocument (document);
						}
					}

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						SideMenu (document);
					}
					
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();

				EditorGUILayout.Space ();
				if (GUILayout.Button ("Create new Document"))
				{
					Undo.RecordObject (this, "Create new Document");

					if (documents.Count > 0)
					{
						List<int> idArray = new List<int>();
						foreach (Document document in documents)
						{
							idArray.Add (document.ID);
						}
						idArray.Sort ();

						Document newDocument = new Document (idArray.ToArray ());
						documents.Add (newDocument);

						DeactivateAllDocuments ();
						ActivateDocument (newDocument);
					}
					else
					{
						Document newDocument = new Document (0);
						documents.Add (newDocument);
						ActivateDocument (newDocument);
					}
				}
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			if (selectedDocument != null && documents.Contains (selectedDocument))
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);

				showSelectedDocument = CustomGUILayout.ToggleHeader (showSelectedDocument, "Document #" + selectedDocument.ID + ": " + selectedDocument.Title);
				if (showSelectedDocument)
				{
					string apiPrefix = "AC.KickStarter.inventoryManager.GetDocument (" + selectedDocument.ID + ")";
					selectedDocument.ShowGUI (apiPrefix, bins);
				}
				else
				{
					EditorGUILayout.EndVertical ();
				}
			}
		}


		private void SideMenu (Document document)
		{
			GenericMenu menu = new GenericMenu ();
			sideDocument = documents.IndexOf (document);
			
			menu.AddItem (new GUIContent ("Insert after"), false, DocumentCallback, "Insert after");
			if (documents.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, DocumentCallback, "Delete");
			}
			if (sideDocument > 0 || sideDocument < documents.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (sideDocument > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, DocumentCallback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, DocumentCallback, "Move up");
			}
			if (sideDocument < documents.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, DocumentCallback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, DocumentCallback, "Move to bottom");
			}

			menu.AddSeparator ("");
			menu.AddItem (new GUIContent ("Find references"), false, DocumentCallback, "Find references");

			if (Application.isPlaying)
			{
				menu.AddSeparator ("");
				if (KickStarter.runtimeDocuments.DocumentIsInCollection (document.ID))
				{
					menu.AddDisabledItem (new GUIContent ("Held by Player"));
				}
				else
				{
					menu.AddItem (new GUIContent ("Give to Player"), false, DocumentCallback, "Give to Player");
				}
			}

			menu.ShowAsContext ();
		}


		private void DocumentCallback (object obj)
		{
			if (sideDocument >= 0)
			{
				Document tempDocument = documents[sideDocument];

				switch (obj.ToString ())
				{
					case "Insert after":
						Undo.RecordObject (this, "Insert Document");
						documents.Insert (sideDocument+1, new Document (GetDocumentIDList ().ToArray ()));
						break;
					
					case "Delete":
						Undo.RecordObject (this, "Delete Document");
						if (tempDocument == selectedDocument)
						{
							DeactivateAllDocuments ();
						}
						documents.RemoveAt (sideDocument);
						break;
					
					case "Move up":
						Undo.RecordObject (this, "Move Document up");
						documents.RemoveAt (sideDocument);
						documents.Insert (sideDocument-1, tempDocument);
						break;
					
					case "Move down":
						Undo.RecordObject (this, "Move Document down");
						documents.RemoveAt (sideDocument);
						documents.Insert (sideDocument+1, tempDocument);
						break;

					case "Move to top":
						Undo.RecordObject (this, "Move Document to top");
						documents.RemoveAt (sideDocument);
						documents.Insert (0, tempDocument);
						break;

					case "Move to bottom":
						Undo.RecordObject (this, "Move Document to bottom");
						documents.Add (tempDocument);
						documents.RemoveAt (sideDocument);
						break;

					case "Find references":
						FindReferences (tempDocument);
						break;

					case "Give to Player":
						KickStarter.runtimeDocuments.AddToCollection (tempDocument);
						ACDebug.Log ("Document " + tempDocument.ID.ToString () + " added to Player's inventory");
						break;
				}
			}
			
			EditorUtility.SetDirty (this);
			AssetDatabase.SaveAssets ();
			
			sideDocument = -1;
		}


		public static int DocumentSelectorList (int ID, string label = "Document:")
		{
			if (KickStarter.inventoryManager != null && KickStarter.inventoryManager.documents != null && KickStarter.inventoryManager.documents.Count > 0)
			{
				int tempNumber = -1;

				string[] labelList = new string[KickStarter.inventoryManager.documents.Count];
				for (int i=0; i<KickStarter.inventoryManager.documents.Count; i++)
				{
					labelList[i] = KickStarter.inventoryManager.documents[i].ID.ToString () + ": " + KickStarter.inventoryManager.documents[i].Title;

					if (KickStarter.inventoryManager.documents[i].ID == ID)
					{
						tempNumber = i;
					}
				}

				if (tempNumber == -1)
				{
					// Wasn't found (was deleted?), so revert to zero
					if (ID != 0)
						ACDebug.LogWarning ("Previously chosen Document no longer exists!");
					tempNumber = 0;
					ID = 0;
				}

				tempNumber = UnityEditor.EditorGUILayout.Popup (label, tempNumber, labelList);
				ID = KickStarter.inventoryManager.documents [tempNumber].ID;
			}
			else
			{
				UnityEditor.EditorGUILayout.HelpBox ("No Documents exist! They can be defined in the Inventory Manager.", UnityEditor.MessageType.Info);
				ID = 0;
			}

			return ID;
		}


		private void DeactivateAllDocuments ()
		{
			selectedDocument = null;
		}


		private List<int> GetDocumentIDList ()
		{
			List<int> idList = new List<int>();
			foreach (Document document in documents)
			{
				idList.Add (document.ID);
			}
			
			idList.Sort ();

			return idList;
		}


		private void ActivateDocument (Document document)
		{
			selectedDocument = document;
			EditorGUIUtility.editingTextField = false;
		}


		// Objectives

		private bool showObjectivesList = true;
		private bool showSelectedObjective = true;
		private Objective selectedObjective;
		private int sideObjective = -1;

		private void ObjectivesGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showObjectivesList = CustomGUILayout.ToggleHeader (showObjectivesList, "Objectives");
			if (showObjectivesList)
			{
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (Mathf.Min (objectives.Count * 21, ACEditorPrefs.MenuItemsBeforeScroll * 21) +5));
				foreach (Objective objective in objectives)
				{
					EditorGUILayout.BeginHorizontal ();
					
					if (GUILayout.Toggle (selectedObjective == objective, objective.ID + ": " + objective.Title, "Button"))
					{
						if (selectedObjective != objective)
						{
							DeactivateAllObjectives ();
							ActivateObjective (objective);
						}
					}

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						SideMenu (objective);
					}
					
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();

				EditorGUILayout.Space ();
				if (GUILayout.Button ("Create new Objective"))
				{
					Undo.RecordObject (this, "Create new Objective");

					List<int> idArray = new List<int>();
					foreach (Objective objective in objectives)
					{
						idArray.Add (objective.ID);
					}
					idArray.Sort ();

					Objective newObjective = new Objective (idArray.ToArray ());
					objectives.Add (newObjective);

					DeactivateAllObjectives ();
					ActivateObjective (newObjective);
				}
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			if (selectedObjective != null && objectives.Contains (selectedObjective))
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);

				showSelectedObjective = CustomGUILayout.ToggleHeader (showSelectedObjective, "Objective #" + selectedObjective.ID + ": " + selectedObjective.Title);
				if (showSelectedObjective)
				{
					string apiPrefix = "AC.KickStarter.inventoryManager.GetObjective (" + selectedObjective.ID + ")";
					selectedObjective.ShowGUI (apiPrefix);
				}
				else
				{
					EditorGUILayout.EndVertical ();
				}
			}
		}


		private void SideMenu (Objective objective)
		{
			GenericMenu menu = new GenericMenu ();
			sideObjective = objectives.IndexOf (objective);
			
			menu.AddItem (new GUIContent ("Insert after"), false, ObjectiveCallback, "Insert after");
			if (objectives.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, ObjectiveCallback, "Delete");
			}
			if (sideObjective > 0 || sideObjective < objectives.Count-1)
			{
				menu.AddSeparator (string.Empty);
			}
			if (sideObjective > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, ObjectiveCallback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, ObjectiveCallback, "Move up");
			}
			if (sideObjective < objectives.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, ObjectiveCallback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, ObjectiveCallback, "Move to bottom");
			}

			menu.AddSeparator (string.Empty);

			menu.ShowAsContext ();
		}


		private void ObjectiveCallback (object obj)
		{
			if (sideObjective >= 0)
			{
				Objective tempObjective = objectives[sideObjective];

				switch (obj.ToString ())
				{
					case "Insert after":
						Undo.RecordObject (this, "Insert Objective");
						List<int> idArray = new List<int>();
						foreach (Objective objective in objectives)
						{
							idArray.Add (objective.ID);
						}
						idArray.Sort ();
						objectives.Insert (sideObjective+1, new Objective (idArray.ToArray ()));
						break;
					
					case "Delete":
						Undo.RecordObject (this, "Delete Objective");
						if (tempObjective == selectedObjective)
						{
							DeactivateAllObjectives ();
						}
						objectives.RemoveAt (sideObjective);
						break;
					
					case "Move up":
						Undo.RecordObject (this, "Move Objective up");
						objectives.RemoveAt (sideObjective);
						objectives.Insert (sideObjective-1, tempObjective);
						break;
					
					case "Move down":
						Undo.RecordObject (this, "Move Objective down");
						objectives.RemoveAt (sideObjective);
						objectives.Insert (sideObjective+1, tempObjective);
						break;

					case "Move to top":
						Undo.RecordObject (this, "Move Objective to top");
						objectives.RemoveAt (sideObjective);
						objectives.Insert (0, tempObjective);
						break;

					case "Move to bottom":
						Undo.RecordObject (this, "Move Objective to bottom");
						objectives.Add (tempObjective);
						objectives.RemoveAt (sideObjective);
						break;
				}
			}
			
			EditorUtility.SetDirty (this);
			AssetDatabase.SaveAssets ();
			
			sideObjective = -1;
		}


		public static int ObjectiveSelectorList (int ID, string label = "Objective:")
		{
			if (KickStarter.inventoryManager != null && KickStarter.inventoryManager.objectives != null && KickStarter.inventoryManager.objectives.Count > 0)
			{
				int tempNumber = -1;

				string[] labelList = new string[KickStarter.inventoryManager.objectives.Count];
				for (int i=0; i<KickStarter.inventoryManager.objectives.Count; i++)
				{
					labelList[i] = KickStarter.inventoryManager.objectives[i].ID.ToString () + ": " + KickStarter.inventoryManager.objectives[i].Title;

					if (KickStarter.inventoryManager.objectives[i].ID == ID)
					{
						tempNumber = i;
					}
				}

				if (tempNumber == -1)
				{
					// Wasn't found (was deleted?), so revert to zero
					if (ID != 0)
						ACDebug.LogWarning ("Previously chosen Objective no longer exists!");
					tempNumber = 0;
					ID = 0;
				}

				tempNumber = UnityEditor.EditorGUILayout.Popup (label, tempNumber, labelList);
				ID = KickStarter.inventoryManager.objectives [tempNumber].ID;
			}
			else
			{
				UnityEditor.EditorGUILayout.HelpBox ("No Objectives exist! They can be defined in the Inventory Manager.", UnityEditor.MessageType.Info);
				ID = 0;
			}

			return ID;
		}


		private void DeactivateAllObjectives ()
		{
			selectedObjective = null;
		}


		private void ActivateObjective (Objective objective)
		{
			selectedObjective = objective;
			EditorGUIUtility.editingTextField = false;
		}


		// Items
		
		private void CreateItemsGUI (List<string> binList)
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showItemList = CustomGUILayout.ToggleHeader (showItemList, "Inventory items");
			if (showItemList)
			{
				if (items != null && items.Count > 0)
				{
					nameFilter = EditorGUILayout.TextField ("Name filter:", nameFilter);
					if (binList != null && binList.Count > 0)
					{
						string[] binArray = new string[binList.Count+1];
						binArray[0] = "(Any)";
						for (int i=0; i<binList.Count; i++)
						{
							binArray[i+1] = binList[i];
						}

						categoryFilter = EditorGUILayout.Popup ("Category filter:", categoryFilter, binArray);
					}
					else
					{
						categoryFilter = 0;
					}
					filterOnStart = EditorGUILayout.Toggle ("Filter by 'Carry on start?'?", filterOnStart);

					EditorGUILayout.Space ();
				}

				int numInFilter = 0;
				foreach (InvItem item in items)
				{
					item.showInFilter = false;
					if (string.IsNullOrEmpty (nameFilter) || item.label.ToLower ().Contains (nameFilter.ToLower ()))
					{
						if (categoryFilter <= 0 || GetBinSlot (item.binID) == (categoryFilter-1))
						{
							if (!filterOnStart || item.carryOnStart)
							{
								item.showInFilter = true;
								numInFilter ++;
							}
						}
					}
				}

				if (numInFilter > 0)
				{
					scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (Mathf.Min (numInFilter * 21, ACEditorPrefs.MenuItemsBeforeScroll * 21) +5));
					foreach (InvItem item in items)
					{
						if (!item.showInFilter) continue;

						EditorGUILayout.BeginHorizontal ();
						
						string buttonLabel = item.label;
						if (buttonLabel == "")
						{
							buttonLabel = "(Untitled)";	
						}
						
						if (GUILayout.Toggle (selectedItem == item, item.id + ": " + buttonLabel, "Button"))
						{
							if (selectedItem != item)
							{
								DeactivateAllItems ();
								ActivateItem (item);
							}
						}
						
						if (GUILayout.Button ("", CustomStyles.IconCog))
						{
							SideMenu (item);
						}
						
						EditorGUILayout.EndHorizontal ();
					}

					EditorGUILayout.EndScrollView ();
					if (numInFilter != items.Count)
					{
						EditorGUILayout.HelpBox ("Filtering " + numInFilter + " out of " + items.Count + " items.", MessageType.Info);
					}
				}
				else if (items.Count > 0)
				{
					EditorGUILayout.HelpBox ("No items that match the above filters have been created.", MessageType.Info);
				}

				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Create new item"))
				{
					Undo.RecordObject (this, "Create inventory item");
					
					ResetFilter ();
					InvItem newItem = CreateNewItem ();
					DeactivateAllItems ();
					ActivateItem (newItem);
				}

				if (GUILayout.Button ("", CustomStyles.IconCog))
				{
					ExportSideMenu ();
				}
				EditorGUILayout.EndHorizontal ();
			}
			EditorGUILayout.EndVertical ();
		}


		private void ImportItems ()
		{
			bool canProceed = EditorUtility.DisplayDialog ("Import inventory items", "AC will now prompt you for a CSV file to import. It is recommended to back up your project beforehand.", "OK", "Cancel");
			if (!canProceed) return;

			string fileName = EditorUtility.OpenFilePanel ("Import inventory item data", "Assets", "csv");
			if (fileName.Length == 0)
			{
				return;
			}
			
			if (System.IO.File.Exists (fileName))
			{
				string csvText = Serializer.LoadFile (fileName);
				string [,] csvOutput = CSVReader.SplitCsvGrid (csvText);

				InvItemImportWizardWindow.Init (this, csvOutput);
			}
		}


		/**
		 * <summary>Creates a new inventory item</summary>
		 * <param name = "newID">If >= 0, the ID number of the item, if available.  If it is already taken, the item will not be created</param>
		 * <returns>The newly-created item</returns>
		 */
		public InvItem CreateNewItem (int newID = -1)
		{
			List<int> idList = GetIDList ();
			if (newID >= 0)
			{
				if (idList.Contains (newID))
				{
					return null;
				}
				InvItem newItem = new InvItem (newID);
				items.Add (newItem);
				return newItem;
			}
			else
			{
				InvItem newItem = new InvItem (idList.ToArray ());
				items.Add (newItem);
				return newItem;
			}
		}
		
		
		private void CreatePropertiesGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showPropertiesList = CustomGUILayout.ToggleHeader (showPropertiesList, "Inventory properties");
			if (showPropertiesList)
			{
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (Mathf.Min (invVars.Count * 21, ACEditorPrefs.MenuItemsBeforeScroll * 21) +5));
				foreach (InvVar invVar in invVars)
				{
					EditorGUILayout.BeginHorizontal ();
					
					string buttonLabel = invVar.label;
					if (buttonLabel == "")
					{
						buttonLabel = "(Untitled)";	
					}
					
					if (GUILayout.Toggle (selectedInvVar == invVar, invVar.id + ": " + buttonLabel, "Button"))
					{
						if (selectedInvVar != invVar)
						{
							DeactivateAllInvVars ();
							ActivateItem (invVar);
						}
					}
					
					if (GUILayout.Button ("", CustomStyles.IconCog))
					{
						SideMenu (invVar);
					}
					
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();
				
				if (GUILayout.Button ("Create new property"))
				{
					Undo.RecordObject (this, "Create inventory property");
					
					InvVar newInvVar = new InvVar (GetIDArrayProperty ());
					invVars.Add (newInvVar);
					DeactivateAllInvVars ();
					ActivateItem (newInvVar);
				}
			}
			EditorGUILayout.EndVertical ();
		}
		
		
		private void ActivateItem (InvItem item)
		{
			selectedItem = item;
			EditorGUIUtility.editingTextField = false;
		}
		
		
		private void ActivateItem (InvVar invVar)
		{
			selectedInvVar = invVar;
			EditorGUIUtility.editingTextField = false;
		}
		
		
		private void DeactivateAllItems ()
		{
			selectedItem = null;
			EditorGUIUtility.editingTextField = false;
		}
		
		
		private void DeactivateAllInvVars ()
		{
			selectedInvVar = null;
		}
		
		
		private void ActivateRecipe (Recipe recipe)
		{
			selectedRecipe = recipe;
		}
		
		
		private void DeactivateAllRecipes ()
		{
			selectedRecipe = null;
		}


		private void ExportSideMenu ()
		{
			GenericMenu menu = new GenericMenu ();
			menu.AddItem (new GUIContent ("Import items..."), false, ExportCallback, "Import");
			menu.AddItem (new GUIContent ("Export items..."), false, ExportCallback, "Export");

			if (Application.isPlaying && items.Count > 0)
			{
				menu.AddItem (new GUIContent ("Give all to Player"), false, ExportCallback, "Give all to Player");
			}
			menu.ShowAsContext ();
		}


		private void ExportCallback (object obj)
		{
			switch (obj.ToString ())
			{
				case "Import":
					ImportItems ();
					break;

				case "Export":
					InvItemExportWizardWindow.Init (this);
					break;

				case "Give all to Player":
					foreach (InvItem item in items)
					{
						KickStarter.runtimeInventory.Add (item.id);
					}
					ACDebug.Log ("All items added to Player's inventory");
					break;
			}
		}
		
		
		private void SideMenu (InvItem item)
		{
			GenericMenu menu = new GenericMenu ();
			sideItem = items.IndexOf (item);
			
			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (items.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}
			if (sideItem > 0 || sideItem < items.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (sideItem > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (sideItem < items.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}

			menu.AddSeparator ("");
			menu.AddItem (new GUIContent ("Find references"), false, Callback, "Find references");

			if (Application.isPlaying)
			{
				if (KickStarter.runtimeInventory.IsCarryingItem (item.id))
				{
					menu.AddDisabledItem (new GUIContent ("Held by Player"));
				}
				else
				{
					menu.AddItem (new GUIContent ("Give to Player"), false, Callback, "Give to Player");
				}
			}

			menu.ShowAsContext ();
		}


		private void SideInteractionMenu (InvItem item, int index)
		{
			GenericMenu menu = new GenericMenu ();
			sideInteraction = index;
			sideItem = items.IndexOf (item);
			
			menu.AddItem (new GUIContent ("Insert after"), false, InteractionCallback, "Insert after");
			if (items.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, InteractionCallback, "Delete");
			}
			if (sideInteraction > 0 || sideInteraction < item.interactions.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (sideInteraction > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, InteractionCallback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, InteractionCallback, "Move up");
			}
			if (sideInteraction < item.interactions.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, InteractionCallback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, InteractionCallback, "Move to bottom");
			}

			menu.ShowAsContext ();
		}


		private void SideMenu (InvVar invVar)
		{
			GenericMenu menu = new GenericMenu ();
			sideItem = invVars.IndexOf (invVar);
			
			menu.AddItem (new GUIContent ("Insert after"), false, PropertyCallback, "Insert after");
			if (invVars.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, PropertyCallback, "Delete");
			}
			if (sideItem > 0 || sideItem < invVars.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (sideItem > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, PropertyCallback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, PropertyCallback, "Move up");
			}
			if (sideItem < invVars.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, PropertyCallback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, PropertyCallback, "Move to bottom");
			}

			menu.ShowAsContext ();
		}


		private void SideMenu (Recipe recipe)
		{
			GenericMenu menu = new GenericMenu ();
			sideItem = recipes.IndexOf (recipe);
			
			menu.AddItem (new GUIContent ("Insert after"), false, RecipeCallback, "Insert after");
			if (recipes.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, RecipeCallback, "Delete");
			}
			if (sideItem > 0 || sideItem < recipes.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (sideItem > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, RecipeCallback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, RecipeCallback, "Move up");
			}
			if (sideItem < recipes.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, RecipeCallback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, RecipeCallback, "Move to bottom");
			}

			menu.ShowAsContext ();
		}

		
		private void Callback (object obj)
		{
			if (sideItem >= 0)
			{
				ResetFilter ();
				InvItem tempItem = items[sideItem];
				
				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (this, "Insert item");
					items.Insert (sideItem+1, new InvItem (GetIDList ().ToArray ()));
					break;
					
				case "Delete":
					Undo.RecordObject (this, "Delete item");
					DeactivateAllItems ();
					items.RemoveAt (sideItem);
					break;
					
				case "Move up":
					Undo.RecordObject (this, "Move item up");
					items.RemoveAt (sideItem);
					items.Insert (sideItem-1, tempItem);
					break;
					
				case "Move down":
					Undo.RecordObject (this, "Move item down");
					items.RemoveAt (sideItem);
					items.Insert (sideItem+1, tempItem);
					break;

				case "Move to top":
					Undo.RecordObject (this, "Move item to top");
					items.RemoveAt (sideItem);
					items.Insert (0, tempItem);
					break;

				case "Move to bottom":
					Undo.RecordObject (this, "Move item to bottom");
					items.Add (tempItem);
					items.RemoveAt (sideItem);
					break;

				case "Find references":
					FindReferences (tempItem);
					break;

				case "Give to Player":
					KickStarter.runtimeInventory.Add (tempItem.id);
					ACDebug.Log ("Item " + tempItem.label + " added to Player's inventory");
					break;
				}
			}
			
			EditorUtility.SetDirty (this);
			AssetDatabase.SaveAssets ();
			
			sideItem = -1;
		}


		private void InteractionCallback (object obj)
		{
			if (sideInteraction >= 0 && sideItem >= 0)
			{
				ResetFilter ();
				InvInteraction tempInteraction = items[sideItem].interactions[sideInteraction];
				
				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (this, "Insert item");
					InvInteraction newInteraction = new InvInteraction (cursorManager.cursorIcons[0]);
					selectedItem.interactions.Insert (sideInteraction+1, newInteraction);
					break;
					
				case "Delete":
					Undo.RecordObject (this, "Delete interaction");
					selectedItem.interactions.RemoveAt (sideInteraction);
					break;
					
				case "Move up":
					Undo.RecordObject (this, "Move interaction up");
					selectedItem.interactions.RemoveAt (sideInteraction);
					selectedItem.interactions.Insert (sideInteraction-1, tempInteraction);
					break;
					
				case "Move down":
					Undo.RecordObject (this, "Move interaction down");
					selectedItem.interactions.RemoveAt (sideInteraction);
					selectedItem.interactions.Insert (sideInteraction+1, tempInteraction);
					break;

				case "Move to top":
					Undo.RecordObject (this, "Move interaction to top");
					selectedItem.interactions.RemoveAt (sideInteraction);
					selectedItem.interactions.Insert (0, tempInteraction);
					break;

				case "Move to bottom":
					Undo.RecordObject (this, "Move interaction to bottom");
					selectedItem.interactions.Add (tempInteraction);
					selectedItem.interactions.RemoveAt (sideInteraction);
					break;
				}
			}
			
			EditorUtility.SetDirty (this);
			AssetDatabase.SaveAssets ();
			
			sideInteraction = -1;
		}
		
		
		private void PropertyCallback (object obj)
		{
			if (sideItem >= 0)
			{
				ResetFilter ();
				InvVar tempVar = invVars[sideItem];
				
				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (this, "Insert property");
					invVars.Insert (sideItem+1, new InvVar (GetIDArrayProperty ()));
					break;
					
				case "Delete":
					Undo.RecordObject (this, "Delete property");
					DeactivateAllInvVars ();
					invVars.RemoveAt (sideItem);
					break;
					
				case "Move up":
					Undo.RecordObject (this, "Move property up");
					invVars.RemoveAt (sideItem);
					invVars.Insert (sideItem-1, tempVar);
					break;
					
				case "Move down":
					Undo.RecordObject (this, "Move property down");
					invVars.RemoveAt (sideItem);
					invVars.Insert (sideItem+1, tempVar);
					break;

				case "Move to top":
					Undo.RecordObject (this, "Move property to top");
					invVars.RemoveAt (sideItem);
					invVars.Insert (0, tempVar);
					break;

				case "Move to bottom":
					Undo.RecordObject (this, "Move property to bottom");
					invVars.Add (tempVar);
					invVars.RemoveAt (sideItem);
					break;
				}
			}
			
			EditorUtility.SetDirty (this);
			AssetDatabase.SaveAssets ();
			
			sideItem = -1;
		}


		private void RecipeCallback (object obj)
		{
			if (sideItem >= 0)
			{
				Recipe tempRecipe = recipes[sideItem];
				
				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (this, "Insert recipe");
					recipes.Insert (sideItem+1, new Recipe (GetIDArrayRecipe ()));
					break;
					
				case "Delete":
					Undo.RecordObject (this, "Delete recipe");
					DeactivateAllRecipes ();
					recipes.RemoveAt (sideItem);
					break;
					
				case "Move up":
					Undo.RecordObject (this, "Move recipe up");
					recipes.RemoveAt (sideItem);
					recipes.Insert (sideItem-1, tempRecipe);
					break;
					
				case "Move down":
					Undo.RecordObject (this, "Move recipe down");
					recipes.RemoveAt (sideItem);
					recipes.Insert (sideItem+1, tempRecipe);
					break;

				case "Move to top":
					Undo.RecordObject (this, "Move recipe to top");
					recipes.RemoveAt (sideItem);
					recipes.Insert (0, tempRecipe);
					break;

				case "Move to bottom":
					Undo.RecordObject (this, "Move recipe to bottom");
					recipes.Add (tempRecipe);
					recipes.RemoveAt (sideItem);
					break;
				}
			}

			EditorUtility.SetDirty (this);
			AssetDatabase.SaveAssets ();
			
			sideItem = -1;
		}
		
		
		private void CraftingGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCraftingList = CustomGUILayout.ToggleHeader (showCraftingList, "Crafting recipes");
			if (showCraftingList)
			{
				if (items.Count == 0)
				{
					EditorGUILayout.HelpBox ("No inventory items defined!", MessageType.Info);
					EditorGUILayout.EndVertical ();
					return;
				}

				scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (Mathf.Min (recipes.Count * 21, ACEditorPrefs.MenuItemsBeforeScroll * 21) +5));
				foreach (Recipe recipe in recipes)
				{
					EditorGUILayout.BeginHorizontal ();
					
					string buttonLabel = recipe.label;
					if (buttonLabel == "")
					{
						buttonLabel = "(Untitled)";	
					}
					
					if (GUILayout.Toggle (selectedRecipe == recipe, recipe.id + ": " + buttonLabel, "Button"))
					{
						if (selectedRecipe != recipe)
						{
							DeactivateAllRecipes ();
							ActivateRecipe (recipe);
						}
					}

					if (GUILayout.Button ("", CustomStyles.IconCog))
					{
						SideMenu (recipe);
					}
					
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();

				EditorGUILayout.Space ();
				if (GUILayout.Button("Create new recipe"))
				{
					Undo.RecordObject (this, "Create inventory recipe");
					
					Recipe newRecipe = new Recipe (GetIDArrayRecipe ());
					recipes.Add (newRecipe);
					DeactivateAllRecipes ();
					ActivateRecipe (newRecipe);
				}
			}
			EditorGUILayout.EndVertical ();

			if (selectedRecipe != null && recipes.Contains (selectedRecipe))
			{
				string apiPrefix = "AC.KickStarter.inventoryManager.GetRecipe (" + selectedRecipe.id + ")";

				EditorGUILayout.Space ();
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				showCraftingProperties = CustomGUILayout.ToggleHeader (showCraftingProperties, "Recipe '" + selectedRecipe.label + "' properties");
				if (showCraftingProperties)
				{
					selectedRecipe.label = CustomGUILayout.TextField ("Name:", selectedRecipe.label, apiPrefix + ".label", "The recipe's editor name");
					
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Resulting item:", GUILayout.Width (146f));
					int i = GetArraySlot (selectedRecipe.resultID);
					i = CustomGUILayout.Popup (i, GetLabelList (), apiPrefix + ".resultID");
					selectedRecipe.resultID = items[i].id;
					EditorGUILayout.EndHorizontal ();
					
					selectedRecipe.useSpecificSlots = CustomGUILayout.Toggle ("Requires specific pattern?", selectedRecipe.useSpecificSlots, apiPrefix + ".useSpecificSlots", "If True, then the ingredients must be placed in specific slots within a Crafting menu element");
					selectedRecipe.actionListOnCreate = ActionListAssetMenu.AssetGUI ("ActionList when create:", selectedRecipe.actionListOnCreate, "Recipe_" + selectedRecipe.label + "_OnCreate", apiPrefix + ".actionListOnCreate", "The ActionList asset to run when the recipe is created ");

					selectedRecipe.onCreateRecipe = (OnCreateRecipe) CustomGUILayout.EnumPopup ("When click on result:", selectedRecipe.onCreateRecipe, apiPrefix + ".onCreateRecipe", "What happens when the recipe is created");
					if (selectedRecipe.onCreateRecipe == OnCreateRecipe.RunActionList)
					{
						selectedRecipe.invActionList = ActionListAssetMenu.AssetGUI ("ActionList when click:", selectedRecipe.invActionList, "Recipe_" + selectedRecipe.label + "_OnClick", apiPrefix + ".invActionList", "The ActionListAsset to run when clicking on the resulting item");
					}
					
					EditorGUILayout.Space ();
					EditorGUILayout.LabelField ("Ingredients",  CustomStyles.subHeader);
					
					foreach (Ingredient ingredient in selectedRecipe.ingredients)
					{
						int j = selectedRecipe.ingredients.IndexOf (ingredient);

						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.LabelField ("Ingredient:", GUILayout.Width (70f));
						i = GetArraySlot (ingredient.itemID);
						i = CustomGUILayout.Popup (i, GetLabelList (), apiPrefix + ".ingredients [" + j + "].itemID");
						ingredient.itemID = items[i].id;
						
						if (items[i].canCarryMultiple)
						{
							EditorGUILayout.LabelField ("Amount:", GUILayout.Width (50f));
							ingredient.amount = EditorGUILayout.IntField (ingredient.amount, GUILayout.Width (30f));
						}
						
						if (selectedRecipe.useSpecificSlots)
						{
							EditorGUILayout.LabelField ("Slot:", GUILayout.Width (30f));
							ingredient.slotNumber = EditorGUILayout.IntField (ingredient.slotNumber, GUILayout.Width (30f));
						}
						
						if (GUILayout.Button (deleteContent, GUILayout.Width (20f), GUILayout.Height (15f)))
						{
							Undo.RecordObject (this, "Delete ingredient");
							selectedRecipe.ingredients.Remove (ingredient);
							AssetDatabase.SaveAssets();
							break;
						}
						
						EditorGUILayout.EndHorizontal ();
					}
					
					if (GUILayout.Button("Add new ingredient"))
					{
						Undo.RecordObject (this, "Add recipe ingredient");
						
						Ingredient newIngredient = new Ingredient ();
						selectedRecipe.ingredients.Add (newIngredient);
					}
				}
				EditorGUILayout.EndVertical ();
			}
		}
		
		
		private List<int> GetIDList ()
		{
			List<int> idList = new List<int>();
			foreach (InvItem item in items)
			{
				idList.Add (item.id);
			}
			
			idList.Sort ();

			return idList;
		}
		
		
		private int[] GetIDArrayProperty ()
		{
			List<int> idArray = new List<int>();
			foreach (InvVar invVar in invVars)
			{
				idArray.Add (invVar.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}
		
		
		private int[] GetIDArrayRecipe ()
		{
			List<int> idArray = new List<int>();
			foreach (Recipe recipe in recipes)
			{
				idArray.Add (recipe.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}
		
		
		private int GetIconSlot (int _id)
		{
			int i = 0;
			foreach (CursorIcon icon in AdvGame.GetReferences ().cursorManager.cursorIcons)
			{
				if (icon.id == _id)
				{
					return i;
				}
				i++;
			}
			
			return 0;
		}
		
		
		private int GetArraySlot (int _id)
		{
			int i = 0;
			foreach (InvItem item in items)
			{
				if (item.id == _id)
				{
					return i;
				}
				i++;
			}
			
			return 0;
		}
		
		
		private string[] GetLabelList ()
		{
			List<string> labelList = new List<string>();
			foreach (InvItem _item in items)
			{
				labelList.Add (_item.label);
			}
			return labelList.ToArray ();
		}
		
		
		private int GetBinSlot (int _id)
		{
			int i = 0;
			foreach (InvBin bin in bins)
			{
				if (bin.id == _id)
				{
					return i;
				}
				i++;
			}
			
			return 0;
		}
		
		
		private int ChoosePlayerGUI (int playerID, string api, string tooltip)
		{
			List<string> labelList = new List<string>();
			int i = 0;
			int playerNumber = -1;
			
			if (AdvGame.GetReferences ().settingsManager.players.Count > 0)
			{
				foreach (PlayerPrefab playerPrefab in settingsManager.players)
				{
					if (playerPrefab.playerOb != null)
					{
						labelList.Add (playerPrefab.playerOb.name);
					}
					else
					{
						labelList.Add ("(Undefined prefab)");
					}
					
					// If a player has been removed, make sure selected player is still valid
					if (playerPrefab.ID == playerID)
					{
						playerNumber = i;
					}
					
					i++;
				}
				
				if (playerNumber == -1)
				{
					// Wasn't found (item was possibly deleted), so revert to zero
					ACDebug.LogWarning ("Previously chosen Player no longer exists!");
					
					playerNumber = 0;
					playerID = 0;
				}
				
				playerNumber = CustomGUILayout.Popup ("Item is carried by:", playerNumber, labelList.ToArray(), api, tooltip);
				playerID = settingsManager.players[playerNumber].ID;
			}
			return playerID;
		}
		
		
		private void SetTab (int tab)
		{
			showItemsTab = (tab == 0);
			showBinsTab = (tab == 1);
			showCraftingTab = (tab == 2);
			showPropertiesTab = (tab == 3);
			showDocumentsTab = (tab == 4);
			showObjectivesTab = (tab == 5);
		}


		private void FindReferences (InvItem item)
		{
			if (item == null) return;

			if (EditorUtility.DisplayDialog ("Search '" + item.label + "' references?", "The Editor will search assets, and active scenes listed in the Build Settings, for references to the inventory item.  The current scene will need to be saved and listed to be included in the search process. Continue?", "OK", "Cancel"))
			{
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					int totalNumReferences = 0;
					int thisNumReferences = 0;

					// Search other items
					foreach (InvItem otherItem in items)
					{
						if (otherItem.id != item.id)
						{
							foreach (int combineID in otherItem.combineID)
							{
								if (combineID == item.id)
								{
									totalNumReferences ++;
									ACDebug.Log ("Found reference to inventory item '" + item.label + "' in inventory item '" + otherItem.label + "'s list of combine interactions.");
								}
							}
						}
					}

					// Search crafting
					foreach (Recipe recipe in recipes)
					{
						thisNumReferences = 0;
						if (recipe.resultID == item.id)
						{
							thisNumReferences ++;
						}
						foreach (Ingredient ingredient in recipe.ingredients)
						{
							if (ingredient.itemID == item.id)
							{
								thisNumReferences ++;
							}
						}
						if (thisNumReferences > 0)
						{
							ACDebug.Log ("Found " + thisNumReferences  + " reference to inventory item '" + item.label + "' in recipe '" + recipe.label + "'.");
						}
						totalNumReferences += thisNumReferences;
					}

					// Search scenes
					string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
					string[] sceneFiles = AdvGame.GetSceneFiles ();
					foreach (string sceneFile in sceneFiles)
					{
						UnityVersionHandler.OpenScene (sceneFile);

						ActionList[] actionLists = FindObjectsOfType <ActionList>();
						foreach (ActionList actionList in actionLists)
						{
							totalNumReferences += actionList.GetInventoryReferences (item, sceneFile);
						}

						Container [] containers = FindObjectsOfType <Container>();
						foreach (Container container in containers)
						{
							thisNumReferences = container.GetInventoryReferences (item.id);
							if (thisNumReferences > 0)
							{
								totalNumReferences += thisNumReferences;
								ACDebug.Log ("Found " + thisNumReferences + " references to inventory item '" + item.label + "' in Container '" + container.name + "' in scene '" + sceneFile + "'", container);
							}
						}

						Conversation[] conversations = FindObjectsOfType <Conversation>();
						foreach (Conversation conversation in conversations)
						{
							thisNumReferences = conversation.GetInventoryReferences (item.id);
							if (thisNumReferences > 0)
							{
								totalNumReferences += thisNumReferences;
								ACDebug.Log ("Found " + thisNumReferences + " references to inventory item '" + item.label + "' in Conversation '" + conversation.name + "' in scene '" + sceneFile + "'", conversation);
							}
						}

						Hotspot[] hotspots = FindObjectsOfType <Hotspot>();
						foreach (Hotspot hotspot in hotspots)
						{
							thisNumReferences = hotspot.GetInventoryReferences (item.id);
							if (thisNumReferences > 0)
							{
								totalNumReferences += thisNumReferences;
								ACDebug.Log ("Found " + thisNumReferences + " references to inventory item '" + item.label + "' in Hotspot '" + hotspot.name + "'", hotspot);
							}
						}
					}

					UnityVersionHandler.OpenScene (originalScene);

					// Search assets
					if (AdvGame.GetReferences ().speechManager != null)
					{
						ActionListAsset[] allActionListAssets = AdvGame.GetReferences ().speechManager.GetAllActionListAssets ();
						foreach (ActionListAsset actionListAsset in allActionListAssets)
						{
							totalNumReferences += actionListAsset.GetInventoryReferences (item);
						}
					}

					EditorUtility.DisplayDialog ("Inventory search complete", "In total, found " + totalNumReferences + " references to inventory item '" + item.label + "' in the project.  Please see the Console window for full details.", "OK");
				}
			}
		}


		private void FindReferences (Document document)
		{
			if (document == null) return;

			if (EditorUtility.DisplayDialog ("Search '" + document.Title + "' references?", "The Editor will search assets, and active scenes listed in the Build Settings, for references to the document.  The current scene will need to be saved and listed to be included in the search process. Continue?", "OK", "Cancel"))
			{
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					int totalNumReferences = 0;

					// Search scenes
					string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
					string[] sceneFiles = AdvGame.GetSceneFiles ();

					foreach (string sceneFile in sceneFiles)
					{
						UnityVersionHandler.OpenScene (sceneFile);

						ActionList[] actionLists = FindObjectsOfType <ActionList>();
						foreach (ActionList actionList in actionLists)
						{
							totalNumReferences += actionList.GetDocumentReferences (document, sceneFile);
						}
					}

					UnityVersionHandler.OpenScene (originalScene);

					// Search assets
					if (AdvGame.GetReferences ().speechManager != null)
					{
						ActionListAsset[] allActionListAssets = AdvGame.GetReferences ().speechManager.GetAllActionListAssets ();
						foreach (ActionListAsset actionListAsset in allActionListAssets)
						{
							totalNumReferences += actionListAsset.GetDocumentReferences (document);
						}
					}

					EditorUtility.DisplayDialog ("Document search complete", "In total, found " + totalNumReferences + " references to document '" + document.Title + "' in the project.  Please see the Console window for full details.", "OK");
				}
			}
		}
		
		#endif


		/**
		 * <summary>Gets an inventory property.</summary>
		 * <param name = "ID">The ID number of the property to get</param>
		 * <returns>The inventory property.</returns>
		 */
		public InvVar GetProperty (int ID)
		{
			if (invVars.Count > 0 && ID >= 0)
			{
				foreach (InvVar var in invVars)
				{
					if (var.id == ID)
					{
						return var;
					}
				}
			}
			return null;
		}
		

		/**
		 * <summary>Gets a Document.</summary>
		 * <param name = "ID">The ID number of the Document to find</param>
		 * <returns>The Document</returns>
		*/
		public Document GetDocument (int ID)
		{
			foreach (Document document in documents)
			{
				if (document.ID == ID)
				{
					return document;
				}
			}

			return null;
		}


		/**
		 * <summary>Gets an Objective.</summary>
		 * <param name = "ID">The ID number of the Objective to find</param>
		 * <returns>The Objective</returns>
		*/
		public Objective GetObjective (int ID)
		{
			foreach (Objective objective in objectives)
			{
				if (objective.ID == ID)
				{
					return objective;
				}
			}

			return null;
		}

		
		/**
		 * <summary>Gets an inventory item's label.</summary>
		 * <param name = "_id">The ID number of the InvItem to find</param>
		 * <returns>The inventory item's label</returns>
		 */
		public string GetLabel (int _id)
		{
			string result = string.Empty;
			foreach (InvItem item in items)
			{
				if (item.id == _id)
				{
					result = item.label;
				}
			}
			
			return result;
		}
		
		
		/**
		 * <summary>Gets an inventory item.</summary>
		 * <param name = "_name">The name of the InvItem to find</param>
		 * <returns>The inventory item</returns>
		 */
		public InvItem GetItem (string _name)
		{
			foreach (InvItem item in items)
			{
				if (item.label == _name)
				{
					return item;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets an inventory item.</summary>
		 * <param name = "_id">The ID number of the InvItem to find</param>
		 * <returns>The inventory item</returns>
		 */
		public InvItem GetItem (int _id)
		{
			foreach (InvItem item in items)
			{
				if (item.id == _id)
				{
					return item;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets a Recipe.</summary>
		 * <param name = "_id">The ID number of the Recipe to find</param>
		 * <returns>The Recipe</returns>
		 */
		public Recipe GetRecipe (int _id)
		{
			foreach (Recipe recipe in recipes)
			{
				if (recipe.id == _id)
				{
					return recipe;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets an inventory category.</summary>
		 * <param name = "categoryID">The ID number of the inventory category to find</param>
		 * <returns>The inventory category</returns>
		 */
		public InvBin GetCategory (int categoryID)
		{
			foreach (InvBin bin in bins)
			{
				if (bin.id == categoryID)
				{
					return bin;
				}
			}
			return null;
		}
		
		
		/**
		 * <summary>Checks if multiple instances of an inventory item can exist.</summary>
		 * <param name = "itemID">The ID number of the InvItem to find</param>
		 * <returns>True if multiple instances of the inventory item can exist</returns>
		 */
		public bool CanCarryMultiple (int itemID)
		{
			foreach (InvItem item in items)
			{
				if (item.id == itemID)
				{
					return item.canCarryMultiple;
				}
			}
			
			return false;
		}


		/**
		 * <summary>Gets an array of all inventory items in a given category</summary>
		 * <param name = "categoryID">The ID number of the category in question</param>
		 * <returns>An array of all inventory items in the category</returns>
		 */
		public InvItem[] GetItemsInCategory (int categoryID)
		{
			List<InvItem> itemsList = new List<InvItem>();
			foreach (InvItem item in items)
			{
				if (item.binID == categoryID)
				{
					itemsList.Add (item);
				}
			}

			return itemsList.ToArray ();
		}


		/**
		 * <summary>Checks if a given Objective is per-Player, i.e. each player has their own instance of the Objective</summary>
		 * <param name = "objectiveID">The ID of the Objective</param>
		 * <returns>True if the Objective is per-Player</returns>
		 */
		public bool ObjectiveIsPerPlayer (int objectiveID)
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				foreach (Objective objective in objectives)
				{
					if (objective.ID == objectiveID)
					{
						return objective.perPlayer;
					}
				}
				ACDebug.LogWarning ("An Objective with ID=" + objectiveID + " could not be found.");
				return false;
			}
			return false;
		}
		
	}
	
}