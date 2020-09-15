/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionMenuSelect.cs"
 * 
 *	This action selects an element within an enabled menu.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionMenuSelect : Action
	{
		
		public string menuName;
		public int menuNameParameterID = -1;
		public string elementName;
		public int elementNameParameterID = -1;

		public int slotIndex;
		public int slotIndexParameterID = -1;

		public bool selectFirstVisible = false;


		public ActionMenuSelect ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Menu;
			title = "Select element";
			description = "Selects an element within an enabled menu.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			menuName = AssignString (parameters, menuNameParameterID, menuName);
			elementName = AssignString (parameters, elementNameParameterID, elementName);
			slotIndex = AssignInteger (parameters, slotIndexParameterID, slotIndex);
		}

		
		public override float Run ()
		{
			if (!string.IsNullOrEmpty (menuName))
			{
				Menu menu = PlayerMenus.GetMenuWithName (menuName);
				if (menu != null)
				{
					if (selectFirstVisible)
					{
						GameObject elementObject = menu.GetObjectToSelect ();
						if (elementObject != null)
						{
							KickStarter.playerMenus.SelectUIElement (elementObject);
						}
					}
					else if (!string.IsNullOrEmpty (elementName))
					{
						MenuElement menuElement = PlayerMenus.GetElementWithName (menuName, elementName);
						if (menuElement != null)
						{
							menu.Select (elementName, slotIndex);
						}
					}
				}
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			menuNameParameterID = Action.ChooseParameterGUI ("Menu containing element:", parameters, menuNameParameterID, ParameterType.String);
			if (menuNameParameterID < 0)
			{
				menuName = EditorGUILayout.TextField ("Menu containing element:", menuName);
			}

			selectFirstVisible = EditorGUILayout.Toggle ("Select first-visible element?", selectFirstVisible);
			if (!selectFirstVisible)
			{
				elementNameParameterID = Action.ChooseParameterGUI ("Element to select:", parameters, elementNameParameterID, ParameterType.String);
				if (elementNameParameterID < 0)
				{
					elementName = EditorGUILayout.TextField ("Element to select:", elementName);
				}

				slotIndexParameterID = Action.ChooseParameterGUI ("Slot index (optional):", parameters, slotIndexParameterID, ParameterType.Integer);
				if (slotIndexParameterID < 0)
				{
					slotIndex = EditorGUILayout.IntField ("Slot index (optional):", slotIndex);
				}
			}

			AfterRunningOption ();
		}
		
		
		public override string SetLabel ()
		{
			if (!string.IsNullOrEmpty (menuName) && !string.IsNullOrEmpty (elementName))
			{
				return menuName + " - " + elementName;
			}
			return string.Empty;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Menu: Select element' Action</summary>
		 * <param name = "menuName">The name of the menu to select</param>
		 * <param name = "elementName">The name of the element inside the menu to select. If left blank, the first-available element will be selected</param>
		 * <param name = "slotIndex">The index number of the slot to select, if the element supports multiple slots</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuSelect CreateNew (string menuName, string elementName = "", int slotIndex = 0)
		{
			ActionMenuSelect newAction = (ActionMenuSelect) CreateInstance <ActionMenuSelect>();
			newAction.menuName = menuName;
			newAction.elementName = elementName;
			newAction.selectFirstVisible = string.IsNullOrEmpty (elementName);
			newAction.slotIndex = slotIndex;
			return newAction;
		}
		
	}
	
}
