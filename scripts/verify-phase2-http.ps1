param([ValidateSet('Setup','VerifyRestart','Cleanup','Security')][string]$Phase='Setup',[string]$Base='http://localhost:5080')
$ErrorActionPreference='Stop'
$handler=[System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect=$false
$handler.CookieContainer=[System.Net.CookieContainer]::new()
$client=[System.Net.Http.HttpClient]::new($handler)
$client.Timeout=[TimeSpan]::FromSeconds(30)
$statePath=Join-Path $PSScriptRoot '..\obj\phase2-http-state.json'
$script:checks=0
function Check($condition,$name){if(-not $condition){throw "FAIL: $name"};$script:checks++;Write-Output "PASS: $name"}
function Send([string]$method,[string]$route,$data=$null,$headers=@{}){
 $request=[System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($method),$Base+$route)
 if($null -ne $data){$fields=[Collections.Generic.Dictionary[string,string]]::new();foreach($k in $data.Keys){$fields[$k]=[string]$data[$k]};$request.Content=[System.Net.Http.FormUrlEncodedContent]::new($fields)}
 foreach($key in $headers.Keys){[void]$request.Headers.TryAddWithoutValidation($key,[string]$headers[$key])}
 $response=$client.SendAsync($request).GetAwaiter().GetResult()
 $body=$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
 $location=if($response.Headers.Location){$response.Headers.Location.ToString()}else{''}
 $result=[pscustomobject]@{Status=[int]$response.StatusCode;Body=$body;Location=$location}
 $response.Dispose();$request.Dispose();return $result
}
function Value([string]$html,[string]$name){$tag=[regex]::Match($html,'<input[^>]*name="'+[regex]::Escape($name)+'"[^>]*>').Value;return [Net.WebUtility]::HtmlDecode([regex]::Match($tag,'value="([^"]*)"').Groups[1].Value)}
function Form([string]$route){$r=Send GET $route;if($r.Status -ne 200){throw "Form $route returned $($r.Status): $($r.Body.Substring(0,[Math]::Min(200,$r.Body.Length)))"};return @{__RequestVerificationToken=(Value $r.Body '__RequestVerificationToken');Version=(Value $r.Body 'Version');Id=(Value $r.Body 'Id')}}
function Kpi([string]$html,[string]$label,[int]$value){return $html -match ([regex]::Escape($label)+'</span>.*?<div class="kpi-value">'+$value+'</div>')}
try{
 if($Phase -eq 'Security'){
  foreach($route in @('/Students/SaveStudent','/Courses/SaveCourse','/Enrollments/SaveEnrollment','/Students/Delete/1')){Check ((Send POST $route @{}).Status -eq 403) "Writes rejected at $Base$route"}
  foreach($route in @('/Students/Create','/Courses/Create','/Enrollments/Create')){Check ((Send GET $route).Status -eq 403) "Write form rejected at $Base$route"}
  Check ((Send PUT '/Students/SaveStudent' @{}).Status -eq 403) "PUT rejected at $Base"
  Check ((Send DELETE '/Students/Delete/1' @{}).Status -eq 403) "DELETE rejected at $Base"
  $overview=Send GET '/';Check ($overview.Status -eq 200 -and $overview.Body -notmatch 'href="/Students/Create"') "Read dashboard remains available at $Base"
 }elseif($Phase -eq 'Setup'){
  if(Test-Path -LiteralPath $statePath){throw 'Prior HTTP verification state exists; run VerifyRestart/Cleanup before another Setup.'}
  $baseHtml=(Send GET '/').Body
  Check ((Kpi $baseHtml 'Total students' 48) -and (Kpi $baseHtml 'Total enrollments' 158)) 'Seed dashboard baseline'
  $suffix=[Guid]::NewGuid().ToString('N').Substring(0,8)
  $student=Form '/Students/Create'
  $student.Number='QA-HTTP-'+$suffix;$student.Name='HTTP Verification';$student.Email='http.'+$suffix+'@example.test';$student.Department='Business';$student.Year=2;$student.Status='Active'
  $bad=$student.Clone();$bad.Remove('__RequestVerificationToken');Check ((Send POST '/Students/SaveStudent' $bad).Status -eq 400) 'Missing antiforgery token rejected'
  $bad=$student.Clone();$bad.__RequestVerificationToken='invalid';Check ((Send POST '/Students/SaveStudent' $bad).Status -eq 400) 'Invalid antiforgery token rejected'
  $bad=$student.Clone();$bad.Name='';Check ((Send POST '/Students/SaveStudent' $bad).Status -eq 422) 'Server-side required field validation'
  foreach($headers in @(@{Host='public.example.test'},@{'X-Forwarded-For'='127.0.0.1'},@{Origin='https://external.example.test'})){Check ((Send POST '/Students/SaveStudent' $student $headers).Status -eq 403) 'Public/proxied/cross-origin write rejected before mutation'}
  $r=Send POST '/Students/SaveStudent' $student;Check ($r.Status -eq 302 -and $r.Location -match '/Students/Details/(\d+)') 'HTTP student create';$sid=[int]($r.Location.Split('/')[-1])
  $state=@{StudentId=$sid;StudentNumber=$student.Number;StudentEmail=$student.Email;StudentName='HTTP Verification Updated';Suffix=$suffix}
  $state|ConvertTo-Json|Set-Content -LiteralPath $statePath
  Check ((Send POST '/Students/SaveStudent' $student).Status -eq 422) 'HTTP duplicate student rejected'
  $edit=Form "/Students/Edit/$sid";foreach($key in @('Number','Name','Email','Department','Year','Status')){$edit[$key]=$student[$key]};$edit.Name=$state.StudentName
  $stale=$edit.Clone();Check ((Send POST '/Students/SaveStudent' $edit).Status -eq 302) 'HTTP student edit'
  Check ((Send POST '/Students/SaveStudent' $stale).Status -eq 422) 'HTTP stale form rejected'
  $course=Form '/Courses/Create';$course.Code='QA'+$suffix.ToUpper();$course.Title='HTTP Verification Course';$course.Department='Business';$course.Credits=3;$course.Capacity=1;$course.Status='Active'
  $bad=$course.Clone();$bad.Capacity=0;Check ((Send POST '/Courses/SaveCourse' $bad).Status -eq 422) 'HTTP capacity range validation'
  $r=Send POST '/Courses/SaveCourse' $course;Check ($r.Status -eq 302) 'HTTP course create';$cid=[int]($r.Location.Split('/')[-1]);$state.CourseId=$cid;$state.CourseCode=$course.Code;$state|ConvertTo-Json|Set-Content -LiteralPath $statePath
  Check ((Send POST '/Courses/SaveCourse' $course).Status -eq 422) 'HTTP duplicate course rejected'
  $edit=Form "/Courses/Edit/$cid";foreach($key in @('Code','Title','Department','Credits','Capacity','Status')){$edit[$key]=$course[$key]};$edit.Title='HTTP Verification Course Updated';Check ((Send POST '/Courses/SaveCourse' $edit).Status -eq 302) 'HTTP course edit'
  $enrollment=Form '/Enrollments/Create';$enrollment.StudentId=$sid;$enrollment.CourseId=$cid;$enrollment.Term='Fall';$enrollment.AcademicYear=2026;$enrollment.EnrolledOn='2026-11-05';$enrollment.Status='Enrolled';$enrollment.Grade=''
  $r=Send POST '/Enrollments/SaveEnrollment' $enrollment;Check ($r.Status -eq 302) 'HTTP enrollment create';$eid=[int]($r.Location.Split('/')[-1]);$state.EnrollmentId=$eid;$state|ConvertTo-Json|Set-Content -LiteralPath $statePath
  Check ((Send POST '/Enrollments/SaveEnrollment' $enrollment).Status -eq 422) 'HTTP duplicate enrollment rejected'
  $options=(Send GET '/Enrollments/Create').Body;$other=[regex]::Match($options,'<option value="(\d+)">Mira Ashford').Groups[1].Value
  Check (-not [string]::IsNullOrEmpty($other)) 'Existing active fictional student available'
  $full=$enrollment.Clone();$full.StudentId=$other;Check ((Send POST '/Enrollments/SaveEnrollment' $full).Status -eq 422) 'HTTP course capacity enforced'
  $delete=Form '/Students/Create';$delete.Id=$sid;$delete.Version=(Form "/Students/Edit/$sid").Version
  Check ((Send POST "/Students/Delete/$sid" $delete).Status -eq 422) 'HTTP student dependency deletion blocked'
  $delete=Form '/Courses/Create';$delete.Id=$cid;$delete.Version=(Form "/Courses/Edit/$cid").Version
  Check ((Send POST "/Courses/Delete/$cid" $delete).Status -eq 422) 'HTTP course dependency deletion blocked'
  $edit=Form "/Enrollments/Edit/$eid";foreach($key in @('StudentId','CourseId','Term','AcademicYear','EnrolledOn','Status','Grade')){$edit[$key]=$enrollment[$key]};$edit.Status='Completed';Check ((Send POST '/Enrollments/SaveEnrollment' $edit).Status -eq 422) 'HTTP completion requires grade'
  $edit.Status='Withdrawn';Check ((Send POST '/Enrollments/SaveEnrollment' $edit).Status -eq 302) 'HTTP withdrawal releases seat'
  $edit.Version=(Form "/Enrollments/Edit/$eid").Version;$edit.Status='Enrolled';Check ((Send POST '/Enrollments/SaveEnrollment' $edit).Status -eq 302) 'HTTP enrollment update persists'
  $html=(Send GET '/').Body
  Check ((Kpi $html 'Total students' 49) -and (Kpi $html 'Active courses' 12) -and (Kpi $html 'Total enrollments' 159) -and $html.Contains('108 enrolled / 195 available seats')) 'Dashboard updates from committed CRUD records'
  Check ($html.Contains('Nov 2026: 1')) 'Trend chart includes newly saved registration'
  Write-Output 'SETUP COMPLETE: restart the application, then run -Phase VerifyRestart and -Phase Cleanup.'
 }elseif($Phase -eq 'VerifyRestart'){
  $state=Get-Content -LiteralPath $statePath -Raw|ConvertFrom-Json
  $student=Send GET "/Students/Details/$($state.StudentId)";Check ($student.Status -eq 200 -and $student.Body.Contains($state.StudentName)) 'Student edit persists after application process restart'
  $course=Send GET "/Courses/Details/$($state.CourseId)";Check ($course.Status -eq 200 -and $course.Body.Contains('HTTP Verification Course Updated')) 'Course edit persists after application process restart'
  $enrollment=Send GET "/Enrollments/Details/$($state.EnrollmentId)";Check ($enrollment.Status -eq 200 -and $enrollment.Body.Contains('Enrolled')) 'Enrollment persists after application process restart'
  Check ((Kpi (Send GET '/').Body 'Total enrollments' 159)) 'Saved dashboard values persist after application restart'
 }else{
  $state=Get-Content -LiteralPath $statePath -Raw|ConvertFrom-Json
  if($state.EnrollmentId){$eid=$state.EnrollmentId;$edit=Form "/Enrollments/Edit/$eid";$edit.StudentId=$state.StudentId;$edit.CourseId=$state.CourseId;$edit.Term='Fall';$edit.AcademicYear=2026;$edit.EnrolledOn='2026-11-05';$edit.Status='Withdrawn';$edit.Grade='';Check ((Send POST '/Enrollments/SaveEnrollment' $edit).Status -eq 302) 'HTTP withdrawal before deletion';$delete=Form "/Enrollments/Delete/$eid";Check ((Send POST "/Enrollments/Delete/$eid" $delete).Status -eq 302) 'HTTP enrollment removal'}
  if($state.CourseId){$cid=$state.CourseId;$delete=Form "/Courses/Delete/$cid";Check ((Send POST "/Courses/Delete/$cid" $delete).Status -eq 302) 'HTTP course deletion'}
  $sid=$state.StudentId;$delete=Form "/Students/Delete/$sid";Check ((Send POST "/Students/Delete/$sid" $delete).Status -eq 302) 'HTTP student deletion'
  Check ((Send GET "/Students/Details/$sid").Status -eq 404) 'Deleted record returns 404'
  $html=(Send GET '/').Body;Check ((Kpi $html 'Total students' 48) -and (Kpi $html 'Active courses' 11) -and (Kpi $html 'Total enrollments' 158) -and $html.Contains('107 enrolled / 194 available seats')) 'Only QA records removed; seed dashboard restored'
  Remove-Item -LiteralPath $statePath
 }
 Write-Output "VERIFIED $script:checks HTTP $Phase checks."
}finally{$client.Dispose();$handler.Dispose()}
