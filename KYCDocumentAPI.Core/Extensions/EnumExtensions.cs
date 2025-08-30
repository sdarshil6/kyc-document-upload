using System.ComponentModel;
using System.Reflection;

namespace KYCDocumentAPI.Core.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        public static bool TryParseWithSpaces<TEnum>(string input, out TEnum result) where TEnum : struct
        {
            var normalized = input.Replace(" ", "");
            return Enum.TryParse(normalized, ignoreCase: true, out result);
        }
    }
}
