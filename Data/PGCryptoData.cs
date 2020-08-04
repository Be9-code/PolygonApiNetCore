using System;
using System.Collections.Generic;
using System.Text;

namespace PolygonApi.Data
{
	// https://polygon.io/sockets

	#region Crypto

	// Crypto TRADE
	public class CryptoTrade : PolygonBase
	{
		public string pair { get; set; }	// Crypto Pair
		public double p { get; set; }		// Price
		public long t { get; set; }		// Exchange Timestamp Unix ( ms )
		public double s { get; set; }		// Size
		public object c { get; set; }		// Condition
		public int i { get; set; }		// Trade ID ( Optional )
		public int xt { get; set; }		// Exchange ID
		public long r { get; set; }		// Received @ Polygon Timestamp
	}

	// Crypto QUOTE
	public class CryptoQuote : PolygonBase
	{
		public string pair { get; set; }	// Crypto Pair
		public double lp { get; set; }	// Last Trade Price
		public double ls { get; set; }	// Last Trade Size
		public double bp { get; set; }	// Bid Price
		public double bs { get; set; }	// Bid Size
		public double ap { get; set; }	// Ask Price
		public double asz { get; set; }	// Ask Size
		public long t { get; set; }		// Exchange Timestamp Unix ( ms )
		public int xt { get; set; }		// Exchange ID
		public long r { get; set; }		// Received @ Polygon Timestamp
	}

	// Crypto AGGREGATE
	public class CryptoAggregate : PolygonBase
	{
		public string pair { get; set; }	// Crypto Pair
		public double o { get; set; }		// Open Price
		public int ox { get; set; }		// Open Exchange
		public double h { get; set; }		// High Price
		public int hx { get; set; }		// High Exchange
		public double l { get; set; }		// Low Price
		public int lx { get; set; }		// Low Exchange
		public double cl { get; set; }	// Close Price
		public int cx { get; set; }		// Close Exchange
		public double v { get; set; }		// Volume of Trades in Tick
		public long s { get; set; }		// Tick Start Timestamp
		public long e { get; set; }		// Tick End Timestamp
	}

	// Crypto SIP ( NBBO ):
	public class CryptoSIP : PolygonBase
	{
		public string pair { get; set; }		// Crypto Pair
		public double asz { get; set; }		// Ask Size
		public double ap { get; set; }		// Ask Price
		public int ax { get; set; }			// Ask Exchange
		public double bs { get; set; }		// Ask Size
		public double bp { get; set; }		// Ask Price
		public int bx { get; set; }			// Ask Exchange
		public long t { get; set; }			// Tick Start Timestamp
	}

	// https://polygon.io/sockets
	// Crypto LEVEL2:
	//{
	//    "ev": "XL2",                // Event Type
	//    "pair": "BTC-USD",          // Crypto Pair
	//    "b": [[ 6001.00, 1.432 ],   // Bid Prices ( 100 depth cap )
	//        [ 6000.98, 4.665 ],     // [ Price, Size ]
	//        [ 5999.30, .432434 ]],                  
	//    "a": [[ 6001.10, 2.2 ],     // Ask Prices ( 100 depth cap )
	//        [ 6001.45, 1.405 ],     // [ Price, Size ]
	//        [ 6002.10, 10.43 ]],
	//    "t": 1342342342342,         // Timestamp Unix ( ms )
	//    "x": 11,                    // Exchange ID
	//    "r": 1234134124123          // Tick Received @ Polygon Timestamp
	//}

	public class CryptoLevel2 : PolygonBase
	{
		public string pair { get; set; }	// Crypto Pair
		public object b { get; set; }     // Bid Prices ( 100 depth cap )
		public object a { get; set; }     // Ask Prices ( 100 depth cap )
		public long t { get; set; }       // Timestamp Unix ( ms )
		public int xt { get; set; }       // Exchange ID
		public long r { get; set; }       // Received @ Polygon Timestamp
	}

	#region Last

	// https://polygon.io/docs/#!/Stocks--Equities/get_v1_last_stocks_symbol
	public class CryptoLastTrade
	{
		public string status { get; set; }      // Status of this requests response
		public string symbol { get; set; }      // Symbol that was evaluated from the request
		public CryptoTradeLast last { get; set; }
	}
	public class CryptoTradeLast
	{
		public double price { get; set; }       // Price of the trade
		public double size { get; set; }           // Size of this trade
		public int exchange { get; set; }       // Exchange this trade happened on
		public object conditions { get; set; }
		public long timestamp { get; set; }
	}

	#endregion

	#endregion

}
