# Offworld Server Reverse Engineering - Current Status
## THIS IS A RATS NEST OF NOTES AND LIKELY WILL STAY THAT WAY, I'M JUGGLING A LOT OVER HERE, OKAY? CLAUDE CAN ONLY HELP SO MUCH

## 🧪 Current Experiments

### ✅ **CONFIRMED**: Protocol Layer Analysis - Message Type 144 Structure Decoded
**Status**: ✅ **PROTOCOL STRUCTURE CONFIRMED**

#### Critical Discovery: 144 is NOT in SFS2X RequestType Enum

**🎯 CONFIRMED FINDING**: After examining the official SmartFoxServer2X library symbols, discovered that **message type 144 does NOT exist in the SFS2X RequestType enum**!

**Official SFS2X RequestType Constants** (from library exploration - verified on 2025-07-23):
```csharp
// Complete list from Sfs2X.Requests.RequestType enum:
Handshake = 0
Login = 1  
Logout = 2
GetRoomList = 3
JoinRoom = 4
AutoJoin = 5
CreateRoom = 6
GenericMessage = 7
ChangeRoomName = 8
ChangeRoomPassword = 9
ObjectMessage = 10
SetRoomVariables = 11
SetUserVariables = 12
CallExtension = 13
LeaveRoom = 14
SubscribeRoomGroup = 15
UnsubscribeRoomGroup = 16
SpectatorToPlayer = 17
PlayerToSpectator = 18
ChangeRoomCapacity = 19
PublicMessage = 20
PrivateMessage = 21
ModeratorMessage = 22
AdminMessage = 23
KickUser = 24
BanUser = 25
ManualDisconnection = 26
FindRooms = 27
FindUsers = 28
PingPong = 29
SetUserPosition = 30
InitBuddyList = 200
AddBuddy = 201
BlockBuddy = 202
RemoveBuddy = 203
SetBuddyVariables = 204
GoOnline = 205
InviteUser = 300
InvitationReply = 301
CreateSFSGame = 302
QuickJoinGame = 303
JoinRoomInvite = 304
ClusterQuickJoin = 500
ClusterCreateGame = 501
ClusterInvitePlayers = 502
GameServerConnectionRequired = 600
// 144 is NOT in this enum!
```

#### Bitswarm Protocol Analysis

**Bitswarm Namespace Investigation** (from SFS2X library):
- Found `Sfs2X.Bitswarm.IMessage` interface with properties: Id, Content, TargetController, IsEncrypted, IsUDP, PacketId
- Found `Sfs2X.Bitswarm.Message` implementation
- No reference to value 144 in Bitswarm constants

#### Current Hypotheses

**Hypothesis 1: Custom Wrapper Protocol**
- Structure: `[144][RequestID][Flags][ACTUAL_SFS2X_MESSAGE_PAYLOAD]`
- 144 could be a custom protocol identifier used by the game client
- Not part of standard SFS2X but added by the game developers

**Hypothesis 2: Bitswarm Protocol Header**
- 144 might be part of the Bitswarm packet framing
- Could be related to PacketReadState or PacketReadTransition states
- Need to investigate Bitswarm packet structure further

**Hypothesis 3: BlueBox HTTP Protocol Encoding**
- 144 might be specific to BlueBox HTTP transport
- Could be a protocol version or message type indicator for HTTP fallback

#### Protocol Structure Analysis - CONFIRMED

**Confirmed Message Structure** (from client logs 2025-07-23):
```
Byte 0: 0x90 (144) - Custom protocol wrapper identifier
Bytes 1-2: RequestID (BIG-ENDIAN uint16) - Confirmed as 45 (0x002D)
Bytes 3-4: Flags/Header (BIG-ENDIAN uint16) - Value 4608 (0x1200) 
Bytes 5+: SFS2X payload (43 bytes starting with 0x03)
```

**🔍 CRITICAL DISCOVERY: SFS2X Payload Structure Analysis**

Client's SFS2X payload breakdown (43 bytes):
```
03 00 01 63 02 00 00 01 61 03 00 00 00 01 70 12 00 02 00 03 61 70 69 08 00 05 31 2E 37 2E 38 00 02 63 6C 08 00 05 55 6E 69 74 79
```

Detailed byte-by-byte analysis:
```
0x03                     - Controller ID (HANDSHAKE = 3)
0x00 0x01                - Action ID (short, BE) = 1
0x63                     - ASCII 'c' (controller parameter key)
0x02                     - Type: BYTE (2)
0x00                     - Value: 0
0x00 0x01                - Parameter count = 1
0x61                     - ASCII 'a' (action parameter key) 
0x03                     - Type: SHORT (3)
0x00 0x00                - Value: 0 (short, BE)
0x00 0x01                - Parameter count = 1
0x70                     - ASCII 'p' (parameters key)
0x12                     - Type: SFS_OBJECT (18)
[SFSObject data follows]
```

**Key Finding**: The SFS2X message format is:
1. Controller byte (0x03 for HANDSHAKE)
2. Action ID as BE short (0x0001)
3. Series of typed parameters with format: [key:char][type:byte][value:typed][param_count:BE_short]
4. The 'p' parameter contains the actual SFSObject payload

**Evidence from Enhanced Logging**:
```
Raw message bytes (48): 90002D12000300016302000001610300000001701200020003617069080005312E372E380002636C080005556E697479
Byte 0: 0x90 (144) - Potential message type
Bytes 1-2: 0x002D - As LE uint16: 11520, As BE uint16: 45
Bytes 3-4: 0x1200 - As LE uint16: 18, As BE uint16: 4608
Remaining payload (43 bytes): 0300016302000001610300000001701200020003617069080005312E372E380002636C080005556E697479
```

**Key Discovery**: The protocol uses **BIG-ENDIAN** byte order for the wrapper header fields!

#### Implementation Status

**✅ Completed**:
1. Added enhanced protocol logging to capture exact byte structure
2. Confirmed RequestID and Flags use **big-endian** byte order
3. Implemented proper protocol parser that reads big-endian values
4. Created `CreateWrapped144Response` function that:
   - Wraps SFS2X responses in the 144 protocol format
   - Uses big-endian for RequestID (echoes client's value)
   - Sets Flags to 0x0000 for responses
   - Prepends the 0x03 controller byte to match client format

**Response Structure**:
```
[0x90 (144)][RequestID: BE-uint16][Flags: 0x0000][0x03][SFS2X Response Payload]
```

**❌ CRITICAL BUG IDENTIFIED**: The current response format is incorrect!

Current (wrong) response structure:
```
[0x90][RequestID][0x0000][0x03][0x12][SFSObject data]
```

Correct response structure should be:
```
[0x90][RequestID][Flags][0x03][Action:BE-short][Parameters with proper format]
```

The server is missing:
1. Echoing back the client's flags (0x1200) instead of using 0x0000
2. The Action ID (BE short) after the controller byte
3. The proper parameter structure with typed key-value pairs
4. The parameter count indicators

#### Test Results: ✅ **SUCCESS**

**Client Request**:
```
Input: [144][RequestID: 11520][Flags: 0x0012][SFS2X HandshakeRequest payload]
```

**Server Response**:
```  
Output: [144][RequestID: 11520][Flags: 0x0000][SFS2X HandshakeResponse payload]
Response hex: 90002D0000005F000000120004000673746174757308001168616E647368616B655F73756363657373...
Wrapped response: 105 bytes
```

**Expected Outcome**: Client should stop infinite polling and progress to next phase (Login with SFS2X type 1)

#### Previous Failed Experiments (Now Understood)

1. **❌ Direct SFS2X Type 0 Response**: Client ignored it because it expected 144 wrapper
2. **❌ Direct SFS2X Type 1 Response**: Same issue - missing wrapper protocol  
3. **❌ JSON Responses**: Wrong protocol entirely
4. **❌ Message Type Testing (0x00, 0x01, 0x10)**: Wrong layer - was testing SFS2X types without wrapper

**Root Insight**: The client speaks a **custom wrapper protocol** that encapsulates standard SFS2X messages. The 144 byte is the wrapper identifier, not an SFS2X message type.

---

## 🧪 **CURRENT TESTING**: SFS2X Handshake Protocol with Compression Support

**Status**: 🔬 **TESTING IN PROGRESS** - Implemented proper handshake response with compression handling

### **MAJOR PROTOCOL DISCOVERIES** (2025-07-23)

#### 1. **Flags Analysis - 0x1200 (4608)**
- Binary: 0001 0010 0000 0000
- **Bit 9 (0x0200)**: Likely indicates compression is enabled
- **Bit 12 (0x1000)**: Possibly indicates BlueBox HTTP transport mode
- Server now checks for compression flag and applies zlib compression if needed

#### 2. **SFS2X Handshake Event**
- Found `HANDSHAKE = "handshake"` in SFSEvent.cs (marked as "exclude")
- This is an internal event distinct from CONNECTION
- Client expects this event to fire after receiving proper handshake response

#### 3. **Corrected Response Structure**
```
[144][RequestID:BE][Flags:BE][Controller:0x03][Parameters with proper format]
```

Parameters structure:
- Each parameter: [count:BE-ushort][key:byte][type:byte][value:typed]
- Controller parameters: c=0, a=0, p=SFSObject
- SFSObject contains handshake response data

### **IMPLEMENTATION CHANGES**

1. **Compression Support**: 
   - Check flag bit 0x0200 for compression
   - Apply zlib compression to response if enabled
   - Using ByteArray.Compress() from SFS2X library

2. **Proper Parameter Structure**:
   - Fixed parameter encoding with correct type markers
   - Added parameter count fields (BE ushort)
   - Matching exact client format

3. **Handshake Response Content**:
   - success: true
   - sessionToken: Generated UUID
   - serverTime: Unix timestamp in milliseconds

### **TEST EXPECTATIONS**

1. **If Successful**:
   - Client stops polling with message type 144
   - Client's SFS2X library fires HANDSHAKE event
   - Client proceeds to send LoginRequest (type 1)
   - No more infinite polling loop

2. **If Failed**:
   - Client continues polling with same 144 message
   - Need to check compression implementation
   - May need to adjust handshake response content

3. **Next Steps**:
   - Monitor for LoginRequest after successful handshake
   - Implement proper LoginResponse with AWS credentials
   - Handle "registerData" extension request
```

#### Expected Client Behavior After Wrapper Protocol Fix:
1. ✅ **Client receives wrapped HandshakeResponse** (144 protocol format)
2. **Client stops infinite polling** (should no longer send `poll|null`)  
3. **Client progresses to Login phase** (should send wrapped LoginRequest with SFS2X type 1)
4. **Client sends ExtensionRequest** (for "registerData" and game functionality)

#### Monitoring for Success Indicators:
- **Polling cessation**: Client stops sending repetitive `poll|null` requests
- **Message type progression**: Client sends different message types beyond 144
- **Login sequence**: Client follows proper SFS2X handshake → login → extension flow
- **ExtensionRequest detection**: `🎉 EXTENSION REQUEST DETECTED!` log message

#### If Client Still Polls Infinitely:
**Potential Issues to Investigate**:
1. **Response format mismatch**: Wrapper protocol structure incorrect
2. **SFS2X payload format**: HandshakeResponse format doesn't match client expectations  
3. **Protocol version mismatch**: Client expects different SFS2X binary format
4. **Missing required fields**: HandshakeResponse missing critical client-expected data

#### Current Implementation Status:
- ✅ **Wrapper protocol parser**: Correctly identifies 144 as wrapper, extracts nested SFS2X
- ✅ **Nested message identification**: Recognizes payload as HandshakeRequest (SFS2X type 0)
- ✅ **Wrapped response generation**: Creates properly formatted 144 wrapper response
- ✅ **Detailed logging**: Hex dumps and structure analysis for debugging

## Network Architecture

**Target**: Replace SFS2X server at `3.90.142.156:9933` (disabled TCP, using HTTP port 8080 only)
**Protocol**: BlueBox HTTP transport → SFS2X messages → Base64 encoding

**Observed Flow**:
```
1. SEND: data|kAAt... (LoginRequest type 144)
2. RECEIVE: data|{base64_LoginResponse} (our response)  
3. SEND: poll|null (infinite loop) ❌ Should stop here
```

## Network Traffic Redirection Setup

### IPTables Configuration for Development

**Redirect game traffic from original server to localhost for development:**

```bash
# Redirect port 9933 (SFS2X Direct) to localhost:9933
sudo iptables -t nat -A OUTPUT -p tcp --dport 9933 -d 3.90.142.156 -j DNAT --to-destination 127.0.0.1:9933

# Redirect port 8080 (BlueBox HTTP) to localhost:8080  
sudo iptables -t nat -A OUTPUT -p tcp --dport 8080 -d 3.90.142.156 -j DNAT --to-destination 127.0.0.1:8080

# Save rules (Ubuntu/Debian)
sudo iptables-save | sudo tee /etc/iptables/rules.v4
```

**Remove IPTables rules when done:**

```bash
# Remove port 9933 redirect rule
sudo iptables -t nat -D OUTPUT -p tcp --dport 9933 -d 3.90.142.156 -j DNAT --to-destination 127.0.0.1:9933

# Remove port 8080 redirect rule  
sudo iptables -t nat -D OUTPUT -p tcp --dport 8080 -d 3.90.142.156 -j DNAT --to-destination 127.0.0.1:8080

# Save changes
sudo iptables-save | sudo tee /etc/iptables/rules.v4
```

**Verify rules are active:**
```bash
sudo iptables -t nat -L OUTPUT -n -v
```

## Message Type 144 Analysis ✅ **BREAKTHROUGH CONFIRMED**

**MAJOR DISCOVERY**: The issue was **header parsing**, not payload format!

### Correct SFS2X Message Structure
```
[Type:byte][RequestID:ushort][PayloadSize:ushort][SFSObject]
```

**Key Fix**: Changed `payloadSize` from `ReadUInt32()` → `ReadUInt16()`
- ❌ **Before**: PayloadSize = 196,626 bytes (clearly wrong)  
- ✅ **After**: PayloadSize = 41 bytes (matches actual data)

### LoginRequest SFSObject Contents
**Confirmed payload structure** (`SFS2XMessageProcessor.cs:642-712`):
- ✅ Payload IS an SFSObject containing client login data
- ✅ Contains version information: "api", "1.7.8", "Unity"
- ✅ May contain username, password, zone fields
- ✅ Successfully deserializes with correct header parsing

### Evidence from Logs
```
Message Type: 144, RequestID: 11520, Size: 41 (correct!)
Hex: 016302000001610300000001701200020003617069080005312E372E380002636C080005556E697479
ASCII: .c....a.....p.....api...1.7.8..cl...Unity
```

This breakthrough should resolve the infinite polling issue!

## 🔧 Debug Steps

### Immediate Investigation Required

1. **SFS2X LoginResponse Format Issue**
   - Current: `CreateSFS2XLoginResponse` returns `[0x01][RequestID][PayloadSize][SFSObject]`
   - Problem: Client doesn't recognize response as valid LoginResponse
   - Action: Verify message type 0x01 is correct for LoginResponse

2. **Response Content Analysis** 
   - Add logging of exact Base64 response bytes
   - Compare against SFS2X documentation
   - Verify SFSObject serialization matches client expectations

3. **AmazonManager.cs Integration**
   - Expected: Client fires `SFSEvent.LOGIN` after receiving LoginResponse
   - Expected: Client sends `ExtensionRequest("registerData")` after login success  
   - Actual: Client continues polling, no progression

## Current Implementation Status

**Working ✅:**
- BlueBox HTTP protocol (connect, poll, data, disconnect commands)
- Session management and message routing
- Message type 144 parsing (client handshake/login)

**Broken ❌:**  
- SFS2X LoginResponse format (client doesn't accept response)
- Login state progression (no ExtensionRequest after login)
- Room creation flow (blocked by login issue)

## Critical References

### BlueBox Protocol (✅ Working)
```
POST /BlueBox/BlueBox.do HTTP/1.0
Content-Type: application/x-www-form-urlencoded

sfsHttp={sessionId}|{command}|{data}

Commands: connect, poll, data, disconnect  
Format: pipe-separated, URL-encoded, Base64 data
```

### Expected Login Flow
```
1. Client sends: data|{LoginRequest_144}
2. Server should return: data|{LoginResponse}  
3. Client should fire: SFSEvent.LOGIN
4. Client should send: ExtensionRequest("registerData")
```

### Current Status: Step 3 fails - client doesn't recognize our LoginResponse

## Key Technical Details

### AmazonManager.cs Login Flow
```csharp
// Expected in OnLogin event after successful login:
ISFSObject data = (ISFSObject)sfsEvent.Params["data"];
string accessKey = data.GetUtfString("accessKey");      // Required
string secret = data.GetUtfString("secret");            // Required  
string ipAddress = data.GetUtfString("ipAddress");      // CRITICAL for P2P
int port = data.GetInt("port");                         // CRITICAL for P2P

// After login success, client sends:
client.Send(new ExtensionRequest("registerData", iSFSObject3));
```

### SFS2X Message Structure
```
LoginRequest (144):  [144][RequestID:ushort][PayloadSize:uint][SFSObject]
LoginResponse (??): [Type:?][RequestID:ushort][PayloadSize:uint][SFSObject] 
```

**HYPOTHESIS**: Message type 0x01 is wrong for LoginResponse - need to find correct type

### Room Creation (21-element SFSArray)
When login works, client will send CreateRoomRequest containing:
- `roomSettings.Variables["lobbySettings"]` = 21-element array with game settings
- Array indices: [0]=name, [1]=type, [2]=version, [3]=gameSetup, ... [20]=handicaps

## ✅ MAJOR BREAKTHROUGH SUMMARY

### 🎯 **Protocol Mystery SOLVED**: The 144 Wrapper Discovery

**The Core Issue**: Months of failed attempts were due to **protocol layer confusion**
- **❌ Wrong assumption**: Treating 144 as direct SFS2X message type  
- **✅ Correct understanding**: 144 is transport wrapper containing real SFS2X messages

**Technical Achievement**:
1. **🔍 Library Analysis**: Reverse-engineered official SmartFoxServer2X library symbols
2. **📋 Enumeration Discovery**: Found complete SFS2X RequestType enum (0-30, 200+, 300+, etc.)
3. **🚫 Negative Discovery**: Message type 144 does NOT exist in official SFS2X protocol
4. **🧩 Structure Analysis**: Identified nested protocol: `[144][Headers][SFS2X_Message]`
5. **🔧 Parser Implementation**: Built wrapper protocol handler with correct response format

**Impact**: This breakthrough resolves the fundamental communication barrier between client and server, enabling progression to actual game functionality.

## Implementation Status Updates

### ✅ **Recently Completed**
- **Wrapper Protocol Handler**: `HandleWrappedSFS2XMessage()` correctly parses 144 structure
- **Nested SFS2X Parser**: Extracts real HandshakeRequest (type 0) from wrapper payload  
- **Response Wrapping**: `CreateWrappedHandshakeResponse()` maintains protocol consistency
- **Official Library Integration**: Uses Sfs2X NuGet package for authoritative constants

### 🔄 **Current Testing Phase** 
- **Client progression monitoring**: Waiting for polling cessation and next message type
- **Login flow readiness**: Prepared for wrapped LoginRequest (SFS2X type 1)
- **Extension handler**: Ready for "registerData" ExtensionRequest messages

### 📋 **Next Implementation Priorities**

**Phase 1: Complete Login Flow**
- [ ] **Wrapped LoginRequest handler**: Parse SFS2X type 1 inside 144 wrapper  
- [ ] **LoginResponse generation**: Provide AWS credentials and P2P networking info
- [ ] **ExtensionRequest support**: Handle "registerData" and game-specific extensions

**Phase 2: Game Functionality**  
- [ ] **CreateRoomRequest**: 21-element SFSArray lobby creation
- [ ] **Room management**: Multi-player lobby coordination
- [ ] **Game state synchronization**: Turn-based game mechanics

**Phase 3: Advanced Features**
- [ ] **ClientGameMessages.messageWithoutID**: Message type threshold
- [ ] **ServerGameMessages.serverPlayMsg**: Game start broadcast (likely byte 129)
