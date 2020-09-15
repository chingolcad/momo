/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Variables.cs"
 * 
 *	This component allows variables to be stored on a GameObject.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/** This component allows variables to be stored on a GameObject. */
	[AddComponentMenu("Adventure Creator/Logic/Variables")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_variables.html")]
	public class Variables : MonoBehaviour, ITranslatable
	{

		#region _Variables

		/** The List of variables. */
		public List<GVar> vars = new List<GVar>();

		#if UNITY_EDITOR
		public string filter;
		public Vector2 scrollPos;
		#endif

		#endregion


		#region UnityStandards

		protected void Start ()
		{
			if (KickStarter.runtimeLanguages)
			{
				foreach (GVar _var in vars)
				{
					_var.CreateRuntimeTranslations ();
				}
			}

			RememberVariables rememberVariables = GetComponent <RememberVariables>();
			if (rememberVariables != null && rememberVariables.LoadedData) return;

			foreach (GVar var in vars)
			{
				if (var.updateLinkOnStart)
				{
					var.Download (VariableLocation.Component);
				}
				else
				{
					var.Upload (VariableLocation.Component);
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Gets a variable with a particular ID value</summary>
		 * <param name = "_id">The ID number of the variable to get</param>
		 * <returns>The variable with the requested ID value, or null if not found</returns>
		 */
		public GVar GetVariable (int _id)
		{
			foreach (GVar _var in vars)
			{
				if (_var.id == _id)
				{
					_var.Download (VariableLocation.Component, this);
					return _var;
				}
			}
			
			return null;
		}


		/**
		 * <summary>Gets a variable with a particular ID value and type</summary>
		 * <param name = "_id">The ID number of the variable to get</param>
		 * <param name = "_type">The type of variable to get</param>
		 * <returns>The variable with the requested ID value and type, or null if not found</returns>
		 */
		public GVar GetVariable (int _id, VariableType _type)
		{
			GVar _var = GetVariable (_id);
			if (_var.type == _type)
			{
				_var.Download (VariableLocation.Component, this);
				return _var;
			}
			return null;
		}


		/**
		 * <summary>Gets a variable with a particular ID value</summary>
		 * <param name = "_name">The name of the variable to get</param>
		 * <returns>The variable with the requested ID value, or null if not found</returns>
		 */
		public GVar GetVariable (string _name)
		{
			foreach (GVar _var in vars)
			{
				if (_var.label == _name)
				{
					_var.Download (VariableLocation.Component, this);
					return _var;
				}
			}
			
			return null;
		}


		/**
		 * <summary>Gets a variable with a particular ID value and type</summary>
		 * <param name = "_name">The name of the variable to get</param>
		 * <param name = "_type">The type of variable to get</param>
		 * <returns>The variable with the requested ID value and type, or null if not found</returns>
		 */
		public GVar GetVariable (string _name, VariableType _type)
		{
			GVar _var = GetVariable (_name);
			if (_var != null && _var.type == _type)
			{
				_var.Download (VariableLocation.Component, this);
				return _var;
			}
			return null;
		}

		#endregion


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			return vars[index].GetTranslatableString (index);
		}


		public int GetTranslationID (int index)
		{
			return vars[index].GetTranslationID (index);
		}


		#if UNITY_EDITOR

		public int GetNumTranslatables ()
		{
			if (vars != null)
			{
				return vars.Count;
			}
			return 0;
		}


		public bool HasExistingTranslation (int index)
		{
			return vars[index].HasExistingTranslation (index);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			vars[index].SetTranslationID (index, _lineID);
		}


		public string GetOwner (int index)
		{
			return string.Empty;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return vars[index].GetTranslationType (index);
		}


		public bool CanTranslate (int index)
		{
			return vars[index].CanTranslate (index);
		}

		#endif

		#endregion

	}

}