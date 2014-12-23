using SQLite;
using System;
using Windows.UI.Xaml.Media.Imaging;

namespace ICsDatasheetFinder_8._1.Data
{
	public class Manufacturer : Common.BindableBase
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }

		[Unique]
		public string name { get; set; }

		[Ignore]
		public BitmapImage Logo
		{
			get { return _logo; }
			set { SetProperty(ref _logo, value); }
		}

		public string LogoFileName => "\{Uri.EscapeDataString(name)}.jpg";

		private BitmapImage _logo;
	}
}