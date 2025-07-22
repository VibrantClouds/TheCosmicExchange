# Offworld Lobby Server (Alpha)

**Status: Work in Progress - Alpha Development**

A reverse-engineered game server implementation that replaces the original SmartFoxServer 2X (SFS2X) infrastructure used by Offworld Trading Company for multiplayer lobby management and P2P game coordination.

## 📋 Detailed Specifications

For complete technical details, protocol analysis, and implementation requirements, see:
**[@docs/ReverseEngineeringNotes.md](docs/ReverseEngineeringNotes.md)**

## 🎯 What This Server Does

This server replaces the original game's lobby infrastructure by implementing the proprietary BlueBox HTTP protocol used by Offworld Trading Company. The original game connects to `3.90.142.156:9933` - this server provides a drop-in replacement that enables:

- **Lobby Creation & Management** - Players can create and join multiplayer lobbies
- **Game Configuration** - Complete lobby settings including maps, rules, and player assignments  
- **P2P Coordination** - Facilitates Unity NetworkView P2P connections for actual gameplay
- **Session Management** - Handles player authentication and connection state
- **Protocol Compatibility** - Full BlueBox servlet compatibility with the original SFS2X implementation

## 🏗️ Architecture Overview

### Network Protocol Stack
```
Game Client                    Original Infrastructure              This Server
─────────────────              ──────────────────────              ─────────────────
CreateLobbyScreenListener  ->  
LobbyHelpers               ->  
AmazonManager              ->  SFS2X Server (3.90.142.156:9933) -> BlueBoxController
│                              │                                   │
├─ SmartFox client         ->  ├─ BlueBox Servlet               -> ├─ POST /BlueBox/BlueBox.do
├─ CreateRoomRequest       ->  ├─ Room creation                -> ├─ Session management  
├─ LoginRequest            ->  ├─ User authentication          -> ├─ Room management
├─ JoinRoomRequest         ->  ├─ Room joining                 -> ├─ SFS2X protocol compatibility
└─ ExtensionRequest        ->  └─ Custom extensions            -> └─ Message serialization

Unity P2P (Post-Lobby)     ->  Unity NAT Facilitation         -> Unity P2P (unchanged)
```

### Core Components
- **BlueBox Protocol Handler** - HTTP endpoint that processes pipe-separated commands (`connect|poll|data|disconnect`)
- **Session Manager** - Tracks client connections and message queues
- **Room Manager** - Manages lobby creation, joining, and settings
- **SFS2X Message Processor** - Handles the 21-element lobby settings array and game configuration
- **P2P Coordinator** - Facilitates transition from lobby to Unity NetworkView gameplay

## 🚧 Implementation Plan

### Phase 1: Core Protocol (In Progress)
- [x] BlueBox HTTP servlet endpoint (`/BlueBox/BlueBox.do`)
- [x] Session management (connect/disconnect/polling)
- [x] Basic message queuing system
- [x] Clean Architecture project structure
- [ ] Complete SFS2X message serialization
- [ ] Login response with IP/port detection
- [ ] 21-element lobby settings array parsing

### Phase 2: Lobby Management (Planned)
- [ ] Room creation and joining
- [ ] Lobby settings synchronization
- [ ] Player state management
- [ ] Room variable updates
- [ ] Game start coordination

### Phase 3: P2P Integration (Planned)
- [ ] Unity GUID broadcasting
- [ ] P2P connection facilitation
- [ ] Lobby-to-game transition
- [ ] NAT traversal support
- [ ] Connection fallback mechanisms

### Phase 4: Production Readiness (Future)
- [ ] Load testing and optimization
- [ ] Monitoring and diagnostics
- [ ] Error handling and recovery
- [ ] Security hardening
- [ ] Docker deployment

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
- Request parsing and response formatting
- Clean Architecture foundation
- Project structure and build system

### 🚧 In Progress
- SFS2X message processing
- 21-element lobby settings parsing
- Room creation and management
- Login response with networking details

### 📋 Planned
- Complete lobby functionality
- P2P coordination
- Unity GUID handling
- Game start transitions
- Error handling and edge cases

## 🤝 Contributing

This is a reverse engineering project aimed at preserving multiplayer functionality for Offworld Trading Company. Contributions are welcome!

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

## ⚠️ Important Notes

- **Alpha Status**: This server is under active development and not ready for production use
- **Game Compatibility**: Designed specifically for Offworld Trading Company's networking protocol
- **Legal**: This is a clean-room reverse engineering effort for game preservation purposes
- **Protocol Accuracy**: Maintains exact compatibility with the original BlueBox servlet implementation

## 📜 License

This project is for educational and game preservation purposes. See LICENSE file for details.

---

**For complete technical specifications, protocol details, and implementation requirements, see [@docs/ReverseEngineeringNotes.md](docs/ReverseEngineeringNotes.md)**