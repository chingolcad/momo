#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/**
	 * Provides an EditorWindow to manage the export of game text
	 */
	public class ExportWizardWindow : EditorWindow
	{

		private SpeechManager speechManager;
		private List<ExportColumn> exportColumns = new List<ExportColumn>();
		private int sideMenuIndex = -1;

		private bool filterByType = false;
		private bool filterByScene = false;
		private bool filterByText = false;
		private bool filterByTag = false;
		private string textFilter;
		private FilterSpeechLine filterSpeechLine = FilterSpeechLine.Text;
		private AC_TextType typeFilter = AC_TextType.Speech;
		private int tagFilter;
		private int sceneFilter;

		private bool doRowSorting = false;
		private enum RowSorting { ByID, ByType, ByScene, ByAssociatedObject, ByDescription };
		private RowSorting rowSorting = RowSorting.ByID;

		private Vector2 scroll;


		public void _Init (SpeechManager _speechManager, int forLanguage)
		{
			speechManager = _speechManager;

			exportColumns.Clear ();
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.Type));
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.DisplayText));
			if (speechManager != null && forLanguage > 0 && speechManager.languages != null && speechManager.languages.Count > forLanguage)
			{
				exportColumns.Add (new ExportColumn (forLanguage));
			}
		}


		/**
		 * <summary>Initialises the window.</summary>
		 */
		public static void Init (SpeechManager _speechManager, int forLanguage = 0)
		{
			if (_speechManager == null) return;

			ExportWizardWindow window = EditorWindow.GetWindowWithRect <ExportWizardWindow> (new Rect (0, 0, 350, 500), true, "Game text exporter", true);
			window.titleContent.text = "Game text exporter";
			window.position = new Rect (300, 200, 350, 500);
			window._Init (_speechManager, forLanguage);
		}
		
		
		private void OnGUI ()
		{
			if (speechManager == null)
			{
				EditorGUILayout.HelpBox ("A Speech Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			if (speechManager.lines == null || speechManager.lines.Count == 0)
			{
				EditorGUILayout.HelpBox ("No text is available to export - click 'Gather text' in your Speech Manager to find your game's text.", MessageType.Warning);
				return;
			}
			
			if (exportColumns == null)
			{
				exportColumns = new List<ExportColumn>();
				exportColumns.Add (new ExportColumn ());
			}

			EditorGUILayout.LabelField ("Text export wizard", CustomStyles.managerHeader);
			scroll = GUILayout.BeginScrollView (scroll);

			EditorGUILayout.HelpBox ("Choose the fields to export as columns below, then click 'Export CSV'.", MessageType.Info);
			EditorGUILayout.Space ();

			ShowColumnsGUI ();
			ShowRowsGUI ();
			ShowSortingGUI ();

			EditorGUILayout.Space ();
			if (exportColumns.Count == 0)
			{
				GUI.enabled = false;
			}
			if (GUILayout.Button ("Export CSV"))
			{
				Export ();
			}
			GUI.enabled = true;

			EditorGUILayout.Space ();
			GUILayout.EndScrollView ();
		}


		private void ShowColumnsGUI ()
		{
			string[] languagesArray = speechManager.languages.ToArray ();

			EditorGUILayout.LabelField ("Define columns",  CustomStyles.subHeader);
			EditorGUILayout.Space ();
			for (int i=0; i<exportColumns.Count; i++)
			{
				EditorGUILayout.BeginVertical ("Button");

				EditorGUILayout.BeginHorizontal ();
				exportColumns[i].ShowFieldSelector (i);

				if (GUILayout.Button ("", CustomStyles.IconCog))
				{
					SideMenu (i);
				}
				EditorGUILayout.EndHorizontal ();

				exportColumns[i].ShowLanguageSelector (languagesArray);

				EditorGUILayout.EndVertical ();
			}

			EditorGUILayout.Space ();
			if (GUILayout.Button ("Add new column"))
			{
				exportColumns.Add (new ExportColumn ());
			}

			EditorGUILayout.Space ();
		}


		private void ShowRowsGUI ()
		{
			EditorGUILayout.LabelField ("Row filtering", CustomStyles.subHeader);
			EditorGUILayout.Space ();

			filterByType = EditorGUILayout.Toggle ("Filter by type?", filterByType);
			if (filterByType)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("-> Limit to type:", GUILayout.Width (100f));
				typeFilter = (AC_TextType) EditorGUILayout.EnumPopup (typeFilter);
				EditorGUILayout.EndHorizontal ();
			}

			filterByScene = EditorGUILayout.Toggle ("Filter by scene?", filterByScene);
			if (filterByScene)
			{
				string[] sceneNames = speechManager.GetSceneNames ();
				if (sceneNames != null && sceneNames.Length > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("-> Limit to scene:", GUILayout.Width (100f));
					sceneFilter = EditorGUILayout.Popup (sceneFilter, sceneNames);
					EditorGUILayout.EndHorizontal ();
				}
			}

			filterByText = EditorGUILayout.Toggle ("Filter by text:", filterByText);
			if (filterByText)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("-> Limit to text:", GUILayout.Width (100f));
				filterSpeechLine = (FilterSpeechLine) EditorGUILayout.EnumPopup (filterSpeechLine, GUILayout.MaxWidth (100f));
				textFilter = EditorGUILayout.TextField (textFilter);
				EditorGUILayout.EndHorizontal ();
			}

			filterByTag = EditorGUILayout.Toggle ("Filter by tag:", filterByTag);
			if (filterByTag)
			{
				if (typeFilter == AC_TextType.Speech && speechManager.useSpeechTags && speechManager.speechTags != null && speechManager.speechTags.Count > 1)
				{
					if (tagFilter == -1)
					{
						tagFilter = 0;
					}

					List<string> tagNames = new List<string>();
					foreach (SpeechTag speechTag in speechManager.speechTags)
					{
						tagNames.Add (speechTag.label);
					}

					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("-> Limit by tag:", GUILayout.Width (65f));
					tagFilter = EditorGUILayout.Popup (tagFilter, tagNames.ToArray ());
					EditorGUILayout.EndHorizontal ();
				}
				else
				{
					tagFilter = -1;
					EditorGUILayout.HelpBox ("No tags defined - they can be created by clicking 'Edit speech tags' in the Speech Manager.", MessageType.Info);
				}
			}

			EditorGUILayout.Space ();
		}


		private void ShowSortingGUI ()
		{
			EditorGUILayout.LabelField ("Row sorting", CustomStyles.subHeader);
			EditorGUILayout.Space ();

			doRowSorting = EditorGUILayout.Toggle ("Apply row sorting?", doRowSorting);
			if (doRowSorting)
			{
				rowSorting = (RowSorting) EditorGUILayout.EnumPopup ("Sort rows:", rowSorting);
			}
		}


		private void SideMenu (int i)
		{
			GenericMenu menu = new GenericMenu ();

			sideMenuIndex = i;

			if (exportColumns.Count > 1)
			{
				if (i > 0)
				{
					menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, MenuCallback, "Move to top");
					menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, MenuCallback, "Move up");
				}
				if (i < (exportColumns.Count - 1))
				{
					menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, MenuCallback, "Move down");
					menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, MenuCallback, "Move to bottom");
				}
				menu.AddSeparator ("");
			}
			menu.AddItem (new GUIContent ("Delete"), false, MenuCallback, "Delete");
			menu.ShowAsContext ();
		}


		private void MenuCallback (object obj)
		{
			if (sideMenuIndex >= 0)
			{
				int i = sideMenuIndex;
				ExportColumn _column = exportColumns[i];

				switch (obj.ToString ())
				{
				case "Move to top":
					exportColumns.Remove (_column);
					exportColumns.Insert (0, _column);
					break;
					
				case "Move up":
					exportColumns.Remove (_column);
					exportColumns.Insert (i-1, _column);
					break;
					
				case "Move to bottom":
					exportColumns.Remove (_column);
					exportColumns.Insert (exportColumns.Count, _column);
					break;
					
				case "Move down":
					exportColumns.Remove (_column);
					exportColumns.Insert (i+1, _column);
					break;

				case "Delete":
					exportColumns.Remove (_column);
					break;
				}
			}
			
			sideMenuIndex = -1;
		}

		
		private void Export ()
		{
			#if UNITY_WEBPLAYER
			ACDebug.LogWarning ("Game text cannot be exported in WebPlayer mode - please switch platform and try again.");
			#else
			
			if (speechManager == null || exportColumns == null || exportColumns.Count == 0 || speechManager.lines == null || speechManager.lines.Count == 0) return;

			string suggestedFilename = string.Empty;
			if (AdvGame.GetReferences ().settingsManager)
			{
				suggestedFilename = AdvGame.GetReferences ().settingsManager.saveFileName + " - ";
			}
			suggestedFilename += "GameText.csv";
			
			string fileName = EditorUtility.SaveFilePanel ("Export game text", "Assets", suggestedFilename, "csv");
			if (fileName.Length == 0)
			{
				return;
			}

			string[] sceneNames = speechManager.GetSceneNames ();
			List<SpeechLine> exportLines = new List<SpeechLine>();
			foreach (SpeechLine line in speechManager.lines)
			{
				if (filterByType)
				{
					if (line.textType != typeFilter)
					{
						continue;
					}
				}
				if (filterByScene)
				{
					if (sceneNames != null && sceneNames.Length > sceneFilter)
					{
						string selectedScene = sceneNames[sceneFilter] + ".unity";
						string scenePlusExtension = (string.IsNullOrEmpty (line.scene)) ? string.Empty : (line.scene + ".unity");
						
						if ((string.IsNullOrEmpty (line.scene) && sceneFilter == 0)
						    || sceneFilter == 1
						    || (!string.IsNullOrEmpty (line.scene) && sceneFilter > 1 && line.scene.EndsWith (selectedScene))
						    || (!string.IsNullOrEmpty (line.scene) && sceneFilter > 1 && scenePlusExtension.EndsWith (selectedScene)))
						{}
						else
						{
							continue;
						}
					}
				}
				if (filterByText)
				{
					if (!line.Matches (textFilter, filterSpeechLine))
					{
						continue;
					}
				}
				if (filterByTag)
				{
					if (tagFilter == -1
						|| (tagFilter < speechManager.speechTags.Count && line.tagID == speechManager.speechTags[tagFilter].ID))
					{}
					else
					{
						continue;
					}
				}

				exportLines.Add (new SpeechLine (line));
			}

			if (doRowSorting)
			{
				switch (rowSorting)
				{
					case RowSorting.ByID:
						exportLines.Sort ((a, b) => a.lineID.CompareTo (b.lineID));
						break;

					case RowSorting.ByDescription:
						exportLines.Sort ((a, b) => string.Compare (a.description, b.description, System.StringComparison.Ordinal));
						break;

					case RowSorting.ByType:
						exportLines.Sort ((a, b) => string.Compare (a.textType.ToString (), b.textType.ToString (), System.StringComparison.Ordinal));
						break;

					case RowSorting.ByAssociatedObject:
						exportLines.Sort ((a, b) => string.Compare (a.owner, b.owner, System.StringComparison.Ordinal));
						break;

					case RowSorting.ByScene:
						exportLines.Sort ((a, b) => string.Compare (a.scene, b.scene, System.StringComparison.Ordinal));
						break;

					default:
						break;
				}
			}

			List<string[]> output = new List<string[]>();

			string[] languagesArray = speechManager.languages.ToArray ();
			List<string> headerList = new List<string>();
			headerList.Add ("ID");
			foreach (ExportColumn exportColumn in exportColumns)
			{
				headerList.Add (exportColumn.GetHeader (languagesArray));
			}
			output.Add (headerList.ToArray ());
		
			foreach (SpeechLine line in exportLines)
			{
				List<string> rowList = new List<string>();
				rowList.Add (line.lineID.ToString ());
				foreach (ExportColumn exportColumn in exportColumns)
				{
					string cellText = exportColumn.GetCellText (line);
					rowList.Add (cellText);
				}
				output.Add (rowList.ToArray ());
			}

			string fileContents = CSVReader.CreateCSVGrid (output);
			if (!string.IsNullOrEmpty (fileContents) && Serializer.SaveFile (fileName, fileContents))
			{
				int numLines = exportLines.Count;
				ACDebug.Log (numLines.ToString () + " line" + ((numLines != 1) ? "s" : string.Empty) + " exported.");
			}

			#endif
		}


		private class ExportColumn
		{

			public enum ColumnType { DisplayText, Type, AssociatedObject, Scene, Description, TagID, TagName, SpeechOrder, AudioFilePresence };
			private ColumnType columnType;
			private int language;


			public ExportColumn ()
			{
				columnType = ColumnType.DisplayText;
				language = 0;
			}


			public ExportColumn (ColumnType _columnType)
			{
				columnType = _columnType;
				language = 0;
			}


			public ExportColumn (int _language)
			{
				columnType = ColumnType.DisplayText;
				language = _language;
			}


			public void ShowFieldSelector (int i)
			{
				columnType = (ColumnType) EditorGUILayout.EnumPopup ("Column #" + (i+1).ToString (), columnType);

				if (columnType == ColumnType.AudioFilePresence && KickStarter.speechManager.useAssetBundles && KickStarter.speechManager.autoNameSpeechFiles)
				{
					EditorGUILayout.HelpBox ("Presence from asset bundles cannot be determined in Edit mode - files will be searched for in Resources folders.", MessageType.Warning);
				}
			}


			public void ShowLanguageSelector (string[] languages)
			{
				if (columnType == ColumnType.DisplayText)
				{
					language = EditorGUILayout.Popup ("Language:", language, languages);
				}
			}


			public string GetHeader (string[] languages)
			{
				if (columnType == ColumnType.DisplayText)
				{
					if (language > 0)
					{
						if (languages != null && languages.Length > language)
						{
							return languages[language];
						}
						return ("Invalid language");
					}
					return ("Original text");
				}
				return columnType.ToString ();
			}


			public string GetCellText (SpeechLine speechLine)
			{
				string cellText = " ";

				switch (columnType)
				{
					case ColumnType.DisplayText:
						if (language > 0)
						{
							int translation = language-1;
							if (speechLine.translationText != null && speechLine.translationText.Count > translation)
							{
								cellText = speechLine.translationText[translation];
							}
						}
						else
						{
							cellText = speechLine.text;
						}
						break;

					case ColumnType.Type:
						cellText = speechLine.textType.ToString ();
						break;

					case ColumnType.AssociatedObject:
						if (speechLine.isPlayer && string.IsNullOrEmpty (speechLine.owner) && speechLine.textType == AC_TextType.Speech)
						{
							cellText = "Player";
						}
						else
						{
							cellText = speechLine.owner;
						}
						break;

					case ColumnType.Scene:
						cellText = speechLine.scene;
						break;

					case ColumnType.Description:
						cellText = speechLine.description;
						break;

					case ColumnType.TagID:
						cellText = speechLine.tagID.ToString ();
						break;

					case ColumnType.TagName:
						SpeechTag speechTag = KickStarter.speechManager.GetSpeechTag (speechLine.tagID);
						cellText = (speechTag != null) ? speechTag.label : "";
						break;

					case ColumnType.SpeechOrder:
						cellText = speechLine.OrderIdentifier;
						if (cellText == "-0001")
						{
							cellText = string.Empty;
						}
						break;

					case ColumnType.AudioFilePresence:
						if (speechLine.textType == AC_TextType.Speech)
						{
							bool hasAllAudio = speechLine.HasAllAudio ();
							if (hasAllAudio)
							{
								cellText = "Has all audio";
							}
							else
							{
								if (speechLine.HasAudio (0))
								{
									string missingLabel = "Missing ";
									for (int i=1; i<KickStarter.speechManager.languages.Count; i++)
									{
										if (!speechLine.HasAudio (i))
										{
											missingLabel += KickStarter.speechManager.languages[i] + ", ";
										}
									}
									if (missingLabel.EndsWith (", "))
									{
										missingLabel = missingLabel.Substring (0, missingLabel.Length-2);
									}
									cellText = missingLabel;
								}
								else
								{
									cellText = "Missing main audio";
								}
							}
						}
						break;
				}


				if (string.IsNullOrEmpty (cellText))
				{
					cellText = " ";
				}
				return RemoveLineBreaks (cellText);
			}


			private string RemoveLineBreaks (string text)
			{
				if (text.Length == 0) return " ";
	            //text = text.Replace("\r\n", "[break]").Replace("\n", "[break]");
				text = text.Replace("\r\n", "[break]");
				text = text.Replace("\n", "[break]");
				text = text.Replace("\r", "[break]");
	            return text;
	        }

		}
		
		
	}
	
}

#endif