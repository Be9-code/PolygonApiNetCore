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

namespace PolygonApi
{
	public delegate void OnNoParamsDel();
	public delegate void OnTextParamDel( string Text );
	public delegate void OnBoolParamDel( bool Param );
	public delegate void OnPGStatusParamDel( string Status, string Message );

	public class PGonWeb : PGBase
	{
		#region Variables

		public static string HTMLBasePath = BasePath + "HTML/";
		public static string XMLBasePath = BasePath + "XML/";
		public static string XSLBasePath = BasePath + "XSL/";

		public static string XMLConfigFileName = XMLBasePath + "AppConfig.xml";
		public bool IsConnected;
		public bool IsStarted;
		public bool IsRestart;
		public bool IsReconnect;
		public bool Level1TradesEnabled;
		public bool Level1QuotesEnabled;

		public static bool LogJSONText;

		bool LastEnable;

		public string TradesSymbol, QuotesSymbol;
		public static string PGonUrl;
		public static string PGonSocketUrl;

		#endregion

		#region Properties

		public static PGonWeb Instance
		{
			get { return _Instance = _Instance ?? new PGonWeb(); }
			set { _Instance = value; }
		}
		static PGonWeb _Instance = null;

		#endregion

		#region Events

		public event OnTextParamDel OnPGWebRestartEvent;

		public event OnTextParamDel OnExecJsonSecureGetEvent;

		#endregion

		// https://api.polygon.io/v2/ticks/stocks/trades/AMZN/2020-01-28?timestamp=1580227080000&timestampLimit=1580229000000&limit=1000&apiKey=gkS0W4vUuUn_sphz_jMq0CtKk8G8gDiW_tWlia

		public PGonWeb()
		{
			Instance = this;
		}

		public void TerminatePGWebInterface()
		{
			if ( PGWebSocketRef != null )
				PGWebSocketRef.TerminatePGWebSocket();
		}

		#region Start/Connect

		public void Start( string Message = "Starting Polygon..." )
		{
			if ( IsStarted )
				return;
			
			IsStarted = true;

			InitPGWebInterface();

			PGWebSocketRef.Start( Message );
		}

		private void InitPGWebInterface()
		{
		}

		#endregion
	}
}

