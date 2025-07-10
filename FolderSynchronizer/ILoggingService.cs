namespace FolderSynchronizer
{
	/// <summary>
	/// Provides an interface for logging informational and error messages.
	/// </summary>
	public interface ILoggingService : IDisposable
	{
		/// <summary>
		/// Logs a general informational message.
		/// </summary>
		/// <param name="message">The message to log.</param>
		public void Log(string message);
		/// <summary>
		/// Logs an error message with an associated exception.
		/// </summary>
		/// <param name="message">The error message to log.</param>
		/// <param name="e">The exception related to the error.</param>
		public void LogError(string message, Exception e);
	}
}
