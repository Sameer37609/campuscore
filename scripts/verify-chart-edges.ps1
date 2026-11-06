param([string]$Base='http://localhost:5086')
$ErrorActionPreference='Stop'
$handler=[System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect=$false
$handler.CookieContainer=[System.Net.CookieContainer]::new()
$client=[System.Net.Http.HttpClient]::new($handler)
$client.Timeout=[TimeSpan]::FromSeconds(30)
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
 $html=(Send GET '/').Body
 Check ((Kpi $html 'Total students' 0) -and (Kpi $html 'Active courses' 0) -and (Kpi $html 'Total enrollments' 0)) 'Empty database dashboard metrics'
 Check ($html.Contains('No registrations recorded yet.') -and $html.Contains('No students recorded yet.') -and $html -notmatch '(cx|x|points)="[^"]*(NaN|Infinity)') 'Empty chart and department states are friendly and finite'
 $s=Form '/Students/Create';$s.Number='QA-EDGE';$s.Name='Fictional Chart Edge';$s.Email='edge@example.test';$s.Department='Computer Science';$s.Year=1;$s.Status='Active'
 $r=Send POST '/Students/SaveStudent' $s;Check ($r.Status -eq 302) 'Create isolated chart fixture student';$sid=[int]($r.Location.Split('/')[-1]);Check ((Send GET $r.Location).Body.Contains('Record saved successfully.')) 'Saved-record success feedback appears'
 $c=Form '/Courses/Create';$c.Code='E';$c.Title='Single Month Fixture';$c.Department='Computer Science';$c.Credits=3;$c.Capacity=1;$c.Status='Active'
 $r=Send POST '/Courses/SaveCourse' $c;Check ($r.Status -eq 302) 'Create isolated single-character course';$cid=[int]($r.Location.Split('/')[-1])
 Check ((Send GET '/Courses').Status -eq 200) 'Single-character course code renders'
 $e=Form '/Enrollments/Create';$e.StudentId=$sid;$e.CourseId=$cid;$e.Term='Fall';$e.AcademicYear=2026;$e.EnrolledOn='2026-11-06';$e.Status='Enrolled';$e.Grade=''
 $r=Send POST '/Enrollments/SaveEnrollment' $e;Check ($r.Status -eq 302) 'Create isolated one-month registration';$eid=[int]($r.Location.Split('/')[-1])
 $html=(Send GET '/').Body
 Check ($html.Contains('Nov 2026: 1') -and $html.Contains('cx="40.0"') -and -not $html.Contains('class="chart-fill"') -and $html -notmatch '(cx|x|points)="[^"]*(NaN|Infinity)') 'Single-month SVG points and labels are finite and aligned'
 Check ((Kpi $html 'Total students' 1) -and (Kpi $html 'Total enrollments' 1) -and $html.Contains('100%')) 'Single-fixture dashboard derives saved counts and utilization'
 $edit=Form "/Enrollments/Edit/$eid";foreach($key in @('StudentId','CourseId','Term','AcademicYear','EnrolledOn','Status','Grade')){$edit[$key]=$e[$key]};$edit.Status='Withdrawn'
 Check ((Send POST '/Enrollments/SaveEnrollment' $edit).Status -eq 302) 'Withdraw isolated chart fixture'
 Check ((Send POST "/Enrollments/Delete/$eid" (Form "/Enrollments/Delete/$eid")).Status -eq 302) 'Remove isolated chart enrollment'
 Check ((Send POST "/Courses/Delete/$cid" (Form "/Courses/Delete/$cid")).Status -eq 302) 'Remove isolated chart course'
 Check ((Send POST "/Students/Delete/$sid" (Form "/Students/Delete/$sid")).Status -eq 302) 'Remove isolated chart student'
 Check ((Send GET '/').Body.Contains('Record removed successfully.')) 'Removal success feedback appears'
 Check ((Kpi (Send GET '/').Body 'Total students' 0)) 'Only empty isolated fixture database remains'
 Write-Output "VERIFIED $script:checks empty/single-month HTTP chart and CRUD checks."
}finally{$client.Dispose();$handler.Dispose()}
