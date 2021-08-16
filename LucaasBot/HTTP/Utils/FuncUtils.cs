using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    internal static class FuncUtils
    {
        internal static void DispatchEvent<T1>(this Func<T1, Task> func, T1 arg1)
        => Task.Run(async () =>
        {
            try
            {
                var r = func(arg1);
        
                await r;
        
                if (r.Exception != null)
                {
                    Console.WriteLine($"Exception in handler: {r.Exception}");
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"Exception in handler: {x}");
            }
        });

        internal static void DispatchEvent<T1, T2>(this Func<T1, T2, Task> func, T1 arg1, T2 args2)
        => Task.Run(async () =>
        {
            try
            {
                var r = func(arg1, args2);

                await r;

                if (r.Exception != null)
                {
                    Console.WriteLine($"Exception in handler: {r.Exception}");
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"Exception in handler: {x}");
            }
        });
        internal static void DispatchEvent<T1, T2, T3>(this Func<T1, T2, T3, Task> func, T1 arg1, T2 arg2, T3 arg3)
        => Task.Run(async () =>
        {
            try
            {
                var r = func(arg1, arg2, arg3);
        
                await r;
        
                if (r.Exception != null)
                {
                    Console.WriteLine($"Exception in handler: {r.Exception}");
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"Exception in handler: {x}");
            }
        });
    }
}
