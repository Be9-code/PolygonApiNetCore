using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace UtilityFunctions
{
	public delegate void OnOneShotTimerDel( object Data );

	// one-shot timer
	public class OneShotTimer : GenericTimer
	{
		#region Variables

		public event OnOneShotTimerDel OnOneShotTimeoutEvent = null;

		#endregion

		public OneShotTimer( int SecsOrMSecs, OnOneShotTimerDel OnOneShotTimerTickHandler,
								object Data = null, string Name = "", bool Start = true )
		{
			Tag = Name;
			InitGenericTimer( SecsOrMSecs, OnOneShotTimeout, Data, Start );

			OnOneShotTimeoutEvent += OnOneShotTimerTickHandler;
		}

		public void OnOneShotTimeout( object Sender, EventArgs EventArgs )
		{
			( Sender as GenericTimer ).StopGenericTimer();
			OnGenericTimerTickEvent -= OnOneShotTimeout;

			OnOneShotTimeoutEvent?.Invoke( Data );
		}

	}

	public class GenericTimer : DispatcherTimer
	{
		public delegate void OnGenericTimerTickDel( object Sender, EventArgs EventArgs );

		#region Variables

		public TimeSpan TimeSpanRef;
		public object Data;
		public int MaxSecondsLevel = 30;

		#endregion

		#region Events

		public event OnGenericTimerTickDel OnGenericTimerTickEvent;

		#endregion

		public GenericTimer( int SecsOrMSecs, OnGenericTimerTickDel OnGenericTimerTickHandler,
								object Data = null, string Name = "", bool Start = true )
		{
			Tag = Name;
			InitGenericTimer( SecsOrMSecs, OnGenericTimerTickHandler, Data, Start );
		}

		public GenericTimer( TimeSpan TimeSpanRef, OnGenericTimerTickDel OnGenericTimerTickHandler, object Data = null, bool Start = true )
		{
			this.Data = Data;

			OnGenericTimerTickEvent += OnGenericTimerTickHandler;

			Interval = TimeSpanRef;

			if ( Start )
				StartGenericTimer();
		}

		public GenericTimer()
		{
		}

		public void InitGenericTimer( int SecsOrMSecs, OnGenericTimerTickDel OnGenericTimerTickHandler, 
										object Data = null, bool Start = true, bool IsSecs = false )
		{
			this.Data = Data;

			OnGenericTimerTickEvent += OnGenericTimerTickHandler;

			InitInterval( SecsOrMSecs, IsSecs );

			if ( Start )
				StartGenericTimer();
		}

		public void InitInterval( int SecsOrMSecs, bool IsSecs = false )
		{
			// auto-detect seconds/msecs specification using MaxSecondsLevel,
			// a reasonable level to be considered a specification in seconds
			if ( IsSecs || SecsOrMSecs <= MaxSecondsLevel )
				Interval = TimeSpan.FromSeconds( SecsOrMSecs );
			else
				Interval = TimeSpan.FromMilliseconds( SecsOrMSecs );
		}

		public void StartGenericTimer( int SecsOrMSecs )
		{
			InitInterval( SecsOrMSecs );
			StartGenericTimer();
		}

		public virtual void StartGenericTimer()
		{
			Tick += OnTimerTick;
			Start();
		}

		public virtual void StopGenericTimer()
		{
			Stop();
			Tick -= OnTimerTick;
			IsEnabled = false;
		}

		public void PauseGenericTimer( bool Pause )
		{
			if ( Pause )
			{
				Stop();
				IsEnabled = false;
			}
			else
			{
				Start();
				IsEnabled = true;
			}
		}

		public virtual void OnTimerTick( object Sender, EventArgs EventArgs )
		{
			OnGenericTimerTickEvent?.Invoke( Sender, EventArgs );
		}

	}
}
