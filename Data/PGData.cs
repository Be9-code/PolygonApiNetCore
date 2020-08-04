using System;
using System.Collections.Generic;
using System.Text;

namespace PolygonApi.Data
{
	// https://polygon.io/sockets

	public class PolygonBase
	{
		public string ev { get; set; }
	}

	public class PrevCloseResult
	{
		public string T { get; set; }
		public double v { get; set; }
		public double vw { get; set; }
		public double o { get; set; }
		public double c { get; set; }
		public double h { get; set; }
		public double l { get; set; }
		public long t { get; set; }
		public int n { get; set; }
	}

	public class PreviousClose
	{
		public string ticker { get; set; }
		public string status { get; set; }
		public bool adjusted { get; set; }
		public int queryCount { get; set; }
		public int resultsCount { get; set; }
		public IList<PrevCloseResult> results { get; set; }
	}

	public class Open
	{
		public int condition1 { get; set; }
		public int condition2 { get; set; }
		public int condition3 { get; set; }
		public int condition4 { get; set; }
		public int exchange { get; set; }
		public double price { get; set; }
		public int size { get; set; }
		public DateTime timestamp { get; set; }
	}

	public class Close
	{
		public int condition1 { get; set; }
		public int condition2 { get; set; }
		public int condition3 { get; set; }
		public int condition4 { get; set; }
		public int exchange { get; set; }
		public double price { get; set; }
		public int size { get; set; }
		public DateTime timestamp { get; set; }
	}

	public class DailyOpenClose
	{
		public string symbol { get; set; }
		public string from { get; set; }
		public DateTime date { get { return DateTime.Parse( from ); } }
		public double open { get; set; }
		public double close { get; set; }
		public double high { get; set; }
		public double low { get; set; }
		public long volume { get; set; }
		public double afterHours { get; set; }
		public double preMarket { get; set; }
	}

	#region Condition Mappings

	//https://polygon.io/docs/#get_v1_meta_conditions__ticktype__anchor
	//https://api.polygon.io/v1/meta/conditions/trades

	//{
	//  "0": "Regular",
	//  "1": "Acquisition",
	//  "2": "AveragePrice",
	//  "3": "AutomaticExecution",
	//  "4": "Bunched",
	//  "5": "BunchSold",
	//  "6": "CAPElection",
	//  "7": "CashTrade",
	//  "8": "Closing",
	//  "9": "Cross",
	//  "10": "DerivativelyPriced",
	//  "11": "Distribution",
	//  "12": "FormT(ExtendedHours)",
	//  "13": "FormTOutOfSequence",
	//  "14": "InterMarketSweep",
	//  "15": "MarketCenterOfficialClose",
	//  "16": "MarketCenterOfficialOpen",
	//  "17": "MarketCenterOpening",
	//  "18": "MarketCenterReOpenning",
	//  "19": "MarketCenterClosing",
	//  "20": "NextDay",
	//  "21": "PriceVariation",
	//  "22": "PriorReferencePrice",
	//  "23": "Rule155Amex",
	//  "24": "Rule127Nyse",
	//  "25": "Opening",
	//  "26": "Opened",
	//  "27": "RegularStoppedStock",
	//  "28": "ReOpening",
	//  "29": "Seller",
	//  "30": "SoldLast",
	//  "31": "SoldLastStoppedStock",
	//  "32": "SoldOutOfSequence",
	//  "33": "SoldOutOfSequenceStoppedStock",
	//  "34": "Split",
	//  "35": "StockOption",
	//  "36": "YellowFlag",
	//  "37": "OddLot",
	//  "38": "CorrectedConsolidatedClosePrice",
	//  "39": "Unknown",
	//  "40": "Held",
	//  "41": "TradeThruExempt",
	//  "42": "NonEligible",
	//  "43": "NonEligible-extended",
	//  "44": "Cancelled",
	//  "45": "Recovery",
	//  "46": "Correction",
	//  "47": "AsOf",
	//  "48": "AsOfCorrection",
	//  "49": "AsOfCancel",
	//  "50": "OOB",
	//  "51": "Summary",
	//  "52": "Contingent",
	//  "53": "Contingent(Qualified)",
	//  "54": "Errored"
	//}

	#endregion
}
