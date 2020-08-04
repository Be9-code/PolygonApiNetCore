using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Newtonsoft.Json;
using WebSocket4Net;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Windows;
using PolygonApi.Clusters;
using PolygonApi.Helpers;

namespace PolygonApi
{
	public delegate void OnNoParamsDel();
	public delegate void OnTextParamDel( string Text );
	public delegate void OnBoolParamDel( bool Param );
	public delegate bool OnBoolReturnValueParamDel();

	public class PGBase
	{
		#region Variables

		public static string PolygonUrl = "https://api.polygon.io";

		// Note: populate with your Polygon.io Api key
		// by calling PGonApi.SetApiKey( ApiKey );
		// or by setting it directly for each object 
		public string ApiKey
		{
			get { return _ApiKey; }
			set { _ApiKey = value; }
		}
		internal string _ApiKey = null;

		public bool VerboseInfo;

		public static readonly DateTime UnixEpoch =
			new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );

		public static DateTime EarliestAggQueryDate = 
			DateTime.ParseExact( "2004-01-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture );

#if DEBUG
		public static string BasePath = System.AppDomain.CurrentDomain.BaseDirectory;
#else
		public static string BasePath = "";
#endif

		#endregion

		#region Properties

		public static Dispatcher Dispatcher
		{
			get { return _Dispatcher = _Dispatcher ?? Application.Current.Dispatcher; }
			set { _Dispatcher = value; }
		}
		static Dispatcher _Dispatcher = null;

		public StatusValues PGStatus
		{
			get { return _PGStatus = _PGStatus ?? new StatusValues( this ); }
			set { _PGStatus = value; }
		}
		StatusValues _PGStatus = null;

		public static List<PGBase> PGBaseObjs
		{
			get { return _PGBaseObjs = _PGBaseObjs ?? new List<PGBase>(); }
			set { _PGBaseObjs = value; }
		}
		static List<PGBase> _PGBaseObjs = null;

		#endregion

		#region Events

		public event OnTextParamDel OnTextInfoEvent;
		public static event OnTextParamDel OnPGExeceptionEvent;

		#endregion

		public PGBase()
		{
			if ( !PGBaseObjs.Contains( this ) )
				PGBaseObjs.Add( this );
		}

		#region TextInfo

		internal void FireOnTextInfoEvent( string Text )
		{
			if ( Dispatcher != null )
			{
				// handoff to Dispatcher at normal priority
				// to allow for UI thread access
				Dispatcher.BeginInvoke( new Action( () =>
				{
					OnTextInfoEvent?.Invoke( Text );
				} ) );
			}
			else
				OnTextInfoEvent?.Invoke( Text );
		}

		public void SubscribeToTextInfo( OnTextParamDel OnTextInfo, bool Subscribe = true, bool SubscribeToAll = true )
		{
			SubscribeTextInfo( OnTextInfo, Subscribe );
			if ( SubscribeToAll )
			{
				foreach ( var pGBase in PGBaseObjs )
					pGBase.SubscribeTextInfo( OnTextInfo, Subscribe );
			}
		}

		private void SubscribeTextInfo( OnTextParamDel OnTextInfo, bool Subscribe )
		{
			if ( Subscribe )
				OnTextInfoEvent += OnTextInfo;
			else
				OnTextInfoEvent += OnTextInfo;
		}

		#endregion

		#region ApiKey

		public void SetApiKey( string ApiKey, bool SetAll = true )
		{
			this.ApiKey = ApiKey;
			if ( SetAll )
			{
				foreach ( var pGBase in PGBaseObjs )
					pGBase.ApiKey = ApiKey;
			}
		}

		#endregion

		#region Exception handling

		public string HandleJSONTextException( string Name, string JSONText, Exception ex )
		{
			string ErrorMessage = $"{Name}(): JSON: {JSONText}";
			return HandleException( ErrorMessage, ex ); ;
		}

		public string HandleException( string Message, Exception ex )
		{
			Message = RemoveUrlApiKey( Message );

			string StackTrace = ex.StackTrace;
			string ErrorMessage = $"{Message}: {ex.Message}";
			Debug.WriteLine( ErrorMessage );

			OnPGExeceptionEvent?.Invoke( ErrorMessage );

			return ErrorMessage;
		}

		#endregion

		#region Utility functions

		public void SetVerboseInfo( bool VerboseInfo, bool SetAll = true )
		{
			this.VerboseInfo = VerboseInfo;
			if ( SetAll )
			{
				foreach ( var pGBase in PGBaseObjs )
					pGBase.VerboseInfo = VerboseInfo;
			}
		}

		public static string GetSymbolOnly( string Symbol )
		{
			if ( Symbol.Contains( "." ) )
				Symbol = Symbol.Substring( Symbol.IndexOf( "." ) + 1 );
			return Symbol;
		}

		public static string GetChannelOnly( string Channel )
		{
			if ( Channel.Contains( "." ) )
				Channel = Channel.Substring( 0, Channel.IndexOf( "." ) );
			return Channel;
		}

		public static string RemoveUrlApiKey( string Url )
		{
			// default to full Url
			string CleanUrl = Url;

			int Index = Url.ToLower().IndexOf( "apikey=" );
			if ( Index > 0 )
				CleanUrl = Url.Substring( 0, Index - 1 );

			return CleanUrl;
		}

		#endregion

		#region Time helpers

		public static DateTime UnixTimestampMillisToESTDateTime( long millis )
		{
			DateTime dt = UnixEpoch.AddMilliseconds( millis );

			dt = DateTimeToESTDateTime( dt );
			return dt;
		}

		private static readonly TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById( "Eastern Standard Time" );

		public static DateTime DateTimeToESTDateTime( DateTime dt )
		{
			dt = TimeZoneInfo.ConvertTime( dt, estTimeZone );
			return dt;
		}

		#endregion

	}
}

