using System;
using System.Data;
using System.Configuration;
using System.Web;

namespace Tlv.Framework.Logging
{
    public enum Level
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    [Flags]
    public enum Target
    {
        None = 0,
        Ev = 1,
        Screen = 2,
        File = 4,
        Db = 8
    }

    public enum AlertType
    {
        GeneralException = 0,
        DBException = 1,
        DBInfo = 2,
        SecurityException = 3,
        SecurityInfo = 4,
        UIException = 5,
        UIInfo = 6,
        BizLogicInfo = 7,
        BizLogicViolation = 8,
        BizLogicException = 9
    }
}