using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Models;

namespace OffworldLobbyServer.Infrastructure.Services;

/// <summary>
/// In-memory implementation of session management
/// Uses thread-safe ConcurrentDictionary for session storage
/// Provides cryptographically secure session ID generation.
/// </summary>
public class InMemorySessionManager : ISessionManager
{
	private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new ();
	private readonly ILogger<InMemorySessionManager> _logger;

	public InMemorySessionManager(ILogger<InMemorySessionManager> logger)
	{
		_logger = logger;
		_logger.LogInformation("InMemorySessionManager initialized");
	}

	/// <summary>
	/// Creates a new session with a cryptographically secure session ID.
	/// </summary>
	/// <param name="clientIP">Client IP address.</param>
	/// <returns>New session information.</returns>
	public Task<SessionInfo> CreateSessionAsync(string clientIP = "")
	{
		var sessionId = GenerateSecureSessionId();
		var session = new SessionInfo(sessionId, clientIP);

		_sessions[sessionId] = session;

		_logger.LogDebug(
			"Created new session {SessionId} for IP {ClientIP}",
			sessionId, clientIP);

		return Task.FromResult(session);
	}

	/// <summary>
	/// Gets a session by its ID.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <returns>Session info or null if not found.</returns>
	public Task<SessionInfo?> GetSessionAsync(string sessionId)
	{
		if (string.IsNullOrEmpty(sessionId))
		{
			return Task.FromResult<SessionInfo?>(null);
		}

		_sessions.TryGetValue(sessionId, out var session);
		return Task.FromResult(session);
	}

	/// <summary>
	/// Updates session activity and validates session.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <returns>True if session exists and is valid.</returns>
	public Task<bool> UpdateSessionActivityAsync(string sessionId)
	{
		if (string.IsNullOrEmpty(sessionId))
		{
			return Task.FromResult(false);
		}

		if (_sessions.TryGetValue(sessionId, out var session))
		{
			lock (session)
			{
				if (!session.IsConnected)
				{
					_logger.LogWarning("Attempted to update activity for disconnected session {SessionId}", sessionId);
					return Task.FromResult(false);
				}

				session.UpdateActivity();
				_logger.LogTrace("Updated activity for session {SessionId}", sessionId);
				return Task.FromResult(true);
			}
		}

		_logger.LogWarning("Session not found for activity update: {SessionId}", sessionId);
		return Task.FromResult(false);
	}

	/// <summary>
	/// Associates a player ID with a session.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="playerId">Player to associate.</param>
	/// <returns>True if successful.</returns>
	public Task<bool> SetSessionPlayerAsync(string sessionId, CombinedID playerId)
	{
		if (string.IsNullOrEmpty(sessionId))
		{
			return Task.FromResult(false);
		}

		if (_sessions.TryGetValue(sessionId, out var session))
		{
			lock (session)
			{
				if (!session.IsConnected)
				{
					_logger.LogWarning("Attempted to set player for disconnected session {SessionId}", sessionId);
					return Task.FromResult(false);
				}

				session.PlayerId = playerId;
				session.UpdateActivity();

				_logger.LogDebug(
					"Associated player {PlayerId} with session {SessionId}",
					playerId.GetPlayerID(), sessionId);
				return Task.FromResult(true);
			}
		}

		_logger.LogWarning("Session not found for player assignment: {SessionId}", sessionId);
		return Task.FromResult(false);
	}

	/// <summary>
	/// Associates a session with a room.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="roomId">Room identifier.</param>
	/// <returns>True if successful.</returns>
	public Task<bool> SetSessionRoomAsync(string sessionId, int roomId)
	{
		if (string.IsNullOrEmpty(sessionId))
		{
			return Task.FromResult(false);
		}

		if (_sessions.TryGetValue(sessionId, out var session))
		{
			lock (session)
			{
				if (!session.IsConnected)
				{
					_logger.LogWarning("Attempted to set room for disconnected session {SessionId}", sessionId);
					return Task.FromResult(false);
				}

				session.CurrentRoomId = roomId;
				session.UpdateActivity();

				_logger.LogDebug("Associated session {SessionId} with room {RoomId}", sessionId, roomId);
				return Task.FromResult(true);
			}
		}

		_logger.LogWarning("Session not found for room assignment: {SessionId}", sessionId);
		return Task.FromResult(false);
	}

	/// <summary>
	/// Queues a message for a session.
	/// </summary>
	/// <param name="sessionId">Target session.</param>
	/// <param name="message">Base64-encoded message.</param>
	/// <returns>True if successfully queued.</returns>
	public Task<bool> QueueMessageAsync(string sessionId, string message)
	{
		if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(message))
		{
			return Task.FromResult(false);
		}

		if (_sessions.TryGetValue(sessionId, out var session))
		{
			lock (session)
			{
				if (!session.IsConnected)
				{
					_logger.LogWarning("Attempted to queue message for disconnected session {SessionId}", sessionId);
					return Task.FromResult(false);
				}

				session.QueueMessage(message);

				_logger.LogTrace(
					"Queued message for session {SessionId} (queue size: {QueueSize})",
					sessionId, session.QueuedMessageCount);
				return Task.FromResult(true);
			}
		}

		_logger.LogWarning("Session not found for message queuing: {SessionId}", sessionId);
		return Task.FromResult(false);
	}

	/// <summary>
	/// Gets and removes the next message for a session (polling).
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <returns>Next message or null if queue is empty.</returns>
	public Task<string?> PollMessageAsync(string sessionId)
	{
		if (string.IsNullOrEmpty(sessionId))
		{
			return Task.FromResult<string?>(null);
		}

		if (_sessions.TryGetValue(sessionId, out var session))
		{
			lock (session)
			{
				if (!session.IsConnected)
				{
					_logger.LogWarning("Attempted to poll messages for disconnected session {SessionId}", sessionId);
					return Task.FromResult<string?>(null);
				}

				var message = session.DequeueMessage();

				if (message != null)
				{
					_logger.LogTrace(
						"Polled message for session {SessionId} (remaining: {QueueSize})",
						sessionId, session.QueuedMessageCount);
				}

				return Task.FromResult(message);
			}
		}

		_logger.LogWarning("Session not found for message polling: {SessionId}", sessionId);
		return Task.FromResult<string?>(null);
	}

	/// <summary>
	/// Disconnects a session and cleans up resources.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <returns>True if session was found and disconnected.</returns>
	public Task<bool> DisconnectSessionAsync(string sessionId)
	{
		if (string.IsNullOrEmpty(sessionId))
		{
			return Task.FromResult(false);
		}

		if (_sessions.TryRemove(sessionId, out var session))
		{
			lock (session)
			{
				session.Disconnect();
				_logger.LogDebug("Disconnected and removed session {SessionId}", sessionId);
				return Task.FromResult(true);
			}
		}

		_logger.LogWarning("Session not found for disconnection: {SessionId}", sessionId);
		return Task.FromResult(false);
	}

	/// <summary>
	/// Cleans up expired/timed-out sessions.
	/// </summary>
	/// <param name="timeoutMinutes">Session timeout in minutes.</param>
	/// <returns>Number of sessions cleaned up.</returns>
	public Task<int> CleanupExpiredSessionsAsync(int timeoutMinutes = 30)
	{
		var expiredSessions = new List<string>();
		var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

		// First pass: identify expired sessions
		foreach (var kvp in _sessions)
		{
			var session = kvp.Value;
			lock (session)
			{
				if (session.LastActivity < cutoffTime || !session.IsConnected)
				{
					expiredSessions.Add(kvp.Key);
				}
			}
		}

		// Second pass: remove expired sessions
		int cleanedUp = 0;
		foreach (var sessionId in expiredSessions)
		{
			if (_sessions.TryRemove(sessionId, out var session))
			{
				lock (session)
				{
					session.Disconnect();
					cleanedUp++;
				}
			}
		}

		if (cleanedUp > 0)
		{
			_logger.LogInformation(
				"Cleaned up {Count} expired sessions (timeout: {TimeoutMinutes} minutes)",
				cleanedUp, timeoutMinutes);
		}

		return Task.FromResult(cleanedUp);
	}

	/// <summary>
	/// Gets all active sessions.
	/// </summary>
	/// <returns>List of active sessions.</returns>
	public Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync()
	{
		var activeSessions = new List<SessionInfo>();

		foreach (var kvp in _sessions)
		{
			var session = kvp.Value;
			lock (session)
			{
				if (session.IsConnected)
				{
					// Create a snapshot to avoid concurrent modification
					var sessionSnapshot = new SessionInfo(session.SessionId, session.ClientIP)
					{
						PlayerId = session.PlayerId,
						CreatedAt = session.CreatedAt,
						LastActivity = session.LastActivity,
						IsConnected = session.IsConnected,
						CurrentRoomId = session.CurrentRoomId,
						UserAgent = session.UserAgent,
						MessageQueue = new Queue<string>(session.MessageQueue),
					};
					activeSessions.Add(sessionSnapshot);
				}
			}
		}

		return Task.FromResult<IEnumerable<SessionInfo>>(activeSessions);
	}

	/// <summary>
	/// Gets session count.
	/// </summary>
	/// <returns>Number of active sessions.</returns>
	public Task<int> GetSessionCountAsync()
	{
		return Task.FromResult(_sessions.Count);
	}

	/// <summary>
	/// Generates a cryptographically secure session ID
	/// Format: "SESS_" + 16 hex characters (64-bit entropy).
	/// </summary>
	/// <returns>Secure session ID.</returns>
	private static string GenerateSecureSessionId()
	{
		// Generate 8 random bytes (64-bit entropy)
		var randomBytes = new byte[8];
		using (var rng = RandomNumberGenerator.Create())
		{
			rng.GetBytes(randomBytes);
		}

		// Convert to hex string and prepend "SESS_"
		var hex = Convert.ToHexString(randomBytes);
		return $"SESS_{hex}";
	}

	/// <summary>
	/// Gets diagnostics information for monitoring.
	/// </summary>
	/// <returns>Session manager statistics.</returns>
	public Task<object> GetDiagnosticsAsync()
	{
		var totalSessions = _sessions.Count;
		var connectedSessions = 0;
		var totalQueuedMessages = 0;
		var sessionsWithPlayers = 0;
		var sessionsWithRooms = 0;

		foreach (var session in _sessions.Values)
		{
			lock (session)
			{
				if (session.IsConnected)
				{
					connectedSessions++;
				}

				totalQueuedMessages += session.QueuedMessageCount;

				if (session.PlayerId != null)
				{
					sessionsWithPlayers++;
				}

				if (session.CurrentRoomId.HasValue)
				{
					sessionsWithRooms++;
				}
			}
		}

		var diagnostics = new
		{
			TotalSessions = totalSessions,
			ConnectedSessions = connectedSessions,
			DisconnectedSessions = totalSessions - connectedSessions,
			SessionsWithPlayers = sessionsWithPlayers,
			SessionsWithRooms = sessionsWithRooms,
			TotalQueuedMessages = totalQueuedMessages,
			AverageQueueSize = totalSessions > 0 ? (double)totalQueuedMessages / totalSessions : 0.0,
			Timestamp = DateTime.UtcNow,
		};

		return Task.FromResult<object>(diagnostics);
	}
}
