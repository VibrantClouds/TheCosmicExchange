namespace OffworldLobbyServer.Shared.SFS2X.Protocol;

/// <summary>
/// Represents a parsed SFS2X LoginRequest according to confirmed specification.
/// Binary message keys: "zn" (zone), "un" (username), "pw" (password), "p" (parameters)
/// Source: docs/ReverseEngineeringNotes.md:80-90
/// </summary>
public class LoginRequest
{
	/// <summary>
	/// Zone name (typically "BasicExamples" or "Offworld").
	/// </summary>
	public string ZoneName { get; set; } = string.Empty;

	/// <summary>
	/// User name / player ID.
	/// </summary>
	public string UserName { get; set; } = string.Empty;

	/// <summary>
	/// Password (usually empty or hashed).
	/// </summary>
	public string Password { get; set; } = string.Empty;

	/// <summary>
	/// Additional parameters as SFSObject.
	/// </summary>
	public SFSObject Parameters { get; set; } = new();

	/// <summary>
	/// Parses a LoginRequest from SFSObject payload.
	/// </summary>
	/// <param name="payload">SFSObject from message payload.</param>
	/// <returns>Parsed login request.</returns>
	public static LoginRequest FromSFSObject(SFSObject payload)
	{
		var request = new LoginRequest();

		if (payload.ContainsKey("zn"))
			request.ZoneName = payload.GetUtfString("zn");

		if (payload.ContainsKey("un"))
			request.UserName = payload.GetUtfString("un");

		if (payload.ContainsKey("pw"))
			request.Password = payload.GetUtfString("pw");

		if (payload.ContainsKey("p"))
			request.Parameters = payload.GetSFSObject("p");

		return request;
	}

	/// <summary>
	/// Converts login request to SFSObject for serialization.
	/// </summary>
	/// <returns>SFSObject representation.</returns>
	public SFSObject ToSFSObject()
	{
		var obj = new SFSObject();
		obj.PutUtfString("zn", ZoneName);
		obj.PutUtfString("un", UserName);
		obj.PutUtfString("pw", Password);
		obj.PutSFSObject("p", Parameters);
		return obj;
	}

	public override string ToString()
	{
		return $"LoginRequest - Zone: '{ZoneName}', User: '{UserName}', Params: {Parameters.Size} entries";
	}
}