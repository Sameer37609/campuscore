$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$sdk='C:\Program Files\dotnet\dotnet.exe'
$dll=Join-Path $root 'bin\Release\net10.0\CampusCore.dll'
$empty=Join-Path $root ('obj\phase3-empty-config-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($empty)
'{}'|Set-Content -LiteralPath (Join-Path $empty 'appsettings.json')
$cases=@(
 @{Name='Unknown mode';Mode='Invalid';Env='Production';Args=@();Expected='Set CampusCore:Mode'},
 @{Name='Missing mode';Mode=$null;Env='Production';Args=@('--contentRoot',$empty);Expected='Set CampusCore:Mode'},
 @{Name='LocalSql in Production';Mode='LocalSql';Env='Production';Args=@();Expected='restricted to the Development'},
 @{Name='LocalSql remote SQL target';Mode='LocalSql';Env='Development';Connection='Server=untrusted.example.test;Database=Other;Integrated Security=true';Args=@();Expected='restricted to the named CampusCore LocalDB'},
 @{Name='LocalSql missing connection';Mode='LocalSql';Env='Development';Args=@('--contentRoot',$empty);Expected='requires a local development database connection'},
 @{Name='Demo migration command';Mode='PublicDemo';Env='Production';Args=@('--migrate-dev');Expected='Database commands require LocalSql'},
 @{Name='Demo seed command';Mode='PublicDemo';Env='Development';Args=@('--seed-dev');Expected='Database commands require LocalSql'},
 @{Name='Demo SQL verification command';Mode='PublicDemo';Env='Production';Args=@('--verify-database');Expected='Database commands require LocalSql'}
)
foreach($case in $cases){
 $start=[Diagnostics.ProcessStartInfo]::new($sdk);$start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true;$start.WorkingDirectory=$root
 [void]$start.ArgumentList.Add($dll);foreach($arg in $case.Args){[void]$start.ArgumentList.Add($arg)}
 $start.Environment['ASPNETCORE_ENVIRONMENT']=$case.Env;$start.Environment['DOTNET_ENVIRONMENT']=$case.Env;$start.Environment['Development__EnableWrites']='true'
 [void]$start.Environment.Remove('ConnectionStrings__CampusCore');[void]$start.Environment.Remove('CampusCore__Mode');[void]$start.Environment.Remove('ASPNETCORE_URLS')
 if($null -ne $case.Mode){$start.Environment['CampusCore__Mode']=$case.Mode}
 if($case.Connection){$start.Environment['ConnectionStrings__CampusCore']=$case.Connection}
 $process=[Diagnostics.Process]::new();$process.StartInfo=$start;[void]$process.Start();$stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
 if(-not $process.WaitForExit(20000)){$process.Kill($true);throw "FAIL: $($case.Name) started or hung instead of failing closed"}
 $text=$stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()
 if($process.ExitCode -eq 0 -or -not $text.Contains($case.Expected)){throw "FAIL: $($case.Name) unexpected exit $($process.ExitCode): $text"}
 $process.Dispose();Write-Output "PASS: $($case.Name) fails closed before serving requests"
}
Write-Output "VERIFIED $($cases.Count) release configuration-failure checks."
