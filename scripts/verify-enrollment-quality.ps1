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
 $list=Send GET '/Enrollments?status=Completed';$id=[regex]::Match($list.Body,'class="record-title" href="/Enrollments/Details/(\d+)"').Groups[1].Value
 Check (-not [string]::IsNullOrEmpty($id)) 'Completed fictional fixture exists'
 $page=Send GET "/Enrollments/Edit/$id";$f=Form "/Enrollments/Edit/$id"
 foreach($key in @('StudentId','CourseId','Term','AcademicYear','EnrolledOn')){$f[$key]=Value $page.Body $key}
 $gradeBlock=[regex]::Match($page.Body,'<select[^>]*name="Grade"[^>]*>(.*?)</select>','Singleline').Groups[1].Value
 $original=[regex]::Match($gradeBlock,'<option(?=[^>]*selected)[^>]*value="([^"]*)"').Groups[1].Value
 $original=[Net.WebUtility]::HtmlDecode($original); Check (-not [string]::IsNullOrEmpty($original)) 'Original grade captured for fixture restoration'
 $f.Status='Completed';$f.Grade='Z';Check ((Send POST '/Enrollments/SaveEnrollment' $f).Status -eq 422) 'Unknown grade rejected server-side'
 $f.Grade='A';Check ((Send POST '/Enrollments/SaveEnrollment' $f).Status -eq 302) 'Valid grade saved through HTTP'
 $f.Version=(Form "/Enrollments/Edit/$id").Version;$f.Grade='B+';Check ((Send POST '/Enrollments/SaveEnrollment' $f).Status -eq 302) 'Grade correction saved through HTTP'
 Check ([Net.WebUtility]::HtmlDecode((Send GET "/Enrollments/Details/$id").Body).Contains('B+')) 'Saved grade displayed in details'
 $f.Version=(Form "/Enrollments/Edit/$id").Version;$f.Grade='';$f.Status='Withdrawn';Check ((Send POST '/Enrollments/SaveEnrollment' $f).Status -eq 422) 'Completed history cannot be withdrawn'
 $delete=Form '/Enrollments/Create';$delete.Id=$id;$delete.Version=(Form "/Enrollments/Edit/$id").Version;Check ((Send POST "/Enrollments/Delete/$id" $delete).Status -eq 422) 'Completed history cannot be deleted'
 $f.Status='Completed';$f.Grade=$original;$f.Version=(Form "/Enrollments/Edit/$id").Version;Check ((Send POST '/Enrollments/SaveEnrollment' $f).Status -eq 302) 'Original fictional fixture grade restored'
 $list=Send GET '/Courses?q=CS201';$cid=[regex]::Match($list.Body,'class="record-title" href="/Courses/Details/(\d+)"').Groups[1].Value
 $page=Send GET "/Courses/Edit/$cid";$course=Form "/Courses/Edit/$cid";foreach($key in @('Code','Title','Credits','Capacity')){$course[$key]=Value $page.Body $key};$course.Department='Computer Science';$course.Status='Active';$originalCapacity=$course.Capacity
 $course.Capacity=1;Check ((Send POST '/Courses/SaveCourse' $course).Status -eq 422) 'Cannot reduce capacity below enrolled count'
 $course.Capacity=$originalCapacity;$course.Credits=7;Check ((Send POST '/Courses/SaveCourse' $course).Status -eq 422) 'Credit hours upper bound enforced'
 $duplicate=Form '/Students/Create';$duplicate.Number='QA-DUP-EMAIL';$duplicate.Name='Duplicate Fixture';$duplicate.Email='mira.ashford@students.example.edu';$duplicate.Department='Business';$duplicate.Year=1;$duplicate.Status='Active'
 Check ((Send POST '/Students/SaveStudent' $duplicate).Status -eq 422) 'Duplicate student email rejected'
 foreach($headers in @(@{Host='public.example.test'},@{'X-Forwarded-For'='127.0.0.1'},@{'X-Original-For'='127.0.0.1'},@{Origin='https://external.example.test'})){Check ((Send GET '/Students' $null $headers).Status -eq 403) 'Local SQL reads reject public/proxy/origin request context'}
 Write-Output "VERIFIED $script:checks grade, validation and local-read privacy HTTP checks."
}finally{$client.Dispose();$handler.Dispose()}
