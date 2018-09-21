# .net-core-datatable
Microsoft Asp.Net Core server-side support and helpers for jQuery DataTables

Datatables.AspNet
Formely known as DataTables.Mvc, this project startedwith small objectives around 2018, aiming to provide intermediate and experienced developers a tool to avoid the boring process of handling DataTables parameters.
More than a year later and after a full rewrite we are now proud to support Asp.net MVC, WebApi and Asp.Net Core (full .NET Core support).
Unit-testing is a priority to avoid breaking your app and every stable release should provide better and wider test cases.
Stable 2.0.0 version is here!
`2.0.0` stable release now ships with full support for DotNet Core 1.0.0, along with extensions, tests and all the fun we can get. This is the first stable version for `DataTables.AspNet`. We dropped the full migration path because we made everything clean and simple and included some basic usage samples to guide you.
Standard NuGet packages
DataTables.AspNet.Mvc5 with support for Mvc5, registration and automatic binders

DataTables.AspNet.WebApi2 with support for WebApi2, registration and automatic binders

DataTables.AspNet.AspNetCore with support for AspNetCore, dependency injection and automatic binders

IMPORTANT: Deprecated (unlisted) package
DataTables.AspNet.AspNet5
This package has been replaced by DataTables.AspNet.AspNetCore due to Microsoft renaming of the new platform.

Write your own code!
DataTables.AspNet ships with a core project called DataTables.AspNet.Core, which contains basic interfaces and core elements just the way DataTables needs.
Feel free to use it and implement your own classes, methods and extend DataTables.AspNet in your very own way.

Helpers and extensions
DataTables.AspNet.Extensions.AnsiSql enables basic translation from sort and filter into ANSI-SQL WHERE and ORDER BY

DataTables.AspNet.Extensions.DapperExtensions transforms filters into IPredicate and sort into ISort

Those are still alpha1 releases but with nuget packages available. There are no tests yet, they are in a very initial phase and might change a bit in the near future. After they become stable I'll accept pull requests for other extensions (eg: NHibernate, EntityFramework, etc). For now, keep in mind that these two are supposed to set the basic extension standard for DataTables.AspNet.Extensions.

Samples
Samples are provided on the `samples` folder.
There is no wiki yet. I will start writing a very gorgeous wiki, just don't know when. Tons of work and no time. Sorry. I am open to contributors :)
Eager for some new code?
If you are, check out [dev](https://github.com/ghadage70/.net-core-datatable.git) branch. It has the latest code for DataTables.AspNet, including samples and more.
For every release (even unstable ones) there should be a nuget package.
Stable code?
For production code, I do recommend the `master` branch. It holds the stable version. Every stable version has a stable Nuget release.
Still legacy?
Drop it!
2.0.0 (stable) is faster, better coded and fully tested. DataTables.Mvc is now completely discontinued.
Known issues
- There are some issues while trying to run all tests simultaneously. I'll try to fix that by including some test ordering. - Extension methods do not have tests yet and should not be used on production code.

 Example : -


         [HttpPost("[action]")]
        [AllowAnonymous]
        public IActionResult DashBoardModuleGrid([FromForm] DataTables.AspNet.Core.IDataTablesRequest request)
        {
            var data = _ModuleMasterRepository.GetAll();
            var response = data.ToDataSourceResult(request, x => new Admin_ModuleMaster
            {
                PkId = x.PkId,
                ModuleName = x.ModuleName,
                BaseUrl = x.BaseUrl,
                IsActive = x.IsActive,
            });
            return new DataTablesJsonResult(response, true);
        }

Front End Example:

        $("#m_table_1").DataTable({
            responsive: !0,
            searching: false,
            lengthChange: false,
            processing: true,
            serverSide: true,
            pageLength: 50,
            ajax: {
                url: Url,
                type: "POST",
                datatype: "json",
            },
            columns: [{
                data: "PkId",
                defaultContent: "",
                searchable: false,
                visible: false,
            },
            {
                data: "Icon",
                defaultContent: "",
                searchable: false,
                orderable: false,
            }, {
                data: "ModuleName",
                defaultContent: "",
                searchable: false
            }
            ],
        })
    
Add In Startup.cs

services.RegisterDataTables(ctx =>
            {
                var appJson = ctx.ValueProvider.GetValue("data").FirstValue ?? "{}";
                return JsonConvert.DeserializeObject<IDictionary<string, object>>(appJson);
            }, true);
