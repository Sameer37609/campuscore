param([string]$Base='http://localhost:5085')
$ErrorActionPreference='Stop'
$handler=[Net.Http.HttpClientHandler]::new();$handler.AllowAutoRedirect=$false
$client=[Net.Http.HttpClient]::new($handler);$client.Timeout=[TimeSpan]::FromSeconds(20)
$checks=0
function Check($value,$label){if(-not $value){throw "FAIL: $label"};$script:checks++;Write-Output "PASS: $label"}
function Send($method,$path,$headers=@{}){
 $request=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($method),$Base+$path)
 foreach($key in $headers.Keys){[void]$request.Headers.TryAddWithoutValidation($key,[string]$headers[$key])}
 if($method -notin @('GET','HEAD','OPTIONS')){$request.Content=[Net.Http.StringContent]::new('Id=1&Number=ATTACK&Name=ShouldNotSave&__RequestVerificationToken=fake',[Text.Encoding]::UTF8,'application/x-www-form-urlencoded')}
 $r=$client.SendAsync($request).GetAwaiter().GetResult();$body=$r.Content.ReadAsStringAsync().GetAwaiter().GetResult();$all=@{}
 foreach($h in $r.Headers){$all[$h.Key]=[string]::Join(',',$h.Value)}
 foreach($h in $r.Content.Headers){$all[$h.Key]=[string]::Join(',',$h.Value)}
 $result=[pscustomobject]@{Status=[int]$r.StatusCode;Body=$body;Headers=$all};$r.Dispose();$request.Dispose();return $result
}
try{
 foreach($asset in @(@{Path='/css/site.css';Type='text/css'},@{Path='/lib/bootstrap/dist/css/bootstrap.min.css';Type='text/css'},@{Path='/js/site.js';Type='javascript'},@{Path='/icons.svg';Type='image/svg+xml'})){ $r=Send GET $asset.Path;Check ($r.Status -eq 200 -and $r.Body.Length -gt 100 -and $r.Headers['Content-Type'].Contains($asset.Type)) "Nonempty static asset $($asset.Path) has correct media type" }
 $baseline=Send GET '/'
 Check ($baseline.Status -eq 200 -and $baseline.Body.Contains('55%') -and $baseline.Body.Contains('107 enrolled / 194 available seats')) 'Fictional dashboard metrics'
 Check ($baseline.Body.Contains('Jan 2026: 16; Feb 2026: 16; Mar 2026: 16; Aug 2026: 36; Sept 2026: 36; Oct 2026: 38')) 'Chart counts reconcile with the fixed fictional fixture'
 Check ($baseline.Body.Contains('Read-only demo') -and $baseline.Body.Contains('this demo does not save changes')) 'Honest read-only demo context'
 foreach($path in @('/Students','/Courses','/Enrollments','/Students/Details/1','/Courses/Details/1','/Enrollments/Details/1')){
  $r=Send GET $path;Check ($r.Status -eq 200) "Demo read $path"
  Check ($r.Body -notmatch 'href="/(Students|Courses|Enrollments)/(Create|Edit|Delete)' -and $r.Body -notmatch '<form[^>]*method="post"') "No write controls at $path"
 }
 Check ((Send GET '/Students?q=Mira').Body.Contains('<strong>1</strong> matching student')) 'Demo student search'
 Check ((Send GET '/Courses?q=CS201').Body.Contains('Data Structures')) 'Demo course search'
 Check ((Send GET '/Enrollments?q=Mira').Body.Contains('<strong>4</strong> matching enrollments')) 'Demo enrollment search'
 Check ((Send GET '/Enrollments?status=Completed&year=Spring%202026').Body.Contains('<strong>48</strong> matching enrollments')) 'Demo term/year/status filtering'
 Check ((Send GET '/Students?department=Business&status=Active&sort=name-desc').Body.Contains('<strong>10</strong> matching students')) 'Demo combined filters'
 foreach($kind in @('Students','Courses','Enrollments')){
  foreach($sort in @('name','name-desc','id','department')){
   $html=(Send GET "/$kind`?sort=$sort").Body
   $pattern=if($sort -eq 'id'){'<td class="record-id"[^>]*>([^<]+)</td>'}elseif($sort -eq 'department'){'<td data-label="Department">([^<]+)</td>'}else{'<a class="record-title"[^>]*>([^<]+)</a>'}
   $values=@([regex]::Matches($html,$pattern)|ForEach-Object {$_.Groups[1].Value})
   $sorted=@($values|Sort-Object -Descending:($sort -eq 'name-desc'))
   Check ($values.Count -gt 0 -and ($values -join '|') -eq ($sorted -join '|')) "$kind $sort ordering"
  }
  $empty=Send GET "/$kind`?q=zzzz-no-match";Check ($empty.Status -eq 200 -and $empty.Body.Contains('No '+$kind.ToLower()+' found') -and $empty.Body.Contains('Clear filters')) "$kind friendly empty state"
 }
 Check ((Send GET '/Students?page=2').Body.Contains('Showing 11')) 'Demo pagination'
 $encoded=Send GET '/Students?q=%3Cscript%3Ealert(1)%3C%2Fscript%3E';Check ($encoded.Body.Contains('&lt;script&gt;') -and -not $encoded.Body.Contains('<script>alert(1)</script>')) 'Search output is encoded'
 foreach($kind in @('Students','Courses','Enrollments')){
  foreach($path in @("/$kind/Create","/$kind/Edit/1","/$kind/Delete/1")){
   foreach($method in @('GET','HEAD')){Check ((Send $method $path).Status -in $(if($method -eq 'HEAD'){@(403,404,405)}else{@(403)})) "$method write form $path rejected before database/controller activation"}
  }
  foreach($path in @("/$kind/SaveStudent","/$kind/SaveCourse","/$kind/SaveEnrollment","/$kind/Delete/1")){
   foreach($method in @('POST','PUT','PATCH','DELETE','TRACE','PROPFIND','BREW')){Check ((Send $method $path).Status -eq 403) "$method $path rejected"}
  }
 }
 foreach($path in @('/','/Home/Error','/Home/Status','/Management/SaveStudent','/unknown')){Check ((Send POST $path).Status -eq 403) "No mutation through alias/error/unknown route $path"}
 $rejected=Send POST '/Students/SaveStudent';Check ($rejected.Headers['X-Content-Type-Options'] -eq 'nosniff' -and $rejected.Headers['Content-Security-Policy'].Contains("frame-ancestors 'none'")) 'Security headers also cover rejected writes'
 foreach($path in @('/Students/Details/99999','/Courses/Details/99999','/Enrollments/Details/99999','/unknown','/appsettings.json','/appsettings.Development.json','/Program.cs','/README.md','/.git/config','/Data/Migrations/InitialAcademicSchema.sql')){Check ((Send GET $path).Status -eq 404) "Private or missing path $path returns 404"}
 $error=Send GET '/Home/Error';Check ($error.Status -eq 500 -and $error.Body.Contains('Something went wrong') -and $error.Body -notmatch 'InvalidOperationException|SqlException|StackTrace|Server=\(localdb\)') 'Friendly error without implementation or credential disclosure'
 Check ((Send GET '/Home/Status?code=99999').Status -eq 404) 'Invalid status input safely constrained'
 Check ((Send GET '/Students' @{Host='portfolio.example.test';'X-Forwarded-For'='203.0.113.5'}).Status -eq 200) 'Public-demo reads work independently of loopback/SQL credentials'
 Check ((Send GET '/').Body -eq $baseline.Body) 'Rejected mutation attempts leave the fictional dataset unchanged'
 $queue=[Collections.Generic.Queue[string]]::new();$seen=[Collections.Generic.HashSet[string]]::new();$queue.Enqueue('/')
 while($queue.Count -gt 0){$path=$queue.Dequeue();if(-not $seen.Add($path)){continue};$r=Send GET $path;if($r.Status -ne 200){throw "FAIL demo linked path $path status $($r.Status)"};foreach($m in [regex]::Matches($r.Body,'href="([^"]+)"')){$href=[Net.WebUtility]::HtmlDecode($m.Groups[1].Value);if($href.StartsWith('/') -and -not $href.StartsWith('//') -and -not $href.Contains('#')){$queue.Enqueue($href)}}}
 Check ($seen.Count -gt 200) "All $($seen.Count) discovered demo links returned 200"
 Write-Output "VERIFIED $checks public-demo HTTP checks; $($seen.Count) linked URLs."
}finally{$client.Dispose();$handler.Dispose()}
