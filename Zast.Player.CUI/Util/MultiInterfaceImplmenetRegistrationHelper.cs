using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Util
{
    public static class MultiInterfaceImplmenetRegistrationHelper
    {
        public static IServiceCollection AddAllSingleton<T, T1>(this IServiceCollection descriptors)
            where T : class, T1
            where T1 : class
        {
            return descriptors.AddSingleton<T>()
                .AddSingleton<T1>(p => p.GetRequiredService<T>());
        }

        public static IServiceCollection AddAllSingleton<T, T1, T2>(this IServiceCollection descriptors)
            where T : class, T1, T2
            where T1 : class
            where T2 : class
        {
            return descriptors.AddSingleton<T>()
                .AddSingleton<T1>(p => p.GetRequiredService<T>())
                .AddSingleton<T2>(p => p.GetRequiredService<T>());
        }

        public static IServiceCollection AddAllSingleton<T, T1, T2, T3>(this IServiceCollection descriptors)
            where T : class, T1, T2, T3
            where T1 : class
            where T2 : class
            where T3 : class
        {
            return descriptors.AddSingleton<T>()
                .AddSingleton<T1>(p => p.GetRequiredService<T>())
                .AddSingleton<T2>(p => p.GetRequiredService<T>())
                .AddSingleton<T3>(p => p.GetRequiredService<T>());
        }
    }
}
