# Offworld Server Reverse Engineering - Current Status

## 🧪 Current Experiments

### Experiment: Header Structure Investigation  
**Status**: 🔬 **TESTING IN PROGRESS**

#### Previous Failed Attempts:
1. **❌ Position Fix Experiment**: Tried removing duplicate header reading
   - **Result**: Still got "expected 196626, available 41" - same wrong value
   - **Conclusion**: Stream position wasn't the issue

2. **❌ ushort PayloadSize**: Tried reading PayloadSize as 16-bit instead of 32-bit  
   - **Result**: Still wrong values, EndOfStreamException persisted
   - **Conclusion**: PayloadSize field assumption is fundamentally wrong

#### Current Hypothesis: No PayloadSize Field
**Theory**: SFS2X header has no explicit PayloadSize field
- Structure might be: `[Type:1][SFSObject:remaining_bytes]` 
- Or: `[Type:1][RequestID:2][SFSObject:remaining_bytes]`
- Payload size is implicit (read until end of stream)

**Current Test**: Manual Binary Format Reverse Engineering (`SFS2XMessageProcessor.cs:644-693`)

#### Approach: Hex Dump Analysis
Since SFSObject parsing fails consistently, implementing manual binary analysis:

1. **String Location Mapping**: Find exact byte positions of known strings:
   - "api" → hex `617069` 
   - "1.7.8" → hex `312E372E38`
   - "Unity" → hex `556E697479`

2. **Pattern Recognition**: Analyze bytes surrounding each string:
   - Look for length prefixes (common in binary protocols)
   - Identify separators or field markers
   - Map potential key-value pair structures

3. **Structure Discovery**: Reverse engineer the binary format
   - Determine if strings are length-prefixed
   - Identify field boundaries and types
   - Create custom parser based on discovered pattern

#### Progress Update: Pattern Discovery
**Hex Analysis Results**: Found potential length-prefixed string pattern:
- `0003 [617069]` = "api" (3-byte string)
- `0005 [312E372E38]` = "1.7.8" (5-byte string) 
- `0005 [556E697479]` = "Unity" (5-byte string)

**Current Implementation**: Custom parser attempting to parse as structured binary format:
- Header: RequestID + flags
- Data section: Series of `[type][length][data]` fields
- Field identification logic for client info

**Status**: ❌ **FAILED** - Custom parser could not parse binary data
**Results**: Parser failed at position 5 with "unusual field length 256"
**Conclusion**: Binary structure is NOT simple length-prefixed strings as hypothesized

#### New Hypothesis: Message 144 = Client Handshake (Not Login)
**🔬 TESTING**: Message 144 might be **client capability negotiation**, not LoginRequest

**Evidence**:
- Client already uses session ID (connection via BlueBox works)
- Contains version info ("api", "1.7.8", "Unity") not login credentials  
- Function named "HandshakeRequest" not "LoginRequest"
- Client stuck polling = waiting for handshake acknowledgment

**Theory**: Flow should be:
1. BlueBox connection → Session ID ✅
2. Client Handshake (144) → "I'm Unity with API 1.7.8" 
3. Server Handshake Ack → "Acknowledged"
4. Client sends REAL LoginRequest (different message type)

**Status**: ❌ **FAILED** - Client continues infinite polling after handshake acknowledgment
**Results**: Client ignores JSON response completely, still sends `poll|null` requests
**Conclusion**: Response format is wrong - client expects proper SFS2X binary, not JSON

#### New Hypothesis: Missing ExtensionRequest Handler + Improved LoginResponse  
**🔬 TESTING**: Issue is **missing post-login flow**, not just response format

**Evidence from AmazonManager.cs Analysis**:
- After successful login, client sends `ExtensionRequest("registerData", userSettings)` (line 848)
- LoginResponse MUST contain specific fields: `accessKey`, `secret`, `serverTime`, `ipAddress`, `port`
- Client expects SFS2X binary responses, not JSON
- We were missing ExtensionRequest handler for message type 0x30

**Current Implementation**:
- ✅ **Fixed LoginResponse data**: Added all required fields from AmazonManager.cs
- ✅ **Added ExtensionRequest handler**: Handles message type 0x30 for "registerData"
- ✅ **Improved RequestID parsing**: Extract actual RequestID from client message
- ✅ **Added detailed hex logging**: Debug LoginResponse binary format

**Expected Flow**:
1. Client sends LoginRequest (message 144) → Server responds with proper LoginResponse (0x01)
2. Client's SFS2X library triggers `SFSEvent.LOGIN` → AmazonManager.OnLogin() executes  
3. Client sends `ExtensionRequest("registerData")` (0x30) → Server acknowledges with ExtensionResponse (0x31)
4. Client stops polling and becomes ready for lobby operations

**Status**: 🔬 **TESTING** - Ready to test complete login flow

---

## 🚨 Current Problem: Infinite Client Polling 

**Issue**: After LoginRequest/LoginResponse exchange, client sends infinite `poll|null` requests
- BlueBox protocol fix attempted: Changed `data|null` → `data|{response}` ❌ **FAILED**  
- Client still polls indefinitely, suggesting LoginResponse format is incorrect
- Server responding correctly via BlueBox but client not recognizing response

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

## Missing Implementation Elements

- [ ] **ClientGameMessages.messageWithoutID**: Message type threshold
- [ ] **ServerGameMessages.serverPlayMsg**: Game start broadcast (likely byte 129)
- [ ] **ExtensionRequest handler**: "registerData" and other extensions
- [ ] **CreateRoomRequest**: Actual lobby creation message type
