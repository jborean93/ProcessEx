# Changelog for ProcessEx

## v0.2.0 - TBD

* Added `WorkingDirectory` to the `ProcessInfo` object returned by `Get-ProcessEx`
* Fixed `Start-ProcessWith ... -Credential $cred` when running in a non-interactive session
  * Previous this set the `lpDesktop` field in the startup info to `NULL` but due to the Windows rules for non-interactive sessions this failed
  * By explicitly setting the value to the current station/desktop this step will now work

## v0.1.0 - 2021-12-06

* Initial version of the `ProcessEx` module
