// AmazonManager
using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Offworld.GameCore;
using Offworld.GameCore.Text;
using Offworld.SystemCore;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Logging;
using Sfs2X.Requests;
using Sfs2X.Util;
using UnityEngine;

internal class AmazonManager : MDatabaseManager, MWebStatsManager, MLobbyManager
{
	[DynamoDBTable("Daily_Challenges")]
	public class DailyChallengeAmazonDBEntry
	{
		[DynamoDBHashKey]
		public string playerID;

		[DynamoDBGlobalSecondaryIndexHashKey(new string[] { })]
		[DynamoDBRangeKey]
		public int dailyID;

		[DynamoDBGlobalSecondaryIndexRangeKey(new string[] { })]
		[DynamoDBProperty]
		public int time;

		[DynamoDBProperty]
		public sbyte hqType;

		[DynamoDBProperty]
		public int matchID1;

		[DynamoDBProperty]
		public int matchID2;

		[DynamoDBProperty]
		public int matchID3;

		[DynamoDBProperty]
		public int matchID4;

		[DynamoDBProperty]
		public int versionNumber;

		[DynamoDBProperty]
		public string playerName;

		public DailyChallengeAmazonDBEntry()
		{
		}

		public DailyChallengeAmazonDBEntry(DailyChallengeEntry entry)
		{
			playerID = entry.playerID;
			dailyID = entry.dailyID;
			time = entry.time;
			hqType = entry.hqType;
			matchID1 = entry.matchID1;
			matchID2 = entry.matchID2;
			matchID3 = entry.matchID3;
			matchID4 = entry.matchID4;
			versionNumber = entry.versionNumber;
			playerName = entry.playerName;
		}

		public DailyChallengeEntry ReturnGenericEntry()
		{
			DailyChallengeEntry dailyChallengeEntry = new DailyChallengeEntry();
			dailyChallengeEntry.playerID = playerID;
			dailyChallengeEntry.dailyID = dailyID;
			dailyChallengeEntry.time = time;
			dailyChallengeEntry.hqType = hqType;
			dailyChallengeEntry.matchID1 = matchID1;
			dailyChallengeEntry.matchID2 = matchID2;
			dailyChallengeEntry.matchID3 = matchID3;
			dailyChallengeEntry.matchID4 = matchID4;
			dailyChallengeEntry.versionNumber = versionNumber;
			dailyChallengeEntry.playerName = playerName;
			return dailyChallengeEntry;
		}
	}

	[DynamoDBTable("Infinite_Map_Challenge")]
	public class MapChallengeAmazonDBEntry
	{
		[DynamoDBHashKey]
		public string playerID;

		[DynamoDBRangeKey]
		[DynamoDBGlobalSecondaryIndexHashKey(new string[] { })]
		public int mapNumber;

		[DynamoDBProperty]
		[DynamoDBGlobalSecondaryIndexRangeKey(new string[] { })]
		public int time;

		[DynamoDBProperty]
		public sbyte hqType;

		[DynamoDBProperty]
		public int matchID1;

		[DynamoDBProperty]
		public int matchID2;

		[DynamoDBProperty]
		public int matchID3;

		[DynamoDBProperty]
		public int matchID4;

		[DynamoDBProperty]
		public int versionNumber;

		[DynamoDBProperty]
		public string playerName;

		public MapChallengeAmazonDBEntry()
		{
		}

		public MapChallengeAmazonDBEntry(MapChallengeEntry entry)
		{
			playerID = entry.playerID;
			mapNumber = entry.mapNumber;
			time = entry.time;
			hqType = entry.hqType;
			matchID1 = entry.matchID1;
			matchID2 = entry.matchID2;
			matchID3 = entry.matchID3;
			matchID4 = entry.matchID4;
			versionNumber = entry.versionNumber;
			playerName = entry.playerName;
		}

		public MapChallengeEntry ReturnGenericEntry()
		{
			MapChallengeEntry mapChallengeEntry = new MapChallengeEntry();
			mapChallengeEntry.playerID = playerID;
			mapChallengeEntry.mapNumber = mapNumber;
			mapChallengeEntry.time = time;
			mapChallengeEntry.hqType = hqType;
			mapChallengeEntry.matchID1 = matchID1;
			mapChallengeEntry.matchID2 = matchID2;
			mapChallengeEntry.matchID3 = matchID3;
			mapChallengeEntry.matchID4 = matchID4;
			mapChallengeEntry.versionNumber = versionNumber;
			mapChallengeEntry.playerName = playerName;
			return mapChallengeEntry;
		}
	}

	[DynamoDBTable("Misc")]
	public class MiscEntry
	{
		[DynamoDBHashKey]
		public string playerID;

		[DynamoDBRangeKey]
		public string keyName;

		[DynamoDBProperty]
		public int value;

		public MiscEntry()
		{
		}

		public MiscEntry(string playerID, string keyName, int value)
		{
			this.playerID = playerID;
			this.keyName = keyName;
			this.value = value;
		}
	}

	private enum SFSVariableType
	{
		NULL,
		BOOL,
		INT,
		DOUBLE,
		STRING,
		OBJECT,
		ARRAY
	}

	private enum RoomSettingsArrayDataType
	{
		NAME,
		LOBBY_TYPE,
		VERSION,
		GAME_SETUP,
		RULES_SET,
		WANT_REPLAY,
		LOCATION,
		INVALID_HQS,
		WANT_AI,
		MAP_SIZE,
		TERRAIN_CLASS,
		GAME_SPEED,
		MAP_NAME,
		SEED,
		LATITUDE,
		RESOURCE_MINIMUM,
		RESOURCE_PRESENCE,
		COLONY_CLASS,
		OPTIONS,
		TEAMS,
		HANDICAPS,
		NUM_TYPES
	}

	private AppMain APP;

	private AmazonDynamoDBClient mDatabaseClient;

	private DynamoDBContext mAmazonContext;

	private bool IsInitialized;

	private string accessKeyId;

	private string secretAccessKey;

	private Dictionary<string, int> importantStatsDictionary;

	private Dictionary<string, int> statsDictionary;

	private Dictionary<int, List<MapChallengeEntry>> cachedInfiniteLeaderboards = new Dictionary<int, List<MapChallengeEntry>>();

	private Dictionary<int, List<DailyChallengeEntry>> cachedDailyLeaderboards = new Dictionary<int, List<DailyChallengeEntry>>();

	private List<MapChallengeEntry> cachedInfiniteProgressLeaderboard = new List<MapChallengeEntry>();

	private const string defaultZone = "Offworld";

	private const string globalChatName = "QM Chat";

	private bool bVerboseLogging;

	private SmartFox client;

	private bool isInitialized;

	private bool isLoggedIn;

	private bool isLoggingIn;

	private bool hasGameStarted;

	private bool wasKicked;

	private List<MLobbyListener> lobbyListeners;

	private List<Room> kickedFrom;

	private List<string> kicked;

	internal const string defaultHost = "3.90.142.156";

	private const int defaultTcpPort = 9933;

	public string ServerIP => "3.90.142.156";

	public int ServerPort => 9933;

	public void Initialize()
	{
		if (!isLoggedIn)
		{
			Login();
		}
		else
		{
			InitializeDataServices(wantsInitialization: true);
		}
	}

	private void InitializeDataServices(bool wantsInitialization = false)
	{
		if (wantsInitialization && !IsInitialized && accessKeyId != null && secretAccessKey != null)
		{
			APP = AppMain.gApp;
			UnityInitializer.AttachToGameObject(APP.gameObject);
			mDatabaseClient = new AmazonDynamoDBClient(accessKeyId, secretAccessKey, RegionEndpoint.USEast1);
			mAmazonContext = new DynamoDBContext(mDatabaseClient, new DynamoDBContextConfig
			{
				ConsistentRead = false,
				IgnoreNullValues = true
			});
			IsInitialized = true;
		}
	}

	public bool LeaderboardsInitialized()
	{
		return mAmazonContext != null;
	}

	public void FetchDailyChallengeEntries(int numDays, int daysSinceBase, bool bForceUpdate)
	{
		if (!LeaderboardsInitialized())
		{
			return;
		}
		QueryFilter filter = new QueryFilter("dailyID", QueryOperator.Equal, daysSinceBase);
		mAmazonContext.FromQueryAsync(new QueryOperationConfig
		{
			IndexName = "dailyID",
			Filter = filter
		}, delegate(AmazonDynamoDBResult<AsyncSearch<DailyChallengeAmazonDBEntry>> search)
		{
			AsyncSearch<DailyChallengeAmazonDBEntry> result = search.Result;
			result.GetRemainingAsync(delegate(AmazonDynamoDBResult<List<DailyChallengeAmazonDBEntry>> entries)
			{
				List<DailyChallengeEntry> list = entries.Result.Select((DailyChallengeAmazonDBEntry x) => x.ReturnGenericEntry()).ToList();
				cachedDailyLeaderboards.Add(daysSinceBase, list);
				LeaderboardManager.OnDailyChallengeLeaderboardFetched(numDays, daysSinceBase, list);
			});
		});
	}

	public void FetchMapChallengeEntries(int mapNumber, bool bForceUpdate)
	{
		if (!LeaderboardsInitialized())
		{
			return;
		}
		QueryFilter filter = new QueryFilter("mapNumber", QueryOperator.Equal, mapNumber);
		mAmazonContext.FromQueryAsync(new QueryOperationConfig
		{
			IndexName = "mapNumber",
			Filter = filter
		}, delegate(AmazonDynamoDBResult<AsyncSearch<MapChallengeAmazonDBEntry>> search)
		{
			AsyncSearch<MapChallengeAmazonDBEntry> result = search.Result;
			result.GetRemainingAsync(delegate(AmazonDynamoDBResult<List<MapChallengeAmazonDBEntry>> entries)
			{
				List<MapChallengeEntry> list = entries.Result.Select((MapChallengeAmazonDBEntry x) => x.ReturnGenericEntry()).ToList();
				cachedInfiniteLeaderboards.Add(mapNumber, list);
				LeaderboardManager.OnMapChallengeLeaderboardFetched(mapNumber, list);
			});
		});
	}

	public void FetchMapChallengeProgressEntries(bool bForceUpdate)
	{
		if (!LeaderboardsInitialized())
		{
			return;
		}
		QueryFilter filter = new QueryFilter("mapNumber", QueryOperator.Equal, 0);
		mAmazonContext.FromQueryAsync(new QueryOperationConfig
		{
			IndexName = "mapNumber",
			Filter = filter
		}, delegate(AmazonDynamoDBResult<AsyncSearch<MapChallengeAmazonDBEntry>> search)
		{
			AsyncSearch<MapChallengeAmazonDBEntry> result = search.Result;
			result.GetRemainingAsync(delegate(AmazonDynamoDBResult<List<MapChallengeAmazonDBEntry>> entries)
			{
				cachedInfiniteProgressLeaderboard = entries.Result.Select((MapChallengeAmazonDBEntry x) => x.ReturnGenericEntry()).ToList();
				LeaderboardManager.OnMapChallengeProgressLeaderboardFetched(cachedInfiniteProgressLeaderboard);
			});
		});
	}

	public void CheckUploadDailyScore(int days, int value, int[] detail)
	{
		if (cachedDailyLeaderboards != null && cachedDailyLeaderboards.ContainsKey(days))
		{
			int index;
			if ((index = cachedDailyLeaderboards[days].FindIndex((DailyChallengeEntry x) => x.playerID == APP.StoreManager.PlayerID().ToString())) != -1)
			{
				if (cachedDailyLeaderboards[days][index].time > value)
				{
					cachedDailyLeaderboards[days][index].time = value;
					UploadDailyScore(cachedDailyLeaderboards[days][index]);
				}
				return;
			}
			LeaderboardManager.UploadNewDailyChallengeEntry(days, value, detail);
		}
		mAmazonContext.LoadAsync(APP.StoreManager.ProviderID(), days, delegate(AmazonDynamoDBResult<DailyChallengeAmazonDBEntry> result)
		{
			DailyChallengeAmazonDBEntry result2 = result.Result;
			if (result2 == null)
			{
				LeaderboardManager.UploadNewDailyChallengeEntry(days, value, detail);
			}
			else if (result2.time > value)
			{
				result2.time = value;
				if (cachedDailyLeaderboards != null && cachedDailyLeaderboards.ContainsKey(days))
				{
					UploadDailyScore(result2);
				}
			}
		});
	}

	public void UploadDailyScore(DailyChallengeEntry entry)
	{
		if (cachedDailyLeaderboards != null && cachedDailyLeaderboards.ContainsKey(entry.dailyID))
		{
			int index;
			if ((index = cachedDailyLeaderboards[entry.dailyID].FindIndex((DailyChallengeEntry x) => x.playerID == entry.playerID)) != -1)
			{
				cachedDailyLeaderboards[entry.dailyID][index] = entry;
			}
			else
			{
				cachedDailyLeaderboards[entry.dailyID].Add(entry);
			}
			cachedDailyLeaderboards[entry.dailyID].Sort();
		}
		DailyChallengeAmazonDBEntry value = new DailyChallengeAmazonDBEntry(entry);
		mAmazonContext.SaveAsync(value, delegate(AmazonDynamoDBResult response)
		{
			if (response.Exception != null)
			{
				Debug.LogException(response.Exception);
			}
		});
	}

	public void UploadDailyScore(DailyChallengeAmazonDBEntry entry)
	{
		if (cachedDailyLeaderboards != null && cachedDailyLeaderboards.ContainsKey(entry.dailyID))
		{
			DailyChallengeEntry dailyChallengeEntry = entry.ReturnGenericEntry();
			int index;
			if ((index = cachedDailyLeaderboards[entry.dailyID].FindIndex((DailyChallengeEntry x) => x.playerID == entry.playerID)) != -1)
			{
				cachedDailyLeaderboards[entry.dailyID][index] = dailyChallengeEntry;
			}
			else
			{
				cachedDailyLeaderboards[entry.dailyID].Add(dailyChallengeEntry);
			}
			cachedDailyLeaderboards[entry.dailyID].Sort();
		}
		mAmazonContext.SaveAsync(entry, delegate(AmazonDynamoDBResult response)
		{
			if (response.Exception != null)
			{
				Debug.LogException(response.Exception);
			}
		});
	}

	public void CheckUploadMapScore(int mapNumber, int value, int[] detail)
	{
		int index2;
		if (mapNumber != 0 && cachedInfiniteLeaderboards != null && cachedInfiniteLeaderboards.ContainsKey(mapNumber))
		{
			int index;
			if ((index = cachedInfiniteLeaderboards[mapNumber].FindIndex((MapChallengeEntry x) => x.playerID == APP.StoreManager.PlayerID().ToString())) != -1)
			{
				if (cachedInfiniteLeaderboards[mapNumber][index].time > value)
				{
					cachedInfiniteLeaderboards[mapNumber][index].time = value;
					UploadMapScore(cachedInfiniteLeaderboards[mapNumber][index]);
				}
				return;
			}
		}
		else if (mapNumber == 0 && cachedInfiniteProgressLeaderboard != null && (index2 = cachedInfiniteProgressLeaderboard.FindIndex((MapChallengeEntry x) => x.playerID == APP.StoreManager.PlayerID().ToString())) != -1 && cachedInfiniteProgressLeaderboard[index2].time < value)
		{
			cachedInfiniteProgressLeaderboard[index2].time = value;
			UploadMapScore(cachedInfiniteProgressLeaderboard[index2]);
		}
		mAmazonContext.LoadAsync(APP.StoreManager.ProviderID(), mapNumber, delegate(AmazonDynamoDBResult<MapChallengeAmazonDBEntry> result)
		{
			MapChallengeAmazonDBEntry result2 = result.Result;
			if (result2 == null)
			{
				LeaderboardManager.UploadNewMapChallengeEntry(mapNumber, value, detail);
			}
			else if (result2.time > value)
			{
				result2.time = value;
				UploadMapScore(result2.ReturnGenericEntry());
			}
		});
	}

	public void UploadMapScore(MapChallengeEntry entry)
	{
		if (entry.mapNumber != 0)
		{
			if (cachedInfiniteLeaderboards != null && cachedInfiniteLeaderboards.ContainsKey(entry.mapNumber))
			{
				int index;
				if ((index = cachedInfiniteLeaderboards[entry.mapNumber].FindIndex((MapChallengeEntry x) => x.playerID == entry.playerID)) != -1)
				{
					cachedInfiniteLeaderboards[entry.mapNumber][index] = entry;
				}
				else
				{
					cachedInfiniteLeaderboards[entry.mapNumber].Add(entry);
				}
			}
			cachedInfiniteLeaderboards[entry.mapNumber].Sort();
		}
		MapChallengeAmazonDBEntry value = new MapChallengeAmazonDBEntry(entry);
		mAmazonContext.SaveAsync(value, delegate(AmazonDynamoDBResult response)
		{
			if (response.Exception != null)
			{
				Debug.LogException(response.Exception);
			}
		});
	}

	public void UploadMapScore(MapChallengeAmazonDBEntry entry)
	{
		if (entry.mapNumber != 0)
		{
			mAmazonContext.SaveAsync(entry, delegate(AmazonDynamoDBResult response)
			{
				if (response.Exception != null)
				{
					Debug.LogException(response.Exception);
				}
			});
			if (cachedInfiniteLeaderboards != null && cachedInfiniteLeaderboards.ContainsKey(entry.mapNumber))
			{
				MapChallengeEntry mapChallengeEntry = entry.ReturnGenericEntry();
				int index;
				if ((index = cachedInfiniteLeaderboards[entry.mapNumber].FindIndex((MapChallengeEntry x) => x.playerID == entry.playerID)) != -1)
				{
					cachedInfiniteLeaderboards[entry.mapNumber][index] = mapChallengeEntry;
				}
				else
				{
					cachedInfiniteLeaderboards[entry.mapNumber].Add(mapChallengeEntry);
				}
			}
			cachedInfiniteLeaderboards[entry.mapNumber].Sort();
		}
		mAmazonContext.SaveAsync(entry, delegate(AmazonDynamoDBResult response)
		{
			if (response.Exception != null)
			{
				Debug.LogException(response.Exception);
			}
		});
	}

	public bool GetStatImportant(string statName, out int iValue)
	{
		if (importantStatsDictionary == null)
		{
			iValue = 0;
			return false;
		}
		if (!importantStatsDictionary.TryGetValue(statName, out iValue))
		{
			mAmazonContext.LoadAsync(APP.StoreManager.PlayerID(), statName, delegate(AmazonDynamoDBResult<MiscEntry> result)
			{
				importantStatsDictionary.Add(statName, result.Result.value);
			});
		}
		return true;
	}

	public bool GetStatImportant(string statName, out bool bValue)
	{
		if (importantStatsDictionary == null)
		{
			bValue = false;
			return false;
		}
		if (!importantStatsDictionary.TryGetValue(statName, out var value))
		{
			mAmazonContext.LoadAsync(APP.StoreManager.PlayerID(), statName, delegate(AmazonDynamoDBResult<MiscEntry> result)
			{
				importantStatsDictionary.Add(statName, result.Result.value);
			});
		}
		bValue = value == 1;
		return true;
	}

	public bool GetStat(string statName, out int iValue)
	{
		if (statsDictionary == null)
		{
			iValue = 0;
			return false;
		}
		return statsDictionary.TryGetValue(statName, out iValue);
	}

	public bool SetStat(string statName, int iValue)
	{
		if (statsDictionary.ContainsKey(statName))
		{
			if (statsDictionary[statName] != iValue)
			{
				statsDictionary[statName] = iValue;
			}
		}
		else
		{
			statsDictionary.Add(statName, iValue);
		}
		return true;
	}

	public bool SetStatImportant(string statName, int iValue)
	{
		bool flag = true;
		if (importantStatsDictionary.TryGetValue(statName, out var value))
		{
			if (value == iValue)
			{
				flag = false;
			}
			else
			{
				importantStatsDictionary[statName] = iValue;
			}
		}
		else
		{
			importantStatsDictionary.Add(statName, iValue);
		}
		if (flag)
		{
			MiscEntry value2 = new MiscEntry(APP.StoreManager.PlayerID().ToString(), statName, iValue);
			mAmazonContext.SaveAsync(value2, delegate(AmazonDynamoDBResult result)
			{
				if (result.Exception != null)
				{
					Debug.LogException(result.Exception);
				}
			});
		}
		return true;
	}

	public bool SetStatImportant(string statName, bool bValue)
	{
		int iValue = (bValue ? 1 : 0);
		return SetStatImportant(statName, iValue);
	}

	public void Shutdown()
	{
		if (mAmazonContext != null)
		{
			mAmazonContext.Dispose();
		}
		if (client != null)
		{
			Reset(bInitAgain: false);
		}
	}

	public void InitializeService()
	{
		if (!isInitialized)
		{
			APP = AppMain.gApp;
			hasGameStarted = false;
			wasKicked = false;
			if (lobbyListeners == null)
			{
				lobbyListeners = new List<MLobbyListener>();
			}
			kickedFrom = new List<Room>();
			kicked = new List<string>();
			client = new SmartFox();
			client.AddEventListener(SFSEvent.CONNECTION, OnConnection);
			client.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
			client.AddEventListener(SFSEvent.LOGIN, OnLogin);
			client.AddEventListener(SFSEvent.ROOM_JOIN, OnLobbyJoin);
			client.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnLobbyJoinFailed);
			client.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnChatMessage);
			client.AddEventListener(SFSEvent.ROOM_VARIABLES_UPDATE, OnLobbySettingsChanged);
			client.AddEventListener(SFSEvent.ROOM_CREATION_ERROR, OnLobbyCreationFailed);
			client.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
			client.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEntered);
			client.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserLeft);
			if (bVerboseLogging)
			{
				client.AddLogListener(LogLevel.INFO, OnInfoMessage);
				client.AddLogListener(LogLevel.WARN, OnWarningMessage);
			}
			client.AddLogListener(LogLevel.ERROR, OnErrorMessage);
		}
	}

	public void Login()
	{
		if (!IsLoggedIn() && !isLoggingIn)
		{
			isLoggingIn = true;
			ConfigData configData = new ConfigData();
			configData.Host = "3.90.142.156";
			configData.Port = 9933;
			configData.Zone = "Offworld";
			Debug.Log("[SFS2X] Trying to connect");
			client.Connect(configData);
		}
	}

	public bool Initialized()
	{
		return isInitialized;
	}

	public bool IsLoggedIn()
	{
		return isLoggedIn;
	}

	public bool IsInGlobalChat()
	{
		return client.JoinedRooms.Find((Room x) => x.Name == "QM Chat") != null;
	}

	public void Update()
	{
		if (client != null)
		{
			client.ProcessEvents();
		}
	}

	public List<MLobbyListener> GetLobbyListeners()
	{
		return lobbyListeners;
	}

	public void AddLobbyListener(MLobbyListener listener)
	{
		lobbyListeners.Add(listener);
	}

	public void RemoveLobbyListener(MLobbyListener listener)
	{
		lobbyListeners.Remove(listener);
	}

	private void OnConnection(BaseEvent sfsEvent)
	{
		if ((bool)sfsEvent.Params["success"])
		{
			if (!isLoggedIn)
			{
				ISFSObject parameters = new SFSObject();
				string userName = APP.StoreManager.PlayerID().ToString();
				string empty = string.Empty;
				Debug.Log("[SFS2X] Attempting to log in...");
				client.Send(new LoginRequest(userName, empty, "Offworld", parameters));
			}
		}
		else
		{
			Debug.Log("[SFS2X] Connection attempt failed");
			Reset();
		}
	}

	private void Reset(bool bInitAgain = true)
	{
		if (client.JoinedRooms.Count > 0)
		{
			LeaveActiveLobby();
		}
		isLoggingIn = false;
		isInitialized = false;
		Logout();
		client.RemoveAllEventListeners();
		if (bVerboseLogging)
		{
			client.RemoveLogListener(LogLevel.INFO, OnInfoMessage);
			client.RemoveLogListener(LogLevel.WARN, OnWarningMessage);
		}
		client.RemoveLogListener(LogLevel.ERROR, OnErrorMessage);
		if (bInitAgain)
		{
			InitializeService();
			return;
		}
		lobbyListeners.RemoveAll((MLobbyListener x) => true);
		if (client.IsConnected)
		{
			client.Disconnect();
		}
	}

	public void Logout()
	{
		isLoggedIn = false;
		client.HandleLogout();
		if (client.IsConnected)
		{
			client.KillConnection();
		}
	}

	private void OnConnectionLost(BaseEvent sfsEvent)
	{
		Reset();
	}

	private void OnLogin(BaseEvent sfsEvent)
	{
		Debug.Log("[SFS2X] Login successful");
		isLoggedIn = true;
		isLoggingIn = false;
		ISFSObject iSFSObject = (ISFSObject)sfsEvent.Params["data"];
		accessKeyId = iSFSObject.GetUtfString("accessKey");
		secretAccessKey = iSFSObject.GetUtfString("secret");
		APP.ServerTimeOffset = -DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1).Add(new TimeSpan((iSFSObject.GetLong("serverTime") + 25200) * 10000000))).Ticks;
		APP.ServerTimeFetched = true;
		ISFSObject iSFSObject2 = new SFSObject();
		string utfString = iSFSObject.GetUtfString("ipAddress");
		int @int = iSFSObject.GetInt("port");
		StoreHelpers.SetMyID(new CombinedID(APP.StoreManager.ProviderID(), APP.StoreManager.GetStorefrontID(), utfString, @int));
		LobbyHelpers.GetLobbyMemberSettings().PlayerID = StoreHelpers.GetMyID();
		ISFSObject iSFSObject3 = new SFSObject();
		iSFSObject2.PutUtfString("ipAddress", utfString);
		iSFSObject2.PutInt("port", @int);
		iSFSObject2.PutUtfString("playerName", APP.StoreManager.GetMyName());
		iSFSObject2.PutByte("gender", (byte)APP.OptionsSave.GameOptions.playerGender);
		iSFSObject2.PutByteArray("artPacks", new ByteArray(LobbyHelpers.GetLobbyMemberSettings().ArtPackList.ActiveArtPacks.Select((ArtPackType x) => (byte)x).ToArray()));
		iSFSObject2.PutBool("artPacksHiddenIdentities", LobbyHelpers.GetLobbyMemberSettings().ArtPackList.ArtPackHiddenIdentities);
		iSFSObject2.PutUtfString("tachyonID", LobbyHelpers.GetLobbyMemberSettings().TachyonID.ToString());
		iSFSObject3.PutSFSObject("userSettings", iSFSObject2);
		iSFSObject3.PutBool("isReady", val: false);
		client.Send(new ExtensionRequest("registerData", iSFSObject3));
		foreach (MLobbyListener lobbyListener in lobbyListeners)
		{
			lobbyListener.OnLogin();
		}
		if (LobbyHelpers.PendingLobbyID != -1)
		{
			JoinLobby((int)LobbyHelpers.PendingLobbyID);
		}
		InitializeDataServices();
	}

	public void UpdatePlayerData(MLobbyMember newSettings, bool bUpdateData = true)
	{
		ISFSObject iSFSObject = new SFSObject();
		if (bUpdateData)
		{
			ISFSObject iSFSObject2 = new SFSObject();
			iSFSObject2.PutUtfString("playerName", APP.StoreManager.GetMyName());
			iSFSObject2.PutByte("gender", (byte)APP.OptionsSave.GameOptions.playerGender);
			try
			{
				iSFSObject2.PutByteArray("artPacks", new ByteArray(newSettings.ArtPackList.ActiveArtPacks.Select((ArtPackType x) => (byte)x).ToArray()));
				iSFSObject2.PutBool("artPacksHiddenIdentities", newSettings.ArtPackList.ArtPackHiddenIdentities);
			}
			catch (NullReferenceException message)
			{
				Debug.Log(message);
				Debug.LogWarning("[SFS2X] No art pack information was registered on login. Skipping this in lobby member data");
			}
			iSFSObject2.PutUtfString("ipAddress", newSettings.IPaddress);
			iSFSObject2.PutInt("port", newSettings.Port);
			iSFSObject2.PutUtfString("tachyonID", newSettings.TachyonID.ToString());
			iSFSObject.PutSFSObject("userSettings", iSFSObject2);
		}
		iSFSObject.PutBool("isReady", newSettings.IsReady);
		client.Send(new ExtensionRequest("registerData", iSFSObject));
	}

	private void OnInfoMessage(BaseEvent sfsEvent)
	{
		Debug.Log(sfsEvent.Params["message"]);
	}

	private void OnWarningMessage(BaseEvent sfsEvent)
	{
		Debug.LogWarning(sfsEvent.Params["message"]);
	}

	private void OnErrorMessage(BaseEvent sfsEvent)
	{
		Debug.LogError(sfsEvent.Params["message"]);
	}

	public void CreateLobby(MLobbySettings lobbySettings)
	{
		RoomSettings roomSettings = new RoomSettings(string.Concat(DateTime.UtcNow, "_", client.GetRoomListFromGroup("lobbies").Count));
		roomSettings.MaxUsers = (short)lobbySettings.MaxPlayers;
		roomSettings.Variables = getVariablesFromSettings(lobbySettings, bInit: true);
		roomSettings.GroupId = "lobbies";
		if (lobbySettings.Password != string.Empty)
		{
			roomSettings.Password = lobbySettings.Password;
		}
		client.Send(new CreateRoomRequest(roomSettings, autoJoin: true));
	}

	private List<RoomVariable> getVariablesFromSettings(MLobbySettings lobbySettings, bool bInit)
	{
		List<RoomVariable> list = new List<RoomVariable>();
		ISFSArray iSFSArray = new SFSArray();
		iSFSArray.AddUtfString(lobbySettings.Name);
		iSFSArray.AddByte((byte)lobbySettings.KindOfLobby);
		iSFSArray.AddUtfString(lobbySettings.SteamVersionKey);
		iSFSArray.AddShort((short)lobbySettings.GameSetup);
		iSFSArray.AddShort((short)lobbySettings.RulesSet);
		iSFSArray.AddBool(lobbySettings.WantReplayFile);
		iSFSArray.AddShort((short)lobbySettings.Location);
		iSFSArray.AddBoolArray(lobbySettings.InvalidHumanHQ);
		iSFSArray.AddBool(lobbySettings.WantAIplayers);
		iSFSArray.AddByte((byte)lobbySettings.MapSizeIndex);
		iSFSArray.AddShort((short)lobbySettings.TerrainClassIndex);
		iSFSArray.AddByte((byte)lobbySettings.GameSpeedIndex);
		iSFSArray.AddUtfString(lobbySettings.MapName);
		iSFSArray.AddInt(lobbySettings.Seed);
		iSFSArray.AddShort((short)lobbySettings.Latitude);
		iSFSArray.AddByte((byte)lobbySettings.ResourceMinimum);
		iSFSArray.AddByte((byte)lobbySettings.ResourcePresence);
		iSFSArray.AddShort((short)lobbySettings.ColonyClass);
		iSFSArray.AddBoolArray(lobbySettings.GameOptions);
		if (lobbySettings.teamNumbers != null)
		{
			ISFSObject iSFSObject = new SFSObject();
			foreach (PlayerID key in lobbySettings.teamNumbers.Keys)
			{
				iSFSObject.PutShort(key.ToString(), (short)lobbySettings.teamNumbers[key]);
			}
			iSFSArray.AddSFSObject(iSFSObject);
		}
		if (lobbySettings.handicaps != null)
		{
			ISFSObject iSFSObject2 = new SFSObject();
			foreach (PlayerID key2 in lobbySettings.handicaps.Keys)
			{
				iSFSObject2.PutShort(key2.ToString(), (short)lobbySettings.handicaps[key2]);
			}
			iSFSArray.AddSFSObject(iSFSObject2);
		}
		Room roomById = client.GetRoomById(lobbySettings.LobbyID);
		list.Add(new SFSRoomVariable("lobbySettings", iSFSArray, 6));
		if (bInit || roomById.GetVariable("owner") == null || lobbySettings.ownerID.ToString() != roomById.GetVariable("owner").GetStringValue())
		{
			list.Add(new SFSRoomVariable("owner", lobbySettings.ownerID.ToString(), 4));
		}
		if (bInit || roomById.GetVariable("serverGUID") == null || (lobbySettings.serverGUID != null && lobbySettings.serverGUID != roomById.GetVariable("serverGUID").GetStringValue()))
		{
			list.Add(new SFSRoomVariable("serverGUID", lobbySettings.serverGUID, 4));
		}
		if (bInit || roomById.GetVariable("gameStarted") == null)
		{
			list.Add(new SFSRoomVariable("gameStarted", false, 1));
		}
		return list;
	}

	public MLobbySettings.LobbyType GetLobbyType(int lobbyID)
	{
		return (MLobbySettings.LobbyType)(((int?)client.GetRoomById(lobbyID)?.GetVariable("lobbySettings").GetSFSArrayValue().GetByte(1)) ?? (-1));
	}

	private MLobbySettings createLobbySettings(Room source)
	{
		using (new UnityProfileScope("SmartFoxManager:createLobbySettings"))
		{
			if (source.Name == "QM Chat")
			{
				return createLobbySettings(source.MaxUsers, source.Id, source.Name, string.Empty, string.Empty, source.UserCount, null, new MLobbySettings());
			}
			if (client.LastJoinedRoom == source && source.UserCount == 1)
			{
				return createLobbySettings(source.MaxUsers, source.Id, source.Name, APP.StoreManager.PlayerID().ToString(), Network.player.guid, source.UserCount, null);
			}
			return createLobbySettings(source.MaxUsers, source.Id, source.Name, GetLobbyOwner(source.Id), GetServerGUID(source.Id), source.UserCount, source.GetVariable("lobbySettings").GetSFSArrayValue());
		}
	}

	private MLobbySettings createLobbySettings(int maxPlayers, int lobbyID, string name, string ownerID, string guid, int numPlayers, ISFSArray roomVariableArray, MLobbySettings currentSettings = null)
	{
		MLobbySettings mLobbySettings = currentSettings ?? new MLobbySettings(maxPlayers, MLobbySettings.LobbyDefaultType.MULTIPLAYER);
		mLobbySettings.LobbyID = lobbyID;
		if (ownerID != null && ownerID != string.Empty)
		{
			mLobbySettings.ownerID = PlayerID.FromString(ownerID);
		}
		mLobbySettings.MaxPlayers = maxPlayers;
		mLobbySettings.NumPlayersInLobby = numPlayers;
		if (roomVariableArray != null && roomVariableArray.Size() > 0)
		{
			mLobbySettings.Name = roomVariableArray.GetUtfString(0);
			mLobbySettings.KindOfLobby = (MLobbySettings.LobbyType)roomVariableArray.GetByte(1);
			mLobbySettings.SteamVersionKey = roomVariableArray.GetUtfString(2);
			mLobbySettings.GameSetup = (GameSetupType)roomVariableArray.GetShort(3);
			mLobbySettings.RulesSet = (RulesSetType)roomVariableArray.GetShort(4);
			mLobbySettings.WantReplayFile = roomVariableArray.GetBool(5);
			mLobbySettings.Location = (LocationType)roomVariableArray.GetShort(6);
			mLobbySettings.InvalidHumanHQ = roomVariableArray.GetBoolArray(7);
			mLobbySettings.WantAIplayers = roomVariableArray.GetBool(8);
			mLobbySettings.MapSizeIndex = (MapSizeType)roomVariableArray.GetByte(9);
			mLobbySettings.TerrainClassIndex = (TerrainClassType)roomVariableArray.GetShort(10);
			mLobbySettings.GameSpeedIndex = (GameSpeedType)roomVariableArray.GetByte(11);
			mLobbySettings.MapName = roomVariableArray.GetUtfString(12);
			mLobbySettings.Seed = roomVariableArray.GetInt(13);
			mLobbySettings.Latitude = (LatitudeType)roomVariableArray.GetShort(14);
			mLobbySettings.ResourceMinimum = (ResourceMinimumType)roomVariableArray.GetByte(15);
			mLobbySettings.ResourcePresence = (ResourcePresenceType)roomVariableArray.GetByte(16);
			mLobbySettings.ColonyClass = (ColonyClassType)roomVariableArray.GetShort(17);
			mLobbySettings.GameOptions = roomVariableArray.GetBoolArray(18);
			ISFSObject sFSObject = roomVariableArray.GetSFSObject(19);
			string[] keys = sFSObject.GetKeys();
			foreach (string text in keys)
			{
				if (mLobbySettings.teamNumbers.ContainsKey(PlayerID.FromString(text)))
				{
					mLobbySettings.teamNumbers[PlayerID.FromString(text)] = sFSObject.GetShort(text);
				}
				else
				{
					mLobbySettings.teamNumbers.Add(PlayerID.FromString(text), sFSObject.GetShort(text));
				}
			}
			sFSObject = roomVariableArray.GetSFSObject(20);
			string[] keys2 = sFSObject.GetKeys();
			foreach (string text2 in keys2)
			{
				if (mLobbySettings.handicaps.ContainsKey(PlayerID.FromString(text2)))
				{
					mLobbySettings.handicaps[PlayerID.FromString(text2)] = sFSObject.GetShort(text2);
				}
				else
				{
					mLobbySettings.handicaps.Add(PlayerID.FromString(text2), sFSObject.GetShort(text2));
				}
			}
			mLobbySettings.serverGUID = guid;
		}
		return mLobbySettings;
	}

	private void OnLobbyCreationFailed(BaseEvent sfsEvent)
	{
		Debug.Log("[SFS2X] Room Creation Failed!");
		foreach (MLobbyListener lobbyListener in lobbyListeners)
		{
			lobbyListener.OnLobbyCreationFailed();
		}
	}

	private void OnLobbySettingsChanged(BaseEvent sfsEvent)
	{
		using (new UnityProfileScope("SmartFoxManager:onLobbySettingsChanged"))
		{
			if (client.LastJoinedRoom == null || client.LastJoinedRoom != (Room)sfsEvent.Params["room"] || IsLobbyOwner() || hasGameStarted)
			{
				return;
			}
			LobbyHelpers.LobbySettings.NumPlayersInLobby = client.LastJoinedRoom.UserCount;
			List<string> list = (List<string>)sfsEvent.Params["changedVars"];
			if (list.Contains("lobbySettings"))
			{
				Room lastJoinedRoom = client.LastJoinedRoom;
				MLobbySettings mLobbySettings = createLobbySettings(lastJoinedRoom);
				if (!mLobbySettings.ImportantEquals(LobbyHelpers.LobbySettings))
				{
					foreach (MLobbyListener lobbyListener in lobbyListeners)
					{
						lobbyListener.OnImportantLobbySettingsChanged(mLobbySettings);
					}
				}
				LobbyHelpers.LobbySettings.CopyValues(mLobbySettings, bAll: false);
			}
			if (list.Contains("owner"))
			{
				string stringValue = client.LastJoinedRoom.GetVariable("owner").GetStringValue();
				LobbyHelpers.LobbySettings.ownerID = PlayerID.FromString(stringValue);
			}
			if (list.Contains("serverGUID"))
			{
				string stringValue2 = client.LastJoinedRoom.GetVariable("serverGUID").GetStringValue();
				if (stringValue2 != LobbyHelpers.LobbySettings.serverGUID)
				{
					LobbyHelpers.LobbySettings.serverGUID = stringValue2;
					APP.RakNetManager.ConnectToServerNAT(stringValue2);
					APP.NetworkP2P.SetServer(GetCombinedID(LobbyHelpers.LobbySettings.ownerID));
				}
			}
			if (list.Contains("gameStarted"))
			{
				hasGameStarted = client.LastJoinedRoom.GetVariable("gameStarted").GetBoolValue();
			}
		}
	}

	public void UpdatePlayerState(out bool gameStarted, out bool beenKicked)
	{
		gameStarted = hasGameStarted;
		beenKicked = wasKicked;
	}

	public void UpdateLobbyData(int lobbyID, MLobbySettings lobbySettings)
	{
		List<RoomVariable> variablesFromSettings = getVariablesFromSettings(lobbySettings, bInit: false);
		client.Send(new SetRoomVariablesRequest(variablesFromSettings));
	}

	public void PromptPassword(int lobbyID)
	{
		if (PopupManager.GetTopPopup() != null)
		{
			PopupManager.removeTopPopup();
		}
		string password = string.Empty;
		Action<string> inputFieldCallback = delegate(string newText)
		{
			password = newText;
		};
		Action item = delegate
		{
			JoinLobby(lobbyID, password);
		};
		PopupManager.addPopupInputField(TextHelpers.TEXT("TEXT_FINDLOBBY_PASSWORD_INPUT_TITLE", APP.LobbyManager.GetLobbyName(lobbyID).ToText()), TextHelpers.TEXT("TEXT_FINDLOBBY_PASSWORD_INPUT_DEFAULT_TEXT"), inputFieldCallback, new List<string>
		{
			TextHelpers.TEXT("TEXT_FINDLOBBY_BACK"),
			TextHelpers.TEXT("TEXT_FINDLOBBY_JOIN_LOBBY")
		}, new List<Action>
		{
			PopupManager.removeTopPopup,
			item
		});
	}

	public void JoinLobby(MLobbySettings lobbySettings)
	{
		JoinLobby(lobbySettings.LobbyID);
	}

	public void JoinLobby(MLobbySettings lobbySettings, string password)
	{
		JoinLobby(lobbySettings.LobbyID, password);
	}

	public void JoinLobby(int lobbyID)
	{
		if (IsValidLobbyID(lobbyID))
		{
			if (HasPassword(lobbyID))
			{
				PromptPassword(lobbyID);
			}
			else
			{
				client.Send(new JoinRoomRequest(lobbyID));
			}
		}
	}

	public void JoinLobby(int lobbyID, string password)
	{
		client.Send(new JoinRoomRequest(lobbyID, password));
	}

	public bool HasPassword(int lobbyID)
	{
		return client.GetRoomById(lobbyID).IsPasswordProtected;
	}

	public string GetLobbyName(int lobbyID)
	{
		Room roomById = client.GetRoomById(lobbyID);
		if (roomById.GetVariable("lobbySettings") != null)
		{
			return roomById.GetVariable("lobbySettings").GetSFSArrayValue().GetUtfString(0);
		}
		Debug.Log("[SFS2X] Tried to find name for an invalid lobby");
		return string.Empty;
	}

	private void OnLobbyJoin(BaseEvent sfsEvent)
	{
		LobbyHelpers.PendingLobbyID = -1L;
		if (PopupManager.GetTopPopup() != null)
		{
			PopupManager.removeTopPopup();
		}
		if (LobbyHelpers.LobbySettings == null)
		{
			LobbyHelpers.LobbySettings = new MLobbySettings();
		}
		LobbyHelpers.LobbySettings.LobbyID = client.LastJoinedRoom.Id;
		if (client.LastJoinedRoom.Name != "QM Chat" && IsLobbyOwner())
		{
			Debug.LogFormat("[SFS2X] Room Created: {0} (ID: {1})", client.LastJoinedRoom, client.LastJoinedRoom.Id);
			LobbyHelpers.LobbySettings.ownerID = APP.StoreManager.PlayerID();
			LobbyHelpers.LobbySettings.serverGUID = Network.player.guid;
			UpdateLobbyData(client.LastJoinedRoom.Id, LobbyHelpers.LobbySettings);
			StoreHelpers.SetMyID(StoreHelpers.GetMyID());
			APP.RakNetManager.CreateServer(StoreHelpers.GetMyID().Port);
			LobbyHelpers.GetLobbyMemberSettings().PlayerID = StoreHelpers.GetMyID();
			UpdatePlayerData(LobbyHelpers.GetLobbyMemberSettings());
			{
				foreach (MLobbyListener lobbyListener in lobbyListeners)
				{
					lobbyListener.OnLobbyCreated();
				}
				return;
			}
		}
		if (client.LastJoinedRoom.Name == "QM Chat")
		{
			Debug.Log("[SFS2X] Joined global chat");
			{
				foreach (MLobbyListener lobbyListener2 in lobbyListeners)
				{
					lobbyListener2.OnGlobalChatLobbySet();
				}
				return;
			}
		}
		Debug.LogFormat("[SFS2X] Room Joined: {0} (ID: {1})", client.LastJoinedRoom, client.LastJoinedRoom.Id);
		LobbyHelpers.LobbySettings.CopyValues(createLobbySettings(client.LastJoinedRoom), bAll: true);
		APP.NetworkP2P.SetServer(GetCombinedID(LobbyHelpers.LobbySettings.ownerID));
		APP.RakNetManager.ConnectToServerNAT(LobbyHelpers.LobbySettings.serverGUID);
		LobbyHelpers.GetLobbyMemberSettings().Port = Network.player.port;
		StoreHelpers.SetMyID(LobbyHelpers.GetLobbyMemberSettings().PlayerID);
		UpdatePlayerData(LobbyHelpers.GetLobbyMemberSettings());
		foreach (MLobbyListener lobbyListener3 in lobbyListeners)
		{
			lobbyListener3.OnLobbyEntered(result: true);
		}
	}

	public bool IsValidLobbyID(int lobbyID)
	{
		return client.GetRoomById(lobbyID) != null;
	}

	public bool IsValidUserID(PlayerID playerID)
	{
		return client.LastJoinedRoom.GetUserByName(playerID.ToString()) != null;
	}

	public CombinedID GetCombinedID(PlayerID playerID)
	{
		User user = client.LastJoinedRoom.UserList.Find((User x) => x.Name == playerID.ToString());
		ISFSObject sFSObjectValue = user.GetVariable("userSettings").GetSFSObjectValue();
		return new CombinedID(playerID.providerID, playerID.storefrontID, sFSObjectValue.GetUtfString("ipAddress"), sFSObjectValue.GetInt("port"));
	}

	public List<PlayerID> GetLobbyPlayerIDs()
	{
		List<User> userList = client.LastJoinedRoom.UserList;
		return userList.Select((User x) => PlayerID.FromString(x.Name)).ToList();
	}

	public List<MLobbyMember> GetAllLobbyPlayers()
	{
		List<MLobbyMember> list = new List<MLobbyMember>();
		GetUpdatedLobbyPlayers(list);
		return list;
	}

	public bool GetUpdatedLobbyPlayers(List<MLobbyMember> oldMembers)
	{
		bool result = false;
		List<User> userList = client.LastJoinedRoom.UserList;
		if (oldMembers == null)
		{
			oldMembers = new List<MLobbyMember>();
		}
		for (int num = oldMembers.Count - 1; num >= 0; num--)
		{
			string playerID = oldMembers[num].PlayerID.GetPlayerID().ToString();
			if (!userList.Any((User x) => x.Name == playerID))
			{
				result = true;
				oldMembers.RemoveAt(num);
			}
		}
		foreach (User user in userList)
		{
			if (user.Name == APP.StoreManager.PlayerID().ToString())
			{
				int num2 = oldMembers.FindIndex((MLobbyMember x) => x.PlayerID.GetPlayerID().ToString() == user.Name);
				if (num2 != -1)
				{
					oldMembers[num2] = LobbyHelpers.GetLobbyMemberSettings();
				}
				else
				{
					oldMembers.Add(LobbyHelpers.GetLobbyMemberSettings());
				}
				continue;
			}
			MLobbyMember mLobbyMember = oldMembers.Find((MLobbyMember x) => x.PlayerID.GetPlayerID().ToString() == user.Name);
			if (mLobbyMember == null)
			{
				mLobbyMember = new MLobbyMember();
				result = true;
				mLobbyMember.UserID = PlayerID.FromString(user.Name);
				ISFSObject sFSObjectValue = user.GetVariable("userSettings").GetSFSObjectValue();
				mLobbyMember.Name = sFSObjectValue.GetUtfString("playerName");
				mLobbyMember.Gender = (GenderType)sFSObjectValue.GetByte("gender");
				ArtPackList artPackList2 = (mLobbyMember.ArtPackList = new ArtPackList(sFSObjectValue.GetByteArray("artPacks").Bytes.Select((byte x) => (ArtPackType)x).ToList(), sFSObjectValue.GetBool("artPacksHiddenIdentities")));
				PlayerID playerID2 = PlayerID.FromString(user.Name);
				mLobbyMember.PlayerID = new CombinedID(playerID2.providerID, playerID2.storefrontID, sFSObjectValue.GetUtfString("ipAddress"), sFSObjectValue.GetInt("port"));
				if (sFSObjectValue.GetUtfString("tachyonID") != null)
				{
					mLobbyMember.TachyonID = new Guid(sFSObjectValue.GetUtfString("tachyonID"));
				}
				oldMembers.Add(mLobbyMember);
			}
			mLobbyMember.IsReady = user.GetVariable("isReady").GetBoolValue();
		}
		return result;
	}

	private void OnLobbyJoinFailed(BaseEvent sfsEvent)
	{
		if ((string)sfsEvent.Params["errorMessage"] != "Room QM Chat already joined")
		{
			PopupManager.addWarning(TextHelpers.TEXT("TEXT_FINDLOBBY_FAILED_TO_JOIN"), TextHelpers.TEXT("TEXT_FINDLOBBY_FAILED_TO_JOIN_DESC"));
		}
	}

	public void JoinGlobalChatLobby()
	{
		client.Send(new JoinRoomRequest("QM Chat"));
	}

	public void GetLobbyList()
	{
		using (new UnityProfileScope("SmartFoxManager:getLobbyList"))
		{
			List<Room> list = (from r in client.GetRoomListFromGroup("lobbies")
				where !kickedFrom.Contains(r)
				select r).ToList();
			List<MLobbySettings> list2 = new List<MLobbySettings>();
			foreach (Room item in list)
			{
				try
				{
					MLobbySettings mLobbySettings = createLobbySettings(item);
					if (mLobbySettings.SteamVersionKey == ModPath.VersionPlusModOnly)
					{
						list2.Add(mLobbySettings);
					}
				}
				catch (NullReferenceException ex)
				{
					Debug.LogWarning("[SFS2X] Encounted a room with invalid settings. Skipping and printing exception info: " + ex);
				}
			}
			foreach (MLobbyListener lobbyListener in lobbyListeners)
			{
				lobbyListener.OnGetLobbyList(list2);
			}
		}
	}

	public string GetLobbyVersionNumber(int lobbyID)
	{
		return client.GetRoomById(lobbyID).GetVariable("version").GetStringValue();
	}

	public string GetPlayersName(User user)
	{
		try
		{
			return user.GetVariable("userSettings").GetSFSObjectValue().GetUtfString("playerName");
		}
		catch (NullReferenceException ex)
		{
			Debug.Log("[SFS2X] Failed to find player name:" + ex);
			return "Unknown";
		}
	}

	public string GetPlayersName(PlayerID playerID)
	{
		return GetPlayersName(client.LastJoinedRoom.UserList.Find((User x) => x.Name.Equals(playerID.ToString())));
	}

	public void KickPlayerFromLobby(PlayerID playerID)
	{
		KickPlayerFromLobby(playerID.ToString());
	}

	public void KickPlayerFromLobby(string playerID)
	{
		kicked.Add(playerID);
		ISFSObject iSFSObject = new SFSObject();
		iSFSObject.PutInt("roomID", client.LastJoinedRoom.Id);
		iSFSObject.PutUtfString("userID", playerID);
		client.Send(new ExtensionRequest("kickUser", iSFSObject));
	}

	private void OnUserEntered(BaseEvent sfsEvent)
	{
		User user = (User)sfsEvent.Params["user"];
		MLobbyChatUpdate lobbyChatUpdate = new MLobbyChatUpdate(user.GetVariable("userSettings").GetSFSObjectValue().GetUtfString("playerName"), ChatMemberStateChangeType.ENTERED);
		foreach (MLobbyListener lobbyListener in lobbyListeners)
		{
			lobbyListener.OnLobbyChatUpdate(lobbyChatUpdate);
		}
		if (kicked.Contains(user.Name))
		{
			KickPlayerFromLobby(user.Name);
		}
	}

	private void OnUserLeft(BaseEvent sfsEvent)
	{
		User user = (User)sfsEvent.Params["user"];
		if (user.Name == APP.StoreManager.PlayerID().ToString())
		{
			return;
		}
		if (IsLobbyOwner())
		{
			LobbyHelpers.LobbySettings.teamNumbers.Remove(PlayerID.FromString(user.Name));
			LobbyHelpers.LobbySettings.handicaps.Remove(PlayerID.FromString(user.Name));
			UpdateLobbyData(client.LastJoinedRoom.Id, LobbyHelpers.LobbySettings);
		}
		MLobbyChatUpdate lobbyChatUpdate = new MLobbyChatUpdate(user.GetVariable("userSettings").GetSFSObjectValue().GetUtfString("playerName"), ChatMemberStateChangeType.LEFT);
		foreach (MLobbyListener lobbyListener in lobbyListeners)
		{
			lobbyListener.OnLobbyChatUpdate(lobbyChatUpdate);
		}
	}

	public void SendLobbyChat(string text)
	{
		client.Send(new PublicMessageRequest(text));
	}

	private void OnChatMessage(BaseEvent sfsEvent)
	{
		if (wasKicked)
		{
			return;
		}
		User user = (User)sfsEvent.Params["sender"];
		MLobbyChatMessage message = new MLobbyChatMessage(PlayerID.FromString(user.Name), GetPlayersName(user), (string)sfsEvent.Params["message"]);
		foreach (MLobbyListener lobbyListener in lobbyListeners)
		{
			lobbyListener.OnLobbyChatMessage(message);
		}
	}

	public void LeaveActiveLobby()
	{
		if (IsLobbyOwner() && !hasGameStarted)
		{
			APP.RakNetManager.DisconnectClients();
			APP.RakNetManager.DisconnectServer();
		}
		client.Send(new LeaveRoomRequest(client.LastJoinedRoom));
		UpdatePlayerData(LobbyHelpers.GetLobbyMemberSettings(), bUpdateData: false);
		APP.StoreManager.LeaveDummyLobby();
		hasGameStarted = false;
		wasKicked = false;
	}

	public string GetLobbyOwner(int lobbyID)
	{
		RoomVariable variable = client.GetRoomById(lobbyID).GetVariable("owner");
		if (variable == null)
		{
			return string.Empty;
		}
		return variable.GetStringValue();
	}

	public string GetServerGUID(int lobbyID)
	{
		RoomVariable variable = client.GetRoomById(lobbyID).GetVariable("serverGUID");
		if (variable == null)
		{
			return string.Empty;
		}
		return client.GetRoomById(lobbyID).GetVariable("serverGUID").GetStringValue();
	}

	public bool IsLobbyOwner()
	{
		if (client.JoinedRooms.Count == 0)
		{
			return false;
		}
		return IsLobbyOwner(client.LastJoinedRoom.Id, APP.StoreManager.PlayerID());
	}

	public bool IsLobbyOwner(int lobbyID, PlayerID playerID)
	{
		Room roomById = client.GetRoomById(lobbyID);
		if (roomById.Name == "QM Chat")
		{
			return false;
		}
		if (roomById.GetVariable("owner") != null)
		{
			return roomById.GetVariable("owner").GetStringValue() == playerID.ToString();
		}
		if (roomById.Name == client.LastJoinedRoom.Name && roomById.UserCount == 1 && (LobbyHelpers.LobbySettings.ownerID.providerID == string.Empty || LobbyHelpers.LobbySettings.ownerID == APP.StoreManager.PlayerID()))
		{
			return true;
		}
		return false;
	}

	public MLobbySettings GetActiveLobbyInfo(bool bForceUpdate = false)
	{
		using (new UnityProfileScope("SmartFoxManager:getActiveLobbyInfo"))
		{
			if (bForceUpdate)
			{
				Room lastJoinedRoom = client.LastJoinedRoom;
				return createLobbySettings(lastJoinedRoom);
			}
			return LobbyHelpers.LobbySettings;
		}
	}

	public void OnExtensionResponse(BaseEvent sfsEvent)
	{
		ISFSObject iSFSObject = (ISFSObject)sfsEvent.Params["params"];
		switch ((string)sfsEvent.Params["cmd"])
		{
		case "kicked":
			APP.RakNetManager.DisconnectFromServer();
			wasKicked = true;
			kickedFrom.Add((Room)sfsEvent.Params["room"]);
			break;
		case "kickSuccessful":
		{
			MLobbyChatUpdate lobbyChatUpdate = new MLobbyChatUpdate(((ISFSObject)sfsEvent.Params["params"]).GetUtfString("userName"), ChatMemberStateChangeType.KICKED);
			{
				foreach (MLobbyListener lobbyListener in lobbyListeners)
				{
					lobbyListener.OnLobbyChatUpdate(lobbyChatUpdate);
				}
				break;
			}
		}
		case "makeNewServer":
			LobbyHelpers.LobbySettings.ownerID = APP.StoreManager.PlayerID();
			LobbyHelpers.LobbySettings.serverGUID = Network.player.guid;
			APP.RakNetManager.CreateServer(StoreHelpers.GetMyID().Port);
			UpdateLobbyData(client.LastJoinedRoom.Id, LobbyHelpers.LobbySettings);
			break;
		}
	}

	public void SendGameStartingMessage()
	{
		LobbyHelpers.LobbySettings.NumPlayersInLobby = client.LastJoinedRoom.UserCount;
		UpdateLobbyData(client.LastJoinedRoom.Id, LobbyHelpers.LobbySettings);
		hasGameStarted = true;
		client.Send(new SetRoomVariablesRequest(new List<RoomVariable>
		{
			new SFSRoomVariable("gameStarted", true)
		}, client.LastJoinedRoom));
	}
}
