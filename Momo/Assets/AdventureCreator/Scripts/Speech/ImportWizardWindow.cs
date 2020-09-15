#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/**
	 * Provides an EditorWindow to manage the import of game text
	 */
	public class ImportWizardWindow : EditorWindow
	{

		private SpeechManager speechManager;
		private string[,] csvData;
		private int numRows;
		private int numCols;
		private bool failedImport = false;

		private Vector2 scroll;
		private List<ImportColumn> importColumns = new List<ImportColumn>();
	

		public void _Init (SpeechManager _speechManager, string[,] _csvData, int forLanguage)
		{
			speechManager = _speechManager;
			csvData = _csvData;
			failedImport = false;

			if (speechManager != null && csvData != null)
			{
				numCols = csvData.GetLength (0)-1;
				numRows = csvData.GetLength (1);

				if (numRows < 2 || numCols < 1)
				{
					failedImport = true;
					return;
				}

				importColumns = new List<ImportColumn>();
				for (int col=0; col<numCols; col++)
				{
					importColumns.Add (new ImportColumn (csvData [col, 0]));
				}

				if (forLanguage > 0 && speechManager.languages != null && speechManager.languages.Count > forLanguage)
				{
					if (importColumns.Count > 1)
					{
						importColumns [importColumns.Count-1].SetToTranslation (forLanguage-1);
					}
				}
			}
			else
			{
				numRows = numCols = 0;
			}
		}


		/**
		 * <summary>Initialises the window.</summary>
		 */
		public static void Init (SpeechManager _speechManager, string[,] _csvData, int _forLanguage = 0)
		{
			if (_speechManager == null) return;

			ImportWizardWindow window = EditorWindow.GetWindowWithRect <ImportWizardWindow> (new Rect (0, 0, 350, 500), true, "Game text importer", true);

			window.titleContent.text = "Game text importer";
			window.position = new Rect (300, 200, 350, 500);
			window._Init (_speechManager, _csvData, _forLanguage);
		}
		
		
		private void OnGUI ()
		{
			EditorGUILayout.LabelField ("Text import wizard", CustomStyles.managerHeader);

			if (speechManager == null)
			{
				EditorGUILayout.HelpBox ("A Speech Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			if (speechManager.lines == null || speechManager.lines.Count == 0)
			{
				EditorGUILayout.HelpBox ("No text is available to import - click 'Gather text' in your Speech Manager to find your game's text.", MessageType.Warning);
				return;
			}

			if (failedImport || numRows == 0 || numCols == 0 || importColumns == null)
			{
				EditorGUILayout.HelpBox ("There was an error processing the imported file - please check that the format is correct. The correct format can be shown by exporting a CSV file with the Speech Manager.", MessageType.Warning);
				return;
			}

			scroll = GUILayout.BeginScrollView (scroll);

			EditorGUILayout.LabelField ("Detected columns", CustomStyles.subHeader);
			EditorGUILayout.Space ();

			List<string> translations = new List<string>();
			if (speechManager.languages != null && speechManager.languages.Count > 1)
			{
				for (int i=1; i<speechManager.languages.Count; i++)
				{
					translations.Add (speechManager.languages[i]);
				}
			}
			string[] translationsArray = translations.ToArray ();

			EditorGUILayout.HelpBox ("Number of rows: " + (numRows-1).ToString () + "\r\n" + "Number of columns: " + numCols.ToString () + "\r\n" +
									 "Choose the columns to import below, then click 'Import CSV'.", MessageType.Info);
			EditorGUILayout.Space ();

			for (int i=0; i<importColumns.Count; i++)
			{
				importColumns[i].ShowGUI (i, translationsArray);
			}

			EditorGUILayout.Space ();
			if (GUILayout.Button ("Import CSV"))
			{
				Import ();
			}

			EditorGUILayout.Space ();
			EditorGUILayout.EndScrollView ();
		}


		private void Import ()
		{
			#if UNITY_WEBPLAYER
			ACDebug.LogWarning ("Game text cannot be exported in WebPlayer mode - please switch platform and try again.");
			#else
			
			if (speechManager == null || importColumns == null || importColumns.Count == 0 || speechManager.lines == null || speechManager.lines.Count == 0) return;
			int lineID = -1;

			int numUpdated = 0;
			for (int row = 1; row < numRows; row ++)
			{
				if (csvData [0, row] != null && csvData [0, row].Length > 0)
				{
					lineID = -1;
					if (int.TryParse (csvData [0, row], out lineID))
					{
						SpeechLine speechLine = speechManager.GetLine (lineID);

						if (speechLine != null)
						{
							for (int col = 0; col < numCols; col ++)
							{
								if (importColumns.Count > col)
								{
									string cellData = csvData [col, row];
									if (importColumns[col].Process (cellData, speechLine))
									{
										numUpdated ++;
									}
								}
							}
						}
					}
					else
					{
						ACDebug.LogWarning ("Error importing translation (ID:" + csvData [0, row] + ") on row #" + row.ToString () + ".");
					}
				}
			}
	
			EditorUtility.SetDirty (speechManager);

			ACDebug.Log ((numRows-2).ToString () + " line(s) imported, " + numUpdated.ToString () + " line(s) updated.");
			this.Close ();
			#endif
		}


		private class ImportColumn
		{

			private string header;
			private enum ImportColumnType { DoNotImport, ImportAsTranslation, ImportAsDescription };
			private ImportColumnType importColumnType;
			private int translationIndex;


			public ImportColumn (string _header)
			{
				header = _header;
				importColumnType = ImportColumnType.DoNotImport;
				translationIndex = 0;
			}


			public void SetToTranslation (int _translationIndex)
			{
				importColumnType = ImportColumnType.ImportAsTranslation;
				translationIndex = _translationIndex;
			}


			public void ShowGUI (int i, string[] translations)
			{
				EditorGUILayout.BeginVertical ("Button");
				GUILayout.Label ("Column # : " + header);

				if (i > 0)
				{
					importColumnType = (ImportColumnType) EditorGUILayout.EnumPopup ("Import rule:", importColumnType);
					if (importColumnType == ImportColumnType.ImportAsTranslation)
					{
						if (translations == null || translations.Length == 0)
						{
							EditorGUILayout.HelpBox ("No translations found!", MessageType.Warning);
						}
						else
						{
							translationIndex = EditorGUILayout.Popup ("Translation:", translationIndex, translations);
						}
					}
				}
				EditorGUILayout.EndVertical ();
			}


			public bool Process (string cellText, SpeechLine speechLine)
			{
				if (cellText == null) return false;

				cellText = AddLineBreaks (cellText);
				//cellText = cellText.Replace (CSVReader.csvTemp, CSVReader.csvComma);

				if (importColumnType != ImportColumnType.DoNotImport)
				{
					if (importColumnType == ImportColumnType.ImportAsDescription)
					{
						if (speechLine.description != cellText)
						{
							speechLine.description = cellText;
							return true;
						}
					}
					else if (importColumnType == ImportColumnType.ImportAsTranslation)
					{
						if (speechLine.translationText != null && speechLine.translationText.Count > translationIndex)
						{
							if (speechLine.translationText [translationIndex] != cellText)
							{
								speechLine.translationText [translationIndex] = cellText;
								return true;
							}
						}
					}
				}

				return false;
			}


			private string AddLineBreaks (string text)
			{
	            text = text.Replace ("[break]", "\n");
	            return text;
	        }
	
		}
		
		
	}
	
}

#endif