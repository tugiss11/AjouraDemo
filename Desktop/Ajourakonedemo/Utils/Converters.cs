using System;
using System.Collections.Generic;
using System.ComponentModel;

using Catel.MVVM.Converters;

using FieldInfo = System.Reflection.FieldInfo;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils
{
 

        public class EnumIdAndDescriptionSourceConverter : IValueConverter
        {
            private readonly Dictionary<Type, List<string>> _localLists =
              new Dictionary<Type, List<string>>();

            public object Convert(object value, Type targetType, object parameter,
              System.Globalization.CultureInfo culture)
            {
                if (value == null || !value.GetType().IsEnum)
                    return value;
                if (!_localLists.ContainsKey(value.GetType()))
                    CreateList(value.GetType());
                return _localLists[value.GetType()];
            }

            public object ConvertBack(object value, Type targetType, object parameter,
              System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException("There is no backward conversion");
            }

            private void CreateList(Type e)
            {
                var list = new List<string>();

                foreach (var value in Enum.GetValues(e))
                {
                    System.Reflection.FieldInfo info = value.GetType().GetField(value.ToString());
                    var valueDescription = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute), false);

                    string s = string.Format("{0} {1}", System.Convert.ToInt32(value), valueDescription.Length == 1 ? valueDescription[0].Description : value.ToString());
                    list.Add(s);
                }

                _localLists.Add(e, list);
            }
        }


    public class EnumIdAndDescriptionConverter : BaseEnumDescriptionConverter
    {
        protected override void CreateDictionaries(Type e)
        {
            var dictionaries = new LocalDictionaries();

            foreach (var value in Enum.GetValues(e))
            {
                FieldInfo info = value.GetType().GetField(value.ToString());
                var valueDescription = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (valueDescription.Length == 1)
                {
                    string s = string.Format("{0} {1}", (int)value, valueDescription[0].Description);

                    dictionaries.EnumDescriptions.Add((int)value, s);
                    dictionaries.EnumIntValues.Add(s, (int)value);
                }
                else //Use the value for display if not concrete result
                {
                    dictionaries.EnumDescriptions.Add((int)value, value.ToString());
                    dictionaries.EnumIntValues.Add(value.ToString(), (int)value);
                }
            }
            _localDictionaries.Add(e, dictionaries);
        }
    }

    public abstract class BaseEnumDescriptionConverter : IValueConverter
    {

      

        protected class LocalDictionaries
        {
            public readonly Dictionary<int, string> EnumDescriptions = new Dictionary<int, string>();
            public readonly Dictionary<string, int> EnumIntValues = new Dictionary<string, int>();
        }
        protected readonly Dictionary<Type, LocalDictionaries> _localDictionaries = new Dictionary<Type, LocalDictionaries>();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !value.GetType().IsEnum)
                return value;

         

            try
            {

                if (!_localDictionaries.ContainsKey(value.GetType()))
                {
                    CreateDictionaries(value.GetType());
                }

                var dict = _localDictionaries[value.GetType()];

                try
                {
                    var enumDesc = dict.EnumDescriptions[(int)value];
                    return enumDesc;
                }
                catch (KeyNotFoundException)
                {
                    return null;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("value={0}, type={1}, err={2}", value, value.GetType(), err.Message);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !targetType.IsEnum)
                return value;

            if (!_localDictionaries.ContainsKey(targetType))
                CreateDictionaries(targetType);

            int enumInt = _localDictionaries[targetType].EnumIntValues[value.ToString()];
            return Enum.ToObject(targetType, enumInt);
        }

        protected abstract void CreateDictionaries(Type e);
    }
}



