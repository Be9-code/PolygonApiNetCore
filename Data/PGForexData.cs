using System;
using System.Collections.Generic;
using System.Text;

namespace PolygonApi.Data
{
	// https://polygon.io/sockets

	#region Currencies / Forex

	// Forex QUOTE
	public class ForexQuote : PolygonBase
	{
		public string p { get; set; }		// Currency Pair
		public string x { get; set; }		// FX Exchange ID
		public double a { get; set; }		// Ask Price
		public double b { get; set; }		// Bid Price
		public long t { get; set; }		// Quote Timestamp ( Unix MS )
	}

	// Forex Aggregate
	public class ForexAggregate : PolygonBase
	{
		public string pair { get; set; }	// Currency Pair
		public double o { get; set; }		// Open
		public double c { get; set; }		// Close
		public double h { get; set; }		// High
		public double l { get; set; }		// Low
		public long v { get; set; }		// Volume ( Quotes during this duration )
		public long s { get; set; }		// Tick Start Timestamp
	}

	#region Last

	// Forex Last QUOTE
	public class ForexLastQuote : PolygonBase
	{
		public string status { get; set; }      // Status of this requests response
		public string symbol { get; set; }      // Symbol that was evaluated from the request
		public ForexQuoteLast last { get; set; }
	}
	public class ForexQuoteLast
	{
		public double ask { get; set; }
		public double bid { get; set; }
		public int exchange { get; set; }
		public long timestamp { get; set; }
	}

	#endregion

	#endregion

}
