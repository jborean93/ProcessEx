using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ProcessEx.Security
{
    [Flags]
    public enum JobAccessRights
    {
        /// <summary>
        /// JOB_OBJECT_ASSIGN_PROCESS - Required to assign processes to the job object.
        /// </summary>
        AssignProcess = 0x0001,
        /// <summary>
        /// JOB_OBJECT_SET_ATTRIBUTES - Required to set attributes of the job object.
        /// </summary>
        SetAttributes = 0x0002,
        /// <summary>
        /// JOB_OBJECT_QUERY - Required to retrieve certain information about a job object.
        /// </summary>
        Query = 0x0004,
        /// <summary>
        /// JOB_OBJECT_TERMINATE - Required to termiante all processes in the job object.
        /// </summary>
        Terminate = 0x0008,
        /// <summary>
        /// JOB_OBJECT_SET_SECURITY_ATTRIBUTES - No longer supported.
        /// </summary>
        SetSecurityAttributes = 0x0010,

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
        /// JOB_OBJECT_ALL_ACCESS - All possible access rights for a job object.
        /// </summary>
        AllAccess = StandardRightsRequired | 0x1F,
    }

    public class JobAccessRule : AccessRule
    {
        public JobAccessRights ProcessAccessRights { get { return (JobAccessRights)AccessMask; } }

        public JobAccessRule(IdentityReference identityReference, JobAccessRights accessMask, bool isInherited,
            InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : base(identityReference, (int)accessMask, isInherited, inheritanceFlags, propagationFlags, type) { }
    }

    public class JobSecurity : NativeSecurity<JobAccessRights, JobAccessRule>
    {
        public JobSecurity() : base(ResourceType.KernelObject) { }
        public JobSecurity(SafeHandle handle, AccessControlSections includeSections)
            : base(ResourceType.KernelObject, handle, includeSections) { }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask,
            bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new JobAccessRule(identityReference, (JobAccessRights)accessMask,
                isInherited, inheritanceFlags, propagationFlags, type);
        }
    }
}
