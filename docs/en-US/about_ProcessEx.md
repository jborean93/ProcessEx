# ProcessEx
## about_ProcessEx

# SHORT DESCRIPTION
Interact with the Win32 APIs used to create processes.

# LONG DESCRIPTION

ProcessEx provides a way to

- Call the Win32 CreateProcess functions, including:
  - [CreateProcess](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw)
  - [CreateProcessAsUser](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessasuserw)
  - [CreateProcessWithLogon](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createprocesswithlogonw)
  - [CreateProcessWithToken](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createprocesswithtokenw)
- Set low level details like object inheritibility, ConPTY pipes, etc

This is mostly used as a way to have full control over how processes are spawned from the PowerShell session that might not be exposed through the normal process cmdlets.
