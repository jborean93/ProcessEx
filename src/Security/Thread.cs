using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ProcessEx.Security
{
    [Flags]
    public enum ThreadAccessRights
    {
        /// <summary>
        /// THREAD_TERMINATE - Required to termiante a thread.
        /// </summary>
        Terminate = 0x00000001,
        /// <summary>
        /// THREAD_SUSPEND_RESUME - Required to suspend or resume a thread.
        /// </summary>
        SuspendResume = 0x00000002,
        /// <summary>
        /// THREAD_GET_CONTEXT - Required to read the context of a thread.
        /// </summary>
        GetContext = 0x00000008,
        /// <summary>
        /// THREAD_SET_CONTEXT - Write the context of a thread.
        /// </summary>
        SetContext = 0x00000010,
        /// <summary>
        /// THREAD_SET_INFORMATION - Set certain information in the thread object.
        /// </summary>
        SetInformation = 0x00000020,
        /// <summary>
        /// THREAD_QUERY_INFORMATION - Read information from the thread object.
        /// </summary>
        QueryInformation = 0x00000040,
        /// <summary>
        /// THREAD_SET_THREAD_TOKEN - Required to set the impersonation token for a thread.
        /// </summary>
        SetThreadToken = 0x00000080,
        /// <summary>
        /// THREAD_IMPERSONATE - Required to use a thread's security info directly.
        /// </summary>
        Impersonate = 0x00000100,
        /// <summary>
        /// THREAD_DIRECT_IMPERSONATION - Required for a server thread that impersonates a client.
        /// </summary>
        DirectImpersonation = 0x00000200,
        /// <summary>
        /// THREAD_SET_LIMITED_INFORMATION - Set certain information in the thread object.
        /// </summary>
        SetLimitedInformation = 0x00000400,
        /// <summary>
        /// THREAD_QUERY_LIMITED_INFORMATION - Read limited information from the thread object.
        /// </summary>
        QueryLimitedInformation = 0x00000800,

        /// <summary>
        /// DELETE - Required to delete the object.
        /// </summary>
        Delete = 0x00010000,
        /// <summary>
        /// READ_CONTROL - Required to read information in the security descriptor.
        /// </summary>
        ReadControl = 0x00020000,
        /// <summary>
        /// WRITE_DAC - Required to modify the DACL in the security descriptor for the object.
        /// </summary>
        WriteDAC = 0x00040000,
        /// <summary>
        /// WRITE_OWNER - Required to change the owner in the security descriptor for the object.
        /// </summary>
        WriteOwner = 0x00080000,
        /// <summary>
        /// SYNCHRONIZE - Enables a thread to wait until the object is in the signaled state.
        /// </summary>
        Synchronize = 0x00100000,
        /// <summary>
        /// ACCESS_SYSTEM_SECURITY - Required to read/modify the SACL in the security descriptor for the object.
        /// </summary>
        AccessSystemSecurity = 0x01000000,

        /// <summary>
        /// STANDARD_RIGHTS_ALL
        /// </summary>
        StandardRightsAll = Delete | ReadControl | WriteDAC | WriteOwner | Synchronize,
        /// <summary>
        /// STANDARD_RIGHTS_EXECUTE
        /// </summary>
        StandardRightsExecute = ReadControl,
        /// <summary>
        /// STANDARD_RIGHTS_READ
        /// </summary>
        StandardRightsRead = ReadControl,
        /// <summary>
        /// STANDARD_RIGHTS_REQUIRED
        /// </summary>
        StandardRightsRequired = Delete | ReadControl | WriteDAC | WriteOwner,
        /// <summary>
        /// STANDARD_RIGHTS_WRITE
        /// </summary>
        StandardRightsWrite = ReadControl,

        /// <summary>
        /// GENERIC_ALL
        /// </summary>
        GenericAll = 0x10000000,
        /// <summary>
        /// GENERIC_EXECUTE
        /// </summary>
        GenericExecute = 0x20000000,
        /// <summary>
        /// GENERIC_WRITE
        /// </summary>
        GenericWrite = 0x40000000,
        /// <summary>
        /// GENERIC_READ
        /// </summary>
        GenericRead = -2147483648,

        /// <summary>
        /// THREAD_ALL_ACCESS - All possible access rights for a thread object.
        /// </summary>
        AllAccess = StandardRightsRequired | Synchronize | 0xFFF,
    }

    public class ThreadAccessRule : AccessRule
    {
        public ThreadAccessRights ThreadAccessRights { get { return (ThreadAccessRights)AccessMask; } }

        public ThreadAccessRule(IdentityReference identityReference, ThreadAccessRights accessMask, bool isInherited,
            InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : base(identityReference, (int)accessMask, isInherited, inheritanceFlags, propagationFlags, type) { }
    }

    public class ThreadSecurity : NativeSecurity<ThreadAccessRights, ThreadAccessRule>
    {
        public ThreadSecurity() : base(ResourceType.KernelObject) { }
        public ThreadSecurity(SafeHandle handle, AccessControlSections includeSections)
            : base(ResourceType.KernelObject, handle, includeSections) { }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask,
            bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new ThreadAccessRule(identityReference, (ThreadAccessRights)accessMask,
                isInherited, inheritanceFlags, propagationFlags, type);
        }
    }
}
