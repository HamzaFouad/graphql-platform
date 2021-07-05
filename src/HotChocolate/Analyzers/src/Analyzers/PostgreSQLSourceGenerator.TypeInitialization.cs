using System;
using System.Reflection;
using static System.IO.Path;

namespace HotChocolate.Analyzers
{
    public partial class PostgreSQLSourceGenerator
    {
        private const string _dll = ".dll";
        private static string _location =
            GetDirectoryName(typeof(PostgreSQLSourceGenerator).Assembly.Location)!;

        static PostgreSQLSourceGenerator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        private static Assembly? CurrentDomainOnAssemblyResolve(
            object sender,
            ResolveEventArgs args)
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name);
                var path = Combine(_location, assemblyName.Name + _dll);
                return Assembly.LoadFrom(path);
            }
            catch
            {
                return null;
            }
        }
    }
}
