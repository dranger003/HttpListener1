using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        static int SwitchDomain(AppDomain primary)
        {
            var setup = new AppDomainSetup();
            setup.ApplicationBase = primary.SetupInformation.ApplicationBase;

            var secondary = AppDomain.CreateDomain(
                "SecondaryDomain",
                null,
                primary.SetupInformation,
                new PermissionSet(PermissionState.Unrestricted),
                new StrongName[0]
            );

            var result = secondary.ExecuteAssembly(Assembly.GetExecutingAssembly().Location);
            AppDomain.Unload(secondary);

            return result;
        }

        static int Main(string[] args)
        {
            var domain = AppDomain.CurrentDomain;
            if (domain.IsDefaultAppDomain())
            {
                Console.WriteLine("Switching to secondary AppDomain...");
                return SwitchDomain(domain);
            }

            Console.WriteLine("Now running in secondary AppDomain.\n");

            var addresses = new List<IPAddress>();
            addresses.Add(IPAddress.Loopback);
            addresses.AddRange(Dns.GetHostAddresses("").Where(x => x.AddressFamily == AddressFamily.InterNetwork));

            using (var listener = new HttpListener())
            {
                foreach (var address in addresses)
                {
                    var url = String.Format("http://{0}:12345/", address);
                    listener.Prefixes.Add(url);
                    Console.WriteLine("{0}", url);
                }

                listener.Start();
                Console.WriteLine();

                while (true)
                {
                    var context = listener.GetContext();

                    var request = context.Request;
                    var response = context.Response;

                    Console.WriteLine("{0}", request.RawUrl);

                    if (request.RawUrl == "/")
                    {
                        using (var reader = new StreamReader(File.OpenRead("index.cshtml")))
                        {
                            response.ContentEncoding = Encoding.UTF8;
                            response.ContentType = "text/html";

                            using (var writer = new StreamWriter(response.OutputStream, response.ContentEncoding))
                                writer.Write(RazorEngine.Engine.Razor.RunCompile(reader.ReadToEnd(), "templateKey", null, new { Title = "HttpListener1" }));
                        }
                    }
                    else
                    {
                        response.StatusCode = 404;
                        response.Close();
                    }
                }
            }

            return 0;
        }
    }
}
