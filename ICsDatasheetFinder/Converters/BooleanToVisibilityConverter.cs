﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ICsDatasheetFinder.Converters
{
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			// si le paramètre est à true on inverse le résultat
			if (parameter != null)
				if ((parameter as string).ToLower() == "true")
					return (!(bool)value) ? Visibility.Visible : Visibility.Collapsed;
			return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			// si le paramètre est à true on inverse le résultat
			if (parameter != null)
				if ((parameter as string).ToLower() == "true")
					return (((Visibility)value) != Visibility.Visible);
			return ((Visibility)value) == Visibility.Visible;
		}
	}
}