# Offworld Trading Company - Community Server

**Status: Alpha - Core Protocol Implementation Complete**

A community-driven server implementation that restores multiplayer functionality for Offworld Trading Company by replacing the original SmartFoxServer infrastructure.

## What This Does

Offworld Trading Company's multiplayer lobbies originally connected to `3.90.142.156:8080` using SmartFoxServer's BlueBox HTTP protocol. This server implements that exact protocol, allowing the game to connect to a community-hosted replacement instead of the defunct official servers.

**Key Features:**
- Complete BlueBox HTTP protocol compatibility
- Player authentication and session management  
- Lobby creation and room management
- Seamless integration with existing game clients
- No game modifications required

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- Offworld Trading Company (any version)

### Setup & Testing

1. **Clone and build the server:**
```bash
git clone https://github.com/your-repo/TheCosmicExchange.git
cd TheCosmicExchange
dotnet build
```

2. **Start the server:**
```bash
cd src/OffworldLobbyServer.Api
dotnet run
```

3. **Redirect game traffic to your server:**
```bash
# Redirect OTC's server traffic to localhost
sudo iptables -t nat -A OUTPUT -p tcp --dport 8080 -d 3.90.142.156 -j DNAT --to-destination 127.0.0.1:5000
```

4. **Test the connection:**
```bash
curl -X POST http://localhost:5000/BlueBox/BlueBox.do \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "sfsHttp=null%7Cconnect%7Cnull"

# Expected response: connect|SESS_<session_id>
```

5. **Launch Offworld Trading Company** and test multiplayer functionality.

## How It Works

### Protocol Implementation
The server implements SmartFoxServer's BlueBox HTTP protocol used by Offworld Trading Company:

- **HTTP Transport**: Game communicates via HTTP POST to `/BlueBox/BlueBox.do`
- **Command Format**: Pipe-separated commands: `{sessionId}|{command}|{data}`
- **Message Types**: Session management, player authentication, lobby operations
- **Binary Protocol**: SFS2X message format with proper serialization

### Network Architecture
```
Game Client → HTTP Redirect → Community Server → Lobby Management
     ↓              ↓               ↓              ↓
Port 8080 →    iptables rule →  BlueBox API →   Session/Room
```

### Core Components
- **BlueBox Controller**: HTTP endpoint matching original servlet behavior
- **Protocol Processor**: SFS2X binary message handling with confirmed specifications
- **Session Manager**: Player authentication and connection tracking
- **Room Manager**: Lobby creation, joining, and game coordination

## Current Implementation Status

### ✅ **Working**
- BlueBox HTTP protocol endpoint
- Player authentication and login flow
- Session management (connect/poll/disconnect)
- Basic lobby creation and joining
- SFS2X binary message processing
- Extension request handling ("registerData", etc.)

### 🚧 **In Development**
- Complete lobby settings management (21-element configuration array)
- Room variables and dynamic updates
- Enhanced error handling and edge cases
- Comprehensive protocol testing

### 📋 **Planned**
- Advanced room management features
- Performance optimization and load testing
- Docker deployment configuration
- Comprehensive documentation

## Project Structure

```
src/
├── OffworldLobbyServer.Api/           # HTTP API and BlueBox endpoint
├── OffworldLobbyServer.Core/          # Business logic and interfaces
├── OffworldLobbyServer.Infrastructure/ # Service implementations
└── OffworldLobbyServer.Shared/        # Common models and SFS2X protocol

docs/ReverseEngineeringNotes.md        # Detailed protocol specifications
DECOMPILED_GAME_CLASSES/               # Original game code analysis
```

## Community & Contributing

### 💬 Join the Discussion
**Matrix Room**: [#otc-server-creation:matrix.stormcloud.host](https://matrix.to/#/#otc-server-creation:matrix.stormcloud.host)

The Matrix room is the best place to:
- Coordinate testing efforts
- Discuss protocol implementation details
- Share findings and troubleshoot issues
- Plan new features and improvements

### How to Contribute

1. **Protocol Testing**: Help test with real game clients and report compatibility issues
2. **Implementation**: Work on lobby features, room management, or protocol edge cases  
3. **Documentation**: Improve setup guides, protocol documentation, or code comments
4. **Analysis**: Reverse engineer additional game features or protocol details

**Getting Started:**
1. Read [`docs/ReverseEngineeringNotes.md`](docs/ReverseEngineeringNotes.md) for technical details
2. Join the Matrix room to coordinate with other contributors
3. Set up your development environment and test the current implementation
4. Pick an area to contribute and start coding!

## Technical Resources

- **[Protocol Specifications](docs/ReverseEngineeringNotes.md)**: Complete technical documentation
- **[Protocol Skew Analysis](protocol-skew.md)**: Implementation compliance details
- **Matrix Room**: Real-time discussion and support

## Important Notes

- **Game Preservation**: This project aims to preserve Offworld Trading Company's multiplayer functionality
- **Clean Room**: All protocol implementation is based on reverse engineering, not original code
- **Alpha Status**: Under active development; expect bugs and incomplete features
- **Community Driven**: Success depends on community testing and contributions

## License

This project is developed for educational and game preservation purposes. See LICENSE for details.

---

**Ready to help restore OTC multiplayer? Join us in the [Matrix room](https://matrix.to/#/#otc-server-creation:matrix.stormcloud.host)!**