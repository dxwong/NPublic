using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPublic.DI
{
    public partial class ServiceContainer
    {
        public static Dictionary<Type, string> DicToRegister = null;
        private static Dictionary<List<Type>, string> DicReturnTypeInfo = null;
        private static object objLock = null;
        private static ServiceContainer container = null;

        static ServiceContainer()
        {
            container = new ServiceContainer();
            DicToRegister = new Dictionary<Type, string>();
            DicReturnTypeInfo = new Dictionary<List<Type>, string>();
            objLock = new object();
        }

        public ServiceContainer GetContainer()
        {
            return container;
        }

        public static bool IsRegister(Type type)
        {
            if (DicReturnTypeInfo.ContainsValue(type.FullName))
            {
                return true;
            }
            return false;
        }

        private T CreateInstance<T>(Type type)
        {
            List<Type> typesOfParameter = new List<Type>();
            if (DicReturnTypeInfo.ContainsValue(type.FullName))
            {
                //如果有类型数据就不需要再获取一次了
                //typesOfParameter = dicReturnTypeInfo[type.FullName];
                typesOfParameter = DicReturnTypeInfo.FirstOrDefault(q => q.Value == type.FullName).Key;
            }
            else
            {
                lock (objLock)
                {
                    if (!DicReturnTypeInfo.ContainsValue(type.FullName))
                    {
                        //构造函数注入
                        ConstructorInfo constructor = null;
                        var ConstructorsInfo = type.GetConstructors();
                        if (ConstructorsInfo.Count() > 0)
                        {
                            var dicCountParameters = new Dictionary<int, ParameterInfo[]>();
                            foreach (var Constructor in ConstructorsInfo)
                            {
                                var tempParameters = Constructor.GetParameters();
                                dicCountParameters.Add(tempParameters.Count(), tempParameters);
                                if (Constructor.GetCustomAttribute(typeof(ConstructorInjectAttribute)) != null)
                                {
                                    //TODO  将取出来的属性保存下来，下次用到就不用遍历了
                                    constructor = Constructor;
                                    break;
                                }
                            }
                            //如果没有指定特性，则默认取参数最多的一个
                            var parameters = constructor == null ? dicCountParameters.OrderByDescending(c => c.Key).FirstOrDefault().Value : constructor.GetParameters();

                            foreach (var item in parameters)
                            {
                                Type fromType = item.ParameterType;
                                typesOfParameter.Add(fromType);
                            }
                            DicReturnTypeInfo.Add(typesOfParameter, type.FullName);
                        }
                    }
                }
            }
            List<object> param = new List<object>();
            foreach (var pType in typesOfParameter)
            {
                if (DicToRegister.ContainsValue(pType.FullName))
                    param.Add(Resolve<object>(DicToRegister.FirstOrDefault(q => q.Value == type.FullName).Key));
                //param.Add(GetInstance<object>(dicToInstances[pType.FullName]));
                else
                    throw new Exception($"指定类型未注册:{pType.FullName}");
            }
            T t = default(T);
            if (param.Count > 0)
                t = (T)Activator.CreateInstance(type, param.ToArray());
            else
                t = (T)Activator.CreateInstance(type);
            //属性注入
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                //TODO  将取出来的属性保存下来，下次用到就不用遍历了
                var attribute = property.GetCustomAttribute(typeof(PropertyInjectAttribute));
                if (attribute != null)
                    property.SetValue(t, Resolve<object>(property.PropertyType));
            }
            //字段注入
            var filds = type.GetFields();
            foreach (var fild in filds)
            {
                //TODO  将取出来的属性保存下来，下次用到就不用遍历了
                var attribute = fild.GetCustomAttribute(typeof(FieldInjectAttribute));
                if (attribute != null)
                    fild.SetValue(t, Resolve<object>(fild.FieldType));
            }
            return t;
        }
    }
}
