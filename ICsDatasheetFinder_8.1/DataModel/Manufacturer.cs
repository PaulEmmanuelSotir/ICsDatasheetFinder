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

        private BitmapImage _Logo;
        [Ignore]
        public BitmapImage Logo
        {
            get { return _Logo; }
            set { SetProperty(ref _Logo, value); }
        }

        public string LogoFileName
        {
            get
            {
                return string.Format("{0}.jpg", Uri.EscapeDataString(name));
            }
        }
    }
}