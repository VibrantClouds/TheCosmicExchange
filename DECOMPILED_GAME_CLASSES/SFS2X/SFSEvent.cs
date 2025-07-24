

// Sfs2X.Core.SFSEvent
using System.Collections.Generic;
using Sfs2X.Core;

/// <summary>
/// This class represents most of the events dispatched by the SmartFoxServer 2X C# API.
/// </summary>
///
/// <seealso cref="M:Sfs2X.SmartFox.AddEventListener(System.String,Sfs2X.Core.EventListenerDelegate)" />
/// <seealso cref="T:Sfs2X.Core.SFSBuddyEvent" />
public class SFSEvent : BaseEvent
{
	/// <exclude />
	public static readonly string HANDSHAKE = "handshake";

	/// <summary>
	/// Dispatched when the result of the UDP handshake is notified.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to a call to the <see cref="M:Sfs2X.SmartFox.InitUDP(System.String,System.Int32)">SmartFox.InitUDP()</see> method.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>success</term>
	///     <description>(<b>bool</b>) <c>true</c> if UDP connection initialization is successful, <c>false</c> otherwise.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="M:Sfs2X.SmartFox.InitUDP(System.String,System.Int32)" /> example.</example>
	///
	/// <seealso cref="M:Sfs2X.SmartFox.InitUDP(System.String,System.Int32)" />
	public static readonly string UDP_INIT = "udpInit";

	/// <summary>
	/// Dispatched when a connection between the client and a SmartFoxServer 2X instance is attempted.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to a call to the <see cref="M:Sfs2X.SmartFox.Connect(System.String,System.Int32)" /> method.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>success</term>
	///     <description>(<b>bool</b>) The connection result: <c>true</c> if a connection was established, <c>false</c> otherwise.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="M:Sfs2X.SmartFox.Connect(System.String,System.Int32)" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_RETRY" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_RESUME" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_LOST" />
	public static readonly string CONNECTION = "connection";

	/// <summary>
	/// Dispatched when a new lag value measurement is available.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired when the automatic lag monitoring is turned on by passing true to the <see cref="M:Sfs2X.SmartFox.EnableLagMonitor(System.Boolean,System.Int32,System.Int32)">SmartFox.EnableLagMonitor</see> method.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>lagValue</term>
	///     <description>(<b>int</b>) The average of the last ten measured lag values, expressed in milliseconds.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <seealso cref="M:Sfs2X.SmartFox.EnableLagMonitor(System.Boolean,System.Int32,System.Int32)" />
	public static readonly string PING_PONG = "pingPong";

	/// <summary>
	/// Dispatched when a low level socket error is detected, for example bad/inconsistent data.
	/// </summary>
	///
	/// <remarks>
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) The description of the error.</description>
	///   </item>
	/// </list>
	/// </remarks>
	public static readonly string SOCKET_ERROR = "socketError";

	/// <summary>
	/// Dispatched when the connection between the client and the SmartFoxServer 2X instance is interrupted.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to a call to the <see cref="M:Sfs2X.SmartFox.Disconnect" /> method or when the connection between the client and the server is interrupted for other reasons.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters.
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>reason</term>
	///     <description>(<b>string</b>) The reason of the disconnection, among those available in the <see cref="T:Sfs2X.Util.ClientDisconnectionReason" /> class.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>
	/// The following example handles a disconnection event:
	/// <code>
	/// void SomeMethod() {
	/// 	sfs.AddEventListener(SFSEvent.OnConnectionLost, OnConnectionLost);
	/// }
	///
	/// void OnConnectionLost(BaseEvent evt) {
	/// 	Console.WriteLine("Connection was lost, Reason: " + (string)evt.Params["reason"]);                      // .Net / Unity
	/// 	System.Diagnostics.Debug.WriteLine("Connection was lost, Reason: " + (string)evt.Params["reason"]);     // UWP
	/// }
	/// </code>
	/// </example>
	///
	/// <seealso cref="T:Sfs2X.Util.ClientDisconnectionReason" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_RETRY" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_RESUME" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION" />
	public static readonly string CONNECTION_LOST = "connectionLost";

	/// <summary>
	/// Dispatched when the connection between the client and the SmartFoxServer 2X instance is interrupted abruptly while the SmartFoxServer 2X HRC system is available in the Zone.
	/// </summary>
	///
	/// <remarks>
	/// The HRC system allows a broken connection to be re-established transparently within a certain amount of time, without losing any of the current application state.
	/// For example this allows any player to get back to a game without loosing the match because of a sloppy internet connection.<br />
	/// When this event is dispatched the API enter a "freeze" mode where no new requests can be sent until the reconnection is successfully performed.
	/// It is highly recommended to handle this event and freeze the application interface accordingly until the <see cref="F:Sfs2X.Core.SFSEvent.CONNECTION_RESUME" /> event is fired,
	/// or the reconnection fails and the user is definitely disconnected and the <see cref="F:Sfs2X.Core.SFSEvent.CONNECTION_LOST" /> event is fired.
	/// <para />
	/// No parameters are available for this event object.
	/// </remarks>
	///
	/// <example>
	/// The following example shows how to handle a reconnection
	/// <code>
	/// void SomeMethod() {
	/// 	sfs.AddEventListener(SFSEvent.CONNECTION_RETRY, OnConnectionRetry);
	/// 	sfs.AddEventListener(SFSEvent.CONNECTION_RESUME, OnConnectionResume);
	/// 	sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
	/// }
	///
	/// void OnConnectionRetry(BaseEvent evt) {
	/// 	// Freeze your GUI and provide some feedback to the Player
	/// 	...
	/// }
	///
	/// void OnConnectionResume(BaseEvent evt) {
	/// 	// Unfreeze the GUI and let the player continue with the game
	/// 	...
	/// }
	///
	/// void OnConnectionLost(BaseEvent evt) {
	/// 	Console.WriteLine("Ouch, connection was lost! Reason: " + (string)evt.Params["reason"]);                        // .Net / Unity
	/// 	System.Diagnostics.Debug.WriteLine("Ouch, connection was lost! Reason: " + (string)evt.Params["reason"]);       // UWP
	/// }
	/// </code>
	/// </example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_LOST" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_RESUME" />
	public static readonly string CONNECTION_RETRY = "connectionRetry";

	/// <summary>
	/// Dispatched when the connection between the client and the SmartFoxServer 2X instance is re-established after a temporary disconnection,
	/// while the SmartFoxServer 2X HRC system is available in the Zone.
	/// </summary>
	///
	/// <remarks>
	/// The HRC system allows a broken connection to be re-established transparently within a certain amount of time, without losing any of the current application state.
	/// For example this allows any player to get back to a game without loosing the match because of a sloppy internet connection.<br />
	/// When this event is dispatched the application interface should be reverted to the state it had before the disconnection.
	/// In case the reconnection attempt fails, the <see cref="F:Sfs2X.Core.SFSEvent.CONNECTION_LOST" /> event is fired.
	/// <para />
	/// No parameters are available for this event object.
	/// </remarks>
	///
	/// <example>See the <see cref="F:Sfs2X.Core.SFSEvent.CONNECTION_RETRY" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_LOST" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_RETRY" />
	public static readonly string CONNECTION_RESUME = "connectionResume";

	/// <summary>
	/// Dispatched when the client cannot establish a socket connection to the server and the useBlueBox parameter is active in the configuration.
	/// </summary>
	///
	/// <remarks>
	/// The event can be used to notify the user that a second connection attempt is running, using the BlueBox (HTTP tunnelling).
	/// <para />
	/// No parameters are available for this event object.
	/// </remarks>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONNECTION_LOST" />
	public static readonly string CONNECTION_ATTEMPT_HTTP = "connectionAttemptHttp";

	/// <summary>
	/// Dispatched when the external client configuration file is loaded successfully.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to a call to the <see cref="M:Sfs2X.SmartFox.LoadConfig(System.String,System.Boolean)" /> method, but only if the <em>connectOnSuccess</em> argument of that method is set to <c>false</c>;
	/// otherwise the connection is attempted and the related <see cref="F:Sfs2X.Core.SFSEvent.CONNECTION" /> event type is fired.
	/// <para />
	/// No parameters are available for this event object.
	/// </remarks>
	///
	/// <example>See the <see cref="M:Sfs2X.SmartFox.LoadConfig(System.String,System.Boolean)" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONFIG_LOAD_FAILURE" />
	/// <seealso cref="M:Sfs2X.SmartFox.LoadConfig(System.String,System.Boolean)" />
	public static readonly string CONFIG_LOAD_SUCCESS = "configLoadSuccess";

	/// <summary>
	/// Dispatched when an error occurs while loading the external SmartFox configuration file.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to a call to the <see cref="M:Sfs2X.SmartFox.LoadConfig(System.String,System.Boolean)" /> method, typically when the configuration file is not found or it isn't accessible (no read permissions).
	/// <para />
	/// No parameters are available for this event object.
	/// </remarks>
	///
	/// <example>See the <see cref="M:Sfs2X.SmartFox.LoadConfig(System.String,System.Boolean)" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.CONFIG_LOAD_SUCCESS" />
	/// <seealso cref="M:Sfs2X.SmartFox.LoadConfig(System.String,System.Boolean)" />
	public static readonly string CONFIG_LOAD_FAILURE = "configLoadFailure";

	/// <summary>
	/// Dispatched when the current user performs a successful login in a server Zone.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.LoginRequest" /> request.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>user</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the user who performed the login.</description>
	///   </item>
	///   <item>
	///     <term>data</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Data.ISFSObject" /></b>) An object containing custom parameters returned by a custom login system, if any.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.LoginRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.LoginRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.LOGIN_ERROR" />
	public static readonly string LOGIN = "login";

	/// <summary>
	/// Dispatched if an error occurs while the user login is being performed.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.LoginRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.LoginRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.LOGIN" />
	/// <seealso cref="T:Sfs2X.Requests.LoginRequest" />
	public static readonly string LOGIN_ERROR = "loginError";

	/// <summary>
	/// Dispatched when the current user performs logs out of the server Zone.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.LogoutRequest" /> request.
	/// <para />
	/// No parameters are available for this event object.
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.LogoutRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.LogoutRequest" />
	public static readonly string LOGOUT = "logout";

	/// <summary>
	/// Dispatched when a new Room is created inside the Zone under any of the Room Groups that the client subscribed.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.CreateRoomRequest" /> and <see cref="T:Sfs2X.Requests.Game.CreateSFSGameRequest" /> requests in case the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room that was created.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.CreateRoomRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_CREATION_ERROR" />
	/// <seealso cref="T:Sfs2X.Requests.CreateRoomRequest" />
	/// <seealso cref="T:Sfs2X.Requests.Game.CreateSFSGameRequest" />
	public static readonly string ROOM_ADD = "roomAdd";

	/// <summary>
	/// Dispatched when a Room belonging to one of the Groups subscribed by the client is removed from the Zone.
	/// </summary>
	///
	/// <remarks>
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room that was removed.</description>
	///   </item>
	/// </list>
	/// </remarks>
	public static readonly string ROOM_REMOVE = "roomRemove";

	/// <summary>
	/// Dispatched if an error occurs while creating a new Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.CreateRoomRequest" /> and <see cref="T:Sfs2X.Requests.Game.CreateSFSGameRequest" /> requests in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.CreateRoomRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_ADD" />
	/// <seealso cref="T:Sfs2X.Requests.CreateRoomRequest" />
	/// <seealso cref="T:Sfs2X.Requests.Game.CreateSFSGameRequest" />
	public static readonly string ROOM_CREATION_ERROR = "roomCreationError";

	/// <summary>
	/// Dispatched when a Room is joined by the current user.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.JoinRoomRequest" /> and <see cref="T:Sfs2X.Requests.Game.QuickJoinGameRequest" /> requests in case the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room that was joined.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.JoinRoomRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_JOIN_ERROR" />
	/// <seealso cref="T:Sfs2X.Requests.JoinRoomRequest" />
	/// <seealso cref="T:Sfs2X.Requests.Game.QuickJoinGameRequest" />
	public static readonly string ROOM_JOIN = "roomJoin";

	/// <summary>
	/// Dispatched when an error occurs while the current user is trying to join a Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.JoinRoomRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.JoinRoomRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_JOIN" />
	/// <seealso cref="T:Sfs2X.Requests.JoinRoomRequest" />
	public static readonly string ROOM_JOIN_ERROR = "roomJoinError";

	/// <summary>
	/// Dispatched when one of the Rooms joined by the current user is entered by another user.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by a <see cref="T:Sfs2X.Requests.JoinRoomRequest" /> request;
	/// it might be fired or not depending on the Room configuration defined upon its creation (see the <see cref="P:Sfs2X.Requests.RoomSettings.Events">RoomSettings.Events</see> setting).
	/// <para />
	/// <b>NOTE</b>: if the Room is of type <see cref="T:Sfs2X.Entities.MMORoom">MMORoom</see>, this event is never fired and it is substituted by the <see cref="F:Sfs2X.Core.SFSEvent.PROXIMITY_LIST_UPDATE" /> event.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>user</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the user who joined the Room.</description>
	///   </item>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room that was joined by a user.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>
	/// The following example shows how to handle this event type:
	/// <code>
	/// void SomeMethod() {
	/// 	sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
	/// }
	///
	/// void OnUserEnterRoom(BaseEvent evt) {
	/// 	Room room = (Room)evt.Params["room"];
	/// 	User user = (User)evt.Params["user"];
	///
	/// 	Console.WriteLine("User: " + user.Name + " has just joined Room: " + room.Name);                        // .Net / Unity
	/// 	System.Diagnostics.Debug.WriteLine("User: " + user.Name + " has just joined Room: " + room.Name);       // UWP
	/// }
	/// </code>
	/// </example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.USER_EXIT_ROOM" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_JOIN" />
	/// <seealso cref="T:Sfs2X.Requests.JoinRoomRequest" />
	public static readonly string USER_ENTER_ROOM = "userEnterRoom";

	/// <summary>
	/// Dispatched when one of the Rooms joined by the current user is left by another user, or by the current user himself.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by a <see cref="T:Sfs2X.Requests.LeaveRoomRequest" /> request;
	/// it might be fired or not depending on the Room configuration defined upon its creation (see the <see cref="P:Sfs2X.Requests.RoomSettings.Events">RoomSettings.Events</see> setting).
	/// <para />
	/// <b>NOTE</b>: if the Room is of type <see cref="T:Sfs2X.Entities.MMORoom">MMORoom</see>, this event is never fired and it is substituted by the <see cref="F:Sfs2X.Core.SFSEvent.PROXIMITY_LIST_UPDATE" /> event.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>user</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the user who left the Room.</description>
	///   </item>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room that was left by the user.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>
	/// The following example shows how to handle this event type:
	/// <code>
	/// void SomeMethod() {
	/// 	smartFox.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
	/// }
	///
	/// void OnUserExitRoom(BaseEvent evt) {
	/// 	Room room = (Room)evt.Params["room"];
	/// 	User user = (User)evt.Params["user"];
	///
	/// 	Console.WriteLine("User: " + user.Name + " has just left Room: " + room.Name);                          // .Net / Unity
	/// 	System.Diagnostics.Debug.WriteLine("User: " + user.Name + " has just left Room: " + room.Name);         // UWP
	/// }
	/// </code>
	/// </example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.USER_ENTER_ROOM" />
	/// <seealso cref="T:Sfs2X.Requests.LeaveRoomRequest" /> 
	public static readonly string USER_EXIT_ROOM = "userExitRoom";

	/// <summary>
	/// Dispatched when the number of users/players or spectators inside a Room changes.
	/// </summary>
	///
	/// <remarks>
	///
	/// This event is caused by a <see cref="T:Sfs2X.Requests.JoinRoomRequest" />  request or a <see cref="T:Sfs2X.Requests.LeaveRoomRequest" /> request.
	/// The Room must belong to one of the Groups subscribed by the current client; also this event might be fired or not depending on the
	/// Room configuration defined upon its creation (see the <see cref="P:Sfs2X.Requests.RoomSettings.Events">RoomSettings.Events</see> setting).
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room in which the users count changed.</description>
	///   </item>
	///   <item>
	///     <term>uCount</term>
	///     <description>(<b>int</b>) The new users count (players in case of Game Room).</description>
	///   </item>
	///   <item>
	///     <term>sCount</term>
	///     <description>(<b>int</b>) The new spectators count (Game Rooms only).</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>
	/// The following example shows how to handle this event type:
	/// <code>
	/// void SomeMethod() {
	/// 	sfs.AddEventListener(SFSEvent.USER_COUNT_CHANGE, OnUserCountChange);
	/// }
	///
	/// void OnUserCountChange(BaseEvent evt) {
	/// 	Room room = (Room)evt.Params["room"];
	/// 	int uCount = (int)evt.Params["uCount"];
	/// 	int sCount = (int)evt.Params["sCount"];
	///
	/// 	Console.WriteLine("Room: " + room.Name + " contains " + uCount + " users and " + sCount + " spectators");                           // .Net / Unity
	/// 	System.Diagnostics.Debug.WriteLine("Room: " + room.Name + " contains " + uCount + " users and " + sCount + " spectators");          // UWP
	/// }
	/// </code>
	/// </example>
	///
	/// <seealso cref="T:Sfs2X.Requests.JoinRoomRequest" /> 
	/// <seealso cref="T:Sfs2X.Requests.LeaveRoomRequest" /> 
	public static readonly string USER_COUNT_CHANGE = "userCountChange";

	/// <summary>
	/// Dispatched when a public message is received by the current user.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by a <see cref="T:Sfs2X.Requests.PublicMessageRequest" /> request sent by any user in the target Room, including the current user himself.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room at which the message is targeted.</description>
	///   </item>
	///   <item>
	///     <term>sender</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the user who sent the message.</description>
	///   </item>
	///   <item>
	///     <term>message</term>
	///     <description>(<b>string</b>) The message sent by the user.</description>
	///   </item>
	///   <item>
	///     <term>data</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Data.ISFSObject" /></b>) An object containing custom parameters which might accompany the message.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.PublicMessageRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.PublicMessageRequest" />
	public static readonly string PUBLIC_MESSAGE = "publicMessage";

	/// <summary>
	/// Dispatched when a private message is received by the current user.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by a PrivateMessageRequest request sent by any user in the Zone.
	/// <para />
	/// The same event is fired by the sender's client too, so that the user is aware that the message was delivered successfully to the recipient,
	/// and it can be displayed in the private chat area keeping the correct message ordering.
	/// In this case there is no default way to know who the message was originally sent to.
	/// As this information can be useful in scenarios where the sender is chatting privately with more than one user at the same time in separate windows or tabs
	/// (and we need to write his own message in the proper one), the data parameter can be used to store, for example, the id of the recipient user.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>sender</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the user who sent the message.</description>
	///   </item>
	///   <item>
	///     <term>message</term>
	///     <description>(<b>string</b>) The message sent by the user.</description>
	///   </item>
	///   <item>
	///     <term>data</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Data.ISFSObject" /></b>) An object containing custom parameters which might accompany the message.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.PrivateMessageRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.PrivateMessageRequest" />
	public static readonly string PRIVATE_MESSAGE = "privateMessage";

	/// <summary>
	/// Dispatched when the current user receives a message from a moderator user.
	/// </summary>
	///
	/// <remarks>
	/// This event can be caused by the <see cref="T:Sfs2X.Requests.ModeratorMessageRequest" />, <see cref="T:Sfs2X.Requests.KickUserRequest" /> or <see cref="T:Sfs2X.Requests.BanUserRequest" /> requests sent by a user with at least moderation privileges.
	/// Also, this event can be caused by a kick/ban action performed through the SmartFoxServer 2X Administration Tool.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>sender</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the moderator user who sent the message.</description>
	///   </item>
	///   <item>
	///     <term>message</term>
	///     <description>(<b>string</b>) The message sent by the moderator.</description>
	///   </item>
	///   <item>
	///     <term>data</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Data.ISFSObject" /></b>) An object containing custom parameters which might accompany the message.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ModeratorMessageRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.ModeratorMessageRequest" />
	public static readonly string MODERATOR_MESSAGE = "moderatorMessage";

	/// <summary>
	/// Dispatched when the current user receives a message from an administrator user.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by the <see cref="T:Sfs2X.Requests.AdminMessageRequest" /> request sent by a user with administration privileges.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>sender</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the administrator user who sent the message.</description>
	///   </item>
	///   <item>
	///     <term>message</term>
	///     <description>(<b>string</b>) The message sent by the administrator.</description>
	///   </item>
	///   <item>
	///     <term>data</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Data.ISFSObject" /></b>) An object containing custom parameters which might accompany the message.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.AdminMessageRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.AdminMessageRequest" />
	public static readonly string ADMIN_MESSAGE = "adminMessage";

	/// <summary>
	/// Dispatched when an object containing custom data is received by the current user.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by an <see cref="T:Sfs2X.Requests.ObjectMessageRequest" /> request sent by any user in the target Room.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>sender</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the user who sent the message.</description>
	///   </item>
	///   <item>
	///     <term>message</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Data.ISFSObject" /></b>) The content of the message: an object containing the custom parameters sent by the sender.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ObjectMessageRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.ObjectMessageRequest" />
	public static readonly string OBJECT_MESSAGE = "objectMessage";

	/// <summary>
	/// Dispatched when data coming from a server-side Extension is received by the current user.
	/// </summary>
	///
	/// <remarks>
	/// Data is usually sent by the server to one or more clients in response to an ExtensionRequest request, but not necessarily.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>cmd</term>
	///     <description>(<b>string</b>) The name of the command which identifies an action that should be executed by the client. If this event is fired in response to a request sent by the client, it is a common practice to use the same command name passed to the request also in the response.</description>
	///   </item>
	///   <item>
	///     <term>sourceRoom</term>
	///     <description>(<b>int</b>) <b>[DEPRECATED - Use <em>room</em> property]</b>  The id of the Room which the Extension is attached to (for Room Extensions only).</description>
	///   </item>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room which the Extension is attached to (for Room Extensions only).</description>
	///   </item>
	///   <item>
	///     <term>params</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Data.ISFSObject" /></b>) An object containing custom data sent by the Extension.</description>
	///   </item>
	///   <item>
	///     <term>packetId</term>
	///     <description>(<b>long</b>) The id of the packet when the UDP protocol is used. As this is an auto-increment value generated by the server, it can be useful to detect UDP packets received in the wrong order (for UDP communication only).</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ExtensionRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.ExtensionRequest" />
	/// <seealso cref="T:Sfs2X.Entities.Data.SFSObject" />
	public static readonly string EXTENSION_RESPONSE = "extensionResponse";

	/// <summary>
	/// Dispatched when a Room Variable is updated.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by the <see cref="T:Sfs2X.Requests.SetRoomVariablesRequest" /> request. The request could have been sent by a user in the same Room of the current user or,
	/// in case of a global Room Variable, by a user in a Room belonging to one of the Groups subscribed by the current client.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room where the Room Variable update occurred.</description>
	///   </item>
	///   <item>
	///     <term>changedVars</term>
	///     <description>(<b>List&lt;string&gt;</b>) the list of variable names that where modified or created</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.SetRoomVariablesRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.SetRoomVariablesRequest" />
	/// <seealso cref="T:Sfs2X.Entities.Variables.RoomVariable" />
	public static readonly string ROOM_VARIABLES_UPDATE = "roomVariablesUpdate";

	/// <summary>
	/// Dispatched when a User Variable is updated.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by the <see cref="T:Sfs2X.Requests.SetUserVariablesRequest" /> request sent by a user in one of the Rooms joined by the current user.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>user</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the user who updated his own User Variables.</description>
	///   </item>
	///   <item>
	///     <term>changedVars</term>
	///     <description>(<b>List&lt;string&gt;</b>) The list of names of the User Variables that were changed (or created for the first time).</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.SetUserVariablesRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.SetUserVariablesRequest" />
	/// <seealso cref="T:Sfs2X.Entities.Variables.UserVariable" />
	public static readonly string USER_VARIABLES_UPDATE = "userVariablesUpdate";

	/// <summary>
	/// Dispatched when a Group is subscribed by the current user.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.SubscribeRoomGroupRequest" /> request if the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>groupId</term>
	///     <description>(<b>string</b>) The name of the Group that was subscribed.</description>
	///   </item>
	///   <item>
	///     <term>newRooms</term>
	///     <description>(<b>List&lt;<see cref="T:Sfs2X.Entities.Room" />&gt;</b>) A list of objects representing the Rooms belonging to the subscribed Group.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.SubscribeRoomGroupRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.SubscribeRoomGroupRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_GROUP_SUBSCRIBE_ERROR" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_GROUP_UNSUBSCRIBE" />
	public static readonly string ROOM_GROUP_SUBSCRIBE = "roomGroupSubscribe";

	/// <summary>
	/// Dispatched when a Group is unsubscribed by the current user.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.UnsubscribeRoomGroupRequest" /> request if the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>groupId</term>
	///     <description>(<b>string</b>) The name of the Group that was unsubscribed.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.UnsubscribeRoomGroupRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.UnsubscribeRoomGroupRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_GROUP_UNSUBSCRIBE_ERROR" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_GROUP_SUBSCRIBE" />
	public static readonly string ROOM_GROUP_UNSUBSCRIBE = "roomGroupUnsubscribe";

	/// <summary>
	/// Dispatched when an error occurs while a Room Group is being subscribed.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.SubscribeRoomGroupRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.SubscribeRoomGroupRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.SubscribeRoomGroupRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_GROUP_SUBSCRIBE" />
	public static readonly string ROOM_GROUP_SUBSCRIBE_ERROR = "roomGroupSubscribeError";

	/// <summary>
	/// Dispatched when an error occurs while a Room Group is being unsubscribed.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.UnsubscribeRoomGroupRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.UnsubscribeRoomGroupRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.UnsubscribeRoomGroupRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_GROUP_UNSUBSCRIBE" />
	public static readonly string ROOM_GROUP_UNSUBSCRIBE_ERROR = "roomGroupUnsubscribeError";

	/// <summary>
	/// Dispatched when a spectator is turned to a player inside a Game Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.SpectatorToPlayerRequest" /> request if the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room in which the spectator is turned to player.</description>
	///   </item>
	///   <item>
	///     <term>user</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the spectator who was turned to player.</description>
	///   </item>
	///   <item>
	///     <term>playerId</term>
	///     <description>(<b>int</b>) The player id of the user.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.SpectatorToPlayerRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.SpectatorToPlayerRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.SPECTATOR_TO_PLAYER_ERROR" />
	public static readonly string SPECTATOR_TO_PLAYER = "spectatorToPlayer";

	/// <summary>
	/// Dispatched when a player is turned to a spectator inside a Game Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.PlayerToSpectatorRequest" /> request if the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room in which the player is turned to spectator.</description>
	///   </item>
	///   <item>
	///     <term>user</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the player who was turned to spectator.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.PlayerToSpectatorRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.PlayerToSpectatorRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.PLAYER_TO_SPECTATOR_ERROR" />
	public static readonly string PLAYER_TO_SPECTATOR = "playerToSpectator";

	/// <summary>
	/// Dispatched when an error occurs while the current user is being turned from spectator to player in a Game Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.SpectatorToPlayerRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.SpectatorToPlayerRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.SpectatorToPlayerRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.SPECTATOR_TO_PLAYER" />
	public static readonly string SPECTATOR_TO_PLAYER_ERROR = "spectatorToPlayerError";

	/// <summary>
	/// Dispatched when an error occurs while the current user is being turned from player to spectator in a Game Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.PlayerToSpectatorRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.PlayerToSpectatorRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.PlayerToSpectatorRequest" />
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.PLAYER_TO_SPECTATOR" />
	public static readonly string PLAYER_TO_SPECTATOR_ERROR = "playerToSpectatorError";

	/// <summary>
	/// Dispatched when the name of a Room is changed.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.ChangeRoomNameRequest" /> request if the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room which was renamed.</description>
	///   </item>
	///   <item>
	///     <term>oldName</term>
	///     <description>(<b>string</b>) The previous name of the Room.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ChangeRoomNameRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_NAME_CHANGE_ERROR" />
	/// <seealso cref="T:Sfs2X.Requests.ChangeRoomNameRequest" />
	public static readonly string ROOM_NAME_CHANGE = "roomNameChange";

	/// <summary>
	/// Dispatched when an error occurs while attempting to change the name of a Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.ChangeRoomNameRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ChangeRoomNameRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_NAME_CHANGE" />
	/// <seealso cref="T:Sfs2X.Requests.ChangeRoomNameRequest" />
	public static readonly string ROOM_NAME_CHANGE_ERROR = "roomNameChangeError";

	/// <summary>
	/// Dispatched when the password of a Room is set, changed or removed.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.ChangeRoomPasswordStateRequest" /> request if the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room whose password was changed.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ChangeRoomPasswordStateRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_PASSWORD_STATE_CHANGE_ERROR" />
	/// <seealso cref="T:Sfs2X.Requests.ChangeRoomPasswordStateRequest" />
	public static readonly string ROOM_PASSWORD_STATE_CHANGE = "roomPasswordStateChange";

	/// <summary>
	/// Dispatched when an error occurs while attempting to set, change or remove the password of a Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.ChangeRoomPasswordStateRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ChangeRoomPasswordStateRequest" /> example.</example>
	///
	/// <seealso cref="F:Sfs2X.Core.SFSEvent.ROOM_PASSWORD_STATE_CHANGE" />
	/// <seealso cref="T:Sfs2X.Requests.ChangeRoomPasswordStateRequest" />
	public static readonly string ROOM_PASSWORD_STATE_CHANGE_ERROR = "roomPasswordStateChangeError";

	/// <summary>
	/// Dispatched when the capacity of a Room is changed.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.ChangeRoomCapacityRequest" /> request if the operation is executed successfully.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Room" /></b>) An object representing the Room whose capacity was changed.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ChangeRoomCapacityRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.ChangeRoomCapacityRequest" />
	public static readonly string ROOM_CAPACITY_CHANGE = "roomCapacityChange";

	/// <summary>
	/// Dispatched when an error occurs while attempting to change the capacity of a Room.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.ChangeRoomCapacityRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.ChangeRoomCapacityRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.ChangeRoomCapacityRequest" />
	public static readonly string ROOM_CAPACITY_CHANGE_ERROR = "roomCapacityChangeError";

	/// <summary>
	/// Dispatched when a Rooms search is completed.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.FindRoomsRequest" /> request to return the search result.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>rooms</term>
	///     <description>(<b>List&lt;<see cref="T:Sfs2X.Entities.Room" />&gt;</b>) A list of Room objects representing the Rooms matching the search criteria. If no Room is found, the list is empty.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.FindRoomsRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.FindRoomsRequest" />
	public static readonly string ROOM_FIND_RESULT = "roomFindResult";

	/// <summary>
	/// Dispatched when a users search is completed
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.FindUsersRequest" /> request to return the search result.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>rooms</term>
	///     <description>(<b>List&lt;<see cref="T:Sfs2X.Entities.User" />&gt;</b>) A list of objects representing the users matching the search criteria. If no user is found, the list is empty.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.FindUsersRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.FindUsersRequest" />
	public static readonly string USER_FIND_RESULT = "userFindResult";

	/// <summary>
	/// Dispatched when the current user receives an invitation from another user.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by the <see cref="T:Sfs2X.Requests.Game.InviteUsersRequest" /> request; the user is supposed to reply using the <see cref="T:Sfs2X.Requests.Game.InvitationReplyRequest" /> request.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>invitation</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Invitation.Invitation" /></b>) An object representing the invitation received by the current user.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.Game.InvitationReplyRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.Game.InviteUsersRequest" />
	/// <seealso cref="T:Sfs2X.Requests.Game.InvitationReplyRequest" />
	/// <seealso cref="T:Sfs2X.Entities.Invitation.Invitation" />
	public static readonly string INVITATION = "invitation";

	/// <summary>
	/// Dispatched when the current user receives a reply to an invitation he sent previously.
	/// </summary>
	///
	/// <remarks>
	/// This event is caused by the <see cref="T:Sfs2X.Requests.Game.InvitationReplyRequest" /> request sent by the invitee.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>invitee</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.User" /></b>) An object representing the user who replied to the invitation.</description>
	///   </item>
	///   <item>
	///     <term>reply</term>
	///     <description>(<b>int</b>) The answer to the invitation among those available as constants in the <see cref="T:Sfs2X.Entities.Invitation.InvitationReply" /> class.</description>
	///   </item>
	///   <item>
	///     <term>data</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.Data.ISFSObject" /></b>) An object containing custom parameters, for example a message describing the reason of refusal.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.Game.InviteUsersRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Entities.Invitation.InvitationReply" />
	/// <seealso cref="T:Sfs2X.Requests.Game.InviteUsersRequest" />
	/// <seealso cref="T:Sfs2X.Requests.Game.InvitationReplyRequest" />
	public static readonly string INVITATION_REPLY = "invitationReply";

	/// <summary>
	/// Dispatched when an error occurs while the current user is sending a reply to an invitation he received.
	/// </summary>
	/// <remarks>
	/// This event is fired in response to the <see cref="T:Sfs2X.Requests.Game.InvitationReplyRequest" /> request in case the operation failed.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) A message containing the description of the error.</description>
	///   </item>
	///   <item>
	///     <term>errorCode</term>
	///     <description>(<b>short</b>) The error code.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.Game.InvitationReplyRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.Game.InvitationReplyRequest" />
	public static readonly string INVITATION_REPLY_ERROR = "invitationReplyError";

	/// <summary>
	/// Dispatched when one more users or one or more MMOItem objects enter/leave the current user's Area of Interest in MMORooms.
	/// </summary>
	/// <remarks>
	/// This event is fired after an MMORoom is joined and the <see cref="T:Sfs2X.Requests.MMO.SetUserPositionRequest" /> request is sent at least one time.
	/// <para />
	/// <b>NOTE</b>: this event substitutes the default <see cref="F:Sfs2X.Core.SFSEvent.USER_ENTER_ROOM" /> and <see cref="F:Sfs2X.Core.SFSEvent.USER_EXIT_ROOM" /> events available in regular Rooms.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///  <item>
	///     <term>room</term>
	///     <description>(<b>Room</b>) The Room where the event occurred</description>
	///   </item>
	///   <item>
	///     <term>addedUsers</term>
	///     <description>(<b>List&lt;User&gt;</b>) A list of User objects representing the users who entered the current user's Area of Interest.</description>
	///   </item>
	///   <item>
	///     <term>removedUsers</term>
	///     <description>(<b>List&lt;User&gt;</b>) A list of User objects representing the users who left the current user's Area of Interest.</description>
	///   </item>
	///   <item>
	///     <term>addedItems</term>
	///     <description>(<b>List&lt;IMMOItem&gt;</b>) A list of MMOItem objects which entered the current user's Area of Interest.</description>
	///   </item>
	///   <item>
	///     <term>removedItems</term>
	///     <description>(<b>List&lt;IMMOItem&gt;</b>) A list of MMOItem objects which left the current user's Area of Interest.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="T:Sfs2X.Requests.MMO.SetUserPositionRequest" /> example.</example>
	///
	/// <seealso cref="T:Sfs2X.Requests.MMO.SetUserPositionRequest" />
	/// <seealso cref="T:Sfs2X.Entities.MMORoom" />
	/// <seealso cref="T:Sfs2X.Entities.MMOItem" />
	public static readonly string PROXIMITY_LIST_UPDATE = "proximityListUpdate";

	/// <summary>
	/// Dispatched when an MMOItem Variable is updated in an MMORoom.
	/// </summary>
	/// <remarks>
	/// This event is caused by an MMOItem Variable being set, updated or deleted in a server side Extension, and it is received only if the current user has the related MMOItem in his Area of Interest.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>room</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.MMORoom" /></b>) The MMORoom where the MMOItem whose Variables have been updated is located.</description>
	///   </item>
	///   <item>
	///     <term>mmoItem</term>
	///     <description>(<b><see cref="T:Sfs2X.Entities.MMOItem" /></b>) The MMOItem whose variables have been updated.</description>
	///   </item>
	///   <item>
	///     <term>changedVars</term>
	///     <description>(<b>List&lt;string&gt;</b>) The list of names of the MMOItem Variables that were changed (or created for the first time).</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>
	/// The following example shows how to handle the MMOItem Variable update:
	/// <code>
	/// void SomeMethod() {
	/// 	sfs.AddEventListener(SFSEvent.MMOITEM_VARIABLES_UPDATE, OnMMOItemVarsUpdate);
	/// }
	///
	/// void OnMMOItemVarsUpdate(BaseEvent evt) {
	/// 	var changedVars = (List&lt;String&gt;)evt.Params["changedVars"];
	/// 	var item = (IMMOItem) evt.Params["mmoItem"];
	///
	/// 	// Check if the MMOItem was moved
	/// 	if (changedVars.Contains("x") || changedVars.Contains("y"))
	/// 	{
	/// 		// Move the sprite representing the MMOItem
	/// 		...
	/// 	}
	/// }
	/// </code>
	/// </example>
	///
	/// <seealso cref="T:Sfs2X.Entities.Variables.MMOItemVariable" />
	/// <seealso cref="T:Sfs2X.Entities.MMOItem" />
	/// <seealso cref="T:Sfs2X.Entities.MMORoom" />
	public static readonly string MMOITEM_VARIABLES_UPDATE = "mmoItemVariablesUpdate";

	/// <summary>
	/// Dispatched in return to the initialization of an encrypted connection.
	/// </summary>
	///
	/// <remarks>
	/// This event is fired in response to a call to the <see cref="M:Sfs2X.SmartFox.InitCrypto">SmartFox.InitCrypto()</see> method.
	/// <para />
	/// The <see cref="P:Sfs2X.Core.BaseEvent.Params" /> object contains the following parameters:
	/// <list type="table">
	///   <listheader>
	///     <term>Parameter</term>
	///     <description>Description</description>
	///   </listheader>
	///   <item>
	///     <term>success</term>
	///     <description>(<b>bool</b>) <c>true</c> if a unique encryption key was successfully retrieved via HTTPS, <c>false</c> if the transaction failed.</description>
	///   </item>
	///   <item>
	///     <term>errorMessage</term>
	///     <description>(<b>string</b>) If <em>success</em> is <c>false</c>, provides additional details on the occurred error.</description>
	///   </item>
	/// </list>
	/// </remarks>
	///
	/// <example>See the <see cref="M:Sfs2X.SmartFox.InitCrypto" /> example.</example>
	///
	/// <seealso cref="M:Sfs2X.SmartFox.InitCrypto" />
	public static readonly string CRYPTO_INIT = "cryptoInit";

	/// <exclude />
	public SFSEvent(string type, Dictionary<string, object> data)
		: base(type, data)
	{
	}

	/// <exclude />
	public SFSEvent(string type)
		: base(type)
	{
	}
}
