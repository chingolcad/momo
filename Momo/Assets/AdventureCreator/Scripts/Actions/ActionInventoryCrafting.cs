/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionInventoryCrafting.cs"
 * 
 *	This action is used to perform crafting-related tasks.
 * 
 */

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionInventoryCrafting : Action
	{

		public enum ActionCraftingMethod { ClearRecipe, CreateRecipe };
		public ActionCraftingMethod craftingMethod;


		public ActionInventoryCrafting ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Inventory;
			title = "Crafting";
			description = "Either clears the current arrangement of crafting ingredients, or evaluates them to create an appropriate result (if this is not done automatically by the recipe itself).";
		}

		
		public override float Run ()
		{
			if (craftingMethod == ActionCraftingMethod.ClearRecipe)
			{
				KickStarter.runtimeInventory.RemoveRecipes ();
			}
			else if (craftingMethod == ActionCraftingMethod.CreateRecipe)
			{
				PlayerMenus.CreateRecipe ();
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			craftingMethod = (ActionCraftingMethod) EditorGUILayout.EnumPopup ("Method:", craftingMethod);

			AfterRunningOption ();
		}
		
		
		public override string SetLabel ()
		{
			if (craftingMethod == ActionCraftingMethod.CreateRecipe)
			{
				return "Create recipe";
			}
			else if (craftingMethod == ActionCraftingMethod.ClearRecipe)
			{
				return "Clear recipe";
			}
			return string.Empty;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Crafting' Action</summary>
		 * <param name = "craftingMethod">The crafting method to perform</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryCrafting CreateNew (ActionCraftingMethod craftingMethod)
		{
			ActionInventoryCrafting newAction = (ActionInventoryCrafting) CreateInstance <ActionInventoryCrafting>();
			newAction.craftingMethod = craftingMethod;
			return newAction;
		}
	}

}