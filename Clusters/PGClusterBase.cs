using Newtonsoft.Json;
using PolygonApi.Data;
using PolygonApi.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using WebSocket4Net;

namespace PolygonApi.Clusters
{
	public delegate void OnPGStatusParamDel( string ClusterName, string Status, string Message );
	public delegate void OnPGAuthorizedDel( string ClusterName, bool IsAuthorized );
	public delegate void OnPGClusterReadyDel( string ClusterName, bool IsReconnect );
	public delegate void OnPGClusterTextDel( string ClusterName, string Text );
	public delegate void OnPGClusterSubscribeDel( string ClusterName, Dictionary<string, SubscribeSymbolRec> SubscribedSymbols );
	public delegate void OnSymbolQueuedDel( QueuedSymbolRec QueuedSymbol );
	public delegate void OnDailyOpenCloseParamDel( string ClusterName, DailyOpenClose dailyOpenClose );
	public delegate void OnPreviousCloseParamDel( string ClusterName, PreviousClose previousClose );
	public delegate void OnInvalidChannelsDel( string Params, List<string> InvalidChannels );

	public class PGClusterBase : PGBase
	{
		#region Variables

		public string SymbolType;

		public PGonApi PGApi;

		List<QueuedSymbolRec> SymbolsQueue = new List<QueuedSymbolRec>();

		public static List<string> ValidEquitiesChannels = new List<string>()
		{ "T","Q","A","AM" };
		public static List<string> ValidForexChannels = new List<string>()
		{ "C","CA","BONDS","COMMODITIES","METALS" };
		public static List<string> ValidCryptoChannels = new List<string>()
		{ "XT","XQ","XS","XL2" };

		#endregion

		#region Properties

		public PGWebSocket pgWebSocket
		{
			get { return _pgWebSocket = _pgWebSocket ?? new PGWebSocket(); }
			set { _pgWebSocket = value; }
		}
		PGWebSocket _pgWebSocket;

		public string ClusterName
		{
			get { return _ClusterName; }
			set { _ClusterName = pgWebSocket.PGStatus.ClusterName = value; }
		}
		internal string _ClusterName;

		public Dictionary<string, SubscribeSymbolRec> SubscribedSymbols
		{
			get { return _SubscribedSymbols = _SubscribedSymbols ?? new Dictionary<string, SubscribeSymbolRec>(); }
			set { _SubscribedSymbols = value; }
		}
		Dictionary<string, SubscribeSymbolRec> _SubscribedSymbols;

		public List<string> DefaultChannels
		{
			get { return _DefaultChannels = _DefaultChannels ?? new List<string>(); }
			set { _DefaultChannels = value; }
		}
		List<string> _DefaultChannels;

		public List<string> ValidChannels
		{
			get { return _ValidChannels = _ValidChannels ?? new List<string>(); }
			set { _ValidChannels = value; }
		}
		List<string> _ValidChannels;

		public bool IsStarted
		{
			get { return _IsStarted; }
			set { _IsStarted = _pgWebSocket.PGStatus.IsStarted = value; }
		}
		bool _IsStarted;

		#endregion

		#region Events

		public event OnPGAuthorizedDel OnPGAuthorizedEvent;
		public event OnPGClusterReadyDel OnPGClusterReadyEvent;
		public event OnTextParamDel OnPGAuthFailedEvent;
		public event OnTextParamDel OnPGRestartedEvent;
		public event OnPGStatusParamDel OnPGClusterStatusEvent;
		public event OnTextParamDel OnExecJsonSecureGetEvent;
		public event OnTextParamDel OnNoSubscribeSymbolRecEvent;
		public event OnTextParamDel OnWebSocketMessageErrorEvent;
		public event OnInvalidChannelsDel OnInvalidChannelsEvent;

		public event OnDailyOpenCloseParamDel OnDailyOpenCloseEvent;
		public event OnPreviousCloseParamDel OnPreviousCloseEvent;

		public event OnTextParamDel OnWebSocketOpenedEvent
		{ add { pgWebSocket.OnWebSocketOpenedEvent += value; } remove { pgWebSocket.OnWebSocketOpenedEvent -= value; } }
		public event OnTextParamDel OnWebSocketClosedEvent
		{ add { pgWebSocket.OnWebSocketClosedEvent += value; } remove { pgWebSocket.OnWebSocketClosedEvent -= value; } }
		public event OnTextParamDel OnWebSocketErrorEvent
		{ add { pgWebSocket.OnWebSocketErrorEvent += value; } remove { pgWebSocket.OnWebSocketErrorEvent -= value; } }

		public event OnPGClusterSubscribeDel OnSubscribeEvent;
		public event OnPGClusterSubscribeDel OnUnSubscribeEvent;
		public event OnSymbolQueuedDel OnSymbolQueuedEvent;

		#endregion

		public PGClusterBase( PGonApi PGApi, string ClusterName )
		{
			this.PGApi = PGApi;

			// add to Clusters list
			PGApi.AddToPGClusters( this );

			this.ClusterName = PGStatus.ClusterName = ClusterName;
			PGStatus.webSocket = pgWebSocket.webSocket;
		}

		public virtual void InitPGCluster()
		{
			InitValidChannels();
			InitEvents();
		}

		#region Start/Connect

		public virtual void Start()
		{
			if ( IsStarted )
				return;

			string Message = $"Starting Polygon {ClusterName} Cluster...";
			pgWebSocket.Start( Message );
		}

		#endregion

		#region Initialization

		// Note: unless 'overridden' by the Channels param during
		// Cluster.SubscribeSymbol( Pair, List<string> Channels = null )
		// these Channels will be used by default
		public virtual void InitDefaultChannels( List<string> Channels )
		{
			DefaultChannels = Channels;
		}

		#endregion

		#region InitEvents/UnInitEvents

		public virtual void InitEvents()
		{
			UnInitEvents();

			// PGApi is a single source for Cluster TextInfo
			OnTextInfoEvent += PGApi.FireOnTextInfoEvent;

			pgWebSocket.OnSocketMessageEvent += OnSocketMessage;
			pgWebSocket.OnWebSocketOpenedEvent += OnWebSocketOpened;
			pgWebSocket.OnTextInfoEvent += FireOnTextInfoEvent;
		}

		public virtual void UnInitEvents()
		{
			OnTextInfoEvent -= PGApi.FireOnTextInfoEvent;

			pgWebSocket.OnSocketMessageEvent -= OnSocketMessage;
			pgWebSocket.OnWebSocketOpenedEvent -= OnWebSocketOpened;
			pgWebSocket.OnTextInfoEvent -= FireOnTextInfoEvent;
		}

		#endregion

		#region Socket Messages

		public void OnSocketMessage( string JSONText )
		{
			if ( Dispatcher != null )
			{
				// allow for handoff to Dispatcher at highest priority
				Dispatcher.BeginInvoke( DispatcherPriority.Send, new Action( () =>
				{
					OnWebSocketJSONText( JSONText );
				} ) );
			}
			else
			{
				OnWebSocketJSONText( JSONText );
			}
		}

		public void HandleStatusMessage( Status StatusIn )
		{
			if ( StatusIn != null )
			{
				// check for authorization acknowledge
				if ( StatusIn.status.Contains( PGStatusMessages.AuthFailed ) )
				{
					PGStatus.IsAuthFailed = pgWebSocket.PGStatus.IsAuthFailed = true;
					FireOnPGAuthFailedEvent( ClusterName );
				}
				else if ( StatusIn.status.Contains( PGStatusMessages.AuthSuccess ) )
				{
					PGStatus.IsAuthorized = pgWebSocket.PGStatus.IsAuthorized = true;
					PGStatus.IsAuthFailed = pgWebSocket.PGStatus.IsAuthFailed = false;
					FireOnPGAuthorizedEvent( true );

					// restarting?
					if ( pgWebSocket.PGStatus.IsReStart )
					{
						FireOnPGRestartedEvent( ClusterName );
						ReSubscribeSymbols();
						pgWebSocket.PGStatus.IsReStart = false;
					}

					FireOnPGClusterReadyEvent( PGStatus.IsReconnect );
				}

				FirePGClusterStatusEvent( ClusterName, StatusIn );
			}
		}

		public virtual void SendSocketMessage( string Message )
		{
			pgWebSocket.SendWebSocketMessage( Message );
		}

		public virtual void OnWebSocketJSONText( string JSONText )
		{
		}

		#endregion

		#region Subscribe/UnSubscribe

		public virtual string SubscribeSymbol( string Symbol, List<string> Channels = null, bool CreateRec = true )
		{
			string Params = string.Empty;

			lock ( SubscribedSymbols )
			{
				if ( CreateRec )
				{
					SubscribeSymbolRec Rec = GetSubscribeSymbolRec( Symbol );
					if ( Rec == null )
						Rec = CreateSubscribeSymbolRec( Symbol, Channels );

					Channels = Rec.Channels;
					Rec.ChannelsAdded = Channels;
				}

				Params = FormatChannelParamsForSymbol( Symbol, Channels );

				if ( !pgWebSocket.PGStatus.IsConnected )
					QueueSubscribeData( Symbol, Params, Channels, true, CreateRec );
				else
					SendSubscribeMessage( Params );
			}

			return Params;
		}

		public virtual string UnSubscribeSymbol( string Symbol, List<string> Channels = null )
		{
			string Params = string.Empty;

			lock ( SubscribedSymbols )
			{
				if ( Channels == null )
				{
					// get/use all current Channels
					SubscribeSymbolRec Rec = GetSubscribeSymbolRec( Symbol );
					if ( Rec != null )
						Channels = Rec.Channels;
				}

				if ( Channels != null )
				{
					Params = FormatChannelParamsForSymbol( Symbol, Channels );

					if ( !pgWebSocket.PGStatus.IsConnected )
						QueueSubscribeData( Symbol, Params, Channels, false );
					else
						SendUnSubscribeMessage( Params );
				}
			}

			return Params;
		}

		public virtual void SendSubscribeMessage( string Params )
		{
			VerifyValidParams( Params );

			Params = Params.TrimEnd( ',' );
			SendSocketMessage( $@"{{""action"":""subscribe"",""params"":""{Params}""}}" );
			
			if ( VerboseInfo )
				FireOnTextInfoEvent( $"Subscribe: {Params}" );

			lock ( SubscribedSymbols )
			{
				// create new recs from Params for any new symbols
				VerifySubscribeSymbolRecsForParams( Params, true );

				OnSubscribeEvent?.Invoke( ClusterName, SubscribedSymbols );

				// mark Params recs as !New & !Removed
				SetSubscribeSymbolRecsFlags( Params, false, false );
			}
		}

		public virtual void SendUnSubscribeMessage( string Params, List<string> Channels = null )
		{
			SendSocketMessage( $@"{{""action"":""unsubscribe"",""params"":""{Params}""}}" );
			
			if ( VerboseInfo )
				FireOnTextInfoEvent( $"UnSubscribe: {Params}" );

			lock ( SubscribedSymbols )
			{
				// create recs from Params for any Params symbols,
				// since they are used to notify event listeners
				VerifySubscribeSymbolRecsForParams( Params, false );

				// mark Params recs as !New & ChannelsRemoved for event listeners to find
				SetSubscribeSymbolRecsFlags( Params, false, true );

				OnUnSubscribeEvent?.Invoke( ClusterName, SubscribedSymbols );

				// remove unused recs
				VerifyRemainingChannels( Params );
			}
		}

		public virtual void ReSubscribeSymbols()
		{
			lock ( SubscribedSymbols )
			{
				foreach ( var Symbol in SubscribedSymbols.Keys )
					SubscribeSymbol( Symbol );
			}
		}

		public void UnSubscribeAll()
		{
			if ( pgWebSocket.PGStatus.IsConnected )
			{
				List<SubscribeSymbolRec> Recs = new List<SubscribeSymbolRec>( SubscribedSymbols.Values.ToList() );
				foreach ( var Rec in Recs )
					SendUnSubscribeMessage( FormatChannelParamsForSymbol( Rec.Symbol, Rec.Channels ) );
			}
		}

		public virtual bool VerifyValidParams( string Params )
		{
			List<string> InvalidChannels = new List<string>();

			List<string> Symbols = Params.Split( ',' ).ToList();
			foreach ( var Symbol in Symbols )
			{
				string Channel = GetChannelOnly( Symbol );
				if ( !ValidChannels.Contains( Channel ) )
					InvalidChannels.Add( Channel );
			}

			bool AllValid = InvalidChannels.Count == 0; ;
			if ( !AllValid )
				OnInvalidChannelsEvent?.Invoke( Params, InvalidChannels );

			return AllValid;
		}

		private void VerifySubscribeSymbolRecsForParams( string Params, bool IsSubscribe )
		{
			List<string> Symbols = Params.Split( ',' ).ToList();
			foreach ( var Symbol in Symbols )
			{
				SubscribeSymbolRec Rec = GetSubscribeSymbolRec( Symbol, true );
				if ( Rec == null )
					Rec = CreateSubscribeSymbolRec( Symbol );

				// add Params to every record for event listeners to find/use
				Rec.Params = Params;

				if ( IsSubscribe )
					Rec.ChannelsAdded.Clear();
				else
					Rec.ChannelsRemoved.Clear();
				
				foreach ( string Channel in Symbols )
				{
					if ( IsSubscribe )
						Rec.ChannelsAdded.Add( GetChannelOnly( Channel ) );
					else
						Rec.ChannelsRemoved.Add( GetChannelOnly( Channel ) );
				}

				Rec.Params = Params;
			}
		}

		private void SetSubscribeSymbolRecsFlags( string Params, bool IsNew, bool ChannelsWereRemoved )
		{
			List<string> Symbols = Params.Split( ',' ).ToList();
			foreach ( var Symbol in Symbols )
			{
				SubscribeSymbolRec Rec = GetSubscribeSymbolRec( Symbol );
				if ( Rec != null )
				{
					Rec.IsNew = IsNew;
					Rec.ChannelsWereRemoved = ChannelsWereRemoved;
				}
			}
		}

		private void VerifyRemainingChannels( string Params )
		{
			List<string> Symbols = Params.Split( ',' ).ToList();
			foreach ( var Symbol in Symbols )
			{
				SubscribeSymbolRec Rec = GetSubscribeSymbolRec( Symbol );
				if ( Rec != null )
				{
					foreach ( string Channel in Symbols )
						Rec.Channels.Remove( GetChannelOnly( Channel ) );

					if ( Rec.Channels.Count == 0 )
						SubscribedSymbols.Remove( Rec.Symbol );

					// reset values
					Rec.ChannelsWereRemoved = false;
				}
			}
		}

		public SubscribeSymbolRec CreateSubscribeSymbolRec( string Symbol, List<string> Channels = null )
		{
			Symbol = GetSymbolOnly( Symbol );

			SubscribeSymbolRec Rec = GetSubscribeSymbolRec( Symbol, true );
			if ( Rec == null )
			{
				SubscribedSymbols[Symbol] = Rec = new SubscribeSymbolRec( Symbol )
				{
					// if Channels == null, use DefaultChannels for this Cluster
					Channels = Channels != null ? Channels : new List<string>( DefaultChannels )
				};
			}

			return Rec;
		}

		public SubscribeSymbolRec GetSubscribeSymbolRec( string Symbol, bool Create = false )
		{
			Symbol = GetSymbolOnly( Symbol );

			SubscribeSymbolRec Rec;
			if ( !SubscribedSymbols.TryGetValue( Symbol, out Rec ) && !Create )
			{
				// alert event listeners to no rec found
				OnNoSubscribeSymbolRecEvent?.Invoke( Symbol );
			}

			return Rec;
		}

		public void RemoveSymbol( string Symbol )
		{
			lock ( SubscribedSymbols )
			{
				UnSubscribeSymbol( Symbol );
				SubscribeSymbolRec Rec = GetSubscribeSymbolRec( Symbol );
				if ( Rec != null )
					SubscribedSymbols.Remove( GetSymbolOnly( Symbol ) );
			}
		}

		public static string FormatChannelParamsForSymbol( string Symbol, List<string> Channels )
		{
			// format Params by adding Channel.Symbol for each Channel
			string Params = string.Empty;
			if ( Channels != null )
			{
				foreach ( var Channel in Channels )
					Params += $"{Channel}.{Symbol},";
			}

			return Params.TrimEnd(',');
		}

		public bool IsSymbolSubscribed( string Symbol )
		{
			bool IsSubscribed;
			lock ( SubscribedSymbols )
				IsSubscribed = SubscribedSymbols.ContainsKey( Symbol );

			return IsSubscribed;
		}

		#endregion

		#region QueueSubscribeData

		public void QueueSubscribeData( string Symbol, string Params, List<string> Channels = null,
									bool IsSubscribe = true, bool CreateRec = false )
		{
			lock ( SymbolsQueue )
			{
				var QueuedSymbol = new QueuedSymbolRec()
				{ Symbol = Symbol, Params = Params, Channels = Channels, 
					CreateRec = CreateRec, IsSubscribe = true, IsQueued = true };

				SymbolsQueue.Add( QueuedSymbol );

				FireOnTextInfoEvent( $"{Symbol} was queued for subscribe on socket connected" );
				OnSymbolQueuedEvent?.Invoke( QueuedSymbol );
			}
		}

		private void OnWebSocketOpened( string ClusterName )
		{
			lock ( SymbolsQueue )
			{
				// handoff to Dispatcher at normal priority
				// to allow for UI thread access
				Dispatcher.BeginInvoke( new Action( () =>
				{
					foreach ( var QueuedSymbol in SymbolsQueue )
					{
						QueuedSubscribe( QueuedSymbol );

						QueuedSymbol.IsQueued = false;
						OnSymbolQueuedEvent?.Invoke( QueuedSymbol );
					}
				} ) );

				SymbolsQueue.Clear();
			}
		}

		private void QueuedSubscribe( QueuedSymbolRec QueuedSymbol )
		{
			if ( PGStatus.IsReconnect || PGStatus.IsAuthFailed )
				return;

			if ( QueuedSymbol.IsSubscribe )
				SubscribeSymbol( QueuedSymbol.Params, QueuedSymbol.Channels, QueuedSymbol.CreateRec );
			else
				UnSubscribeSymbol( QueuedSymbol.Params, QueuedSymbol.Channels );
		}

		#endregion

		#region ApiKey

		public string AddApiKey( string Url )
		{
			if ( !Url.Contains( "apiKey=" ) )
			{
				string AppendChar = Url.Contains( "?" ) ? "&" : "?";
				Url += $"{AppendChar}apiKey={ApiKey}";
			}
			return Url;
		}

		#endregion

		#region Requests

		public virtual PreviousClose RequestPreviousClose( string Symbol )
		{
			PreviousClose previousClose = null;

			try
			{
				string Url = $@"{PolygonUrl}/v2/aggs/ticker/{Symbol}/prev";
				Debug.WriteLine( $"RequestPreviousClose: {Url}" );

				Url = AddApiKey( Url );
				string JSONText = JsonSecureGet( Url );

				previousClose = JsonConvert.DeserializeObject<PreviousClose>( JSONText );
				previousClose.ticker = NormalizeSymbol( previousClose.ticker );
				FirePreviousCloseEvent( previousClose );
			}
			catch ( Exception ex )
			{
				string Message = $"RequestPreviousClose: error {ex.Message}";
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestPreviousClose", "", ex );
			}

			return previousClose;
		}

		public virtual DailyOpenClose RequestDailyOpenClose( string Symbol, DateTime? dateTime = null )
		{
			DailyOpenClose dailyOpenClose = null;

			try
			{
				// today's value won't be available, so try yesterday
				if ( !dateTime.HasValue )
					dateTime = DateTime.Now.AddDays(-1);

				string Url = $@"{PolygonUrl}/v1/open-close/{Symbol}/{dateTime.Value.ToString( "yyyy-MM-dd" )}";
				Debug.WriteLine( $"RequestDailyOpenClose: {Url}" );

				Url = AddApiKey( Url );

				string JSONText = JsonSecureGet( Url );

				dailyOpenClose = JsonConvert.DeserializeObject<DailyOpenClose>( JSONText );
				dailyOpenClose.symbol = NormalizeSymbol( dailyOpenClose.symbol );
				FireDailyOpenCloseEvent( dailyOpenClose );
			}
			catch ( Exception ex )
			{
				string Message = $"RequestDailyOpenClose: error {ex.Message}";
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestDailyOpenClose", "", ex );
			}

			return dailyOpenClose;
		}

		#endregion

		#region JsonSecureGet

		public string JsonSecureGet( string Url, object data = null,
												Dictionary<string, string> ParamsDict = null )
		{
			string JsonText = string.Empty;
			try
			{
				Uri UriRef = new Uri( Url );

				ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol =
#if DotNet4
				(SecurityProtocolType)SslProtocolsDotNet4.Tls12;
#else
				(SecurityProtocolType)SecurityProtocolType.Tls12;
#endif
				if ( VerboseInfo )
				{
					string CleanUrl = RemoveUrlApiKey( Url );
					FireOnTextInfoEvent( $"ExecJsonSecureGet - Start: {CleanUrl}" );
				}

				//ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
				using ( WebClient webClient = new WebClient() )
				{
					var stream = webClient.OpenRead( UriRef );
					using ( StreamReader sr = new StreamReader( stream ) )
					{
						JsonText = sr.ReadToEnd();
					}
				}

				if ( VerboseInfo )
					FireOnTextInfoEvent( $"ExecJsonSecureGet - End" );
			}
			catch ( Exception ex )
			{
				string Message = ex.Message;

				Message = $"ExecJsonSecureGet(): {Message}, Url: {Url}";
				throw new Exception( Message );
			}
			return JsonText;
		}

		/// <summary>
		/// Certificate validation callback.
		/// </summary>
		private static bool ValidateRemoteCertificate( object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors )
		{
			return true;        // for expediency/trusted cert

			// If the certificate is a valid, signed certificate, return true.
			//if ( error == System.Net.Security.SslPolicyErrors.None )
			//{
			//	return true;
			//}

			//Console.WriteLine( "X509Certificate [{cert.Subject}] Policy Error: '{error.ToString()}'";

			//return false;
		}

		#endregion

		#region FireEvents

		public void FireOnExecJsonSecureGetEvent( string Message )
		{
			OnExecJsonSecureGetEvent?.Invoke( Message );
		}

		public void FirePGClusterStatusEvent( string ClusterName, Status StatusIn )
		{
			switch ( StatusIn.status )
			{
				case PGStatusMessages.Connected:
					PGStatus.PGLogonSucceeded = true;
					break;
				default:
					break;
			}

			OnPGClusterStatusEvent?.Invoke( ClusterName, StatusIn.status, StatusIn.message );

			if ( VerboseInfo )
				FireOnTextInfoEvent( $"{ClusterName} Status: {StatusIn.status}, {StatusIn.message}" );
		}

		public void FireOnPGAuthorizedEvent( bool IsAuthorized )
		{
			OnPGAuthorizedEvent?.Invoke( ClusterName, IsAuthorized );
			FireOnTextInfoEvent( $"{ClusterName} Authorized" );
		}

		public void FireOnPGClusterReadyEvent( bool IsReconnect )
		{
			OnPGClusterReadyEvent?.Invoke( ClusterName, IsReconnect );
			FireOnTextInfoEvent( $"{ClusterName} Ready" );
		}

		public void FireOnPGAuthFailedEvent( string ClusterName )
		{
			OnPGAuthFailedEvent?.Invoke( ClusterName );
			FireOnTextInfoEvent( $"{ClusterName} PGon Authorization Failed" );
		}

		public void FireOnPGRestartedEvent( string ClusterName )
		{
			OnPGRestartedEvent?.Invoke( ClusterName );
			FireOnTextInfoEvent( $"{ClusterName} Restarted" );
		}

		public void FirePreviousCloseEvent( PreviousClose previousClose )
		{
			OnPreviousCloseEvent?.Invoke( ClusterName, previousClose );
		}
		public void FireDailyOpenCloseEvent( DailyOpenClose dailyOpenClose )
		{
			OnDailyOpenCloseEvent?.Invoke( ClusterName, dailyOpenClose );
		}

		#endregion

		#region Miscellaneous

		private void InitValidChannels()
		{
			switch ( ClusterName )
			{
				case PGClusterNames.Equities:
					ValidChannels = ValidEquitiesChannels;
					break;
				case PGClusterNames.Forex:
					ValidChannels = ValidForexChannels;
					break;
				case PGClusterNames.Crypto:
					ValidChannels = ValidCryptoChannels;
					break;
				default:
					throw new Exception( $"Unknown ClusterName: {ClusterName}" );
			}
		}
		public virtual void OnPGApiTerminated()
		{
			pgWebSocket.TerminatePGWebSocket();

			SubscribedSymbols.Clear();
			DefaultChannels.Clear();
		}

		public virtual string NormalizeSymbol( string Symbol )
		{
			return Symbol;
		}

		#endregion


	}

	#region Support classes

	// Handles SubscribeSymbols
	public class SubscribeSymbolRec : PGBase
	{
		#region Variables

		public string Symbol;
		public string Params;
		public bool IsNew = true;
		public bool ChannelsWereRemoved = true;

		#endregion

		#region Properties

		public List<string> Channels
		{
			get { return _Channels = _Channels ?? new List<string>(); }
			set { _Channels = value; }
		}
		List<string> _Channels;

		public List<string> ChannelsAdded
		{
			get { return _ChannelsAdded = _ChannelsAdded ?? new List<string>(); }
			set { _ChannelsAdded = value; }
		}
		List<string> _ChannelsAdded;

		public List<string> ChannelsRemoved
		{
			get { return _ChannelsRemoved = _ChannelsRemoved ?? new List<string>(); }
			set { _ChannelsRemoved = value; }
		}
		List<string> _ChannelsRemoved;

		#endregion

		public SubscribeSymbolRec( string Symbol )
		{
			this.Symbol = GetSymbolOnly( Symbol );
		}

		public void AddChannel( string ChannelName )
		{
			Channels.Add( ChannelName );
		}
		public void RemoveChannel( string ChannelName )
		{
			Channels.Remove( ChannelName );
		}
	}

	// https://polygon.io/sockets
	public static class PGClusterNames
	{
		public const string Equities = "Equities";
		public const string Forex = "Forex";
		public const string Crypto = "Crypto";
	}

	public static class PGEquityChannels
	{
		public const string Trades = "T";
		public const string Quotes = "Q";
		public const string AggMinute = "AM";
		public const string AggSecond = "A";
	}

	public static class PGForexChannels
	{
		public const string CurrenciesForex = "C";
		public const string CFAgg = "CA";
		public const string Bonds = "BONDS";
		public const string Commodities = "COMMODITIES";
		public const string Metals = "METALS";
	}

	public static class PGCryptoChannels
	{
		public const string Trades = "XT";
		public const string Quotes = "XQ";
		public const string ConsolidatedTape = "XS";
		public const string Level2Books = "XL2";
	}

	public static class PGStatusMessages
	{
		public const string Connected = "connected";
		public const string AuthSuccess = "auth_success";
		public const string AuthFailed = "auth_failed";
		public const string NotFound = "notfound";
	}

	#endregion
}
