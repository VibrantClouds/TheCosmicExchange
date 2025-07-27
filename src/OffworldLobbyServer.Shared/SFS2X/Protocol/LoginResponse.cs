namespace OffworldLobbyServer.Shared.SFS2X.Protocol;

/// <summary>
/// Represents a SFS2X LoginResponse according to confirmed specification.
/// Binary structure with "user", "data", "zone" entries as nested SFSObjects.
/// Source: docs/ReverseEngineeringNotes.md:92-118
/// </summary>
public class LoginResponse
{
	/// <summary>
	/// User information object with id, name, privilegeId, isItMe.
	/// </summary>
	public UserInfo User { get; set; } = new();

	/// <summary>
	/// Additional login data (typically empty SFSObject).
	/// </summary>
	public SFSObject Data { get; set; } = new();

	/// <summary>
	/// Zone name the user logged into.
	/// </summary>
	public string Zone { get; set; } = string.Empty;

	/// <summary>
	/// Converts login response to SFSObject for serialization.
	/// Matches confirmed binary structure from protocol specification.
	/// </summary>
	/// <returns>SFSObject with "user", "data", "zone" entries.</returns>
	public SFSObject ToSFSObject()
	{
		var obj = new SFSObject();
		
		// "user" entry - nested SFSObject with user info
		obj.PutSFSObject("user", User.ToSFSObject());
		
		// "data" entry - additional data (usually empty)
		obj.PutSFSObject("data", Data);
		
		// "zone" entry - zone name
		obj.PutUtfString("zone", Zone);
		
		return obj;
	}

	public override string ToString()
	{
		return $"LoginResponse - User: {User.Name} (ID: {User.Id}), Zone: '{Zone}'";
	}
}

/// <summary>
/// User information included in login response.
/// </summary>
public class UserInfo
{
	/// <summary>
	/// User ID (integer).
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// User display name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// User privilege level (byte).
	/// </summary>
	public byte PrivilegeId { get; set; } = 1;

	/// <summary>
	/// Whether this user object represents the current client.
	/// </summary>
	public bool IsItMe { get; set; } = true;

	/// <summary>
	/// Converts user info to SFSObject.
	/// </summary>
	/// <returns>SFSObject with user fields.</returns>
	public SFSObject ToSFSObject()
	{
		var obj = new SFSObject();
		obj.PutInt("id", Id);
		obj.PutUtfString("name", Name);
		obj.PutByte("privilegeId", PrivilegeId);
		obj.PutBool("isItMe", IsItMe);
		return obj;
	}
}