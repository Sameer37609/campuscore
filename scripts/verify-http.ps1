$ErrorActionPreference='Stop'
$base='http://localhost:5080'
$checks=0
$routes=@('/','/Students','/Courses','/Enrollments','/Students/Details/1','/Courses/Details/1','/Enrollments/Details/1','/Students?q=Mira','/Students?department=Business&status=Active&year=Year%202&sort=name-desc','/Students?page=2','/Students?q=zzzz-no-match','/Courses?q=CS201','/Enrollments?status=Completed&year=Spring%202026','/css/site.css','/js/site.js','/icons.svg','/favicon.svg','/lib/bootstrap/dist/css/bootstrap.min.css')
foreach($route in $routes){
 $r=Invoke-WebRequest ($base+$route) -SkipHttpErrorCheck
 if($r.StatusCode -ne 200){throw "FAIL $route $($r.StatusCode)"}
 $checks++; Write-Output "PASS 200 $route"
}
foreach($route in @('/Students/Details/9999','/Courses/Details/9999','/Enrollments/Details/9999','/not-a-page')){
 $r=Invoke-WebRequest ($base+$route) -SkipHttpErrorCheck
 if($r.StatusCode -ne 404 -or $r.Content -notmatch 'Page not found'){throw "FAIL 404 $route"}
 $checks++; Write-Output "PASS friendly 404 $route"
}
$r=Invoke-WebRequest ($base+'/Home/Error') -SkipHttpErrorCheck
if($r.StatusCode -ne 500 -or $r.Content -notmatch 'Something went wrong'){throw 'FAIL error page'}
$checks++; Write-Output 'PASS friendly 500'
$r=Invoke-WebRequest ($base+'/Students/SaveStudent') -Method Post -SkipHttpErrorCheck
if($r.StatusCode -ne 400){throw 'FAIL readonly methods'}
$checks++; Write-Output 'PASS POST without antiforgery rejected (400)'
# Crawl every rendered local link, including pagination and linked details.
$queue=[Collections.Generic.Queue[string]]::new()
$seen=[Collections.Generic.HashSet[string]]::new()
$queue.Enqueue('/')
while($queue.Count -gt 0){
 $route=$queue.Dequeue()
 if(-not $seen.Add($route)){continue}
 $r=Invoke-WebRequest ($base+$route) -SkipHttpErrorCheck
 if($r.StatusCode -ne 200){throw "FAIL linked route $route $($r.StatusCode)"}
 foreach($match in [regex]::Matches($r.Content,'href="([^"]+)"')){
  $href=[Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
  if($href.StartsWith('/') -and -not $href.StartsWith('//') -and -not $href.Contains('#')){$queue.Enqueue($href)}
 }
}
Write-Output "PASS all $($seen.Count) discovered links returned 200"
$html=(Invoke-WebRequest $base).Content
foreach($metric in @('48','11','158','55%','107 enrolled / 194 available seats')){if(-not $html.Contains($metric)){throw "FAIL dashboard metric $metric"}}
Write-Output 'PASS rendered dashboard metrics reconcile with verified dataset'
$search=(Invoke-WebRequest ($base+'/Students?q=Mira')).Content
if($search -notmatch 'Mira Ashford' -or $search -match 'Avery Linden'){throw 'FAIL rendered search'}
$empty=(Invoke-WebRequest ($base+'/Students?q=zzzz-no-match')).Content
if($empty -notmatch 'No Students' -and $empty -notmatch 'No students found'){throw 'FAIL empty state'}
$filter=(Invoke-WebRequest ($base+'/Enrollments?status=Completed&year=Spring%202026')).Content
if($filter -notmatch '<strong>48</strong> matching'){throw 'FAIL rendered enrollment filters'}
$injection=(Invoke-WebRequest ($base+'/Students?q=%3Cscript%3Ealert(1)%3C%2Fscript%3E')).Content
if($injection.Contains('<script>alert(1)</script>') -or -not $injection.Contains('&lt;script&gt;')){throw 'FAIL output encoding'}
Write-Output 'PASS rendered search, empty state, term/status filters and Razor output encoding'
Write-Output "VERIFIED $checks explicit HTTP checks plus $($seen.Count) discovered links."
