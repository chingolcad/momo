/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionComment.cs"
 * 
 *	This action simply displays a comment in the Editor / Inspector.
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
	public class ActionComment : Action
	{
		
		public string commentText = "";

		protected enum ACLogType { No, AsInfo, AsWarning, AsError };
		[SerializeField] protected ACLogType acLogType = ACLogType.AsInfo;
		protected string convertedText;
		
		
		public ActionComment ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ActionList;
			title = "Comment";
			description = "Prints a comment for debug purposes.";
		}


		public override void AssignValues (System.Collections.Generic.List<ActionParameter> parameters)
		{
			convertedText = AdvGame.ConvertTokens (commentText, 0, null, parameters);
		}


		public override float Run ()
		{
			if (acLogType != ACLogType.No && !string.IsNullOrEmpty (convertedText))
			{
				if (acLogType == ACLogType.AsInfo)
				{
					Log (convertedText);
				}
				else if (acLogType == ACLogType.AsWarning)
				{
					LogWarning (convertedText);
				}
				else if (acLogType == ACLogType.AsError)
				{
					LogError (convertedText);
				}
			}
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			EditorStyles.textField.wordWrap = true;
			commentText = EditorGUILayout.TextArea (commentText, GUILayout.MaxWidth (280f));

			acLogType = (ACLogType) EditorGUILayout.EnumPopup ("Display in Console?", acLogType);

			AfterRunningOption ();
		}
		
		
		public override string SetLabel ()
		{
			if (!string.IsNullOrEmpty (commentText))
			{
				int i = commentText.IndexOf ("\n");
				if (i > 0)
				{
					return commentText.Substring (0, i);
				}
				return commentText;
			}
			return string.Empty;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			string updatedCommentText = AdvGame.ConvertLocalVariableTokenToGlobal (commentText, oldLocalID, newGlobalID);
			if (commentText != updatedCommentText)
			{
				wasAmended = true;
				commentText = updatedCommentText;
			}
			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool isAffected = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			string updatedCommentText = AdvGame.ConvertGlobalVariableTokenToLocal (commentText, oldGlobalID, newLocalID);
			if (commentText != updatedCommentText)
			{
				isAffected = true;
				if (isCorrectScene)
				{
					commentText = updatedCommentText;
				}
			}
			return isAffected;
		}


		public override int GetVariableReferences (List<ActionParameter> parameters, VariableLocation location, int varID, Variables _variables)
		{
			int thisCount = 0;
			string tokenText = AdvGame.GetVariableTokenText (location, varID);

			if (!string.IsNullOrEmpty (tokenText) && commentText.Contains (tokenText))
			{
				thisCount ++;
			}
			thisCount += base.GetVariableReferences (parameters, location, varID, _variables);
			return thisCount;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'ActionList: Pause' Action with key variables already set.</summary>
		 * <param name = "text">The text to display in the Console</param>
		 * <param name = "displayAsWarning">If True, the text will display as a Warning. Otherwise, it will display as Info</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionComment CreateNew (string text, bool displayAsWarning = false)
		{
			ActionComment newAction = (ActionComment) CreateInstance <ActionComment>();
			newAction.commentText = text;
			newAction.acLogType = (displayAsWarning) ? ACLogType.AsWarning : ACLogType.AsInfo;
			return newAction;
		}
		
	}
	
}