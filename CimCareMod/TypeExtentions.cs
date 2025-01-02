using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CimCareMod
{
    public static class TypeExtensions
    {
        public static IEnumerable<FieldInfo> GetAllFieldsFromType(this Type type)
        {
            if (type == null)
            {
                return Enumerable.Empty<FieldInfo>();
            }
            BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (type.BaseType != null)
            {
                return type.GetFields(bindingAttr).Concat(type.BaseType.GetAllFieldsFromType());
            }
            return type.GetFields(bindingAttr);
        }

        public static FieldInfo GetFieldByName(this Type type, string name)
        {
            return type.GetAllFieldsFromType().Where(p => p.Name == name).FirstOrDefault();
        }
    }
}
