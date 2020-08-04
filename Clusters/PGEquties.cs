using Newtonsoft.Json;
using PolygonApi.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using WebSocket4Net;

namespace PolygonApi.Clusters
{
	public delegate void OnTradeParamDel( Trade trade );
	public delegate void OnQuoteParamDel( Quote quote );
	public delegate void OnAMinuteParamDel( AMinute AMinRef );
	public delegate void OnASecondParamDel( ASecond ASecRef );
	public delegate void OnLastTradeParamDel( LastTrade lastTrade );
	public delegate void OnLastQuoteParamDel( LastQuote LastQuote );
	public delegate void OnEquitiesSnapShotParamDel( EquitiesSnapShot SnapShot );
	public delegate void OnSymbolDataParamDel( SymbolDataRec SymbolDataRef );

	public class PGEquities : PGClusterBase
	{
		#region Variables

		public bool ConsolidateLevel1;
		public bool TextInfoEnabled;

		public static List<string> DefaultEquitiesChannels = new List<string>()
			{
				PGEquityChannels.Trades, PGEquityChannels.Quotes,
				PGEquityChannels.AggMinute, PGEquityChannels.AggSecond
			};

		#endregion

		#region Properties

		public SymbolDataHandler SymDataHandler
		{
			get { return _SymDataHandler = _SymDataHandler ?? new SymbolDataHandler(); }
			set { _SymDataHandler = value; }
		}
		SymbolDataHandler _SymDataHandler = null;

		#endregion

		#region Events

		public event OnTradeParamDel OnTradeEvent;
		public event OnQuoteParamDel OnQuoteEvent;
		public event OnAMinuteParamDel OnAMinuteEvent;
		public event OnASecondParamDel OnASecondEvent;
		public event OnLastTradeParamDel OnLastTradeEvent;
		public event OnLastQuoteParamDel OnLastQuoteEvent;
		public event OnEquitiesSnapShotParamDel OnEquitiesSnapShotEvent;
		public event OnSymbolDataParamDel OnSymbolDataEvent;

		#endregion

		public PGEquities( PGonApi PGApi )
			: base( PGApi, PGClusterNames.Equities )
		{
			SymbolType = "Symbol";

			InitDefaultChannels( DefaultEquitiesChannels );
		}

		#region InitEvents/UnInitEvents

		public override void InitEvents()
		{
			UnInitEvents();
			base.InitEvents();
		}

		public override void UnInitEvents()
		{
			base.UnInitEvents();
		}

		#endregion

		#region Socket Messages

		// =====>>>>>>>>>> JSONText Message Received

		public override void OnWebSocketJSONText( string JSONText )
		{
			try
			{
				string ObjJSONText;

				List<object> PGBaseList = JsonConvert.DeserializeObject<List<object>>( JSONText );
				foreach ( var pgBase in PGBaseList )
				{
					ObjJSONText = pgBase.ToString();
					PolygonBase pGBase = JsonConvert.DeserializeObject<PolygonBase>( ObjJSONText );

					switch ( pGBase.ev )
					{
						case "Q":
							Quote quote = JsonConvert.DeserializeObject<Quote>( ObjJSONText );
							if ( quote != null )
							{
								OnQuoteEvent?.Invoke( quote );
							}
							break;

						case "T":
							Trade trade = JsonConvert.DeserializeObject<Trade>( ObjJSONText );
							if ( trade != null )
							{
								OnTradeEvent?.Invoke( trade );
							}
							break;

						case "status":
							Status status = JsonConvert.DeserializeObject<Status>( ObjJSONText );
							HandleStatusMessage( status );
							break;

						case "A":
							ASecond ASecRef = JsonConvert.DeserializeObject<ASecond>( ObjJSONText );
							if ( ASecRef != null )
							{
								OnASecondEvent?.Invoke( ASecRef );
							}
							break;

						case "AM":
							AMinute AMinRef = JsonConvert.DeserializeObject<AMinute>( ObjJSONText );
							if ( AMinRef != null )
							{
								OnAMinuteEvent?.Invoke( AMinRef );
							}
							break;

						default:
							break;
					}
				}
			}
			catch ( System.Exception ex )
			{
				HandleJSONTextException( "websocket_MessageReceived", JSONText, ex );
			}
		}

		#endregion

		#region Trade/Quote

		private void OnTrade( Trade trade )
		{
			if ( ConsolidateLevel1 )
			{
				SymbolDataRec SymbolData = SymDataHandler.OnTrade( trade );
				OnSymbolDataEvent?.Invoke( SymbolData );
			}
		}

		private void OnQuote( Quote quote )
		{
			if ( ConsolidateLevel1 )
			{
				SymbolDataRec SymbolData = SymDataHandler.OnQuote( quote );
				OnSymbolDataEvent?.Invoke( SymbolData );
			}
		}

		protected virtual void OnLastTrade( LastTrade lastTrade )
		{
			if ( ConsolidateLevel1 )
			{
				SymbolDataRec SymbolData = SymDataHandler.OnLastTrade( lastTrade );
				OnSymbolDataEvent?.Invoke( SymbolData );
			}
		}

		protected virtual void OnLastQuote( LastQuote LastQuote )
		{
			if ( ConsolidateLevel1 )
			{
				SymbolDataRec SymbolData = SymDataHandler.OnLastQuote( LastQuote );
				OnSymbolDataEvent?.Invoke( SymbolData );
			}
		}

		#endregion

		#region Symbol/Trades & Quotes Requests

		public LastTrade RequestLastTrade( string Symbol )
		{
			LastTrade lastTrade = null;

			try
			{
				string Url = $@"{PolygonUrl}/v1/last/stocks/{Symbol}";
				Debug.WriteLine( $"RequestLastTrade: {Url}" );

				Url = AddApiKey( Url );

				string JSONText = JsonSecureGet( Url );

				lastTrade = JsonConvert.DeserializeObject<LastTrade>( JSONText );
				if ( lastTrade != null )
					OnLastTradeEvent?.Invoke( lastTrade );
			}
			catch ( Exception ex )
			{
				string Message = $"RequestLastTrade: error {ex.Message}";
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestLastTrade", "", ex );
			}

			return lastTrade;
		}

		public LastQuote RequestLastQuote( string Symbol )
		{
			LastQuote LastQuote = null;

			try
			{
				string Url = $@"{PolygonUrl}/v1/last_quote/stocks/{Symbol}";
				Debug.WriteLine( $"RequestLastQuote: {Url}" );

				Url = AddApiKey( Url );

				string JSONText = JsonSecureGet( Url );

				LastQuote = JsonConvert.DeserializeObject<LastQuote>( JSONText );
				if ( LastQuote != null )
					OnLastQuoteEvent?.Invoke( LastQuote );
			}
			catch ( Exception ex )
			{
				string Message = $"RequestLastQuote: error {ex.Message}";
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestLastQuote", "", ex );
			}

			return LastQuote;
		}

		#endregion

		#region SnapShot Requests

		public EquitiesSnapShot RequestEquitiesSnapShot( string Symbol )
		{
			EquitiesSnapShot SnapShot = null;

			try
			{
				string Url = $@"{PolygonUrl}/v2/snapshot/locale/us/markets/stocks/tickers/{Symbol}";
				Debug.WriteLine( $"RequestEquitiesSnapShot: {Url}" );

				Url = AddApiKey( Url );

				string JSONText = JsonSecureGet( Url );

				SnapShot = JsonConvert.DeserializeObject<EquitiesSnapShot>( JSONText );
				if ( SnapShot != null )
					OnEquitiesSnapShotEvent?.Invoke( SnapShot );
			}
			catch ( Exception ex )
			{
				string Message = $"RequestEquitiesSnapShot: error {ex.Message}";
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestEquitiesSnapShot", "", ex );
			}

			return SnapShot;
		}

		#endregion

		#region Bar Requests

		public string RequestTickBarsData( string Url )
		{
			string JSONText = RequestBarData( Url );
			return JSONText;
		}

		public string RequestBarData( string Url )
		{
			string JSONText = string.Empty;

			if ( pgWebSocket.PGStatus.IsConnected )
			{
				try
				{
					Url = AddApiKey( Url );
					JSONText = JsonSecureGet( Url );
				}
				catch ( Exception ex )
				{
					string Message = $"RequestBarData: error {ex.Message}";
					FireOnExecJsonSecureGetEvent( Message );

					HandleJSONTextException( "RequestBarData", "JSONText", ex );
				}
			}
			return JSONText;
		}

		#endregion

		#region ConsolidateLevel1

		public void SetConsolidateLevel1( bool Enabled )
		{
			ConsolidateLevel1 = Enabled;
			if ( Enabled )
			{
				OnTradeEvent += OnTrade;
				OnQuoteEvent += OnQuote;
				OnLastTradeEvent += OnLastTrade;
				OnLastQuoteEvent += OnLastQuote;
			}
			else
			{
				OnTradeEvent -= OnTrade;
				OnQuoteEvent -= OnQuote;
				OnLastTradeEvent -= OnLastTrade;
				OnLastQuoteEvent -= OnLastQuote;
			}
		}

		#endregion

	}

	// Handles consolidation of Trades/Quotes
	public class SymbolDataHandler : PGBase
	{
		#region Variables

		public Dictionary<string, SymbolDataRec> SymbolDataRecs = new Dictionary<string, SymbolDataRec>();

		#endregion

		public SymbolDataRec OnTrade( Trade trade )
		{
			SymbolDataRec SymbolData = GetSymbolDataRec( trade.sym );
			if ( SymbolData != null )
			{
				SymbolData.Type = SymbolDataTypes.Trade;
				SymbolData.LastPrice = trade.p;
				SymbolData.LastSize = trade.s;
				SymbolData.TimeStamp = UnixTimestampMillisToESTDateTime( trade.t );
				SymbolData.UnixNanoSecs = trade.t;
			}
			return SymbolData;
		}

		public SymbolDataRec OnQuote( Quote quote )
		{
			SymbolDataRec SymbolData = GetSymbolDataRec( quote.sym );
			if ( SymbolData != null )
			{
				SymbolData.Type = SymbolDataTypes.Quote;
				SymbolData.Bid = quote.bp;
				SymbolData.Ask = quote.ap;
				SymbolData.BidSize = quote.bs;
				SymbolData.AskSize = quote.asz;
				SymbolData.TimeStamp = UnixTimestampMillisToESTDateTime( quote.t );
				SymbolData.UnixNanoSecs = quote.t;
			}
			return SymbolData;
		}

		public SymbolDataRec OnLastTrade( LastTrade lastTrade )
		{
			SymbolDataRec SymbolData = GetSymbolDataRec( lastTrade.symbol );
			if ( SymbolData != null )
			{
				SymbolData.Type = SymbolDataTypes.LastTrade;
				SymbolData.LastPrice = lastTrade.last.price;
				SymbolData.LastSize = lastTrade.last.size;
				SymbolData.TimeStamp = UnixTimestampMillisToESTDateTime( lastTrade.last.timestamp );
				SymbolData.UnixNanoSecs = lastTrade.last.timestamp;
			}
			return SymbolData;
		}

		public SymbolDataRec OnLastQuote( LastQuote LastQuote )
		{
			SymbolDataRec SymbolData = GetSymbolDataRec( LastQuote.symbol );
			if ( SymbolData != null )
			{
				SymbolData.Type = SymbolDataTypes.LastQuote;
				SymbolData.Bid = LastQuote.last.bidprice;
				SymbolData.Ask = LastQuote.last.askprice;
				SymbolData.AskSize = LastQuote.last.asksize;
				SymbolData.BidSize = LastQuote.last.bidsize;
				SymbolData.TimeStamp = UnixTimestampMillisToESTDateTime( LastQuote.last.timestamp );
				SymbolData.UnixNanoSecs = LastQuote.last.timestamp;
			}
			return SymbolData;
		}

		public void VerifySymbolDataRec( string Symbol )
		{
			lock ( SymbolDataRecs )
			{
				if ( !SymbolDataRecs.Keys.Contains( Symbol ) )
					SymbolDataRecs[Symbol] = new SymbolDataRec( Symbol );
			}
		}

		public SymbolDataRec GetSymbolDataRec( string Symbol, bool Create = true )
		{
			SymbolDataRec SymbolData;
			lock ( SymbolDataRecs )
			{
				SymbolDataRecs.TryGetValue( Symbol, out SymbolData );
				if ( Create && SymbolData == null )
					SymbolDataRecs[Symbol] = SymbolData = new SymbolDataRec( Symbol );
			}
			return SymbolData;
		}

	}

	public static class SymbolDataTypes
	{
		public static string Trade = "Trade";
		public static string Quote = "Quote";
		public static string LastTrade = "LastTrade";
		public static string LastQuote = "LastQuote";
	}


	// SymbolDataRec is a consolidation of Trades/Quotes
	public class SymbolDataRec
	{
		public SymbolDataRec( string Symbol )
		{
			this.Symbol = Symbol;
		}
		public string Type;
		public string Symbol;
		public double LastPrice;
		public int LastSize;
		public double Bid;
		public double Ask;
		public int BidSize;
		public int AskSize;
		public int AccumulatedVolume;
		public DateTime TimeStamp;
		public long UnixNanoSecs;
	}

	public class QueuedSymbolRec
	{
		public string Symbol;
		public string Params;
		public List<string> Channels;
		public bool CreateRec;
		public bool IsSubscribe;
		public bool IsQueued;
	}
}
