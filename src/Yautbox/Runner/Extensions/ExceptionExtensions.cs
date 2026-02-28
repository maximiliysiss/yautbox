using System;
using System.Threading.Tasks;

namespace Yautbox.Runner.Extensions;

internal static class ExceptionExtensions
{
    public static bool IsCancel(this Exception ex) => ex is OperationCanceledException or TaskCanceledException;
}
