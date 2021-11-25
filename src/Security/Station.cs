using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ProcessEx.Security
{
    [Flags]
    public enum StationAccessRights
    {
        /// <summary>
        /// WINSTA_ENUMDESKTOPS - Required to enumerate existing desktop objects.
        /// </summary>
        EnumDesktops = 0x00000001,
        /// <summary>
        /// WINSTA_READATTRIBUTES - Required to read the attribnutes of a window station object.
        /// </summary>
        ReadAttributes = 0x00000002,
        /// <summary>
        /// WINSTA_ACCESSCLIPBOARD - Required to use the clipboard.
        /// </summary>
        AccessClipboard = 0x000000004,
        /// <summary>
        /// WINSTA_CREATEDESKTOP - Required to create new desktop objects on the window station.
        /// </summary>
        CreateDesktop = 0x00000008,
        /// <summary>
        /// WINSTA_WRITEATTRIBUTES - Required to modify the attributes of a window station object.
        /// </summary>
        WriteAttributes = 0x00000010,
        /// <summary>
        /// WINSTA_ACCESSGLOBALATOMS - Required to manipulate global atoms.
        /// </summary>
        AccessGlobalAtoms = 0x00000020,
        /// <summary>
        /// WINSTA_EXITWINDOWS - Required to successully exist the window in a station.
        /// </summary>
        ExitWindows = 0x00000040,
        /// <summary>
        /// WINSTA_ENUMERATE - Required for the window station to be enumerated.
        /// </summary>
        Enumerate = 0x00000100,
        /// <summary>
        /// WINSTA_READSCREEN - Required to access screen contents.
        /// </summary>
        ReadScreen = 0x00000200,


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
        /// SYNCHRONIZE - Not supported for window station objects.
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
        /// GENERIC_ALL - StandardRightsRequired | AccessClipboard | AccessGlobalAtoms | CreateDesktop |
        /// EnumDesktops | Enumerate | ExitWindows | ReadAttributes | ReadScreen | WriteAttributes
        /// </summary>
        GenericAll = 0x10000000,
        /// <summary>
        /// GENERIC_EXECUTE - StandardRightsExecute | AccessGlobalAtoms | ExitWindows
        /// </summary>
        GenericExecute = 0x20000000,
        /// <summary>
        /// GENERIC_WRITE - StandardRightsWrite | AccessClipboard | CreateDesktop | WriteAttributes
        /// </summary>
        GenericWrite = 0x40000000,
        /// <summary>
        /// GENERIC_READ - StandardRightsRead | EnumDesktops | Enumerate | ReadAttributes | ReadScreen
        /// </summary>
        GenericRead = -2147483648,

        /// <summary>
        /// WINSTA_ALL_ACCESS - All possible access rights for a window station object.
        /// </summary>
        AllAccess = StandardRightsRequired | 0x37F,
    }

    public class StationAccessRule : AccessRule
    {
        public StationAccessRights ProcessAccessRights { get { return (StationAccessRights)AccessMask; } }

        public StationAccessRule(IdentityReference identityReference, StationAccessRights accessMask, bool isInherited,
            InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : base(identityReference, (int)accessMask, isInherited, inheritanceFlags, propagationFlags, type) { }
    }

    public class StationSecurity : NativeSecurity<StationAccessRights, StationAccessRule>
    {
        public StationSecurity() : base(ResourceType.WindowObject) { }
        public StationSecurity(SafeHandle handle, AccessControlSections includeSections)
            : base(ResourceType.WindowObject, handle, includeSections) { }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask,
            bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new StationAccessRule(identityReference, (StationAccessRights)accessMask,
                isInherited, inheritanceFlags, propagationFlags, type);
        }
    }
}
