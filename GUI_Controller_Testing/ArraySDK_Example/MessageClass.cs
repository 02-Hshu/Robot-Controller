using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK_Example
{
    /// <summary>
    /// Format of the message
    /// </summary>
    public struct structMessage
    {
        public string strMessage;
        public int iRet;
        public int iIndex;
        public bool bOK;
        public bool bCancel;

        public structMessage( string str, int i1, int i2, bool b1, bool b2)
        {
            this.strMessage = str;
            this.iRet = i1;
            this.iIndex = i2;
            this.bOK = b1;
            this.bCancel = b2;
        }
    }
    /// <summary>
    /// Represents the list of messages
    /// </summary>
    public static class MessageClass
    {
        public const int CLOSEFORM = 0;
        public static structMessage ScanConverterMessage = new structMessage("Invalid values of the Scan Converter", CLOSEFORM, 0, true, false);
    }

}
