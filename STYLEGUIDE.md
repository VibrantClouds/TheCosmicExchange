# Offworld Lobby Server - Style Guide

This document outlines coding conventions and style guidelines for the Offworld Lobby Server project. Following these guidelines ensures consistency, readability, and maintainability across the codebase.

## 📋 Table of Contents

- [General Principles](#general-principles)
- [C# Coding Standards](#c-coding-standards)
- [Automated Linting](#automated-linting)
- [Project Structure](#project-structure)
- [Naming Conventions](#naming-conventions)
- [Documentation](#documentation)
- [Git Commit Messages](#git-commit-messages)
- [Testing Guidelines](#testing-guidelines)
- [Architecture Patterns](#architecture-patterns)

## 🎯 General Principles

### Code Quality
- **Readability over cleverness** - Write code that is easy to understand
- **Consistency** - Follow established patterns within the codebase
- **SOLID principles** - Write maintainable, extensible code
- **DRY (Don't Repeat Yourself)** - Avoid code duplication
- **KISS (Keep It Simple, Stupid)** - Prefer simple solutions

### Performance
- **Protocol accuracy first** - Maintain exact compatibility with original BlueBox protocol
- **Optimize only when needed** - Profile before optimizing
- **Memory conscious** - Be aware of allocations in hot paths

## 🔧 C# Coding Standards

### General Formatting

Follow Microsoft's official C# coding conventions with these specifics:

```csharp
// ✅ Good - Clear, consistent formatting
public class SessionManager : ISessionManager
{
	private readonly ILogger<SessionManager> _logger;
	private readonly ConcurrentDictionary<string, SessionInfo> _sessions;
	
	public SessionManager(ILogger<SessionManager> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_sessions = new ConcurrentDictionary<string, SessionInfo>();
	}
	
	public async Task<SessionInfo> CreateSessionAsync(string clientIP)
	{
		var sessionId = GenerateSessionId();
		var session = new SessionInfo
		{
			SessionId = sessionId,
			ClientIP = clientIP,
			CreatedAt = DateTime.UtcNow,
			LastActivity = DateTime.UtcNow
		};
		
		_sessions.TryAdd(sessionId, session);
		_logger.LogInformation("Created session {SessionId} for {ClientIP}", sessionId, clientIP);
		
		return session;
	}
}
```

### Indentation and Spacing
- **Tabs for indentation** (not spaces)
- **Single space** after control flow keywords (`if`, `for`, `while`)
- **No trailing whitespace**
- **Empty line** between methods and logical sections

### Braces
- **Allman style** - Opening brace on new line
- **Always use braces** for single-line if/for/while statements

```csharp
// ✅ Good
if (condition)
{
	DoSomething();
}

// ❌ Bad
if (condition) DoSomething();
```

### Variable Declarations
- **One declaration per line**
- **Initialize variables** where possible
- **Use var** for obvious types, explicit types for clarity

```csharp
// ✅ Good
var sessionId = Guid.NewGuid().ToString();
string clientIP = GetClientIP();
SessionInfo session = null;

// ❌ Bad  
var session = null; // Type not obvious
string sessionId = Guid.NewGuid().ToString(); // Type obvious from right side
```

## ⚙️ Automated Linting

The project includes automated linting and code analysis to enforce these style guidelines. The configuration files automatically enforce:

### Configuration Files

**`.editorconfig`** - Cross-editor formatting rules:
- Tab indentation (size 4)
- LF line endings
- UTF-8 encoding
- Trim trailing whitespace
- Insert final newline

**`Directory.Build.props`** - MSBuild analyzer configuration:
- Enables .NET analyzers
- Includes StyleCop.Analyzers
- Enforces code style in build
- References custom ruleset

**`CodeAnalysis.ruleset`** - Custom analyzer rules:
- Microsoft.CodeAnalysis.NetAnalyzers rules
- StyleCop.Analyzers with project-specific customizations
- Performance, design, usage, and security rules
- Tab indentation enforcement (SA1027)

**`stylecop.json`** - StyleCop configuration:
- Tab indentation settings
- Documentation rules configuration
- Naming conventions enforcement
- File layout rules

### Build Integration

The linting rules are enforced during:
- **Build time** - Code analysis runs with `dotnet build`
- **IDE integration** - Real-time feedback in Visual Studio/VS Code
- **CI/CD** - Automated validation in build pipelines

### Running Lints

```bash
# Build with full code analysis
dotnet build

# Run only code analysis without building
dotnet build --verbosity normal

# Build treating warnings as errors (for CI)
dotnet build -p:TreatWarningsAsErrors=true
```

### Key Enforced Rules

- **SA1027**: Use tabs correctly (enforces tab indentation)
- **SA1309**: Disabled (allows `_privateField` naming)
- **SX1309**: Enabled (requires underscore prefix for private fields)
- **SA1101**: Disabled (no `this.` prefix requirement)
- **SA1633-SA1641**: Disabled (no file headers required)
- **CA2007**: Disabled (ConfigureAwait not needed in ASP.NET Core)

### IDE Configuration

**Visual Studio**: The `.editorconfig` and analyzer packages automatically configure the IDE.

**VS Code**: Install the C# extension. Settings are applied automatically.

**JetBrains Rider**: EditorConfig support is built-in.

### Suppressing Rules

For legitimate cases where rules need to be suppressed:

```csharp
// Suppress specific rule with justification
#pragma warning disable CA1303 // Do not pass literals as localized parameters
public const string ProtocolErrorPrefix = "err01|"; // BlueBox protocol constant
#pragma warning restore CA1303

// Suppress with attribute (for classes/methods)
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", 
    Justification = "Protocol compatibility class - documented in external spec")]
public class SFSArray
{
    // Implementation
}
```

### Adding New Rules

To modify linting rules:

1. Edit `CodeAnalysis.ruleset` for rule severity changes
2. Update `stylecop.json` for StyleCop-specific settings  
3. Modify `.editorconfig` for cross-editor formatting rules
4. Test changes with `dotnet build`

### Handling Existing Code

Existing files may show warnings after enabling linting rules. To fix formatting issues:

```bash
# Format all files automatically (if using dotnet format)
dotnet format

# Or manually fix using your IDE:
# - Visual Studio: Edit -> Advanced -> Format Document
# - VS Code: Shift+Alt+F (or Ctrl+Shift+P -> "Format Document")
# - Rider: Ctrl+Alt+L
```

**Note**: When editing existing files, IDEs will automatically apply the new formatting rules from `.editorconfig`.

## 📁 Project Structure

### Clean Architecture Layers

```
src/
├── OffworldLobbyServer.Api/           # Presentation Layer
│   ├── Controllers/                   # HTTP controllers
│   ├── Middleware/                    # Custom middleware
│   ├── Filters/                       # Action filters
│   └── Extensions/                    # DI extensions
├── OffworldLobbyServer.Core/          # Application Layer
│   ├── Interfaces/                    # Service contracts
│   ├── Models/                        # Domain models
│   ├── Services/                      # Business logic
│   ├── Exceptions/                    # Custom exceptions
│   └── Validators/                    # Input validation
├── OffworldLobbyServer.Infrastructure/ # Infrastructure Layer
│   ├── Services/                      # External concerns
│   ├── Persistence/                   # Data access
│   └── External/                      # Third-party integrations
└── OffworldLobbyServer.Shared/        # Cross-cutting Concerns
    ├── Constants/                     # Application constants
    ├── Extensions/                    # Utility extensions
    └── Models/                        # Shared DTOs
```

### File Organization
- **One public class per file**
- **File name matches class name**
- **Organize using statements** (System first, then third-party, then project)

```csharp
// ✅ Good - SessionManager.cs
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;

namespace OffworldLobbyServer.Infrastructure.Services
{
	public class SessionManager : ISessionManager
	{
		// Implementation
	}
}
```

## 🏷️ Naming Conventions

### Classes and Methods
- **PascalCase** for classes, methods, properties, events
- **Descriptive names** that explain purpose
- **Verb phrases** for methods, **noun phrases** for classes

```csharp
// ✅ Good
public class BlueBoxMessageProcessor
{
	public async Task<string> ProcessIncomingMessage(string rawMessage)
	{
		// Implementation
	}
}

// ❌ Bad
public class BBMsgProc
{
	public async Task<string> DoStuff(string msg)
	{
		// Implementation  
	}
}
```

### Fields and Variables
- **camelCase** for local variables and method parameters
- **_camelCase** with underscore prefix for private fields
- **PascalCase** for constants

```csharp
// ✅ Good
public class SessionManager
{
	private readonly ILogger<SessionManager> _logger;
	private const int DefaultTimeoutMinutes = 30;
	
	public async Task ProcessSession(string sessionId)
	{
		var currentTime = DateTime.UtcNow;
		// Implementation
	}
}
```

### Interfaces
- **I prefix** followed by descriptive name
- **Focused contracts** - single responsibility

```csharp
// ✅ Good
public interface ISessionManager
{
	Task<SessionInfo> CreateSessionAsync(string clientIP);
	Task<bool> ValidateSessionAsync(string sessionId);
	Task DisconnectSessionAsync(string sessionId);
}

// ❌ Bad
public interface IManager // Too generic
{
	Task DoEverything();
}
```

### SFS2X Protocol Naming
- **Preserve original names** from decompiled classes when possible
- **Add comments** explaining protocol significance

```csharp
// ✅ Good - Matches original protocol
public class SFSArray
{
	// Original SFS2X array structure for BlueBox compatibility
	public void AddInt(int value) { }
	public void AddBool(bool value) { }
}

// Extension methods for clarity
public static class SFSArrayExtensions
{
	public static void AddLobbyName(this SFSArray array, string lobbyName)
	{
		// Index 0: Lobby name (as per protocol spec)
		array.AddUtfString(lobbyName);
	}
}
```

## 📚 Documentation

### XML Documentation
- **All public members** must have XML documentation
- **Include examples** for complex methods
- **Document parameters and return values**

```csharp
/// <summary>
/// Processes incoming BlueBox protocol messages and returns appropriate responses.
/// Handles the pipe-separated command format: sessionId|command|data
/// </summary>
/// <param name="sessionId">The client session identifier</param>
/// <param name="command">BlueBox command (connect|poll|data|disconnect)</param>
/// <param name="data">Command payload data, may be base64 encoded</param>
/// <returns>Protocol-compliant response string</returns>
/// <exception cref="ArgumentException">Thrown when command format is invalid</exception>
/// <example>
/// <code>
/// var response = await ProcessBlueBoxCommand("SESS_123", "connect", "null");
/// // Returns: "connect|SESS_123"
/// </code>
/// </example>
public async Task<string> ProcessBlueBoxCommand(string sessionId, string command, string data)
{
	// Implementation
}
```

### Code Comments
- **Explain WHY, not WHAT** - Code should be self-documenting for what it does
- **Protocol references** - Link to specific protocol documentation
- **Reverse engineering notes** - Explain decisions based on original game analysis

```csharp
// ✅ Good - Explains why and references protocol
public string GenerateSessionId()
{
	// Original SFS2X uses SESS_ prefix followed by 16-character hex string
	// This maintains compatibility with client-side session validation
	// Reference: AmazonManager.cs line 247 in decompiled game code
	return $"SESS_{Guid.NewGuid():N}"[..20];
}

// ❌ Bad - States the obvious
public string GenerateSessionId()
{
	// Generate a session ID
	return $"SESS_{Guid.NewGuid():N}"[..20];
}
```

## 📝 Git Commit Messages

### Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Code style changes (formatting, etc.)
- **refactor**: Code refactoring
- **test**: Adding or updating tests
- **protocol**: Protocol implementation or compatibility changes

### Examples
```
feat(bluebox): implement poll command with message queuing

Add support for BlueBox poll command that handles long-polling
for queued messages. Maintains compatibility with original
SFS2X timeout behavior (30 second default).

References: docs/ReverseEngineeringNotes.md section 3.2
```

```
fix(session): correct session timeout calculation

Session cleanup was using local time instead of UTC, causing
premature timeouts for clients in different timezones.

Fixes #42
```

## 🧪 Testing Guidelines

### Test Structure
- **Arrange-Act-Assert** pattern
- **One assertion per test** when possible
- **Descriptive test names** that explain scenario

```csharp
// ✅ Good
[Test]
public async Task ProcessBlueBoxCommand_WithConnectCommand_ReturnsValidSessionId()
{
	// Arrange
	var processor = new BlueBoxMessageProcessor(_mockSessionManager.Object);
	
	// Act
	var result = await processor.ProcessBlueBoxCommand("null", "connect", "null");
	
	// Assert
	Assert.That(result, Does.StartWith("connect|SESS_"));
}

[Test] 
public async Task ProcessBlueBoxCommand_WithInvalidCommand_ReturnsErrorResponse()
{
	// Arrange
	var processor = new BlueBoxMessageProcessor(_mockSessionManager.Object);
	
	// Act
	var result = await processor.ProcessBlueBoxCommand("SESS_123", "invalid", "null");
	
	// Assert
	Assert.That(result, Is.EqualTo("err01|Unknown command"));
}
```

### Protocol Compatibility Tests
- **Test exact protocol responses** against known good responses
- **Include edge cases** from original game behavior
- **Base64 encoding tests** for data payloads

```csharp
[Test]
public void SFSArray_SerializesToBase64_MatchesOriginalFormat()
{
	// Arrange - Create lobby settings array matching game format
	var lobbySettings = new SFSArray();
	lobbySettings.AddUtfString("Test Lobby");           // Index 0: Lobby name
	lobbySettings.AddInt(1);                            // Index 1: Lobby type
	// ... add remaining 19 elements
	
	// Act
	var serialized = lobbySettings.ToBase64();
	
	// Assert - Compare with known good response from packet capture
	var expected = "known_good_base64_from_packet_capture";
	Assert.That(serialized, Is.EqualTo(expected));
}
```

## 🏗️ Architecture Patterns

### Dependency Injection
- **Constructor injection** preferred
- **Interface segregation** - focused contracts
- **Validate dependencies** in constructor

```csharp
// ✅ Good
public class BlueBoxController : ControllerBase
{
	private readonly ISessionManager _sessionManager;
	private readonly IRoomManager _roomManager;
	private readonly ILogger<BlueBoxController> _logger;
	
	public BlueBoxController(
		ISessionManager sessionManager,
		IRoomManager roomManager, 
		ILogger<BlueBoxController> logger)
	{
		_sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
		_roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
}
```

### Error Handling
- **Fail fast** for programming errors
- **Graceful degradation** for expected errors
- **Protocol-compliant error responses** for client errors

```csharp
// ✅ Good
public async Task<string> HandlePollCommand(string sessionId)
{
	try
	{
		// Validate session exists - expected error
		if (!await _sessionManager.ValidateSessionAsync(sessionId))
		{
			_logger.LogWarning("Poll request for invalid session: {SessionId}", sessionId);
			return "err01|Invalid http session !"; // Exact original error message
		}
		
		var message = await _sessionManager.GetNextMessageAsync(sessionId);
		return message != null ? $"poll|{message}" : "poll|null";
	}
	catch (Exception ex)
	{
		// Unexpected error - log and return generic error
		_logger.LogError(ex, "Unexpected error in poll command for session {SessionId}", sessionId);
		return "err01|Internal server error";
	}
}
```

### Async/Await
- **Async all the way** - avoid sync over async
- **ConfigureAwait(false)** in libraries (not needed in ASP.NET Core controllers)
- **Meaningful method names** with Async suffix

```csharp
// ✅ Good
public async Task<SessionInfo> CreateSessionAsync(string clientIP)
{
	var session = new SessionInfo
	{
		SessionId = GenerateSessionId(),
		ClientIP = clientIP,
		CreatedAt = DateTime.UtcNow
	};
	
	await _repository.SaveSessionAsync(session);
	return session;
}
```

## 🔍 Code Review Checklist

### Before Submitting PR
- [ ] Code follows style guidelines
- [ ] All tests pass
- [ ] Protocol compatibility verified
- [ ] Documentation updated
- [ ] No sensitive information committed
- [ ] Performance impact considered
- [ ] Error handling implemented

### Review Focus Areas
- [ ] **Protocol accuracy** - Does it match original behavior?
- [ ] **Error handling** - Are edge cases covered?
- [ ] **Performance** - Any obvious bottlenecks?
- [ ] **Security** - No information leakage?
- [ ] **Testability** - Can this be easily tested?
- [ ] **Maintainability** - Is the code clear and extensible?

---

## 📞 Questions?

If you have questions about these guidelines or need clarification on specific scenarios:

1. Check existing code for established patterns
2. Refer to the [reverse engineering notes](docs/ReverseEngineeringNotes.md)
3. Open an issue for discussion
4. Follow the principle: **Protocol compatibility first, clean code second**

Remember: The primary goal is maintaining exact compatibility with the original Offworld Trading Company multiplayer protocol while building a maintainable, extensible server implementation.