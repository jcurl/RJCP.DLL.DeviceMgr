namespace RJCP.Native.Win32
{
    using System.Runtime.ConstrainedExecution;
    using Microsoft.Win32.SafeHandles;

    internal class SafeDevInst : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected SafeDevInst() : base(true) { }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            // Here, we must obey all rules for constrained execution regions.

            // If ReleaseHandle failed, it can be reported via the "releaseHandleFailed" managed debugging assistant
            // (MDA). This MDA is disabled by default, but can be enabled in a debugger or during testing to diagnose
            // handle corruption problems. We do not throw an exception because most code could not recover from the
            // problem.

            // There is no documentation that this handle should be closed. When trying to close through single stepping
            // with a debugger, we get an exception on Windows 10. Further, we see that when querying the root multiple
            // times, the actual value of the handle is constant, thus suggesting that the implementation doesn't need
            // to be freed.

            // So in effect, we use this class to wrap the result of the Win32 API in a type safe manner, but don't need
            // the atomic / clean disposal of the object.

            //return Kernel32.CloseHandle(handle);

            SetHandleAsInvalid();
            return true;
        }
    }
}
