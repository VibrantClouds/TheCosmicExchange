# Protocol Skew Analysis: Current Implementation vs. Confirmed SFS2X Specifications

This document compares our current implementation against the confirmed protocol specifications documented in `docs/ReverseEngineeringNotes.md` and identifies areas that need alignment.

## Executive Summary

Our implementation has **significant protocol mismatches** that prevent proper client compatibility. The core issue is that we've implemented **custom message formats** instead of following the confirmed SFS2X binary protocol structure.

**Status**: 🚨 **CRITICAL PROTOCOL DIVERGENCE**
- Client exhibits infinite polling behavior due to protocol mismatch
- Custom message types (0x90/144) don't match standard SFS2X format
- Binary message structure differs from confirmed specifications

## Critical Protocol Mismatches

### 1. Message Structure Format ❌

**Our Implementation**:
```
[Message Type: 1 byte][Request ID: 2 bytes][Message Size: 4 bytes][Payload]
```
*Source: `SFS2XBinaryMessageProcessor.cs:79-83, 400-402`*

**Confirmed Specification**:
```
[Header: 1 byte - Controller ID][Action: 1 byte - Action ID][Length: 4 bytes][SFSObject: Variable length]
```
*Source: `docs/ReverseEngineeringNotes.md:41-46`*

**Impact**: Client cannot parse our responses because we're using the wrong message format entirely.

### 2. Controller/Action Structure ❌

**Our Implementation**: Uses custom message types like:
- `SFS2XMessageType.LoginRequest = 0x00`
- `SFS2XMessageType.CreateRoomRequest = 0x10` 
- Custom type `144` (0x90) processing

*Source: `SFS2XBinaryMessageProcessor.cs:568-587, SFS2XMessageProcessor.cs:111`*

**Confirmed Specification**:
- **Controllers**: `0x00` (SystemController), `0x01` (ExtensionController)
- **System Actions**: `0x00` (HANDSHAKE), `0x01` (LOGIN_REQUEST), `0x04` (JOIN_ROOM), etc.

*Source: `docs/ReverseEngineeringNotes.md:48-58`*

**Impact**: Our message types don't align with the official Controller+Action structure that clients expect.

### 3. Login Response Structure ❌

**Our Implementation**: Custom binary structure with success flag + data object
```csharp
payloadWriter.Write(loginResp.Success);  // Custom success boolean
loginResp.Data.Serialize(payloadWriter); // Custom serialization
```
*Source: `SFS2XBinaryMessageProcessor.cs:374-375`*

**Confirmed Specification**: Specific binary format with nested SFSObjects
```
Binary Structure (hex):
80 00 00 45  // Header: 0x80 (binary), Length: 69 bytes  
00 01        // Controller: 0 (System), Action: 1 (LOGIN)
12 00 03     // SFSObject with 3 entries
```
*Source: `docs/ReverseEngineeringNotes.md:93-118`*

**Impact**: Client doesn't recognize our login responses, preventing state progression.

### 4. SFSObject Binary Format Mismatch ⚠️

**Our Implementation**: Type-first serialization
```csharp
writer.WriteByte((byte)SFS2XDataType.SFS_OBJECT);  // Type first
writer.WriteShort((short)obj.Size);                // Then count
```
*Source: `SFS2XSerializer.cs:21-22`*

**Confirmed Specification**: Direct type+data encoding
```
Type Encoding (1 byte type + data):
- 0x08: UTF_STRING (2 bytes length + UTF-8 data)
- 0x12: SFS_OBJECT
```
*Source: `docs/ReverseEngineeringNotes.md:62-76`*

**Assessment**: ⚠️ **PARTIALLY CORRECT** - Type IDs match specification, but serialization order may differ.

### 5. BlueBox HTTP Transport ✅

**Our Implementation**: Correct BlueBox command handling
```csharp
// Handles: connect|poll|data|disconnect commands
// Proper session management with SESS_xxx format
// Base64 encoding/decoding for binary data
```
*Source: `BlueBoxController.cs:110-127`*

**Confirmed Specification**: Matches exactly
```
POST /BlueBox/BlueBox.do HTTP/1.1
sfsHttp=null|connect|null
sfsHttp=SESS_xxx|poll|null  
sfsHttp=SESS_xxx|data|<base64_encoded_sfs2x_message>
```
*Source: `docs/ReverseEngineeringNotes.md:17-36`*

**Assessment**: ✅ **FULLY COMPLIANT** - This layer works correctly.

## Missing Protocol Features

### 1. Extension Message Support ❌

**Current State**: Placeholder implementation
```csharp
private async Task<byte[]> HandleBinaryExtensionRequest(...)
{
    _logger.LogDebug("Binary ExtensionRequest for session {SessionId} - not implemented", sessionId);
    return CreateBinarySuccessResponse(header.RequestId, "Extension handled");
}
```
*Source: `SFS2XBinaryMessageProcessor.cs:296-300`*

**Required**: Full ExtensionRequest support for "registerData"
```javascript
// Client sends after login:
client.Send(new ExtensionRequest("registerData", gameData));
```
*Source: `docs/ReverseEngineeringNotes.md:357`*

### 2. Proper Login Data Structure ❌

**Current State**: Basic data object
```csharp
loginResponse.Data.PutUtfString("accessKey", "fake_aws_access_key");
loginResponse.Data.PutUtfString("secret", "fake_aws_secret_key");
```
*Source: `SFS2XBinaryMessageProcessor.cs:163-164`*

**Required**: Complete user object with nested structure
```
"user" entry - nested SFSObject with 4 entries:
  - "id": INT (42)  
  - "name": UTF_STRING ("Player123")
  - "privilegeId": BYTE (1)
  - "isItMe": BOOL (true)
```
*Source: `docs/ReverseEngineeringNotes.md:99-109`*

## Code Simplification Opportunities

### 1. Eliminate Redundant Message Processors 🔄

**Current**: Two separate processors for same protocol
- `SFS2XMessageProcessor.cs` (25,233 tokens) - BlueBox messages  
- `SFS2XBinaryMessageProcessor.cs` (609 lines) - "Native" binary

**Simplification**: Merge into single processor using confirmed protocol format, since both should use the same SFS2X binary structure inside BlueBox transport.

### 2. Remove Custom Message Types 🔄

**Current**: Custom enum with made-up values
```csharp
public enum SFS2XMessageType : byte
{
    LoginRequest = 0x00,        // Custom
    CreateRoomRequest = 0x10,   // Custom  
    ExtensionRequest = 0x30,    // Custom
}
```
*Source: `SFS2XBinaryMessageProcessor.cs:568-587`*

**Simplification**: Replace with standard Controller+Action constants matching the confirmed specification.

### 3. Standardize Response Creation 🔄

**Current**: Multiple response creation methods scattered across processors
- `SerializeBinaryResponse()` in binary processor
- `EncodeResponse()` in message processor
- `CreateBinaryErrorResponse()` for errors

**Simplification**: Single response factory using confirmed SFS2X binary format.

## Implementation Priority Todo List

### 🔥 Phase 1: Critical Protocol Fixes

1. **Replace Custom Message Structure**
   - [ ] Remove `SFS2XMessageType` enum  
   - [ ] Implement Controller+Action parsing (`[0x00][0x01]` for System Login)
   - [ ] Update message header parsing in `SFS2XBinaryMessageProcessor.cs:78-90`

2. **Fix Login Response Format**  
   - [ ] Implement confirmed binary structure with nested SFSObjects
   - [ ] Add proper "user", "data", "zone" entries per specification
   - [ ] Update `HandleBinaryLoginRequest()` in `SFS2XBinaryMessageProcessor.cs:95-186`

3. **Validate SFSObject Serialization**
   - [ ] Test round-trip serialization/deserialization
   - [ ] Verify type IDs match confirmed specification exactly
   - [ ] Fix any byte-order or format issues in `SFS2XSerializer.cs`

### ⚠️ Phase 2: Feature Completion

4. **Implement Extension Support**
   - [ ] Add proper ExtensionRequest parsing for Controller 0x01
   - [ ] Handle "registerData" extension calls
   - [ ] Support ExtensionResponse format per specification

5. **Complete Room Management**
   - [ ] Fix CreateRoomRequest parsing for 21-element lobbySettings array
   - [ ] Implement JoinRoomRequest with proper SFSObject structure
   - [ ] Add room variables and user list support

### 🧹 Phase 3: Code Cleanup  

6. **Merge Duplicate Processors**
   - [ ] Combine `SFS2XMessageProcessor` and `SFS2XBinaryMessageProcessor`
   - [ ] Use single confirmed protocol format for all message handling
   - [ ] Remove redundant message parsing logic

7. **Simplify Response Creation**
   - [ ] Create unified response factory using confirmed binary format
   - [ ] Remove custom JSON-like response encoding
   - [ ] Standardize error handling across all message types

## Success Criteria

### Immediate Goals
- [ ] Client stops infinite polling after handshake
- [ ] Client successfully progresses from login → registerData extension  
- [ ] Client can create and join rooms

### Long-term Goals  
- [ ] All message formats match confirmed protocol specifications exactly
- [ ] End-to-end client communication flow works without protocol hacks
- [ ] Codebase uses single, clean implementation of SFS2X protocol

## Files Requiring Updates

### High Priority
- `src/OffworldLobbyServer.Core/Services/SFS2XBinaryMessageProcessor.cs` - Complete rewrite needed
- `src/OffworldLobbyServer.Core/Services/SFS2XMessageProcessor.cs` - Protocol format fixes
- `src/OffworldLobbyServer.Shared/SFS2X/SFS2XSerializer.cs` - Validation and potential fixes

### Medium Priority  
- Response classes and data structures - Proper SFS2X object creation
- Message type enums - Replace with Controller+Action constants
- Session management - Ensure proper state transitions

### Low Priority (Working)
- `src/OffworldLobbyServer.Api/Controllers/BlueBoxController.cs` - ✅ Fully compliant with specification