using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ProcessEx.Security
{
    [Flags]
    public enum DesktopAccessRights
    {
        /// <summary>
        /// DESKTOP_READOBJECTS - Required to read objects on the desktop.
        /// </summary>
        ReadObjects = 0x00000001,
        /// <summary>
        /// DESKTOP_CREATEWINDOW - Required to create a window on the desktop.
        /// </summary>
        CreateWindow = 0x00000002,
        /// <summary>
        /// DESKTOP_CREATEMENU - Required to create a menu on the desktop.
        /// </summary>
        CreateMenu = 0x00000004,
        /// <summary>
        /// DESKTOP_HOOKCONTROL - Required to establish any of the window hooks.
        /// </summary>
        HookControl = 0x00000008,
        /// <summary>
        /// DESKTOP_JOURNALRECORD - Required to perform journal recording on a desktop.
        /// </summary>
        JournalRecord = 0x00000010,
        /// <summary>
        /// DESKTOP_JOURNALPLAYBACK - Required to perform journal playbook on a desktop.
        /// </summary>
        JournalPlayback = 0x00000020,
        /// <summary>
        /// DESKTOP_ENUMERATE - Required for the desktop to be enumerated.
        /// </summary>
        Enumerate = 0x00000040,
        /// <summary>
        /// DESKTOP_WRITEOBJECTS - Required to write objects on the desktop.
        /// </summary>
        WriteObjects = 0x00000080,
        /// <summary>
        /// DESKTOP_SWITCHDESKTOP - Required to activate the desktop using the SwitchDesktop function.
        /// </summary>
        SwitchDesktop = 0x00000100,

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
        /// SYNCHRONIZE - Not supported for desktop objects.
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
        /// GENERIC_ALL - CreateMenu | CreateWindow | Enumerate | HookControl | JournalPlayback | JournalRecord |
        /// ReadObjects | SwitchDesktop | WriteObjects | StandardRightsRequired
        /// </summary>
        GenericAll = 0x10000000,
        /// <summary>
        /// GENERIC_EXECUTE - SwitchDesktop | StandardRightsExecute
        /// </summary>
        GenericExecute = 0x20000000,
        /// <summary>
        /// GENERIC_WRITE - CreateMenu | CreateWindow | HookControl | JournalPlayback | JournalRecord | WriteObjects |
        /// StandardRightsWrite
        /// </summary>
        GenericWrite = 0x40000000,
        /// <summary>
        /// GENERIC_READ - Enumerate | ReadObjects | StandardRightsRead
        /// </summary>
        GenericRead = -2147483648,

        /// <summary>
        /// DESKTOP_ALL_ACCESS - All possible access rights for a desktop object.
        /// </summary>
        AllAccess = StandardRightsRequired | 0x1FF,
    }

    public class DesktopAccessRule : AccessRule
    {
        public DesktopAccessRights ProcessAccessRights { get { return (DesktopAccessRights)AccessMask; } }

        public DesktopAccessRule(IdentityReference identityReference, DesktopAccessRights accessMask, bool isInherited,
            InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : base(identityReference, (int)accessMask, isInherited, inheritanceFlags, propagationFlags, type) { }
    }

    public class DesktopSecurity : NativeSecurity<DesktopAccessRights, DesktopAccessRule>
    {
        public DesktopSecurity() : base(ResourceType.WindowObject) { }
        public DesktopSecurity(SafeHandle handle, AccessControlSections includeSections)
            : base(ResourceType.WindowObject, handle, includeSections) { }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask,
            bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new DesktopAccessRule(identityReference, (DesktopAccessRights)accessMask,
                isInherited, inheritanceFlags, propagationFlags, type);
        }
    }
}
