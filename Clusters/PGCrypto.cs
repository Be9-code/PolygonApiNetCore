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
	public delegate void OnCryptoTradeParamDel( CryptoTrade trade );
	public delegate void OnCryptoQuoteParamDel( CryptoQuote quote );
	public delegate void OnCryptoAggParamDel( CryptoAggregate cryptoAgg );
	public delegate void OnCryptoSIPParamDel( CryptoSIP CryptoSIPRef );
	public delegate void OnCryptoLevel2ParamDel( CryptoLevel2 cryptoLevel2 );
	public delegate void OnCryptoLastTradeParamDel( CryptoLastTrade lastTrade );

	public class PGCrypto : PGClusterBase
	{
		#region Variables

		public static List<string> DefaultCryptoChannels = new List<string>()
			{
				PGCryptoChannels.Trades, PGCryptoChannels.Quotes,
				PGCryptoChannels.Level2Books,
				//PGCryptoChannels.ConsolidatedTape, 
			};

		#endregion

		#region Properties

		#endregion

		#region Events

		public event OnCryptoTradeParamDel OnCryptoTradeEvent;
		public event OnCryptoQuoteParamDel OnCryptoQuoteEvent;
		public event OnCryptoAggParamDel OnCryptoAggEvent;
		public event OnCryptoSIPParamDel OnCryptoSIPEvent;
		public event OnCryptoLevel2ParamDel OnCryptoLevel2Event;
		public event OnCryptoLastTradeParamDel OnCryptoLastTradeEvent;

		#endregion

		public PGCrypto( PGonApi PGApi )
			: base( PGApi, PGClusterNames.Crypto )
		{
			SymbolType = "Pair";

			InitDefaultChannels( DefaultCryptoChannels );
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
				foreach ( var PGBase in PGBaseList )
				{
					ObjJSONText = PGBase.ToString();
					PolygonBase pGBase = JsonConvert.DeserializeObject<PolygonBase>( ObjJSONText );

					switch ( pGBase.ev )
					{
						case "XQ":
							CryptoQuote quote = JsonConvert.DeserializeObject<CryptoQuote>( ObjJSONText );
							if ( quote != null )
							{
								OnCryptoQuoteEvent?.Invoke( quote );
							}
							break;

						case "XT":
							CryptoTrade trade = JsonConvert.DeserializeObject<CryptoTrade>( ObjJSONText );
							if ( trade != null )
							{
								OnCryptoTradeEvent?.Invoke( trade );
							}
							break;

						case "XA":
							CryptoAggregate cryptoAgg = JsonConvert.DeserializeObject<CryptoAggregate>( ObjJSONText );
							if ( cryptoAgg != null )
							{
								OnCryptoAggEvent?.Invoke( cryptoAgg );
							}
							break;

						case "XS":
							CryptoSIP CryptoSIPRef = JsonConvert.DeserializeObject<CryptoSIP>( ObjJSONText );
							if ( CryptoSIPRef != null )
							{
								OnCryptoSIPEvent?.Invoke( CryptoSIPRef );
							}
							break;

						case "XL2":
							CryptoLevel2 cryptoLevel2 = JsonConvert.DeserializeObject<CryptoLevel2>( ObjJSONText );
							if ( cryptoLevel2 != null )
							{
								OnCryptoLevel2Event?.Invoke( cryptoLevel2 );
							}
							break;

						case "status":
							Status StatusIn = JsonConvert.DeserializeObject<Status>( ObjJSONText );
							HandleStatusMessage( StatusIn );
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

		#region Requests

		public override PreviousClose RequestPreviousClose( string Symbol )
		{
			Symbol = FormatCryptoSymbol( Symbol );
			return base.RequestPreviousClose( Symbol );
		}

		public override DailyOpenClose RequestDailyOpenClose( string Symbol, DateTime? dateTime = null )
		{
			Symbol = FormatCryptoSymbol( Symbol );
			return base.RequestDailyOpenClose( Symbol );
		}

		public CryptoLastTrade RequestCryptoLastTrade( string Symbol )
		{
			CryptoLastTrade lastTrade = null;

			try
			{
				string Url = $@"{PolygonUrl}/v1/last/crypto/{Symbol}";
				Debug.WriteLine( $"Request CryptoLastQuote: {Url}" );

				Url = AddApiKey( Url );

				string JSONText = JsonSecureGet( Url );

				lastTrade = JsonConvert.DeserializeObject<CryptoLastTrade>( JSONText );
				if ( lastTrade != null )
					OnCryptoLastTradeEvent?.Invoke( lastTrade );
			}
			catch ( Exception ex )
			{
				string Message = $"RequestCryptoLastTrade: error {ex.Message}";
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestCryptoLastTrade", "", ex );
			}

			return lastTrade;
		}

		#endregion

		private static string FormatCryptoSymbol( string Symbol )
		{
			return Symbol = $"X:{Symbol.Replace( "/", "" )}";
		}

		public override string NormalizeSymbol( string Symbol )
		{
			if ( Symbol.Contains( ":" ) )
				Symbol = Symbol.Substring( Symbol.IndexOf( ":" ) + 1 );
			if ( !Symbol.Contains( @"/" ) )
				Symbol = string.Format( @"{0}/{1}", Symbol.Substring( 0, 3 ), Symbol.Substring( 3 ) );
			return Symbol;
		}
	}

}
