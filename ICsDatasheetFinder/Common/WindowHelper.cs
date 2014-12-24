using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ICsDatasheetFinder.Common
{
	// TODO : il faut faire passer qqchose qui hérite de EventArgs en paramètre plutôt qu'un WindowState directement
	public delegate void StateChangedEventHandler(object sender, WindowState state);

	public class WindowHelper
	{
		public WindowHelper(Page page)
		{
			_page = page;
			_page.Loaded += page_Loaded;
			_page.Unloaded += page_Unloaded;
		}

		public event StateChangedEventHandler StateChanged;

		private void page_Loaded(object sender, RoutedEventArgs e)
		{
			Window.Current.SizeChanged += Window_SizeChanged;
			DetermineState(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
		}

		private void page_Unloaded(object sender, RoutedEventArgs e)
		{
			Window.Current.SizeChanged -= Window_SizeChanged;
		}

		private void Window_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
		{
			DetermineState(e.Size.Width, e.Size.Height, true);
		}

		private void DetermineState(double width, double height, bool transitions = false)
		{
			var state = States.First(x => x.MatchCriterium(width, height));
			VisualStateManager.GoToState(_page, state.State, transitions);
			if (state != _currentState)
			{
				_currentState = state;
				StateChanged?.Invoke(this, state);
			}
		}

		public IEnumerable<WindowState> States { get; set; }

		private WindowState _currentState;
		private Page _page;
	}

	public class WindowState
	{
		public string State { get; set; }

		public Func<double, double, bool> MatchCriterium { get; set; }
	}
}
