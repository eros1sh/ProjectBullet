using Microsoft.Extensions.DependencyInjection;
using System;

namespace ProjectBullet.Native
{
    public static class SP
    {
        private static IServiceProvider instance;

        public static void Init(IServiceProvider instance) => SP.instance = instance;

        public static T GetService<T>()
            => instance.GetService<IServiceScopeFactory>().CreateScope().ServiceProvider.GetRequiredService<T>();
    }
}
