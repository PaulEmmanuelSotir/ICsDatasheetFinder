using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;

namespace ICsDatasheetFinder_8._1.Data
{
    public class DatasheetPage
    {
        public DatasheetPage(uint pageNumber, string imagePath)
        {
            _pageNumber = pageNumber;
            _pageImage = imagePath;
        }
        //private CoreDispatcher dispatcher;
        //public async Task SetPageImageSourceAsync(Windows.Storage.Streams.IRandomAccessStream streamSource)
        //{
        //    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        //        {
        //            _pageImage = new BitmapImage();
        //            await _pageImage.SetSourceAsync(streamSource);
        //        });
        //}

        private string _pageImage;
        public String PageImage
        {
            get
            {
                return _pageImage;
            }
        }

        private uint _pageNumber;
        public uint PageNumber
        {
            get
            {
                return _pageNumber;
            }
        }
    }
}
