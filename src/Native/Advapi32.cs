using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace ProcessEx.Native
{
    internal partial class Helpers
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_GROUPS
        {
            public Int32 GroupCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public SID_AND_ATTRIBUTES[] Groups;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }

        [Flags]
        public enum LogonFlags : uint
        {
            NONE = 0x00000000,
            LOGON_WITH_PROFILE = 0x00000001,
            LOGON_NETCREDENTIALS_ONLY = 0x00000002,
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            TokenIsAppContainer,
            TokenCapabilities,
            TokenAppContainerSid,
            TokenAppContainerNumber,
            TokenUserClaimAttributes,
            TokenDeviceClaimAttributes,
            TokenRestrictedUserClaimAttributes,
            TokenRestrictedDeviceClaimAttributes,
            TokenDeviceGroups,
            TokenRestrictedDeviceGroups,
            TokenSecurityAttributes,
            TokenIsRestricted,
            TokenProcessTrustLevel,
            TokenPrivateNameSpace,
            TokenSingletonAttributes,
            TokenBnoIsolation,
            TokenChildProcessFlags,
            TokenIsLessPrivilegedAppContainer,
            TokenIsSandboxed,
            TokenOriginatingProcessTrustLevel,
        }
    }

    internal static class Advapi32
    {
        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessAsUserW(
            SafeHandle hToken,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpApplicationName,
            StringBuilder lpCommandLine,
            SafeHandle lpProcessAttributes,
            SafeHandle lpThreadAttributes,
            bool bInheritHandles,
            CreationFlags dwCreationFlags,
            SafeHandle lpEnvironment,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpCurrentDirectory,
            ref Helpers.STARTUPINFOEX lpStartupInfo,
            out Helpers.PROCESS_INFORMATION lpProcessInformation);

        public static ProcessInfo CreateProcessAsUser(SafeHandle token, string? applicationName, string? commandLine,
            SafeHandle processAttributes, SafeHandle threadAttributes, bool inherit, CreationFlags creationFlags,
            SafeHandle environment, string? currentDirectory, Helpers.STARTUPINFOEX startupInfo)
        {
            StringBuilder cmdLine = new StringBuilder(commandLine);
            if (!CreateProcessAsUserW(token, applicationName, cmdLine, processAttributes, threadAttributes, inherit,
                creationFlags, environment, currentDirectory, ref startupInfo, out var pi))
            {
                throw new NativeException("CreateProcessAsUser");
            }

            return new ProcessInfo(pi, cmdLine.ToString());
        }

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessWithLogonW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpUsername,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpDomain,
            SafeHandle lpPassword,
            Helpers.LogonFlags dwLogonFlags,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpApplicationName,
            StringBuilder lpCommandLine,
            CreationFlags dwCreationFlags,
            SafeHandle lpEnvironment,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpCurrentDirectory,
            ref Helpers.STARTUPINFOEX lpStartupInfo,
            out Helpers.PROCESS_INFORMATION lpProcessInformation);

        public static ProcessInfo CreateProcessWithLogon(string username, string? domain, SafeHandle password,
            Helpers.LogonFlags logonFlags, string? applicationName, string? commandLine, CreationFlags creationFlags,
            SafeHandle environment, string? currentDirectory, Helpers.STARTUPINFOEX startupInfo)
        {
            StringBuilder cmdLine = new StringBuilder(commandLine);
            if (!CreateProcessWithLogonW(username, domain, password, logonFlags, applicationName, cmdLine,
                creationFlags, environment, currentDirectory, ref startupInfo, out var pi))
            {
                throw new NativeException("CreateProcessWithLogon");
            }

            return new ProcessInfo(pi, cmdLine.ToString());
        }

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessWithTokenW(
            SafeHandle hToken,
            Helpers.LogonFlags dwLogonFlags,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpApplicationName,
            StringBuilder lpCommandLine,
            CreationFlags dwCreationFlags,
            SafeHandle lpEnvironment,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpCurrentDirectory,
            ref Helpers.STARTUPINFOEX lpStartupInfo,
            out Helpers.PROCESS_INFORMATION lpProcessInformation);

        public static ProcessInfo CreateProcessWithToken(SafeHandle token, Helpers.LogonFlags logonFlags,
            string? applicationName, string? commandLine, CreationFlags creationFlags, SafeHandle environment,
            string? currentDirectory, Helpers.STARTUPINFOEX startupInfo)
        {
            StringBuilder cmdLine = new StringBuilder(commandLine);
            if (!CreateProcessWithTokenW(token, logonFlags, applicationName, cmdLine, creationFlags, environment,
                currentDirectory, ref startupInfo, out var pi))
            {
                throw new NativeException("CreateProcessWithToken");
            }

            return new ProcessInfo(pi, cmdLine.ToString());
        }

        [DllImport("Advapi32.dll", EntryPoint = "GetTokenInformation", SetLastError = true)]
        private static extern bool NativeGetTokenInformation(
            SafeHandle TokenHandle,
            Helpers.TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            Int32 TokenInformationLength,
            out Int32 ReturnLength);

        public static SafeMemoryBuffer GetTokenInformation(SafeHandle token, Helpers.TOKEN_INFORMATION_CLASS infoClass)
        {
            NativeGetTokenInformation(token, infoClass, IntPtr.Zero, 0, out var returnLength);

            SafeMemoryBuffer buffer = new SafeMemoryBuffer(returnLength);
            if (!NativeGetTokenInformation(token, infoClass, buffer.DangerousGetHandle(), returnLength,
                out _))
            {
                int errCode = Marshal.GetLastWin32Error();
                buffer.Dispose();
                throw new NativeException("GetTokenInformation", errCode);
            }

            return buffer;
        }

        [DllImport("Advapi32.dll", EntryPoint = "OpenProcessToken", SetLastError = true)]
        private static extern bool NativeOpenProcessToken(
            SafeHandle ProcessHandle,
            TokenAccessLevels DesiredAccess,
            out SafeNativeHandle TokenHandle);

        public static SafeNativeHandle OpenProcessToken(SafeHandle process, TokenAccessLevels access)
        {
            if (!NativeOpenProcessToken(process, access, out var token))
                throw new NativeException("OpenProcessToken");

            return token;
        }
    }
}
