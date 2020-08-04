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
	public delegate void OnForexQuoteParamDel( ForexQuote quote );
	public delegate void OnForexAggParamDel( ForexAggregate forexAgg );
	public delegate void OnForexLastQuoteParamDel( ForexLastQuote LastQuote );

	public class PGForex : PGClusterBase
	{
		#region Variables

		public static List<string> DefaultForexChannels = new List<string>()
			{
				PGForexChannels.CurrenciesForex, PGForexChannels.Commodities,
				PGForexChannels.Bonds, PGForexChannels.CFAgg, PGForexChannels.Metals
			};

		#endregion

		#region Properties

		#endregion

		#region Events

		public event OnForexQuoteParamDel OnForexQuoteEvent;
		public event OnForexAggParamDel OnForexAggEvent;
		public event OnForexLastQuoteParamDel OnForexLastQuoteEvent;

		#endregion

		public PGForex( PGonApi PGApi )
			: base( PGApi, PGClusterNames.Forex )
		{
			SymbolType = "Pair";

			InitDefaultChannels( DefaultForexChannels );
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
						case "C":
							ForexQuote quote = JsonConvert.DeserializeObject<ForexQuote>( ObjJSONText );
							if ( quote != null )
							{
								OnForexQuoteEvent?.Invoke( quote );
							}
							break;

						case "CA":
							ForexAggregate forexAgg = JsonConvert.DeserializeObject<ForexAggregate>( ObjJSONText );
							if ( forexAgg != null )
							{
								OnForexAggEvent?.Invoke( forexAgg );
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
			Symbol = FormatForexSymbol( Symbol );
			return base.RequestPreviousClose( Symbol );
		}

		public override DailyOpenClose RequestDailyOpenClose( string Symbol, DateTime? dateTime = null )
		{
			Symbol = FormatForexSymbol( Symbol );
			return base.RequestDailyOpenClose( Symbol );
		}

		public ForexLastQuote RequestForexLastQuote( string Symbol )
		{
			ForexLastQuote LastQuote = null;

			try
			{
				string Url = $@"{PolygonUrl}/v1/last_quote/currencies/{Symbol}";
				Debug.WriteLine( $"Request ForexLastQuote: {Url}" );

				Url = AddApiKey( Url );

				string JSONText = JsonSecureGet( Url );

				LastQuote = JsonConvert.DeserializeObject<ForexLastQuote>( JSONText );
				if ( LastQuote != null )
					OnForexLastQuoteEvent?.Invoke( LastQuote );
			}
			catch ( Exception ex )
			{
				string Message = $"RequestForexLastQuote: error {ex.Message}";
				FireOnExecJsonSecureGetEvent( Message );

				HandleJSONTextException( "RequestForexLastQuote", "", ex );
			}

			return LastQuote;
		}

		#endregion

		private static string FormatForexSymbol( string Symbol )
		{
			return Symbol = $"C:{Symbol.Replace( "/", "" )}";
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
