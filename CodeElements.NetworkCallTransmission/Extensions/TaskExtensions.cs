using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmission.Extensions
{
    internal static class TaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forget(this Task task)
        {
            //Nothing here
        }
    }
}