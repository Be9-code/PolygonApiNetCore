using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Newtonsoft.Json;
using WebSocket4Net;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using PolygonApi.Clusters;

namespace PolygonApi
{
	public class PGWebSocket : PGBase
	{
		#region Variables

		internal WebSocket webSocket;

		public string PGonWebSocketUrl = "wss://socket.polygon.io";

		#endregion

		#region Properties

		#endregion

		#region Events

		public event OnTextParamDel OnWebSocketOpenedEvent;
		public event OnTextParamDel OnWebSocketClosedEvent;
		public event OnTextParamDel OnWebSocketErrorEvent;
		public event OnTextParamDel OnPGWebRestartEvent;
		public event OnTextParamDel OnSocketMessageEvent;

		#endregion

		public PGWebSocket()
		{
			PGStatus.webSocket = webSocket;
		}

		#region Start/Connect

		public void Start( string Message = "Starting Polygon..." )
		{
			try
			{
				if ( PGStatus.IsStarted )
					return;

				PGStatus.IsStarted = true;

				if ( webSocket != null )
					TerminatePGWebSocket();

				string ClusterUrl = GetClusterUrl();
				string Url = $@"{PGonWebSocketUrl}/{ClusterUrl}";

				// Note: SslProtocolsDotNet4 used for .Net4.0
#if DotNet4
				webSocket = new WebSocket( Url, sslProtocols: (SslProtocols)SslProtocolsDotNet4.Tls12 );
#else
				// Note: SslProtocols used for > .Net4.0
				webSocket = new WebSocket( Url, sslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls );
#endif

				InitEvents();
				OpenWebSocket();
			}
			catch ( Exception ex )
			{
				string ErrorMessage = $"PGWebSocket: Start(): {ex.Message}";
				HandleException( ErrorMessage, ex );
			}
		}


		#region InitEvents/UnInitEvents

		public virtual void InitEvents()
		{
			UnInitEvents();
			webSocket.Opened += websocket_Opened;
			webSocket.Error += websocket_Error;
			webSocket.Closed += websocket_Closed;
			webSocket.MessageReceived += websocket_MessageReceived;
		}

		public virtual void UnInitEvents()
		{
			webSocket.Opened -= websocket_Opened;
			webSocket.Error -= websocket_Error;
			webSocket.Closed -= websocket_Closed;
			webSocket.MessageReceived -= websocket_MessageReceived;
		}

		#endregion

		private void websocket_Opened( object sender, EventArgs e )
		{
			FireOnTextInfoEvent( $"{PGStatus.ClusterName} Web Socket Connected!" );

			// send authorization request, will receive acknowledge
			// in PGClusterBase.HandleStatusMessage()
			webSocket.Send( $@"{{""action"":""auth"",""params"":""{ApiKey}""}}" );

			PGStatus.IsConnected = true;
			OnWebSocketOpenedEvent?.Invoke( PGStatus.ClusterName );
		}

		private void websocket_Error( object sender, SuperSocket.ClientEngine.ErrorEventArgs e )
		{
			string Message = $"{PGStatus.ClusterName} WebSocket Error: {e.Exception.Message}";
			FireOnTextInfoEvent( Message );

			OnWebSocketErrorEvent?.Invoke( e.Exception.Message );
		}

		private void websocket_Closed( object sender, EventArgs e )
		{
			try
			{
				FireOnTextInfoEvent( $"{PGStatus.ClusterName} Connection Closed..." );
				OnWebSocketClosedEvent?.Invoke( PGStatus.ClusterName );

				string ClosedMessage = DateTime.Now.ToString();
				PGStatus.IsConnected = false;

				// Reconnect logic...
				if ( PGStatus.AutoReconnect && PGStatus.IsAuthorized &&
					!PGStatus.IsTerminate && !PGStatus.IsAuthFailed )
				{
					ReStart( ClosedMessage );
				}
			}
			catch ( Exception ex )
			{
				string ErrorMessage = $"websocket_Closed(): {ex.Message}";
			}
		}

		private void websocket_MessageReceived( object sender, MessageReceivedEventArgs e )
		{
			OnSocketMessageEvent?.Invoke( e.Message );
		}

		private void ReStart( object ClosedMessage )
		{
			OnPGWebRestartEvent?.Invoke( ClosedMessage as string );

			string Message = $"Restarting Polygon {PGStatus.ClusterName}...";
			FireOnTextInfoEvent( Message );

			PGStatus.IsReconnect = true;
			PGStatus.IsReStart = true;

			if ( PGStatus.IsStarted )
				OpenWebSocket( Message );
			else
			{
				Start( Message );
			}
		}

		private void OpenWebSocket( string Message = "Starting Polygon..." )
		{
			FireOnTextInfoEvent( $"{PGStatus.ClusterName} {Message}" );
			this.webSocket.Open();
		}

		#endregion

		#region SocketMessage

		public void SendWebSocketMessage( string Message )
		{
			if ( !PGStatus.IsConnected )
				return;

			webSocket.Send( Message );
		}

		#endregion

		public void TerminatePGWebSocket( bool PGApiTerminated = true )
		{
			PGStatus.IsTerminate = PGApiTerminated;
			if ( webSocket != null )
			{
				webSocket.Close();
				if ( PGApiTerminated )
					webSocket.Dispose();
			}
		}

		private string GetClusterUrl()
		{
			string ClusterUrl;
			switch ( PGStatus.ClusterName )
			{
				case PGClusterNames.Equities:
					ClusterUrl = "stocks";
					break;
				case PGClusterNames.Forex:
					ClusterUrl = "forex";
					break;
				case PGClusterNames.Crypto:
					ClusterUrl = "crypto";
					break;
				default:
					throw new Exception( $"Unknown status.ClusterName: {PGStatus.ClusterName}" );
			}

			return ClusterUrl;
		}

	}

	// DotNet4.0: Defines the possible versions of System.Security.Authentication.SslProtocols.
	[Flags]
	public enum SslProtocolsDotNet4
	{
		//
		// Summary:
		//     Allows the operating system to choose the best protocol to use, and to block
		//     protocols that are not secure. Unless your app has a specific reason not to,
		//     you should use this field.
		None = 0,
		//
		// Summary:
		//     Specifies the SSL 2.0 protocol. SSL 2.0 has been superseded by the TLS protocol
		//     and is provided for backward compatibility only.
		Ssl2 = 12,
		//
		// Summary:
		//     Specifies the SSL 3.0 protocol. SSL 3.0 has been superseded by the TLS protocol
		//     and is provided for backward compatibility only.
		Ssl3 = 48,
		//
		// Summary:
		//     Specifies the TLS 1.0 security protocol. The TLS protocol is defined in IETF
		//     RFC 2246.
		Tls = 192,
		//
		// Summary:
		//     Use None instead of Default. Default permits only the Secure Sockets Layer (SSL)
		//     3.0 or Transport Layer Security (TLS) 1.0 protocols to be negotiated, and those
		//     options are now considered obsolete. Consequently, Default is not allowed in
		//     many organizations. Despite the name of this field, System.Net.Security.SslStream
		//     does not use it as a default except under special circumstances.
		Default = 240,
		//
		// Summary:
		//     Specifies the TLS 1.1 security protocol. The TLS protocol is defined in IETF
		//     RFC 4346.
		Tls11 = 768,
		//
		// Summary:
		//     Specifies the TLS 1.2 security protocol. The TLS protocol is defined in IETF
		//     RFC 5246.
		Tls12 = 3072,
		//
		// Summary:
		//     Specifies the TLS 1.3 security protocol. The TLS protocol is defined in IETF
		//     RFC 8446.
		Tls13 = 12288
	}

}

