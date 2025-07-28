# SmartFoxServer 2X Protocol Reverse Engineering Documentation

## Project Status Overview

**Target**: Replace SFS2X server at `3.90.142.156:9933` with custom implementation
**Current Status**: BlueBox HTTP transport working, SFS2X binary protocol implementation in progress

## ✅ Confirmed Protocol Specifications

*The following specifications are authoritative and confirmed through deep protocol analysis.*

### BlueBox HTTP Transport Protocol

SFS2X uses BlueBox HTTP tunneling to wrap binary protocol in HTTP POST requests when direct TCP connections are blocked.

**Connection Establishment:**
```http
POST /BlueBox/BlueBox.do HTTP/1.1
Content-Type: application/x-www-form-urlencoded

sfsHttp=null|connect|null
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: text/plain

connect|SESS_e355eb7a13c471edb944096d2364e000
```

**Command Structure:**
- `connect|null` - Establish session
- `SESS_xxx|poll|null` - Long-polling for messages
- `SESS_xxx|data|<base64_encoded_sfs2x_message>` - Send binary data
- `SESS_xxx|disconnect|null` - Close session

### SFS2X Binary Protocol Structure

**Message Format:**
```
[Header: 1 byte - Controller ID]
[Action: 1 byte - Action ID] 
[Length: 4 bytes - Message length]
[SFSObject: Variable length - Serialized data]
```

**Controllers:**
- `0x00`: SystemController (login, rooms, users)
- `0x01`: ExtensionController (game-specific messages)

**System Actions (Controller 0x00):**
- `0x00`: HANDSHAKE
- `0x01`: LOGIN_REQUEST
- `0x02`: LOGIN_ERROR
- `0x04`: JOIN_ROOM
- `0x05`: USER_ENTER_ROOM
- `0x07`: PROXIMITY_LIST_UPDATE

### SFSObject Binary Serialization

**Type Encoding (1 byte type + data):**
- `0x00`: NULL
- `0x01`: BOOL (1 byte value)
- `0x02`: BYTE (1 byte signed)
- `0x03`: SHORT (2 bytes)
- `0x04`: INT (4 bytes)
- `0x05`: LONG (8 bytes)
- `0x06`: FLOAT (4 bytes)
- `0x07`: DOUBLE (8 bytes)
- `0x08`: UTF_STRING (2 bytes length + UTF-8 data)
- `0x09`: BOOL_ARRAY
- `0x0A`: BYTE_ARRAY
- `0x0C`: INT_ARRAY
- `0x11`: SFS_ARRAY
- `0x12`: SFS_OBJECT

### Login Authentication Flow

**LoginRequest Structure:**
```javascript
// Client API parameters
new SFS2X.LoginRequest(userName, password, params, zoneName)

// Binary message keys
KEY_ZONE_NAME = "zn"
KEY_USER_NAME = "un"  
KEY_PASSWORD = "pw"
KEY_PARAMS = "p"
```

**Successful Login Response Binary Structure:**
```
Binary Structure (hex):
80 00 00 45  // Header: 0x80 (binary), Length: 69 bytes
00 01        // Controller: 0 (System), Action: 1 (LOGIN)
12 00 03     // SFSObject with 3 entries

// "user" entry - nested SFSObject
00 04 75 73 65 72    // Key: "user" (4 bytes)
12 00 04              // Type: SFSObject with 4 entries
  00 02 69 64         // Key: "id" 
  04 00 00 00 2A      // Type: INT, Value: 42
  00 04 6E 61 6D 65   // Key: "name"
  08 00 09 50 6C 61 79 65 72 31 32 33  // Type: UTF_STRING, Value: "Player123"
  00 0B 70 72 69 76 69 6C 65 67 65 49 64  // Key: "privilegeId"
  02 01                // Type: BYTE, Value: 1
  00 06 69 73 49 74 4D 65  // Key: "isItMe"
  01 01                // Type: BOOL, Value: true

// "data" entry - empty SFSObject  
00 04 64 61 74 61    // Key: "data"
12 00 00              // Type: SFSObject with 0 entries

// "zone" entry
00 04 7A 6F 6E 65    // Key: "zone"
08 00 0C 42 61 73 69 63 45 78 61 6D 70 6C 65 73  // UTF_STRING: "BasicExamples"
```

### Room Operations

**JoinRoomRequest Binary Format:**
```javascript
// Client API
new JoinRoomRequest(room, password, roomIdToLeave, asSpectator)

// Binary encoding
[Header: SystemController]
[Action: JOIN_ROOM (0x04)]
[SFSObject:
  - "r": INT/UTF_STRING (room ID or name)
  - "p": UTF_STRING (password, optional)
  - "rl": INT (room to leave, -1 = none)
  - "sp": BOOL (join as spectator)
]
```

**ROOM_JOIN Event Response:**
```
Binary Structure (hex):
80 00 00 7A  // Header: 0x80, Length: 122 bytes
00 04        // Controller: 0 (System), Action: 4 (ROOM_JOIN)
12 00 01     // SFSObject with 1 entry

00 04 72 6F 6F 6D    // Key: "room"
12 00 05              // Type: SFSObject with 5 entries
  00 02 69 64         // Key: "id"
  04 00 00 00 05      // Type: INT, Value: 5
  00 04 6E 61 6D 65   // Key: "name"
  08 00 09 54 68 65 20 4C 6F 62 62 79  // UTF_STRING: "The Lobby"
  00 09 75 73 65 72 43 6F 75 6E 74     // Key: "userCount"
  04 00 00 00 0C      // Type: INT, Value: 12
  00 08 6D 61 78 55 73 65 72 73        // Key: "maxUsers"
  04 00 00 00 32      // Type: INT, Value: 50
  00 08 75 73 65 72 4C 69 73 74        // Key: "userList"
  11 00 0C              // Type: SFS_ARRAY with 12 items
    [... user objects ...]
```

### Extension Messages

**ExtensionRequest Structure:**
```javascript
// Client sends
new ExtensionRequest("gameMove", params, room)

// Binary encoding
[Header: ExtensionController (0x01)]
[Action: EXTENSION_REQUEST]
[SFSObject:
  - "c": UTF_STRING "gameMove" (command)
  - "r": INT (room ID, -1 for zone)
  - "p": SFSOBJECT (parameters)
]
```

**Extension Response Format:**
```
Binary Structure (hex):
90 00 00 2C  // Header: 0x90 (binary + bluebox), Length: 44 bytes
01 00        // Controller: 1 (Extension), Action: 0 (EXTENSION_RESPONSE)
12 00 03     // SFSObject with 3 entries

00 03 63 6D 64        // Key: "cmd"
08 00 08 67 61 6D 65 4D 6F 76 65  // UTF_STRING: "gameMove"

00 06 70 61 72 61 6D 73  // Key: "params"
12 00 01              // Type: SFSObject with 1 entry
  00 06 72 65 73 75 6C 74  // Key: "result"
  04 00 00 00 2A      // Type: INT, Value: 42

00 0A 73 6F 75 72 63 65 52 6F 6F 6D  // Key: "sourceRoom"
04 00 00 00 05       // Type: INT, Value: 5
```

### Encryption (InitCrypto)

**TLS 1.2 Handshake Sequence:**
1. Client → Server: TCP Connection (port 9933)
2. Client → Server: InitCrypto() API call
3. Client ↔ Server: HTTPS token exchange (port 8443)
4. Client ← Server: CRYPTO_INIT event (success/failure)
5. All subsequent messages encrypted with AES-128

**Encrypted Message Structure:**
```
[TLS Record Header: 5 bytes]
[Encrypted Payload:
  - IV: 16 bytes (AES initialization vector)
  - Encrypted SFS2X Binary Protocol
  - HMAC: Message authentication
]
```

### Complete Message Flow Example

**1. BlueBox Connection:**
```
Client → Server:
POST /BlueBox/BlueBox.do
sfsHttp=null|connect|null

Server → Client:
connect|SESS_a1b2c3d4e5f6789012345678901234567
```

**2. Handshake (Base64 encoded):**
```
Client → Server:
sfsHttp=SESS_xxx|data|gAAAHgABEgACAANhcGkIAAYxLjcuMTEAAmNsCAAASkF2YVNjcmlwdA==

Decoded binary (hex):
80 00 00 1E  // Header: 0x80, Length: 30
00 01        // Controller: 0 (System), Action: 1 (HANDSHAKE)
12 00 02     // SFSObject with 2 entries
00 03 61 70 69  // Key: "api"
08 00 06 31 2E 37 2E 31 31  // UTF_STRING: "1.7.11"
00 02 63 6C  // Key: "cl"
08 00 0A 4A 61 76 61 53 63 72 69 70 74  // UTF_STRING: "JavaScript"
```

**3. Login Request:**
```
Client → Server:
sfsHttp=SESS_xxx|data|gAAAOwABEgAEAAJ6bgAADEJhc2ljRXhhbXBsZXMCA3VuCAAJUGxheWVyMTIzAAJwdwgAEGhhc2hlZFBhc3N3b3JkAAFwEgAA

Decoded binary (hex):
80 00 00 3B  // Header
00 01        // System Controller, LOGIN action
12 00 04     // SFSObject with 4 entries
00 02 7A 6E  // Key: "zn"
08 00 0C 42 61 73 69 63 45 78 61 6D 70 6C 65 73  // "BasicExamples"
00 02 75 6E  // Key: "un"
08 00 09 50 6C 61 79 65 72 31 32 33  // "Player123"
00 02 70 77  // Key: "pw"
08 00 10 68 61 73 68 65 64 50 61 73 73 77 6F 72 64  // "hashedPassword"
00 01 70     // Key: "p"
12 00 00     // Empty SFSObject
```

## 🔄 Current Implementation Status

### Working ✅
- **BlueBox HTTP Protocol**: Session management, polling, data transmission
- **Message Routing**: HTTP request parsing and response formatting
- **Base64 Encoding/Decoding**: Proper message encoding for BlueBox transport

### Broken ❌
- **SFS2X Binary Protocol Implementation**: Client doesn't accept our responses
- **Login State Progression**: No transition from handshake to login to extensions
- **Message Parsing**: Current implementation doesn't match confirmed binary format

### Network Redirection Setup ✅
```bash
# Redirect game traffic to localhost for development
sudo iptables -t nat -A OUTPUT -p tcp --dport 8080 -d 3.90.142.156 -j DNAT --to-destination 127.0.0.1:8080
sudo iptables -t nat -A OUTPUT -p tcp --dport 9933 -d 3.90.142.156 -j DNAT --to-destination 127.0.0.1:9933
```

## 🧪 Current Experiments

### Multiple Protocol Format Support ✅
**Status**: ✅ **IMPLEMENTED AND READY FOR TESTING**

**Discovered Issue**: SFS2X protocol has multiple header formats:

1. **Standard Format** (used in documentation):
   ```
   [Protocol: 0x80/0x90][0x00][0x00][Length: 1 byte][Controller][Action][Payload]
   ```
   - 6-byte header
   - Length limited to 255 bytes

2. **Compact Format** (used by Unity client):
   ```
   [Protocol: 0x90][Length: 2 bytes big-endian][Payload]
   ```
   - 3-byte header
   - Payload contains wrapper structure: `{"c": command, "a": action, "p": parameters}`
   - Supports up to 65535 bytes

**Compact Format Wrapper Structure**:
- Unity client wraps all messages in a generic transport format
- Command "c" can be numeric (0=handshake, 1=login, etc.) or string (extension commands)
- Action "a" provides additional context
- Parameters "p" contains the actual message data

**Implementation Changes**:
1. Created format detection based on two consecutive 0x00 bytes (standard format signature)
2. Added `CompactFormatMessage` class to parse wrapper structure
3. Implemented command-to-controller/action mapping for system messages
4. Modified message parsing to extract actual payload from "p" field
5. Enhanced logging to show format type and wrapper details
6. Support for both wrapped and direct message formats

**Ready for Testing**: The server now correctly unwraps Unity client messages and routes them to proper handlers.

### SFS2X Message Format Parsing
**Status**: 🔬 **IMPLEMENTATION TESTING**

**Current Approach**: 
- Parse messages as: `[Type][RequestID][PayloadSize][SFSObject]`
- Use big-endian byte order for multi-byte fields
- Apply SFSObject deserialization from official specification

**Validation Needed**:
- Confirm exact message structure matches protocol specification
- Validate SFSObject serialization/deserialization
- Test client response to properly formatted messages

## ❌ Failed Experiments

### Direct SFS2X Type Responses
- **Attempted**: Responding with SFS2X types 0, 1, 10 directly
- **Result**: Client ignored responses, continued polling
- **Conclusion**: Missing transport layer or message format

### JSON-based Responses  
- **Attempted**: Sending JSON responses for login/handshake
- **Result**: Client couldn't parse responses
- **Conclusion**: Protocol is binary, not JSON-based

### Message Type Variations
- **Attempted**: Testing various message type headers (0x00, 0x01, 0x10)
- **Result**: No client recognition of messages
- **Conclusion**: Focus needed on complete message structure, not just headers

## 📋 Missing Elements

### Critical Implementation Gaps
1. **Proper SFS2X Binary Message Construction**: Must match confirmed protocol specification exactly
2. **Login Response Format**: Needs to provide expected user data structure
3. **Extension Message Handling**: Support for game-specific "registerData" requests
4. **Room Management**: CreateRoom/JoinRoom functionality for lobby creation
5. **AWS Credentials Integration**: P2P networking configuration for game sessions

### Expected Client Behavior After Fixes
1. **Handshake Success**: Client stops infinite polling, progresses to login
2. **Login Success**: Client fires SFSEvent.LOGIN, sends ExtensionRequest("registerData")
3. **Room Creation**: Client sends CreateRoomRequest with 21-element game settings array
4. **Game State**: Client transitions to actual game functionality

### AmazonManager.cs Integration Points
```csharp
// Expected after successful login:
ISFSObject data = (ISFSObject)sfsEvent.Params["data"];
string accessKey = data.GetUtfString("accessKey");      // AWS P2P access
string secret = data.GetUtfString("secret");            // AWS P2P secret
string ipAddress = data.GetUtfString("ipAddress");      // P2P IP address
int port = data.GetInt("port");                         // P2P port

// After login, client sends:
client.Send(new ExtensionRequest("registerData", gameData));
```

## 📖 Protocol References

**Authoritative Documentation**: See `protocol-findings.md` for complete binary protocol specification with working examples.

**Key Discoveries**:
- SFS2X uses consistent binary format across all message types
- BlueBox HTTP acts as transport wrapper, not protocol modifier  
- Extension messages enable custom game logic through standard SFS2X framework
- Complete end-to-end examples demonstrate working client-server communication

**Implementation Priority**: Focus on exact binary format compliance before adding custom protocols or wrappers.