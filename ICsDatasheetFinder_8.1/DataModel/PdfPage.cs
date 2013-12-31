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
		public DatasheetPage(uint pageNumber, Windows.Foundation.Rect dimensions, string imagePath)
		{
			_pageNumber = pageNumber;
			_dimensions = dimensions;
			_pageImage = imagePath;
		}

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

		private Windows.Foundation.Rect _dimensions;
		public double Width
		{
			get
			{
				return _dimensions.Width;
			}
		}
		public double Height
		{
			get
			{
				return _dimensions.Height;
			}
		}
	}
}
