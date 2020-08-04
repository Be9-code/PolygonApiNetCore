using PolygonApi.Clusters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace PolygonApi
{
	public class PGonApi : PGBase
	{
		#region Variables

		public bool ApiTerminated;

		#endregion

		#region Properties

		public PGEquities Equities
		{
			get { return _Equities = _Equities ?? new PGEquities( this ); }
			set { _Equities = value; }
		}
		PGEquities _Equities = null;

		public PGForex Forex
		{
			get { return _Forex = _Forex ?? new PGForex( this ); }
			set { _Forex = value; }
		}
		PGForex _Forex = null;

		public PGCrypto Crypto
		{
			get { return _Crypto = _Crypto ?? new PGCrypto( this ); }
			set { _Crypto = value; }
		}
		PGCrypto _Crypto = null;

		public List<PGClusterBase> PGClusters
		{
			get { return _PGClusters = _PGClusters ?? new List<PGClusterBase>(); }
			set { _PGClusters = value; }
		}
		List<PGClusterBase> _PGClusters = null;

		#endregion

		#region Events

		#endregion

		public PGonApi()
		{
		}

		#region InitEvents/UnInitEvents

		public void InitEvents()
		{
			UnInitEvents();

		}

		public void UnInitEvents()
		{
		}

		#endregion

		public void AddToPGClusters( PGClusterBase PGCluster )
		{
			if ( !PGClusters.Contains( PGCluster ) )
				PGClusters.Add( PGCluster );
		}

		public PGClusterBase GetPGCluster( string ClusterName )
		{
			PGClusterBase PGCluster = PGClusters.Find( x => x.ClusterName == ClusterName );
			return PGCluster;
		}

		public void InitPGClusters()
		{
			foreach ( var Cluster in PGClusters )
				Cluster.InitPGCluster();
		}

		public void StartPGCluster( string ClusterName )
		{
			PGClusterBase PGCluster = GetPGCluster( ClusterName );
			if ( PGCluster != null )
				PGCluster.Start();
		}

		public void OnClusterTextInfo( string Text )
		{
			// PGApi is a single source for Cluster TextInfo
			// Note: hooked up in PGClusterBase.InitEvents()
			FireOnTextInfoEvent( Text );
		}

		public PGClusterBase GetCluster( string ClusterName )
		{
			PGClusterBase Cluster = null;
			switch ( ClusterName )
			{
				case PGClusterNames.Equities:
					Cluster = Equities;
					break;
				case PGClusterNames.Forex:
					Cluster = Forex;
					break;
				case PGClusterNames.Crypto:
					Cluster = Crypto;
					break;
				default:
					throw new Exception( $"Unknown ClusterName: {ClusterName}" );
			}

			return Cluster;
		}

		public void UnSubscribeAllChannels()
		{
			foreach ( var Cluster in PGClusters )
				Cluster.UnSubscribeAll();
		}

		public void TerminateApiInterface()
		{
			if ( ApiTerminated )
				return;

			ApiTerminated = true;

			FireOnTextInfoEvent( $"Api Terminated" );

			UnSubscribeAllChannels();

			foreach ( var Cluster in PGClusters )
				Cluster.OnPGApiTerminated();

			PGClusters.Clear();
		}

	}

}
