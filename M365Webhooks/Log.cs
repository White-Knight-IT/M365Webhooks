namespace M365Webhooks
{
	/// <summary>
	/// A static class that represents the single log file to be used by the entire program
	/// </summary>
	public static class Log
	{
		private static TextWriter _textWriter = new StreamWriter(Configuration.LogPath, true);

		/// <summary>
		/// Determines if logging is enabled based on a log path being supplied in config.json
		/// </summary>
		/// <returns>Logging enabled true/false</returns>
		private static bool LoggingEnabled()
		{
			//Only deem logging enabled if a log path is specified
			if (!string.IsNullOrEmpty(Configuration.LogPath) && !string.IsNullOrWhiteSpace(Configuration.LogPath))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Opens the log file and writes a line to it, then closes the log again
		/// </summary>
		/// <param name="line">The line to write to the log</param>
		/// <param name="writeToConsole">Write to console or not in addition to log file</param>
		public static void WriteLine(string line, bool writeToConsole=true)
		{
			if (writeToConsole)
			{
				Console.WriteLine(LogTimeStamp() + line + "\n");
			}

			if (LoggingEnabled())
			{
				_textWriter.WriteLine(LogTimeStamp() + line + "\n");
			}
		}

		/// <summary>
		/// Flushes any contents still to be wrote to the file to disk and closes the log
		/// </summary>
		public static void CloseLog()
		{
			_textWriter.Flush();
			_textWriter.Close();
		}

		/// <summary>
		/// Creates a time stamp to append on Log/Console entries
		/// </summary>
		/// <returns>String timestamp in [Date - Time]: format</returns>
		public static string LogTimeStamp()
        {
			return "[" + DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString() + "]: ";
		}

	}
}


