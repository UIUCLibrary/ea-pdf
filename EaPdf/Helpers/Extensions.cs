using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    static class Extensions
    {

        /// <summary>
        /// https://stackoverflow.com/questions/1415140/can-my-enums-have-friendly-names
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string? GetDescriptionLower(this Enum value)
        {
            Type type = value.GetType();
            string? name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo? field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute? attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description.ToLowerInvariant();
                    }
                }
            }
            return name?.ToLowerInvariant();
        }
    }
}
