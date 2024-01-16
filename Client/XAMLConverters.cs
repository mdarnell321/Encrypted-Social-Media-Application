using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ESMA
{

	public class NullVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
		{
			return value == null ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BoolToVisible : IValueConverter
	{
		public object Convert(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
		{
			return (bool)value == true ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
    public class EmptyToBool : IValueConverter
    {
        public object Convert(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
        {
            return (string)value == "" ? false : true;
        }

        public object ConvertBack(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToVisibleOpposite : IValueConverter
    {
        public object Convert(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
        {
            return (bool)value == false ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ChannelTypeToVisible : IValueConverter
	{
		public object Convert(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
		{
			return (int)value == 1 ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
	public class BlankToVisible : IValueConverter
	{
		public object Convert(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
		{
			return (string)value == "" ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
    public class BlankToInvisible : IValueConverter
    {
        public object Convert(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
        {
            return (string)value != "" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targettype, object param, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
