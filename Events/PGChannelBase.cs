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
using WebSocket4Net;

namespace PolygonApi.Channels
{
	public class PGChannelBase : PGBase
	{
		#region Variables

		public bool IsStarted;
		public bool IsRestart;
		public bool IsReconnect;

		public List<string> AdvisedSymbols = new List<string>();

		PGonApi PGApi;

		#endregion

		#region Properties

		public static PGWebSocket PGWebSocketRef
		{
			get { return _PGWebSocketRef = _PGWebSocketRef ?? new PGWebSocket(); }
			set { _PGWebSocketRef = value; }
		}
		static PGWebSocket _PGWebSocketRef = null;

		public WebSocket webSocket
		{
			get { return _webSocket = _webSocket ?? PGWebSocketRef.webSocket; }
			set { _webSocket = value; }
		}
		WebSocket _webSocket = null;

		internal string ApiKey
		{
			get { return _ApiKey; }
			set { _ApiKey = PGWebSocketRef.ApiKey = value; }
		}
		internal string _ApiKey = null;

		internal string ChannelName
		{
			get { return _ChannelName; }
			set { _ChannelName = PGWebSocketRef.ChannelName = value; }
		}
		internal string _ChannelName = null;

		#endregion

		#region Events

		public event OnBoolParamDel OnPGReadyEvent;
		public event OnNoParamsDel OnPGRestartedEvent;
		public event OnTextParamDel OnExecJsonSecureGetEvent;

		public event OnNoParamsDel OnWebSocketOpenedEvent
		{ add { PGWebSocketRef.OnWebSocketOpenedEvent += value; } remove { PGWebSocketRef.OnWebSocketOpenedEvent -= value; } }
		public event OnNoParamsDel OnWebSocketClosedEvent
		{ add { PGWebSocketRef.OnWebSocketClosedEvent += value; } remove { PGWebSocketRef.OnWebSocketClosedEvent -= value; } }
		public event OnTextParamDel OnWebSocketErrorEvent
		{ add { PGWebSocketRef.OnWebSocketErrorEvent += value; } remove { PGWebSocketRef.OnWebSocketErrorEvent -= value; } }

		#endregion

		public PGChannelBase( PGonApi PGApi, string ChannelName )
		{
			this.PGApi = PGApi;
			this.ChannelName = ChannelName;
			ApiKey = PGWebSocketRef.ApiKey = PGApi.ApiKey;
		}

		#region Start/Connect

		public virtual void Start( string Message = "Starting Polygon..." )
		{
			if ( IsStarted )
				return;

			IsStarted = true;

			PGWebSocketRef.Start( Message );
		}

		#endregion

		#region InitEvents/UnInitEvents

		public virtual void InitEvents()
		{
			UnInitEvents();
			PGWebSocketRef.OnWebSocketOpenedEvent += OnWebSocketOpened;
		}

		public virtual void UnInitEvents()
		{
			PGWebSocketRef.OnWebSocketOpenedEvent -= OnWebSocketOpened;
		}

		#endregion

		public virtual void OnJSONTextReceived( string JSONText )
		{
		}

		public virtual void OnWebSocketOpened()
		{
			string Text = string.Format( "{0} WebSocketOpened", ChannelName );
			FireOnAppendTextEvent( Text );
		}

		public virtual bool HandleAdviseSymbol( string Symbol )
		{
			bool IsAdvised = CheckIsAdvised( Symbol );
			if ( !IsAdvised )
				AdvisedSymbols.Add( Symbol );
			return IsAdvised;
		}

		public virtual bool HandleUnAdviseSymbol( string Symbol )
		{
			bool WasAdvised = CheckIsAdvised( Symbol );
			if ( WasAdvised )
				AdvisedSymbols.Remove( Symbol );
			return WasAdvised;
		}

		public bool CheckIsAdvised( string Symbol )
		{
			return AdvisedSymbols.Contains( Symbol );
		}

		public virtual void SubscribeToChannels( string Symbol )
		{
		}

		public virtual void UnSubscribeFromChannels( string Symbol )
		{
		}

		public void UnAdviseAllSymbols()
		{
			if ( PGWebSocketRef.IsConnected )
			{
				foreach ( var Symbol in AdvisedSymbols )
					HandleUnAdviseSymbol( Symbol );
			}
		}

		public static string JsonSecureGet( string Url, object data = null,
												Dictionary<string, string> ParamsDict = null )
		{
			string JsonText = string.Empty;
			try
			{
				Uri UriRef = new Uri( Url );

				ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = (SecurityProtocolType)SecurityProtocolType.Tls12;

				Debug.WriteLine( string.Format( "ExecJsonSecureGet - Start: {0}", Url ) );

				//ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
				using ( WebClient webClient = new WebClient() )
				{
					var stream = webClient.OpenRead( UriRef );
					using ( StreamReader sr = new StreamReader( stream ) )
					{
						JsonText = sr.ReadToEnd();
					}
				}
				Debug.WriteLine( string.Format( "ExecJsonSecureGet - End" ) );
				//Debug.WriteLine( string.Format( "ExecJsonSecureGet - End: {0}", Url ) );
			}
			catch ( Exception ex )
			{
				string Message = ex.Message;

				Message = string.Format( "ExecJsonSecureGet(): {0}, Url: {1}", Message, Url );
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

			//Console.WriteLine( "X509Certificate [{0}] Policy Error: '{1}'",
			//	cert.Subject,
			//	error.ToString() );

			//return false;
		}

		public string AddApiKey( string PolygonUrl )
		{
			if ( !PolygonUrl.Contains( "apiKey=" ) )
			{
				string AppendChar = PolygonUrl.Contains( "?" ) ? "&" : "?";
				PolygonUrl += string.Format( "{0}apiKey={1}", AppendChar, ApiKey );
			}
			return PolygonUrl;
		}

		public void AppendText( string Text )
		{
			FireOnAppendTextEvent( Text );
		}

		internal void FireOnExecJsonSecureGetEvent( string Message )
		{
			OnExecJsonSecureGetEvent?.Invoke( Message );
		}

		internal void FireOnPGReadyEvent( bool IsReconnect )
		{
			OnPGReadyEvent?.Invoke( IsReconnect );
		}

		internal void FireOnPGRestartedEvent()
		{
			OnPGRestartedEvent?.Invoke();
		}

	}

}
