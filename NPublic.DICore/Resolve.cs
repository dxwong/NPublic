using System;
using System.Linq;

namespace NPublic.DI
{
    public partial class ServiceContainer
    {
        /// <summary>
        /// 获取实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            return Resolve<T>(typeof(T));
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public T Resolve<T>(Type type)
        {
            if (type.IsInterface)
            {
                if (DicToRegister.ContainsValue(type.FullName))
                    return Resolve<T>(DicToRegister.FirstOrDefault(q => q.Value == type.FullName).Key);
                else
                    return default(T);
            }
            else
            {
                return CreateInstance<T>(type);
            }
        }
    }
}
