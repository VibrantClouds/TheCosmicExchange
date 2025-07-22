using OffworldLobbyServer.Shared.Models;

namespace OffworldLobbyServer.Core.Models;

/// <summary>
/// Represents an active BlueBox session
/// Tracks connection state and message queuing.
/// </summary>
public class SessionInfo
{
	/// <summary>
	/// Unique session identifier
	/// Format: 16-20 character alphanumeric string.
	/// </summary>
	public string SessionId { get; set; } = string.Empty;

	/// <summary>
	/// Player associated with this session.
	/// </summary>
	public CombinedID? PlayerId { get; set; }

	/// <summary>
	/// When the session was created.
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Last activity timestamp
	/// Used for session timeout.
	/// </summary>
	public DateTime LastActivity { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Whether the session is currently connected.
	/// </summary>
	public bool IsConnected { get; set; } = true;

	/// <summary>
	/// Current room/lobby the session is in.
	/// </summary>
	public int? CurrentRoomId { get; set; }

	/// <summary>
	/// Queued messages waiting to be polled
	/// Base64-encoded message payloads.
	/// </summary>
	public Queue<string> MessageQueue { get; set; } = new ();

	/// <summary>
	/// Client connection metadata.
	/// </summary>
	public string UserAgent { get; set; } = string.Empty;

	/// <summary>
	/// Client IP address.
	/// </summary>
	public string ClientIP { get; set; } = string.Empty;

	/// <summary>
	/// Creates a new session info.
	/// </summary>
	public SessionInfo()
	{
	}

	/// <summary>
	/// Creates a new session with specified ID.
	/// </summary>
	/// <param name="sessionId">Unique session identifier.</param>
	/// <param name="clientIP">Client IP address.</param>
	public SessionInfo(string sessionId, string clientIP = "")
	{
		SessionId = sessionId;
		ClientIP = clientIP;
		UpdateActivity();
	}

	/// <summary>
	/// Updates the last activity timestamp.
	/// </summary>
	public void UpdateActivity()
	{
		LastActivity = DateTime.UtcNow;
	}

	/// <summary>
	/// Checks if the session has timed out.
	/// </summary>
	/// <param name="timeoutMinutes">Timeout in minutes.</param>
	/// <returns>True if timed out.</returns>
	public bool IsTimedOut(int timeoutMinutes = 30)
	{
		return DateTime.UtcNow.Subtract(LastActivity).TotalMinutes > timeoutMinutes;
	}

	/// <summary>
	/// Adds a message to the queue for polling.
	/// </summary>
	/// <param name="message">Base64-encoded message.</param>
	public void QueueMessage(string message)
	{
		MessageQueue.Enqueue(message);
		UpdateActivity();
	}

	/// <summary>
	/// Gets and removes the next queued message.
	/// </summary>
	/// <returns>Next message or null if queue is empty.</returns>
	public string? DequeueMessage()
	{
		UpdateActivity();
		return MessageQueue.Count > 0 ? MessageQueue.Dequeue() : null;
	}

	/// <summary>
	/// Gets the number of queued messages.
	/// </summary>
	/// <returns>Message count.</returns>
	public int QueuedMessageCount => MessageQueue.Count;

	/// <summary>
	/// Clears all queued messages.
	/// </summary>
	public void ClearMessages()
	{
		MessageQueue.Clear();
		UpdateActivity();
	}

	/// <summary>
	/// Disconnects the session and clears resources.
	/// </summary>
	public void Disconnect()
	{
		IsConnected = false;
		ClearMessages();
		CurrentRoomId = null;
		UpdateActivity();
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		var playerInfo = PlayerId?.GetPlayerID() ?? "Unknown";
		var roomInfo = CurrentRoomId?.ToString() ?? "None";
		return $"Session {SessionId[..8]}... - Player: {playerInfo}, Room: {roomInfo}, Messages: {QueuedMessageCount}";
	}
}
