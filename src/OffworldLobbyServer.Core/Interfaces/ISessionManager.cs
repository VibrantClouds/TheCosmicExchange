using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Models;

namespace OffworldLobbyServer.Core.Interfaces;

/// <summary>
/// Manages BlueBox session lifecycle and message queuing.
/// </summary>
public interface ISessionManager
{
	/// <summary>
	/// Creates a new session with a unique session ID.
	/// </summary>
	/// <param name="clientIP">Client IP address.</param>
	/// <returns>New session information.</returns>
	Task<SessionInfo> CreateSessionAsync(string clientIP = "");

	/// <summary>
	/// Gets a session by its ID.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <returns>Session info or null if not found.</returns>
	Task<SessionInfo?> GetSessionAsync(string sessionId);

	/// <summary>
	/// Updates session activity and validates session.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <returns>True if session exists and is valid.</returns>
	Task<bool> UpdateSessionActivityAsync(string sessionId);

	/// <summary>
	/// Associates a player ID with a session.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="playerId">Player to associate.</param>
	/// <returns>True if successful.</returns>
	Task<bool> SetSessionPlayerAsync(string sessionId, CombinedID playerId);

	/// <summary>
	/// Associates a session with a room.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="roomId">Room identifier.</param>
	/// <returns>True if successful.</returns>
	Task<bool> SetSessionRoomAsync(string sessionId, int roomId);

	/// <summary>
	/// Queues a message for a session.
	/// </summary>
	/// <param name="sessionId">Target session.</param>
	/// <param name="message">Base64-encoded message.</param>
	/// <returns>True if successfully queued.</returns>
	Task<bool> QueueMessageAsync(string sessionId, string message);

	/// <summary>
	/// Gets and removes the next message for a session (polling).
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <returns>Next message or null if queue is empty.</returns>
	Task<string?> PollMessageAsync(string sessionId);

	/// <summary>
	/// Disconnects a session and cleans up resources.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <returns>True if session was found and disconnected.</returns>
	Task<bool> DisconnectSessionAsync(string sessionId);

	/// <summary>
	/// Cleans up expired/timed-out sessions.
	/// </summary>
	/// <param name="timeoutMinutes">Session timeout in minutes.</param>
	/// <returns>Number of sessions cleaned up.</returns>
	Task<int> CleanupExpiredSessionsAsync(int timeoutMinutes = 30);

	/// <summary>
	/// Gets all active sessions.
	/// </summary>
	/// <returns>List of active sessions.</returns>
	Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync();

	/// <summary>
	/// Gets session count.
	/// </summary>
	/// <returns>Number of active sessions.</returns>
	Task<int> GetSessionCountAsync();

	/// <summary>
	/// Gets diagnostics information for monitoring.
	/// </summary>
	/// <returns>Session manager statistics.</returns>
	Task<object> GetDiagnosticsAsync();
}
