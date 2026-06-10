using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace $safeprojectname$.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;

            // Enumの現在の値（Free, Trial, Pro, Large, Smallなど）を文字列化
            string currentEnumString = value.ToString() ?? "";
            // 比較対象の文字列
            string targetTarget = parameter.ToString() ?? "";

            bool isInverse = false;

            // パラメーターが "!" で始まる場合は反転モードにする
            if (targetTarget.StartsWith("!"))
            {
                isInverse = true;
                targetTarget = targetTarget.Substring(1);
            }

            // 大文字小文字を区別せずに比較
            bool isMatch = currentEnumString.Equals(targetTarget, StringComparison.OrdinalIgnoreCase);

            // 反転モードなら結果をひっくり返す
            if (isInverse) isMatch = !isMatch;

            return isMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}