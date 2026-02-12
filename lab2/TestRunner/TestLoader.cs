using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace TestRunner
{
    public class TestLoader : IDisposable
    {
        private CustomAssemblyLoadContext _context;
        private WeakReference _weakRef;

        public Assembly LoadAssembly(string path)
        {
            _context = new CustomAssemblyLoadContext(path);
            _weakRef = new WeakReference(_context, trackResurrection: true);
            return _context.LoadFromAssemblyPath(path);
        }

        public void Unload()
        {
            _context?.Unload();
            _context = null;
        }

        public bool IsAlive => _weakRef != null && _weakRef.IsAlive;

        public void Dispose()
        {
            Unload();
        }

        private class CustomAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly string _resolverPath;
            public CustomAssemblyLoadContext(string mainAssemblyPath) : base(isCollectible: true)
            {
                _resolverPath = Path.GetDirectoryName(mainAssemblyPath);
            }
            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (assemblyName.Name == "TestFramework") return null;
                string p = Path.Combine(_resolverPath, assemblyName.Name + ".dll");
                return File.Exists(p) ? LoadFromAssemblyPath(p) : null;
            }
        }
    }
}