using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CodeElements.NetworkCallTransmissionProtocol.Extensions
{
    internal static class MethodInfoExtensions
    {
        public static byte[] GetMethodId(this MethodInfo methodInfo, MD5 md5)
        {
            return
                md5.ComputeHash(
                    Encoding.UTF8.GetBytes(methodInfo.Name + methodInfo.ReturnParameter?.ParameterType.FullName +
                                           string.Join("",
                                               methodInfo.GetParameters()
                                                   .Select(x => x.Position.ToString() + x.ParameterType.FullName))));
        }
    }
}