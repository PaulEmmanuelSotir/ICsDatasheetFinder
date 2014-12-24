using ICsDatasheetFinder.Common;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace ICsDatasheetFinder.Views
{
	/// <summary>
	/// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
	/// </summary>
	public sealed partial class DatasheetView : Page
	{
		public DatasheetView()
		{
			this.InitializeComponent();

			_windowHelper = new WindowHelper(this)
			{
				States = new List<WindowState>()
				{
					new WindowState { State = "Vertical", MatchCriterium = (w, h) => h >= w },
					new WindowState { State = "Horizontal", MatchCriterium = (w, h) => h < w}
				}
			};
		}

		private WindowHelper _windowHelper;
	}
}
