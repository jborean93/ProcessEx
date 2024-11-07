$buffer = [byte[]]::new(4096)
$in = [Console]::OpenStandardInput()
try {
    $data = while ($true) {
        $read = $in.Read($buffer, 0, $buffer.Length)
        if ($read -eq 0) {
            break
        }

        $buffer[0..($read - 1)]
    }

    ($data | ForEach-Object ToString X2) -join ''
}
finally {
    $in.Dispose()
}
