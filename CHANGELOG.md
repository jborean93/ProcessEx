# Changelog for ProcessEx

## v0.4.0 - TBD

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
