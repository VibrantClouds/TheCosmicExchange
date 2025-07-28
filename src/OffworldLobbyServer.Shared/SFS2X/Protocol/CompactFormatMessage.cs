namespace OffworldLobbyServer.Shared.SFS2X.Protocol;

/// <summary>
/// Represents a compact format message wrapper used by Unity clients.
/// Structure: { "c": command, "a": action, "p": parameters }
/// </summary>
public class CompactFormatMessage
{
	/// <summary>
	/// Command identifier. Can be numeric or string.
	/// </summary>
	public object? Command { get; set; }

	/// <summary>
	/// Action identifier. Usually numeric.
	/// </summary>
	public int Action { get; set; }

	/// <summary>
	/// Parameters object containing the actual message data.
	/// </summary>
	public SFSObject Parameters { get; set; } = new();

	/// <summary>
	/// Parses a CompactFormatMessage from an SFSObject.
	/// </summary>
	public static CompactFormatMessage? FromSFSObject(SFSObject obj)
	{
		// Must have at least "c" and "p" keys
		if (!obj.ContainsKey("c") || !obj.ContainsKey("p"))
			return null;

		var message = new CompactFormatMessage();

		// Command can be various types - check type first
		var cmdType = obj.GetValueType("c");
		if (cmdType == typeof(int))
		{
			message.Command = obj.GetInt("c");
		}
		else if (cmdType == typeof(byte))
		{
			message.Command = (int)obj.GetByte("c"); // Convert to int for consistency
		}
		else if (cmdType == typeof(short))
		{
			message.Command = (int)obj.GetShort("c"); // Convert to int for consistency
		}
		else if (cmdType == typeof(string))
		{
			message.Command = obj.GetUtfString("c");
		}

		// Action is typically numeric
		if (obj.ContainsKey("a"))
		{
			var actionType = obj.GetValueType("a");
			if (actionType == typeof(int))
			{
				message.Action = obj.GetInt("a");
			}
			else if (actionType == typeof(byte))
			{
				message.Action = obj.GetByte("a");
			}
			else if (actionType == typeof(short))
			{
				message.Action = obj.GetShort("a");
			}
			else
			{
				message.Action = 0; // Default
			}
		}

		// Parameters must be an SFSObject
		message.Parameters = obj.GetSFSObject("p");

		return message;
	}

	/// <summary>
	/// Determines the SFS2X controller and action based on the compact format command.
	/// </summary>
	public (byte controller, byte action) GetControllerAction()
	{
		// Convert command to int for easier comparison
		int? cmdValue = Command switch
		{
			byte b => b,
			int i => i,
			short s => s,
			_ => null
		};

		// Check numeric commands
		if (cmdValue.HasValue)
		{
			switch (cmdValue.Value)
			{
				case 0:
					// Command 0: Check parameters to determine actual message type
					if (Parameters.ContainsKey("api") && Parameters.ContainsKey("cl"))
						return (0x00, 0x00); // System/Handshake
					break;

				case 1:
					// Command 1: Likely login
					if (Parameters.ContainsKey("zn") && Parameters.ContainsKey("un"))
						return (0x00, 0x01); // System/Login
					break;

				case 4:
					// Command 4: Likely join room
					if (Parameters.ContainsKey("r"))
						return (0x00, 0x04); // System/JoinRoom
					break;
			}
		}
		// Check string commands (extension requests)
		else if (Command is string cmdStr && !string.IsNullOrEmpty(cmdStr))
		{
			return (0x01, 0x00); // Extension/Request
		}

		// Default: treat as extension request if has command
		return (0x01, 0x00);
	}

	/// <summary>
	/// Converts to a standard extension request format if applicable.
	/// </summary>
	public SFSObject ToExtensionFormat()
	{
		var obj = new SFSObject();
		
		// For extension requests, command becomes "c"
		if (Command is string cmdStr)
		{
			obj.PutUtfString("c", cmdStr);
			obj.PutInt("r", -1); // Default room ID
			obj.PutSFSObject("p", Parameters);
		}

		return obj;
	}

	public override string ToString()
	{
		return $"CompactMessage - Command: {Command}, Action: {Action}, Params: {Parameters.Size} entries";
	}
}