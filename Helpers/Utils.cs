using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocket4Net;

namespace PolygonApi.Helpers
{

	#region StatusValues

	public class Utils
	{
		public static bool IsWeekday( DateTime dateTime )
		{
			return ( dateTime.DayOfWeek >= DayOfWeek.Monday && dateTime.DayOfWeek <= DayOfWeek.Friday );
		}
		public static bool IsWeekend( DateTime dateTime )
		{
			return ( dateTime.DayOfWeek > DayOfWeek.Friday && dateTime.DayOfWeek < DayOfWeek.Monday );
		}
	}

	public class StatusValues
	{
		#region Properties

		public static List<StatusValues> StatusValuesObjs
		{
			get { return _StatusValuesObjs = _StatusValuesObjs ?? new List<StatusValues>(); }
			set { _StatusValuesObjs = value; }
		}
		static List<StatusValues> _StatusValuesObjs = null;

		#endregion

		public PGBase pGBase;
		public WebSocket webSocket;
		public string ClusterName;
		public bool PGLogonSucceeded;
		public bool AutoReconnect = true;
		public bool IsConnected;
		public bool IsStarted;
		public bool IsReconnect;
		public bool IsAuthorized;
		public bool IsAuthFailed;
		public bool IsTerminate;
		public bool IsReStart;

		public StatusValues( PGBase pGBase )
		{
			StatusValuesObjs.Add( this );
		}

		public static StatusValues GetClusterStatusValues( string ClusterName )
		{
			StatusValues SVRef = StatusValuesObjs.Find( x => x.ClusterName == ClusterName );
			return SVRef;
		}
	}

	#endregion

}
