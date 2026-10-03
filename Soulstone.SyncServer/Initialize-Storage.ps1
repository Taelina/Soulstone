param(
    [string]$KeyPath = (Join-Path $PSScriptRoot '../secrets/publications.key')
)

$ErrorActionPreference = 'Stop'
$keyDestination = [System.IO.Path]::GetFullPath($KeyPath)
if (Test-Path -LiteralPath $keyDestination) {
    throw 'The key file already exists. Keep the existing key; never replace it for an existing database.'
}
New-Item -ItemType Directory -Path ([System.IO.Path]::GetDirectoryName($keyDestination)) -Force | Out-Null
$keyBytes = New-Object byte[] 32
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $generator.GetBytes($keyBytes)
    $keyHex = [System.BitConverter]::ToString($keyBytes).Replace('-', '')
    # CreateNew refuses replacement, including a concurrent invocation.
    $stream = [System.IO.File]::Open($keyDestination, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
    try {
        if ($env:OS -eq 'Windows_NT') {
            $acl = New-Object System.Security.AccessControl.FileSecurity
            $acl.SetAccessRuleProtection($true, $false)
            $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
            $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($identity, 'FullControl', 'Allow')))
            $system = New-Object System.Security.Principal.SecurityIdentifier('S-1-5-18')
            $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($system, 'Read', 'Allow')))
            Set-Acl -LiteralPath $keyDestination -AclObject $acl
        }
        $encodedKey = [System.Text.Encoding]::UTF8.GetBytes($keyHex + "`n")
        $stream.Write($encodedKey, 0, $encodedKey.Length)
    } finally {
        $stream.Dispose()
    }
} finally {
    $generator.Dispose()
    [Array]::Clear($keyBytes, 0, $keyBytes.Length)
}
Write-Output 'Storage key created. Back it up separately from the encrypted database before starting the backend.'
