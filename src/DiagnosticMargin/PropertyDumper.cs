using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Utilities;
using System.Windows;
using System.Windows.Controls;

namespace DiagnosticMargin
{
    internal static class PropertyDumper
    {
        private static string TypeName(Type type)
        {
            string name = type.Name;
            Type[] formals = type.GetGenericArguments();
            if (formals.Length > 0)
            {
                name += "<";
                for (int t = 0; t < formals.Length; ++t)
                {
                    name += TypeName(formals[t]);
                    if (t < formals.Length - 1)
                    {
                        name += ", ";
                    }
                }
                name += ">";
            }
            return name;
        }

        public static FrameworkElement PropertyDisplay(IPropertyOwner propertyOwner)
        {
            ListBox list = new ListBox();

            foreach (KeyValuePair<Object, Object> pair in propertyOwner.Properties.PropertyList)
            {
                string lhs;
                string rhsType;
                if (pair.Value == null)
                {
                    lhs = rhsType = "?null";
                }
                else
                {
                    Type keyType = pair.Key.GetType();
                    lhs = typeof(Type).IsAssignableFrom(keyType)
                        ? "typeof(" + pair.Key.ToString() + ")"
                        : pair.Key.ToString();
                    rhsType = TypeName(pair.Value.GetType());
                    if (pair.Value is WeakReference)
                    {
                        Object target = (pair.Value as WeakReference).Target;
                        if (target != null)
                        {
                            rhsType = rhsType + "(" + TypeName(target.GetType()) + ")";
                        }
                    }
                }

                list.Items.Add(String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} -> {1}", lhs, rhsType));
            }

            return list;
        }
    }
}
