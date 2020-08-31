using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPublic.NContainer
{

    //创建对象是使用无参构造器
    public class ElevenContainer : IElevenContainer
    {
        private Dictionary<string, Type> typeContainer = new Dictionary<string, Type>();
        public void RegisterType<IT, T>()
        {
            if (!typeContainer.ContainsKey(typeof(IT).FullName))
            {
                typeContainer.Add(typeof(IT).FullName, typeof(T));
            }
        }

        public IT Resolve<IT>()
        {
            string key = typeof(IT).FullName;
            if (!typeContainer.ContainsKey(typeof(IT).FullName))
            {
                throw new Exception("没有为{key}的初始化");
            }
            Type type = typeContainer[key];
            //CreateInstance默认调用无参构造器,想要创建的对象的构造器如果是无参的，可以调用该方法
            return (IT)Activator.CreateInstance(type);

        }

    }

    //创建对象使用有属性标记的构造器，构造器依赖抽象类，抽象类构造器是无参构造器

    public class ElevenContainer2 : IElevenContainer
    {
        private Dictionary<string, Type> typeContainer = new Dictionary<string, Type>();
        public void RegisterType<IT, T>()
        {
            if (!typeContainer.ContainsKey(typeof(IT).FullName))
                typeContainer.Add(typeof(IT).FullName, typeof(T));
        }

        public IT Resolve<IT>()
        {
            string key = typeof(IT).FullName;
            if (!typeContainer.ContainsKey(typeof(IT).FullName))
            {
                throw new Exception("没有为{key}的初始化");
            }
            Type type = typeContainer[key];
            var ctorArray = type.GetConstructors();
            ConstructorInfo ctor = null;
            if (ctorArray.Count(c => c.IsDefined(typeof(InjectionConstractorAttribute), true)) > 0)
            {
                //获取有标记属性InjectionConstractorAttribute的构造函数
                ctor = ctorArray.FirstOrDefault(c => c.IsDefined(typeof(InjectionConstractorAttribute), true));
            }
            else
            {
                //获取构造器参数最多的构造函数
                ctor = ctorArray.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            }

            var paraList = ctor.GetParameters();
            Object[] paramArray = new object[paraList.Length];
            int i = 0;
            foreach (var param in paraList)
            {
                //方法一：构造函数依赖的参数对象的构造函数是无参的
                //获取参数接口类型(代码是上层依赖下层的抽象)
                Type interfaceType = param.ParameterType;
                if (typeContainer.ContainsKey(interfaceType.FullName))
                {
                    //获取参数
                    Type paramType = this.typeContainer[interfaceType.FullName];
                    //创建参数对象
                    Object objectParam = Activator.CreateInstance(paramType);
                    paramArray[i] = objectParam;
                    i++;
                }
            }
            return (IT)Activator.CreateInstance(type, paramArray);

        }



        //创建对象使用有属性标记的构造器，构造器依赖抽象类，抽象类构造器是构造器依赖另外的抽象类，使用递归

        public class ElevenContainer3 : IElevenContainer
        {
            private Dictionary<string, Type> typeContainer = new Dictionary<string, Type>();
            public void RegisterType<IT, T>()
            {
                if (!typeContainer.ContainsKey(typeof(IT).FullName))
                    typeContainer.Add(typeof(IT).FullName, typeof(T));
            }

            public IT Resolve<IT>()
            {
                string key = typeof(IT).FullName;
                if (!typeContainer.ContainsKey(typeof(IT).FullName))
                {
                    throw new Exception("没有为{key}的初始化");
                }
                Type type = typeContainer[key];
                return (IT)Resolve(type);

            }

            public object Resolve(Type type)
            {
                var ctorArray = type.GetConstructors();
                ConstructorInfo ctor = null;
                if (ctorArray.Count(c => c.IsDefined(typeof(InjectionConstractorAttribute), true)) > 0)
                {
                    //获取有标记属性InjectionConstractorAttribute的构造函数
                    ctor = ctorArray.FirstOrDefault(c => c.IsDefined(typeof(InjectionConstractorAttribute), true));
                }
                else
                {
                    //获取构造器参数最多的构造函数
                    ctor = ctorArray.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
                }

                var paraList = ctor.GetParameters();
                Object[] paramArray = new object[paraList.Length];
                int i = 0;
                foreach (var param in paraList)
                {
                    Type interfaceType = param.ParameterType;
                    if (typeContainer.ContainsKey(interfaceType.FullName))
                    {
                        Type paramType = this.typeContainer[interfaceType.FullName];
                        var ParaObject = Resolve(paramType);
                        paramArray[i] = ParaObject;
                        i++;
                    }
                }
                return Activator.CreateInstance(type, paramArray);

            }



            private class InjectionConstractorAttribute
            {
            }
        }
    }
}