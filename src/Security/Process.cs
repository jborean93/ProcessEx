using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ProcessEx.Security
{
    [Flags]
    public enum ProcessAccessRights
    {
        /// <summary>
        /// PROCESS_TERMINATE - Required to terminate a process.
        /// </summary>
        Terminate = 0x00000001,
        /// <summary>
        /// PROCESS_CREATE_THREAD - Required to create a thread.
        /// </summary>
        CreateThread = 0x00000002,
        /// <summary>
        /// PROCESS_VM_OPERATION - Required to perform an operation on the address space of a process.
        /// </summary>
        VMOperation = 0x00000008,
        /// <summary>
        /// PROCESS_VM_READ - Required to read memory in a process.
        /// </summary>
        VMRead = 0x00000010,
        /// <summary>
        /// PROCESS_VM_WRITE - Required to write to memory in a process.
        /// </summary>
        VMWrite = 0x00000020,
        /// <summary>
        /// PROCESS_DUP_HANDLE - Required to duplicate a handle.
        /// </summary>
        DupHandle = 0x00000040,
        /// <summary>
        /// PROCESS_CREATE_PROCESS - Required to create a process.
        /// </summary>
        CreateProcess = 0x00000080,
        /// <summary>
        /// PROCESS_SET_QUOTA - Required to set memory limits.
        /// </summary>
        SetQuota = 0x00000100,
        /// <summary>
        /// PROCESS_SET_INFORMATION - Required to set certain information about a process.
        /// </summary>
        SetInformation = 0x00000200,
        /// <summary>
        /// PROCESS_QUERY_INFORMATION - Required to retrieve certain information about a process.
        /// </summary>
        QueryInformation = 0x00000400,
        /// <summary>
        /// PROCESS_SUSPEND_RESUME - Required to suspend or resume a process.
        /// </summary>
        SuspendResume = 0x00000800,
        /// <summary>
        /// PROCESS_QUERY_LIMITED_INFORMATION - Required to retrieved certain limited information about a process.
        /// </summary>
        QueryLimitedInformation = 0x00001000,

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
        /// PROCESS_ALL_ACCESS - All possible access rights for a process object.
        /// </summary>
        AllAccess = StandardRightsRequired | Synchronize | 0x1FFF,
    }

    public class ProcessSecurity : NativeSecurity<ProcessAccessRights, ProcessAccessRule>
    {
        public ProcessSecurity() : base(ResourceType.KernelObject) { }
        public ProcessSecurity(SafeHandle handle, AccessControlSections includeSections)
            : base(ResourceType.KernelObject, handle, includeSections) { }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask,
            bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new ProcessAccessRule(identityReference, (ProcessAccessRights)accessMask,
                isInherited, inheritanceFlags, propagationFlags, type);
        }
    }

    public class ProcessAccessRule : AccessRule
    {
        public ProcessAccessRights ProcessAccessRights { get { return (ProcessAccessRights)AccessMask; } }

        public ProcessAccessRule(IdentityReference identityReference, ProcessAccessRights accessMask, bool isInherited,
            InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : base(identityReference, (int)accessMask, isInherited, inheritanceFlags, propagationFlags, type) { }
    }
}
