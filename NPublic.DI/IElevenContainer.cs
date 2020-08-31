using System;
using System.Collections.Generic;
using System.Text;

namespace NPublic.NContainer
{
    public interface IElevenContainer
    {
        void RegisterType<IT, T>();
        IT Resolve<IT>();
    }

    internal class InjectionConstractorAttribute
    {
    }
}
