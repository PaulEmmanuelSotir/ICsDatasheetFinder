using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage.Streams;
using Windows.Storage;
using System.Collections.ObjectModel;
using Caliburn.Micro;
using SQLite;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using Windows.Foundation;
using System.Collections.ObjectModel;

namespace ICsDatasheetFinder_8._1.Data
{
    public sealed class DatasheetDataSource
    {
        // Static datasource instance
        private static DatasheetDataSource _datasheetDataSource = new DatasheetDataSource();

        private SQLiteConnection connection;

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
                if (_datasheetDataSource.connection == null)
                    _datasheetDataSource.connection = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.DATABASE_FILE_NAME), SQLiteOpenFlags.ReadOnly);

                _datasheetDataSource._allManufacturers = (from manu in _datasheetDataSource.connection.Table<Manufacturer>().ToList()
                                                         orderby manu.name
                                                         group manu by manu.name.ToUpper()[0] into g
                                                         select g).ToList();
                SQLiteConnectionPool.Shared.Reset();
                _datasheetDataSource.connection = null;
            }
            return _datasheetDataSource._allManufacturers;
        }

        private List<IGrouping<char, Manufacturer>> _allManufacturers = new List<IGrouping<char, Manufacturer>>();

        /*     public static Manufacturer GetManufacturer(string Id)
             {
                 // Simple linear search is acceptable for small data sets
                 var matches = _datasheetDataSource._allManufacturers.Where((man) => man.Id.Equals(Id));
                 if (matches.Count() == 1) return matches.First();
                 return null;
             }*/

        public static IList<Part> SearchForDatasheet(string queryRef, int MaxRsltCount = -1, ulong startingPos = 0)
        {
            return SearchForDatasheet(queryRef, null, MaxRsltCount, startingPos);
        }

        public static IList<Part> SearchForDatasheet(string queryRef, IList<Manufacturer> manufacturers, int MaxRsltCount = -1, ulong startingPos = 0)
        {
            if (queryRef != string.Empty && queryRef != null)
            {
                if (_datasheetDataSource.connection == null)
                {
                    _datasheetDataSource.connection = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.DATABASE_FILE_NAME), SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.ReadOnly);
                    string query = MaxRsltCount >= 0 ? "select * from Part where reference LIKE '%{0}%' {1} LIMIT {3} OFFSET {2}" : "select * from Part where reference LIKE '%{0}%' {1} LIMIT 10000000 OFFSET {2}";

                    if (manufacturers != null)
                    {
                        if (manufacturers.Count == 0)
                            return new List<Part>();
                        var man1 = manufacturers[0];
                        manufacturers.RemoveAt(0);
                        String Ids = manufacturers.Aggregate<Manufacturer, String>("AND ManufacturerId IN ( '" + man1.Id + "'", (total, next) => total + ", '" + next.Id + "'") + " )";
                        query = string.Format(query, queryRef, Ids, startingPos, MaxRsltCount);
                    }
                    else
                        query = string.Format(query, queryRef, "", startingPos, MaxRsltCount);

                    var rslt = _datasheetDataSource.connection.Query<Part>(string.Format(query, queryRef, MaxRsltCount, startingPos));
                    SQLiteConnectionPool.Shared.Reset();
                    _datasheetDataSource.connection = null;

                    return rslt;
                }
                return new List<Part>();
            }
            return null;
        }

        // TODO : mettre à jour le nombre de composants !!
        public static readonly ulong PART_NUMBER = 581647;
    }
}
