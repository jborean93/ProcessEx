using System;
using System.ComponentModel;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace ProcessEx
{
    internal enum Win32ErrorCode : int
    {
        ERROR_SUCCESS = 0x00000000,
        ERROR_ACCESS_DENIED = 0x00000005,
        ERROR_INVALID_HANDLE = 0x00000006,
        ERROR_INVALID_DATA = 0x0000000D,
        ERROR_BAD_LENGTH = 0x00000018,
        ERROR_INVALID_PARAMETER = 0x00000057,
        ERROR_INSUFFICIENT_BUFFER = 0x0000007A,
        ERROR_MORE_DATA = 0x000000EA,
        ERROR_NO_TOKEN = 0x000003F0,
        ERROR_NOT_FOUND = 0x00000490,
        ERROR_CANT_OPEN_ANONYMOUS = 0x00000543,
    }

    public class NativeException : Win32Exception
    {
        public string Function { get; }

        public NativeException(string function) : this(function, Marshal.GetLastWin32Error()) { }
        public NativeException(string function, int errorCode) : base(errorCode)
        {
            Function = function;
        }
    }

    internal class ErrorHelper
    {
        public static ErrorRecord GenerateWin32Error(NativeException exception, string message,
            object? targetObject = null)
        {
            string errorId = exception.Function + ",";
            try
            {
                errorId += Enum.GetName(typeof(Win32ErrorCode), exception.NativeErrorCode);
            }
            catch (ArgumentException)
            {
                errorId += String.Format("0x{2:X8}", exception.NativeErrorCode);
            }

            ErrorCategory category = ErrorCategory.NotSpecified;
            switch (exception.NativeErrorCode)
            {
                case (int)Win32ErrorCode.ERROR_ACCESS_DENIED:
                case (int)Win32ErrorCode.ERROR_CANT_OPEN_ANONYMOUS:
                    category = ErrorCategory.PermissionDenied;
                    break;

                case (int)Win32ErrorCode.ERROR_INVALID_HANDLE:
                    category = ErrorCategory.InvalidData;
                    break;

                case (int)Win32ErrorCode.ERROR_BAD_LENGTH:
                case (int)Win32ErrorCode.ERROR_INSUFFICIENT_BUFFER:
                case (int)Win32ErrorCode.ERROR_INVALID_PARAMETER:
                    category = ErrorCategory.InvalidArgument;
                    break;

                case (int)Win32ErrorCode.ERROR_NO_TOKEN:
                    category = ErrorCategory.ObjectNotFound;
                    break;
            }

            ErrorRecord record = new ErrorRecord(exception, errorId, (ErrorCategory)category, targetObject);
            string errorMessage = String.Format("{0} ({1} Win32ErrorCode {2} - 0x{2:X8})",
                message, exception.Message, exception.NativeErrorCode);
            record.ErrorDetails = new ErrorDetails(errorMessage);

            return record;
        }
    }
}
