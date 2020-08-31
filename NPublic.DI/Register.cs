using System;
using System.Reflection;

namespace NPublic.DI
{
    public partial class ServiceContainer
    {
        /// <summary>
        /// 接口注册
        /// </summary>
        /// <param name="toNameSpace">目标程序集命名空间</param>
        public void Register(string toNameSpace)
        {
            var toAssembly = Assembly.Load(toNameSpace);
            var types = toAssembly.GetTypes();
            Register(types);
        }

        /// <summary>
        /// 接口注册
        /// </summary>
        /// <param name="types">类型数组</param>
        public void Register(params Type[] types)
        {
            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces();
                foreach (var inter in interfaces)
                {
                    if (DicToRegister.ContainsValue(inter.FullName)) continue;
                    DicToRegister.Add(type, inter.FullName);
                }
            }
        }

        /// <summary>
        /// 接口注册
        /// </summary>
        /// <typeparam name="TFrom">来源类型</typeparam>
        /// <typeparam name="TTo">目标类型</typeparam>
        public void Register<TFrom, TTo>()
        {
            Register(typeof(TFrom), typeof(TTo));
        }

        /// <summary>
        /// 接口注册
        /// </summary>
        /// <param name="fromType">来源类型</param>
        /// <param name="toType">目标类型</param>
        public void Register(Type fromType, Type toType)
        {
            DicToRegister.Add(toType, fromType.FullName);
        }
    }
}
