using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace ICsDatasheetFinder.Data
{
	public sealed class DatasheetDataSource
	{
		public static async Task LoadManufacturersImagesAsync()
		{
			if (_datasheetDataSource._allManufacturers.Count == 0)
			{
				await Task.Factory.StartNew(() =>
				{
					GetManufacturers();
				});
			}

			var dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
			await Task.Run(() =>
			{
				foreach (IGrouping<char, Manufacturer> g in _datasheetDataSource._allManufacturers)
				{
					foreach (Manufacturer manu in g)
					{
						// Load asyncronously bitmaps
						// TODO : store task, check exceptions...
						dispatcher.RunAsync(CoreDispatcherPriority.Low, new DispatchedHandler(() =>
						{
							manu.Logo = new BitmapImage();

							manu.Logo.ImageFailed += new Windows.UI.Xaml.ExceptionRoutedEventHandler((sender, args) =>
								{
									// If manufacturer's logo isn't available, we load a default logo
									manu.Logo = new BitmapImage(new Uri("ms-appx:///Data/ManufacturersImages/default.jpg"));
								});
							// Load manufacturer's logo otherwise (we use absolute path, instead of 'ms-appx:///' because we dont want LogoFileName to be parsed due to 'Uri.EscapeDataString(name)' function)
							manu.Logo.UriSource = new Uri(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, @"Data\ManufacturersImages\", manu.LogoFileName));
						}));
					}
				}
			});
		}

		public static List<IGrouping<char, Manufacturer>> GetManufacturers()
		{
			if (_datasheetDataSource._allManufacturers.Count == 0)
			{
				using (var connection = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.DATABASE_FILE_NAME), SQLiteOpenFlags.ReadOnly))
				{
					_datasheetDataSource._allManufacturers = (from manu in connection.Table<Manufacturer>().ToList()
															  orderby manu.name
															  group manu by manu.name.ToUpper()[0] into g
															  select g).ToList();
				}
			}
			return _datasheetDataSource._allManufacturers;
		}

		public static IList<Part> SearchForDatasheet(string queryRef, CancellationToken CancelToken, uint? MaxRsltCount = null)
		{
			return SearchForDatasheet(queryRef, CancelToken, null, MaxRsltCount);
		}

		public static IList<Part> SearchForDatasheet(string queryRef, CancellationToken CancelToken, IList<Manufacturer> manufacturers, uint? MaxRsltCount = null)
		{
			if (queryRef != null)
			{
				queryRef = queryRef.Replace("\'", string.Empty);

				if (queryRef != string.Empty)
				{
					// If there is a non null empty manufacturers list, then we return an empty result
					if (manufacturers?.Count == 0)
						return new List<Part>();

					var manusQuery = manufacturers != null ? "AND ManufacturerId IN ( '" + String.Join("', '", manufacturers.Select((Manu) => Manu.Id)) + "' )" : string.Empty;
					var query = MaxRsltCount != null ? $"select * from Part where reference LIKE '%{queryRef}%' {manusQuery} LIMIT {MaxRsltCount}" : $"select * from Part where reference LIKE '%{queryRef}%' {manusQuery}";

					using (var connection = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.DATABASE_FILE_NAME), SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.ReadOnly))
					{
						CancelToken.ThrowIfCancellationRequested();
						var rslt = connection.Query<Part>(query);
						CancelToken.ThrowIfCancellationRequested();

						return rslt;
					}
				}
			}
			return null;
		}

		private static DatasheetDataSource _datasheetDataSource = new DatasheetDataSource();

		private List<IGrouping<char, Manufacturer>> _allManufacturers = new List<IGrouping<char, Manufacturer>>();
	}
}
