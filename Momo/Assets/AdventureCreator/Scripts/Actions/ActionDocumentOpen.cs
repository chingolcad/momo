/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionDocumentOpen.cs"
 * 
 *	This action makes a Document active for display in a Menu.
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
	public class ActionDocumentOpen : Action
	{

		public int documentID;
		public int parameterID = -1;
		public bool addToCollection = false;

		
		public ActionDocumentOpen ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Document;
			title = "Open";
			description = "Opens a document, causing any Menu of 'Appear type: On View Document' to open.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			documentID = AssignDocumentID (parameters, parameterID, documentID);
		}


		public override float Run ()
		{
			Document document = KickStarter.inventoryManager.GetDocument (documentID);

			if (document != null)
			{
				if (addToCollection)
				{
					KickStarter.runtimeDocuments.AddToCollection (document);
				}
				KickStarter.runtimeDocuments.OpenDocument (document);
			}

			return 0f;
		}
		

		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Document:", parameters, parameterID, ParameterType.Document);
			if (parameterID < 0)
			{
				documentID = InventoryManager.DocumentSelectorList (documentID);
			}
			addToCollection = EditorGUILayout.Toggle ("Add to collection?", addToCollection);

			AfterRunningOption ();
		}


		public override string SetLabel ()
		{
			Document document = KickStarter.inventoryManager.GetDocument (documentID);
			if (document != null)
			{
				return document.Title;
			}
			return string.Empty;
		}


		public override int GetDocumentReferences (List<ActionParameter> parameters, int _docID)
		{
			if (parameterID < 0 && documentID == _docID)
			{
				return 1;
			}
			return 0;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Document: Open' Action</summary>
		 * <param name = "documentID">The ID number of the document to open</param>
		 * <param name = "addToCollection">If True, the document will be added to the player's collection if not there already</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionDocumentOpen CreateNew (int documentID, bool addToCollection)
		{
			ActionDocumentOpen newAction = (ActionDocumentOpen) CreateInstance <ActionDocumentOpen>();
			newAction.documentID = documentID;
			newAction.addToCollection = addToCollection;
			return newAction;
		}
		
	}

}