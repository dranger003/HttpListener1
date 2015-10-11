using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;

using RazorEngine.Templating;

namespace HttpListener1
{
    class Program
    {
        static int Main(string[] args)
        {
            var domain0 = AppDomain.CurrentDomain;
            if (domain0.IsDefaultAppDomain())
            {
                Console.WriteLine("Spawning secondary AppDomain...");

                var setup = new AppDomainSetup();
                setup.ApplicationBase = domain0.SetupInformation.ApplicationBase;

                var domain1 = AppDomain.CreateDomain(
                    "Domain1",
                    null,
                    domain0.SetupInformation,
                    new PermissionSet(PermissionState.Unrestricted),
                    new StrongName[0]
                );

                var result = domain1.ExecuteAssembly(Assembly.GetExecutingAssembly().Location);
                AppDomain.Unload(domain1);

                return result;
            }

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add("http://localhost:12345/");
                listener.Start();

                var context = listener.GetContext();

                var request = context.Request;
                Console.WriteLine(request.Url);

                var html = "";
                using (var reader = new StreamReader(File.OpenRead("index.cshtml")))
                    html = RazorEngine.Engine.Razor.RunCompile(reader.ReadToEnd(), "templateKey", null, new { Title = "HttpListener1" });

                var data = Encoding.UTF8.GetBytes(html);
                var response = context.Response;
                response.ContentLength64 = data.Length;

                using (var stream = response.OutputStream)
                    stream.Write(data, 0, data.Length);
            }

            Console.ReadKey(true);

            return 0;
        }
    }
}
