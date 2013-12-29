using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage.Streams;
using Windows.Storage;
using System.Collections.ObjectModel;
using Caliburn.Micro;
using SQLite;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using Windows.Foundation;

namespace ICsDatasheetFinder_8._1.Data
{
    public sealed class DatasheetDataSource
    {
        private static DatasheetDataSource _datasheetDataSource = new DatasheetDataSource();

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
                            // TODO dans windows 8.1 utiliser StorageFile.IsAvailable
                            manu.Logo = new BitmapImage();

                            manu.Logo.ImageFailed += new Windows.UI.Xaml.ExceptionRoutedEventHandler((sender, args) =>
                                {
                                    // If manufacturer's logo isn't available, we load a default logo
                                    manu.Logo = new BitmapImage(new Uri("ms-appx:///Data/ManufacturersImages/default.jpg"));
                                });
                            // Load manufacturer's logo otherwise (we use absolute path, instead of 'ms-appx:///' because we dont want LogoFileName to be parsed due to 'Uri.EscapeDataString(name)' function)
                            string path = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, @"Data\ManufacturersImages\", manu.LogoFileName);
                            manu.Logo.UriSource = new Uri(path);
                        }));
                    }
                }
            });
        }

        public static List<IGrouping<char, Manufacturer>> GetManufacturers()
        {
            if (_datasheetDataSource._allManufacturers.Count == 0)
            {
                SQLiteConnection connection = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.DATABASE_FILE_NAME), SQLiteOpenFlags.ReadOnly);
                _datasheetDataSource._allManufacturers = (from manu in connection.Table<Manufacturer>().ToList()
                                                          orderby manu.name
                                                          group manu by manu.name.ToUpper()[0] into g
                                                          select g).ToList();
                connection.Close();
            }
            return _datasheetDataSource._allManufacturers;
        }

        private List<IGrouping<char, Manufacturer>> _allManufacturers = new List<IGrouping<char, Manufacturer>>();

        public static IList<Part> SearchForDatasheet(string queryRef, CancellationToken CancelToken, int MaxRsltCount = -1)
        {
            return SearchForDatasheet(queryRef, CancelToken, null, MaxRsltCount);
        }

        public static IList<Part> SearchForDatasheet(string queryRef, CancellationToken CancelToken, IList<Manufacturer> manufacturers, int MaxRsltCount = -1)
        {
            if (queryRef != string.Empty && queryRef != null)
            {
                string query = string.Empty;
                SQLiteConnection connection = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.DATABASE_FILE_NAME), SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.ReadOnly);

                query = MaxRsltCount >= 0 ? "select * from Part where reference LIKE '%{0}%' {1} LIMIT {2}" : "select * from Part where reference LIKE '%{0}%' {1}";

                if (CancelToken.IsCancellationRequested)
                {
                    connection.Close();
                    CancelToken.ThrowIfCancellationRequested();
                }

                if (manufacturers != null)
                {
                    if (manufacturers.Count == 0)
                        return new List<Part>();
                    String Ids = "AND ManufacturerId IN ( '" + String.Join("', '", manufacturers.Select((Manu) => Manu.Id)) + "' )";
                    query = string.Format(query, queryRef, Ids, MaxRsltCount);
                }
                else
                    query = string.Format(query, queryRef, "", MaxRsltCount);

                if (CancelToken.IsCancellationRequested)
                {
                    connection.Close();
                    CancelToken.ThrowIfCancellationRequested();
                }

                var rslt = connection.Query<Part>(string.Format(query, queryRef, MaxRsltCount));
                connection.Close();

                CancelToken.ThrowIfCancellationRequested();

                return rslt;
            }
            return null;
        }
    }
}
