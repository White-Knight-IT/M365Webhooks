
namespace M365Webhooks
{
	/// <summary>
    /// A static class that represents the single log file to be used by the entire program
    /// </summary>
	public static class Log
	{
		private static StreamWriter? _logFileStream = null;

		/// <summary>
        /// Opens the log file and writes a line to it, then closes the log again
        /// </summary>
        /// <param name="line">The line to write to the log</param>
		public static async void WriteLine(string line)
        {
			if(LoggingEnabled())
            {
				_logFileStream = new StreamWriter(Configuration.logPath, true);
				await _logFileStream.WriteLineAsync("["+DateTime.Now.ToShortDateString()+" - "+DateTime.Now.ToLongTimeString()+"]: "+line+"\n");
				CloseLog();
            }
        }

		/// <summary>
        /// Flushes any contents still to be wrote to the file to disk and closes the log
        /// </summary>
		public static async void CloseLog()
        {
			await _logFileStream.FlushAsync();
			_logFileStream.Close();
        }

		/// <summary>
        /// Determines if logging is enabled based on a log path being supplied in config.json
        /// </summary>
        /// <returns>Logging enabled true/false</returns>
		private static bool LoggingEnabled()
		{
			//Only deem logging enabled if a log path is specified
			if (!string.IsNullOrEmpty(Configuration.logPath) && !string.IsNullOrWhiteSpace(Configuration.logPath))
			{
				return true;
			}

			return false;
		}

	}
}

