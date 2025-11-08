using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Utilities
{
    public class TORTests
    {
        private static TORTests _instance;
        public static TORTests Instance 
        {
            get
            {
                if( _instance == null ) _instance = new TORTests();
                return _instance;
            }
        }


        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public void TriggerCorruptedMemoryStateException()
        {
            try
            {
                var ptr = new IntPtr(42);
                Marshal.StructureToPtr(42, ptr, true);
            }
            catch (Exception e)
            {
                TORCommon.Log(e.ToString(), NLog.LogLevel.Error);
                Process.GetCurrentProcess().Kill();
            }
        }
    }
}
