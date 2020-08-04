using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using WebSocket4Net;

namespace PolygonApi.Channels
{
	public delegate void OnTradeParamDel( Trade TradeRef );
	public delegate void OnQuoteParamDel( Quote QuoteRef );
	public delegate void OnAMinuteParamDel( AMinute AMinRef );
	public delegate void OnASecondParamDel( ASecond ASecRef );
	public delegate void OnDailyOpenCloseParamDel( DailyOpenClose DailyOpenCloseRef );
	public delegate void OnPreviousCloseParamDel( PreviousClose PreviousCloseRef );
	public delegate void OnLastTradeParamDel( LastTrade LastTradeRef );
	public delegate void OnLastQuoteParamDel( LastQuote LastQuoteRef );

	public class PGEquities : PGChannelBase
	{
		#region Variables

		public bool Level1TradesEnabled;
		public bool Level1QuotesEnabled;

		List<QuoteTradeSymbols> QuoteTradeSymbolsQueue = new List<QuoteTradeSymbols>();

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
		public event OnDailyOpenCloseParamDel OnDailyOpenCloseEvent;
		public event OnPreviousCloseParamDel OnPreviousCloseEvent;
		public event OnLastTradeParamDel OnLastTradeEvent;
		public event OnLastQuoteParamDel OnLastQuoteEvent;
		public event OnPGStatusParamDel OnPGStatusEvent;

		#endregion

		public PGEquities( PGonApi PGApi )
			: base( PGApi, PGChannelNames.Equities )
		{
		}

		#region InitEvents/UnInitEvents

		public override void InitEvents()
		{
			base.InitEvents();
			UnInitEvents();
		}

		public override void UnInitEvents()
		{
			base.UnInitEvents();
		}

		#endregion

		public override void Start( string Message = "Starting Polygon..." )
		{
			base.Start( "Starting Polygon Equities channel..." );
		}

		#region Advise/UnAdvise

		public bool AdviseSymbol( string Symbol )
		{
			SymDataHandler.VerifySymbolDataRec( Symbol );
			bool IsAdvised = HandleAdviseSymbol( Symbol );
			return IsAdvised;
		}

		public bool UnAdviseSymbol( string Symbol )
		{
			bool WasAdvised = HandleUnAdviseSymbol( Symbol );
			return WasAdvised;
		}

		public override bool HandleAdviseSymbol( string Symbol )
		{
			bool IsAdvised = base.HandleAdviseSymbol( Symbol );
			if ( IsAdvised )
				EnableLevel1Data( true, Symbol, Symbol );
			return IsAdvised;
		}

		public override bool HandleUnAdviseSymbol( string Symbol )
		{
			bool WasAdvised = base.HandleUnAdviseSymbol( Symbol );
			return WasAdvised;
		}

		#endregion

		#region Socket Messages

		// =====>>>>>>>>>> JSONText Message Received

		public override void OnJSONTextReceived( string JSONText )
		{
			try
			{
				string ObjJSONText;

				List<object> PGBaseList = JsonConvert.DeserializeObject<List<object>>( JSONText );
				foreach ( var PGBase in PGBaseList )
				{
					ObjJSONText = PGBase.ToString();
					PolygonBase PGBaseRef = JsonConvert.DeserializeObject<PolygonBase>( ObjJSONText );

					switch ( PGBaseRef.ev )
					{
						case "Q":
							Quote QuoteRef = JsonConvert.DeserializeObject<Quote>( ObjJSONText );
							if ( QuoteRef != null )
							{
								OnQuoteEvent?.Invoke( QuoteRef );
							}
							break;

						case "T":
							Trade TradeRef = JsonConvert.DeserializeObject<Trade>( ObjJSONText );
							if ( TradeRef != null )
							{
								OnTradeEvent?.Invoke( TradeRef );
							}
							break;

						case "status":
							Status StatusRef = JsonConvert.DeserializeObject<Status>( ObjJSONText );
							if ( StatusRef != null )
							{
								if ( StatusRef.status.Contains( PGStatusMessages.AuthSuccess ) )
								{
									if ( IsReconnect )
										FireOnPGRestartedEvent();

									FireOnPGReadyEvent( IsReconnect );
								}

								OnPGStatusEvent?.Invoke( StatusRef.status, StatusRef.message );
							}
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

		#region Trade/Quote,JSONText

		// this handler is attached to PGWebRef.OnTradeEvent
		// which is fired for realtime data
		private void OnTrade( Trade TradeRef )
		{
			SymbolDataRec SymbolData = SymDataHandler.OnTrade( TradeRef );
			UpdateFromSymbolData( "Trade", SymbolData );
		}

		// this handler is attached to PGWebRef.OnQuoteEvent
		// which is fired for realtime data
		private void OnQuote( Quote QuoteRef )
		{
			SymbolDataRec SymbolData = SymDataHandler.OnQuote( QuoteRef );
			UpdateFromSymbolData( "***> Quote", SymbolData );
		}

		private void OnLastTrade( LastTrade LastTradeRef )
		{
			SymbolDataRec SymbolData = SymDataHandler.OnLastTrade( LastTradeRef );
			UpdateFromSymbolData( "==> LastTrade", SymbolData );
		}

		private void OnLastQuote( LastQuote LastQuoteRef )
		{
			SymbolDataRec SymbolData = SymDataHandler.OnLastQuote( LastQuoteRef );
			UpdateFromSymbolData( "==> LastQuote", SymbolData );
		}

		#endregion

		public override void SubscribeToChannels( string Symbol )
		{
			webSocket.Send( string.Format( @"{{""action"":""subscribe"",""params"":""Q.{0}""}}", Symbol ) );
			webSocket.Send( string.Format( @"{{""action"":""subscribe"",""params"":""T.{0}""}}", Symbol ) );
			webSocket.Send( string.Format( @"{{""action"":""subscribe"",""params"":""A.{0}""}}", Symbol ) );
			webSocket.Send( string.Format( @"{{""action"":""subscribe"",""params"":""AM.{0}""}}", Symbol ) );
		}

		public override void UnSubscribeFromChannels( string Symbol )
		{
			webSocket.Send( string.Format( @"{{""action"":""unsubscribe"",""params"":""Q.{0}""}}", Symbol ) );
			webSocket.Send( string.Format( @"{{""action"":""unsubscribe"",""params"":""T.{0}""}}", Symbol ) );
			webSocket.Send( string.Format( @"{{""action"":""unsubscribe"",""params"":""A.{0}""}}", Symbol ) );
			webSocket.Send( string.Format( @"{{""action"":""unsubscribe"",""params"":""AM.{0}""}}", Symbol ) );
		}

		public void AdviseAMinute( string Symbol )
		{
			webSocket.Send( string.Format( @"{{""action"":""subscribe"",""params"":""AM.{0}""}}", Symbol ) );
		}

		public void AdviseASecond( string Symbol )
		{
			webSocket.Send( string.Format( @"{{""action"":""subscribe"",""params"":""A.{0}""}}", Symbol ) );
		}

		#region Other Requests

		public void RequestPreviousClose( string Symbol )
		{
			try
			{
				string Url = string.Format( @"{0}/v2/aggs/ticker/{1}/prev", PolygonUrl, Symbol );
				PolygonUrl = AddApiKey( PolygonUrl );

				Debug.WriteLine( string.Format( "RequestPreviousClose: {0}", PolygonUrl ) );
				string JSONText = JsonSecureGet( PolygonUrl );

				OnRequestPreviousClose( JSONText );
			}
			catch ( Exception ex )
			{
				string Message = ex.Message;
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestLastTrade", "", ex );
			}
		}

		public void OnRequestPreviousClose( string JSONText )
		{
			PreviousClose PreviousCloseRef = JsonConvert.DeserializeObject<PreviousClose>( JSONText );
			if ( PreviousCloseRef != null )
				OnPreviousCloseEvent?.Invoke( PreviousCloseRef );
		}

		public void RequestDailyOpenClose( string Symbol )
		{
			try
			{
				string Url = string.Format( @"{0}/v1/open-close/{1}", PolygonUrl, Symbol );
				PolygonUrl = AddApiKey( PolygonUrl );

				Debug.WriteLine( string.Format( "RequestDailyOpenClose: {0}", PolygonUrl ) );

				string JSONText = JsonSecureGet( PolygonUrl );

				OnRequestDailyOpenClose( JSONText );
			}
			catch ( Exception ex )
			{
				string Message = ex.Message;
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestLastTrade", "", ex );
			}
		}

		public void OnRequestDailyOpenClose( string JSONText )
		{
			DailyOpenClose DailyOpenCloseRef = JsonConvert.DeserializeObject<DailyOpenClose>( JSONText );
			if ( DailyOpenCloseRef != null )
				OnDailyOpenCloseEvent?.Invoke( DailyOpenCloseRef );
		}

		#endregion

		#region Symbol/Trades & Quotes Requests

		public void RequestLastTrade( string Symbol )
		{
			try
			{
				string Url = string.Format( @"{0}/v1/last/stocks/{1}", PolygonUrl, Symbol );
				PolygonUrl = AddApiKey( PolygonUrl );

				Debug.WriteLine( string.Format( "RequestLastTrade: {0}", PolygonUrl ) );

				string JSONText = JsonSecureGet( PolygonUrl );

				OnRequestLastTrade( JSONText );
			}
			catch ( Exception ex )
			{
				string Message = ex.Message;
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestLastTrade", "", ex );
			}
		}

		public void OnRequestLastTrade( string JSONText )
		{
			LastTrade LastTradeRef = JsonConvert.DeserializeObject<LastTrade>( JSONText );
			if ( LastTradeRef != null )
				OnLastTradeEvent?.Invoke( LastTradeRef );
		}

		public void RequestLastQuote( string Symbol )
		{
			try
			{
				string Url = string.Format( @"{0}/v1/last_quote/stocks/{1}", PolygonUrl, Symbol );
				PolygonUrl = AddApiKey( PolygonUrl );

				Debug.WriteLine( string.Format( "RequestLastQuote: {0}", PolygonUrl ) );

				string JSONText = JsonSecureGet( PolygonUrl );

				OnRequestLastQuote( JSONText );
			}
			catch ( Exception ex )
			{
				string Message = ex.Message;
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestLastQuote", "", ex );
			}
		}

		public void OnRequestLastQuote( string JSONText )
		{
			LastQuote LastQuoteRef = JsonConvert.DeserializeObject<LastQuote>( JSONText );
			if ( LastQuoteRef != null )
				OnLastQuoteEvent?.Invoke( LastQuoteRef );
		}

		#endregion

		public void EnableLevel1Data( bool Enable, string QuotesSymbol = "", string TradesSymbol = "" )
		{
			if ( !PGWebSocketRef.IsConnected )
			{
				lock ( QuoteTradeSymbolsQueue )
				{
					QuoteTradeSymbolsQueue.Add( new QuoteTradeSymbols
					{ Enable = Enable, QuotesSymbol = QuotesSymbol, TradesSymbol = TradesSymbol } );

					OnWebSocketOpenedEvent += HandleEnableLevel1Data;
				}
				Start();
			}
			else
				ExecEnableLevel1Data( Enable, QuotesSymbol, TradesSymbol );
		}

		private void HandleEnableLevel1Data()
		{
			OnWebSocketOpenedEvent -= HandleEnableLevel1Data;
			lock ( QuoteTradeSymbolsQueue )
			{
				foreach ( var QTSymbols in QuoteTradeSymbolsQueue )
					ExecEnableLevel1Data( QTSymbols.Enable, QTSymbols.QuotesSymbol, QTSymbols.TradesSymbol );

				QuoteTradeSymbolsQueue.Clear();
			}
		}

		private void ExecEnableLevel1Data( bool Enable, string QuotesSymbol = "", string TradesSymbol = "" )
		{
			if ( IsReconnect )
				return;

			string Subscribe = Enable ? "subscribe" : "unsubscribe";
			if ( Level1QuotesEnabled )
			{
				if ( !string.IsNullOrEmpty( QuotesSymbol ) )
					webSocket.Send( string.Format( @"{{""action"":""{0}"",""params"":""Q.{1}""}}", Subscribe, QuotesSymbol ) );
				if ( !string.IsNullOrEmpty( TradesSymbol ) )
					webSocket.Send( string.Format( @"{{""action"":""{0}"",""params"":""T.{1}""}}", Subscribe, QuotesSymbol ) );
			}
		}

		#region Bar Requests

		public string RequestTickBarsData( string PolygonUrl )
		{
			string JSONText = RequestBarData( PolygonUrl );
			return JSONText;
		}

		public string RequestBarData( string PolygonUrl )
		{
			string JSONText = string.Empty;

			if ( PGWebSocketRef.IsConnected )
			{
				try
				{
					PolygonUrl = AddApiKey( PolygonUrl );
					//Debug.WriteLine( PolygonUrl );

					JSONText = JsonSecureGet( PolygonUrl );
				}
				catch ( Exception ex )
				{
					string Message = ex.Message;
					FireOnExecJsonSecureGetEvent( Message );

					HandleJSONTextException( "RequestBarData", "JSONText", ex );
				}
			}
			return JSONText;
		}

		#endregion

		private void UpdateFromSymbolData( string Type, SymbolDataRec SymbolData )
		{
			string Text = string.Format( "{0}: Symbol: {1}, Price: {2}, Size: {3}, " +
										"Bid: {4}, Ask: {5}, " +
										"BidSize: {6}, AskSize: {7} " +
										"Time: {8} EST",
										Type, SymbolData.Symbol,
										SymbolData.LastPrice, SymbolData.LastSize,
										SymbolData.Bid, SymbolData.Ask,
										SymbolData.BidSize, SymbolData.AskSize,
										SymbolData.TimeStamp );
			AppendText( Text );
		}
	}

	// Handles consolidation of Trades/Quotes
	public class SymbolDataHandler : PGBase
	{
		#region Variables

		public bool IsSimulationMode;
		public Dictionary<string, SymbolDataRec> SymbolDataRecs = new Dictionary<string, SymbolDataRec>();

		#endregion

		public SymbolDataRec OnTrade( Trade TradeRef )
		{
			if ( IsSimulationMode )
				VerifySymbolDataRec( TradeRef.sym );

			SymbolDataRec SymbolData = GetSymbolDataRec( TradeRef.sym );
			if ( SymbolData != null )
			{
				SymbolData.LastPrice = TradeRef.p;
				SymbolData.LastSize = TradeRef.s;
				SymbolData.TimeStamp = DateTimeFromUnixTimestampMillis( TradeRef.t );
			}
			return SymbolData;
		}

		public SymbolDataRec OnQuote( Quote QuoteRef )
		{
			if ( IsSimulationMode )
				VerifySymbolDataRec( QuoteRef.sym );

			SymbolDataRec SymbolData = GetSymbolDataRec( QuoteRef.sym );
			if ( SymbolData != null )
			{
				SymbolData.Bid = QuoteRef.bp;
				SymbolData.Ask = QuoteRef.ap;
				SymbolData.BidSize = QuoteRef.bs;
				SymbolData.AskSize = QuoteRef.ask;
				SymbolData.TimeStamp = DateTimeFromUnixTimestampMillis( QuoteRef.t );
			}
			return SymbolData;
		}

		public SymbolDataRec OnLastTrade( LastTrade LastTradeRef )
		{
			if ( IsSimulationMode )
				VerifySymbolDataRec( LastTradeRef.symbol );

			SymbolDataRec SymbolData = GetSymbolDataRec( LastTradeRef.symbol );
			if ( SymbolData != null )
			{
				SymbolData.LastPrice = LastTradeRef.last.price;
				SymbolData.LastSize = LastTradeRef.last.size;
				SymbolData.TimeStamp = DateTimeFromUnixTimestampMillis( LastTradeRef.last.timestamp );
			}
			return SymbolData;
		}

		public SymbolDataRec OnLastQuote( LastQuote LastQuoteRef )
		{
			if ( IsSimulationMode )
				VerifySymbolDataRec( LastQuoteRef.symbol );

			SymbolDataRec SymbolData = GetSymbolDataRec( LastQuoteRef.symbol );
			if ( SymbolData != null )
			{
				SymbolData.Bid = LastQuoteRef.last.bidprice;
				SymbolData.Ask = LastQuoteRef.last.askprice;
				SymbolData.AskSize = LastQuoteRef.last.asksize;
				SymbolData.BidSize = LastQuoteRef.last.bidsize;
				SymbolData.TimeStamp = DateTimeFromUnixTimestampMillis( LastQuoteRef.last.timestamp );
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

		public SymbolDataRec GetSymbolDataRec( string Symbol )
		{
			SymbolDataRec SymbolData;
			lock ( SymbolDataRecs )
			{
				SymbolDataRecs.TryGetValue( Symbol, out SymbolData );
			}
			return SymbolData;
		}

	}


	// SymbolDataRec is a consolidation of Trades/Quotes
	public class SymbolDataRec
	{
		public SymbolDataRec( string Symbol )
		{
			this.Symbol = Symbol;
		}
		public string Symbol;
		public double LastPrice;
		public double LastSize;
		public double Bid;
		public double Ask;
		public double BidSize;
		public double AskSize;
		public DateTime TimeStamp;
	}

	class QuoteTradeSymbols
	{
		public bool Enable;
		public string QuotesSymbol;
		public string TradesSymbol;
	}
}
