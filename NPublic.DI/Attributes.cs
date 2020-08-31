using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPublic.DI
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class ConstructorInjectAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyInjectAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class FieldInjectAttribute : Attribute { }
}
