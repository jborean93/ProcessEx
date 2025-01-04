# Changelog for ProcessEx

## v0.5.0 - TBD

* Added support for `ProcessIntString` parameters accepting `uint` values as returned by WMI/CIM calls
* Added `-ArgumentEscaping` to `Start-ProcessWith` and `Start-ProcessEx`
* Added `Credential` attribute transformer to transform username to PSCredential prompt on `-Credential` parameters
* Add support for cancelling `Start-ProcessEx` and `Start-ProcessWith` with `-Wait`

## v0.4.0 - 2024-11-07

* Added `Invoke-ProcessWith` and `Invoke-ProcessEx` to invoke a process and capture the output like a normal call operator
* Added `-ArgumentEscaping` to `ConvertTo-EscapedArgument` with the ability to escape MSI style properties
* Added `New-StartupInfo -ChildProcessPolicy` to control the child process creation policy as an extended startupinfo attribute

## v0.3.0 - 2023-02-15

* Added `JobList` to `New-StartupInfo`
  * This provides an easier way to specify the jobs a new process is a member of
  * Requires Windows 10 or Server 2016

## v0.2.0 - 2022-10-02

* Added `WorkingDirectory` to the `ProcessInfo` object returned by `Get-ProcessEx`
* Fixed `Start-ProcessWith ... -Credential $cred` when running in a non-interactive session
  * Previous this set the `lpDesktop` field in the startup info to `NULL` but due to the Windows rules for non-interactive sessions this failed
  * By explicitly setting the value to the current station/desktop this step will now work

## v0.1.0 - 2021-12-06

* Initial version of the `ProcessEx` module
