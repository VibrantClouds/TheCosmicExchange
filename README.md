# TCE [The Cosmic Exchange] - Offworld Trading Company Server Implementation (Alpha)

**Status: ✅ Dual-Protocol Infrastructure Complete - Room Operations In Progress**

A reverse-engineered game server implementation that replaces the original SmartFoxServer 2X (SFS2X) infrastructure used by Offworld Trading Company for multiplayer lobby management and P2P game coordination.

Note a CLAUDE.md file in the directory root, while not required, I've personally used it as a tool to deal with boilerplate. Follow good practices. Do not blindly accept what LLM Code Assists recommend. Always be a pilot and guide it, always validate results which come from it. If you do not understand the output, do not accept it.

## 📋 Detailed Specifications

For complete technical details, protocol analysis, and implementation requirements, see:
**[@docs/ReverseEngineeringNotes.md](docs/ReverseEngineeringNotes.md)**

## 🎯 What This Server Does

This server replaces the original game's lobby infrastructure by implementing the a BlueBox compliant HTTP server used by Offworld Trading Company. The original game connects to `3.90.142.156:9933` (direct SFS2X) with automatic HTTP fallback to port 8080 which we will be using ourselves.


## 🏗️ **Dual-Protocol Architecture**

### Network Protocol Stack (✅ **IMPLEMENTED**)
```
Game Client                    Original Infrastructure              This Server
─────────────────              ──────────────────────              ─────────────────
AmazonManager.Connect()
│
└─ Primary: BlueBox HTTP   -> HTTP Transport (8080)          -> BlueBoxController (8080)
   ├─ HTTP POST             -> /BlueBox/BlueBox.do             -> HTTP POST endpoint
   ├─ Base64 + Pipes        -> Tunneled SFS2X messages        -> SFS2XMessageProcessor
   └─ Polling mechanism     -> Message queue polling          -> Session-based queuing

P2P Transition              -> Unity NAT Facilitation         -> Lobby-to-game handoff (IP/port data)
```

### ✅ **Implemented Core Components**
- **🔧 BlueBoxController** - HTTP endpoint processing pipe-separated commands (port 8080)
- **🔧 Session Manager** - Unified client tracking across both transport methods
- **🔧 Room Manager** - Lobby creation, joining, and 21-element settings management  
- **🔧 Binary Protocol Handler** - Native SFS2X message parsing with proper headers
- **🔧 P2P Data Provider** - Critical IP/port information for lobby-to-game transitions

## 🚧 Implementation Status

### ✅ **Phase 1-2: Dual-Protocol Infrastructure (COMPLETE)**
- [x] **BlueBox HTTP Protocol (Port 8080)** - HTTP fallback with pipe-separated commands  
- [x] **Session Management** - Unified tracking across both transport methods
- [x] **Clean Architecture** - Proper separation of concerns and dependency injection. Goodness I hope it's clean at least.. This is an Alpha still after all

### 🔄 **Phase 3: SFS2X Protocol Implementation (MOSTLY COMPLETE)**
- [🔄] **Authentication Protocol** - LoginRequest/LoginResponse with zone validation
- [] **Player Data Extraction** - playerName, gender, tachyonID from game parameters
- [] **Binary Serialization** - Proper SFS2X header format and response generation
- [] **P2P Network Data** - Critical IP/port information for lobby-to-game transitions
- [] **Error Handling** - SFS2X-compliant error responses with request correlation
- [] **Room Creation** - Basic CreateRoomRequest implemented, needs 21-element array refinement

### 🔄 **Phase 3: Room Operations (IN PROGRESS)**  
- [🔄] **Complete CreateRoomRequest** - Match exact 21-element lobbySettings SFSArray format
- [ ] **JoinRoomRequest** - Room joining with password support and user validation
- [ ] **ExtensionRequest** - Handle "registerData" and other custom game extensions
- [ ] **Room Variables** - Dynamic lobby setting updates and synchronization

### 📋 **Phase 4: Production Features (PLANNED)**
- [ ] **Transport Abstraction** - Unified business logic across both protocols
- [ ] **Integration Testing** - Automated testing for dual-protocol scenarios  
- [ ] **Monitoring & Metrics** - Protocol usage tracking and performance monitoring
- [ ] **Configuration Flexibility** - Dynamic port binding and protocol toggles

### 🚀 **Phase 5: Game Integration (FUTURE)**
- [ ] **Unity P2P Facilitation** - Complete lobby-to-game transition support
- [ ] **Load Testing** - Multi-client stress testing and optimization
- [ ] **Security Hardening** - Input validation and DoS protection
- [ ] **Docker Deployment** - Containerized production deployment

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### Running the Server
```bash
# Clone the repository
git clone <repository-url>
cd OffworldReverseEng

# Build the solution
dotnet build

# Run the API server
cd src/OffworldLobbyServer.Api
dotnet run

# Server will start on http://localhost:5000
# BlueBox endpoint available at: http://localhost:5000/BlueBox/BlueBox.do
```

### Testing the Connection
```bash
# Test BlueBox connect command
curl -X POST http://localhost:5000/BlueBox/BlueBox.do \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "sfsHttp=null%7Cconnect%7Cnull"

# Expected response: connect|SESS_<session_id>
```

## 📁 Project Structure

This project follows Clean Architecture principles:

```
src/
├── OffworldLobbyServer.Api/           # Web API layer (Controllers, Middleware)
│   ├── Controllers/BlueBoxController.cs   # Main BlueBox protocol handler
│   └── Program.cs                         # Application entry point
├── OffworldLobbyServer.Core/          # Business logic layer
│   ├── Interfaces/                        # Service contracts
│   ├── Models/                           # Domain models
│   ├── Services/SFS2XMessageProcessor.cs # SFS2X protocol handling
│   └── Serialization/                    # Message serialization
├── OffworldLobbyServer.Infrastructure/ # Data access layer
│   └── Services/                         # Concrete implementations
├── OffworldLobbyServer.Shared/        # Common utilities
│   ├── SFS2X/                           # SFS2X data structures
│   └── Models/                          # Shared models
tests/                                 # Unit and integration tests
docs/                                  # Documentation
DECOMPILED_GAME_CLASSES/              # Original game code analysis
```

## 🔧 Protocol Implementation Details

### Port Configuration & Failover Behavior
The SmartFoxServer client uses a dual-port architecture with automatic failover:

**Primary Connection (Port 8080)** 
- BlueBox HTTP transport layer
- Activated when port 9933 is unreachable or redirected
- HTTP-tunneled SFS2X protocol messages

**Observed Wireshark Behavior**: When using IPTables to redirect port 9933/8080 traffic to your replacement server, the game client automatically switches to BlueBox HTTP mode on port 8080 which also needs redirected. This is expected SmartFoxServer failover functionality.

### BlueBox Command Format
The game uses HTTP POST with pipe-separated commands:
```
POST /BlueBox/BlueBox.do
Content-Type: application/x-www-form-urlencoded

sfsHttp={sessionId}|{command}|{data}
```

### Key Protocol Commands
- **connect** - Establish new session: `null|connect|null` → `connect|SESS_<id>`
- **poll** - Long-polling for messages: `SESS_<id>|poll|null` → `poll|<data>` or `poll|null`
- **data** - Send game data: `SESS_<id>|data|<base64_data>` → `data|null`
- **disconnect** - Close session: `SESS_<id>|disconnect|null` → `disconnect|null`

### Critical Lobby Data Structure
The game sends a 21-element SFS2X array containing complete lobby configuration:
```
Index 0:  Lobby name
Index 1:  Lobby type (Public/Friends/Password)
Index 2:  Version compatibility key
Index 3:  Game setup type
Index 4:  Rules set
Index 5:  Replay recording enabled
Index 6:  Map location (Mars/Ceres/Io/Europa)
Index 7:  HQ restrictions (bool array)
Index 8:  AI players enabled
Index 9:  Map size (auto-calculated from player count)
Index 10: Terrain generation type
Index 11: Game speed setting
Index 12: Custom map name
Index 13: Random seed
Index 14: Map latitude
Index 15: Resource minimum
Index 16: Resource presence
Index 17: Colony class
Index 18: Game options (bool array ~32 elements)
Index 19: Team assignments (SFSObject: PlayerID → team)
Index 20: Handicap assignments (SFSObject: PlayerID → handicap)
```

## 📊 Development Status

### ✅ Working
- BlueBox HTTP protocol endpoint
- Basic session management (connect/poll/disconnect)
- Clean Architecture foundation

### 🚧 In Progress
- SFS2X message processing
- 21-element lobby settings parsing
- Room creation and management
- Login response with networking details
- Request parsing and response formatting
- Project structure and build system

### 📋 Planned
- Complete lobby functionality
- P2P coordination
- Unity GUID handling
- Game start transitions
- Error handling and edge cases

## 🤝 Contributing

This is a reverse engineering project aimed at preserving multiplayer functionality for Offworld Trading Company. Contributions are welcome!

### Community & Discussion

💬 **Join our Matrix room for discussion and testing coordination:**  
[https://matrix.to/#/#otc-server-creation:matrix.stormcloud.host](https://matrix.to/#/#otc-server-creation:matrix.stormcloud.host)

This is the best place to:
- Discuss protocol analysis and implementation details
- Coordinate testing efforts with other contributors
- Share findings and ask questions
- Get help with setup and development

### Areas Needing Help
- **Protocol Analysis** - Analyzing additional message formats and edge cases
- **Testing** - Creating test cases for protocol compatibility
- **Documentation** - Improving technical documentation and setup guides
- **Implementation** - Working on lobby management and P2P coordination

### Getting Started
1. Read the [detailed reverse engineering notes](docs/ReverseEngineeringNotes.md)
2. Set up the development environment
3. Run the existing tests to understand the current functionality
4. Pick an area to contribute to from the implementation plan above
5. Join the Matrix room to coordinate with other contributors

## ⚠️ Important Notes

- **Alpha Status**: This server is under active development and not ready for production use
- **Game Compatibility**: Designed specifically for Offworld Trading Company's networking protocol
- **Legal**: This is a clean-room reverse engineering effort for game preservation purposes
- **Protocol Accuracy**: Maintains exact compatibility with the original BlueBox servlet implementation

## 📜 License

This project is for educational and game preservation purposes. See LICENSE file for details.

---

**For complete technical specifications, protocol details, and implementation requirements, see [@docs/ReverseEngineeringNotes.md](docs/ReverseEngineeringNotes.md)**