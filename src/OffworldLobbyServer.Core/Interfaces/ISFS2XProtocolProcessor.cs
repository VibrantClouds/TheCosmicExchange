using OffworldLobbyServer.Shared.SFS2X;

namespace OffworldLobbyServer.Core.Interfaces;

/// <summary>
/// Interface for processing SFS2X protocol messages according to confirmed specifications.
/// Handles both BlueBox HTTP transport and direct TCP connections with unified protocol logic.
/// </summary>
public interface ISFS2XProtocolProcessor
{
	/// <summary>
	/// Processes a base64-encoded SFS2X message from BlueBox HTTP transport.
	/// </summary>
	/// <param name="sessionId">Session identifier from BlueBox.</param>
	/// <param name="base64Data">Base64-encoded binary message.</param>
	/// <returns>Base64-encoded response message or null for acknowledgment.</returns>
	Task<string?> ProcessBlueBoxMessage(string sessionId, string base64Data);

	/// <summary>
	/// Processes raw binary SFS2X message data from direct TCP connections.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="messageData">Raw binary message data.</param>
	/// <returns>Binary response data or null.</returns>
	Task<byte[]?> ProcessBinaryMessage(string sessionId, byte[] messageData);

	/// <summary>
	/// Parses SFS2X message header according to confirmed specification:
	/// [Controller:1][Action:1][Length:4][SFSObject payload]
	/// </summary>
	/// <param name="messageData">Raw message bytes.</param>
	/// <returns>Parsed message header and payload offset.</returns>
	SFS2XMessageHeader ParseMessageHeader(byte[] messageData);

	/// <summary>
	/// Creates a properly formatted SFS2X response message.
	/// </summary>
	/// <param name="controller">Controller ID (0x00=System, 0x01=Extension).</param>
	/// <param name="action">Action ID within controller.</param>
	/// <param name="payload">SFSObject payload data.</param>
	/// <returns>Complete binary message with header.</returns>
	byte[] CreateResponseMessage(byte controller, byte action, SFSObject payload);
}