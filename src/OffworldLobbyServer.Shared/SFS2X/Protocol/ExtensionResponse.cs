namespace OffworldLobbyServer.Shared.SFS2X.Protocol;

/// <summary>
/// Represents a SFS2X ExtensionResponse according to confirmed specification.
/// Binary structure with "cmd", "params", "sourceRoom" entries.
/// Source: docs/ReverseEngineeringNotes.md:177-194
/// </summary>
public class ExtensionResponse
{
	/// <summary>
	/// Original command that was executed.
	/// </summary>
	public string Command { get; set; } = string.Empty;

	/// <summary>
	/// Response parameters as SFSObject.
	/// </summary>
	public SFSObject Parameters { get; set; } = new();

	/// <summary>
	/// Source room ID where the extension was executed.
	/// </summary>
	public int SourceRoom { get; set; }

	/// <summary>
	/// Converts extension response to SFSObject for serialization.
	/// Matches confirmed binary structure from protocol specification.
	/// </summary>
	/// <returns>SFSObject with "cmd", "params", "sourceRoom" entries.</returns>
	public SFSObject ToSFSObject()
	{
		var obj = new SFSObject();
		obj.PutUtfString("cmd", Command);
		obj.PutSFSObject("params", Parameters);
		obj.PutInt("sourceRoom", SourceRoom);
		return obj;
	}

	/// <summary>
	/// Creates an extension response from a request.
	/// </summary>
	/// <param name="request">Original extension request.</param>
	/// <param name="responseParams">Response parameters.</param>
	/// <returns>Extension response.</returns>
	public static ExtensionResponse FromRequest(ExtensionRequest request, SFSObject responseParams)
	{
		return new ExtensionResponse
		{
			Command = request.Command,
			Parameters = responseParams,
			SourceRoom = request.RoomId
		};
	}

	public override string ToString()
	{
		return $"ExtensionResponse - Command: '{Command}', SourceRoom: {SourceRoom}, Params: {Parameters.Size} entries";
	}
}