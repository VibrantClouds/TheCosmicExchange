namespace OffworldLobbyServer.Shared.SFS2X.Protocol;

/// <summary>
/// Represents a SFS2X ExtensionRequest according to confirmed specification.
/// Binary encoding with "c" (command), "r" (room), "p" (parameters) keys.
/// Source: docs/ReverseEngineeringNotes.md:162-175
/// </summary>
public class ExtensionRequest
{
	/// <summary>
	/// Extension command name (e.g., "registerData", "gameMove").
	/// </summary>
	public string Command { get; set; } = string.Empty;

	/// <summary>
	/// Target room ID (-1 for zone-level extension).
	/// </summary>
	public int RoomId { get; set; } = -1;

	/// <summary>
	/// Extension parameters as SFSObject.
	/// </summary>
	public SFSObject Parameters { get; set; } = new();

	/// <summary>
	/// Parses an ExtensionRequest from SFSObject payload.
	/// </summary>
	/// <param name="payload">SFSObject from message payload.</param>
	/// <returns>Parsed extension request.</returns>
	public static ExtensionRequest FromSFSObject(SFSObject payload)
	{
		var request = new ExtensionRequest();

		if (payload.ContainsKey("c"))
			request.Command = payload.GetUtfString("c");

		if (payload.ContainsKey("r"))
			request.RoomId = payload.GetInt("r");

		if (payload.ContainsKey("p"))
			request.Parameters = payload.GetSFSObject("p");

		return request;
	}

	/// <summary>
	/// Converts extension request to SFSObject for serialization.
	/// </summary>
	/// <returns>SFSObject representation.</returns>
	public SFSObject ToSFSObject()
	{
		var obj = new SFSObject();
		obj.PutUtfString("c", Command);
		obj.PutInt("r", RoomId);
		obj.PutSFSObject("p", Parameters);
		return obj;
	}

	public override string ToString()
	{
		return $"ExtensionRequest - Command: '{Command}', Room: {RoomId}, Params: {Parameters.Size} entries";
	}
}