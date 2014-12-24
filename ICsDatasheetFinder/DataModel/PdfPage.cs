using System;

namespace ICsDatasheetFinder.Data
{
	public class DatasheetPage
	{
		public DatasheetPage(uint pageNumber, Windows.Foundation.Rect dimensions, string imagePath)
		{
			PageNumber = pageNumber;
			PageImage = imagePath;
			Width = dimensions.Width;
			Height = dimensions.Height;
		}

		public String PageImage { get; }

		public uint PageNumber { get; }

		public double Width { get; }

		public double Height { get; }
	}
}
