namespace AC
{

	/**
	 * An interface that is called when saving and loading save files, allowing custom global save data to be injected.
	 */
	public interface ISave
	{

		/**
		 * <summary>Called before the saving occurs.</summary>
		 */
		void PreSave ();

		/**
		 * <summary>Called after loading occurs.</summary>
		 */
		void PostLoad ();

	}


	/**
	 * An interface that is called when saving and loading options data, allowing custom PlayerPrefs to be injected.
	 */
	public interface ISaveOptions
	{

		/**
		 * <summary>Called before options are saved.</summary>
		 */
		void PreSaveOptions ();

		/**
		 * <summary>Called after options are loaded.</summary>
		 */
		void PostLoadOptions ();
		
	}

}