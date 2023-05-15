using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using XamlX.IL;

namespace XamlX.TypeSystem
{
    #if !XAMLX_CECIL_INTERNAL
    public
    #endif
    partial class CecilTypeSystem : IXamlTypeSystem,  IAssemblyResolver
    {
        private List<CecilAssembly> _asms = new List<CecilAssembly>();
        private Dictionary<string, CecilAssembly> _assemblyCache = new Dictionary<string, CecilAssembly>();
        private Dictionary<TypeReference, IXamlType> _typeReferenceCache = new Dictionary<TypeReference, IXamlType>();
        private Dictionary<AssemblyDefinition, CecilAssembly> _assemblyDic 
            = new Dictionary<AssemblyDefinition, CecilAssembly>();
        private Dictionary<string, IXamlType> _unresolvedTypeCache = new Dictionary<string, IXamlType>();
        
        private CustomMetadataResolver _resolver;
        private CecilTypeCache _typeCache;
        public void Dispose()
        {
            foreach (var asm in _asms)
                asm.Assembly.Dispose();
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name) => ResolveWrapped(name, true)?.Assembly;
        public AssemblyNameReference CoerceReference(AssemblyNameReference name) => ResolveWrapped(name, false)?.Assembly?.Name ?? name;
        private CecilAssembly ResolveWrapped(AssemblyNameReference name, bool throwOnNotFound)
        {
            if (_assemblyCache.TryGetValue(name.FullName, out var rv))
                return rv;
            foreach (var asm in _asms)
                if (asm.Assembly.Name.Equals(name))
                    return _assemblyCache[name.FullName] = asm;
            foreach (var asm in _asms)
                if (asm.Assembly.Name.Name == name.Name)
                    return _assemblyCache[name.FullName] = asm;
            return throwOnNotFound ? throw new AssemblyResolutionException(name) : null;
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => Resolve(name);

        public CecilTypeSystem(IEnumerable<string> paths, string targetPath = null)
        {
            if (targetPath != null)
                paths = paths.Concat(new[] {targetPath});
            _resolver = new CustomMetadataResolver(this);
            _typeCache = new CecilTypeCache(this);
            foreach (var path in paths.Distinct())
            {
                var isTarget = path == targetPath;
                var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters(ReadingMode.Deferred)
                {
                    ReadWrite = isTarget,
                    InMemory = true,
                    AssemblyResolver = this,
                    MetadataResolver = _resolver,
                    ThrowIfSymbolsAreNotMatching = false,
                    SymbolReaderProvider = isTarget ? new DefaultSymbolReaderProvider(false) : null,
                    ApplyWindowsRuntimeProjections = false,
                    MetadataImporterProvider = new CecilMetadataImporterProvider(this)
                });

                var wrapped = RegisterAssembly(asm);
                if (path == targetPath)
                {
                    TargetAssembly = wrapped;
                    TargetAssemblyDefinition = asm;
                }
            }    
        }

        public IXamlAssembly TargetAssembly { get; private set; }
        public AssemblyDefinition TargetAssemblyDefinition { get; private set; }
        public IEnumerable<IXamlAssembly> Assemblies => _asms.AsReadOnly();
        public IXamlAssembly FindAssembly(string name) => _asms.FirstOrDefault(a => a.Assembly.Name.Name == name);

        public IXamlType FindType(string name)
        {
            foreach (var asm in _asms)
            {
                var found = asm.FindType(name);
                if (found != null)
                    return found;
            }
            return null;
        }

        public IXamlType FindType(string name, string assembly) 
            => FindAssembly(assembly)?.FindType(name);


        public TypeReference GetTypeReference(IXamlType t) => ((ITypeReference)t).Reference;
        public MethodReference GetMethodReference(IXamlMethod t) => ((CecilMethod)t).IlReference;
        public MethodReference GetMethodReference(IXamlConstructor t) => ((CecilConstructor)t).IlReference;

        CecilAssembly FindAsm(AssemblyDefinition d)
        {
            _assemblyDic.TryGetValue(d, out var asm);
            return asm;
        }

        static string GetTypeReferenceKey(TypeReference reference)
        {
            if (reference is GenericParameter gp)
            {
                if (gp.Owner is TypeReference tr)
                    return tr.FullName + "|GenericParameter|" + reference.FullName;
                else if (gp.Owner is MethodReference mr)
                    return GetTypeReferenceKey(mr.DeclaringType) + mr.FullName + "|GenericParameter|" +
                           reference.FullName;
                else 
                    throw new ArgumentException("Unable to get key for " + gp.Owner.GetType().FullName);
            }

            return reference.FullName;
        }
        
        IXamlType Resolve(TypeReference reference)
        {
            if (!_typeReferenceCache.TryGetValue(reference, out var rv))
            {

                TypeDefinition resolved = null;
                try
                {
                    resolved = reference.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    
                }

                if (resolved != null)
                {
                    rv = _typeCache.Get(reference);
                }
                else
                {
                    var key = GetTypeReferenceKey(reference);

                    if (!_unresolvedTypeCache.TryGetValue(key, out rv))
                        _unresolvedTypeCache[key] =
                            rv = new UnresolvedCecilType(reference);
                }
                _typeReferenceCache[reference] = rv;
            }
            return rv;
        }

        public IXamlAssembly RegisterAssembly(AssemblyDefinition asm)
        {
            var wrapped = new CecilAssembly(this, asm);
            _asms.Add(wrapped);
            _assemblyDic[asm] = wrapped;
            return wrapped;
        }
        
        public AssemblyDefinition CreateAndRegisterAssembly(string name, Version version, ModuleKind kind)
        {
            var def = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(name, version), name,
                new ModuleParameters()
                {
                    AssemblyResolver = this,
                    MetadataResolver = this._resolver,
                    Kind = kind
                });
            RegisterAssembly(def);
            return def;
        }

        private IXamlMethod Resolve(MethodDefinition method, TypeReference declaringType)
        {
            return new CecilMethod(this, method, declaringType);
        }

        private CecilType GetTypeFor(TypeReference reference) => _typeCache.Get(reference);

        interface ITypeReference
        {
            TypeReference Reference { get; }
        }

        public IXamlTypeBuilder<IXamlILEmitter> CreateTypeBuilder(TypeDefinition def)
        {
            return new CecilTypeBuilder(this, FindAsm(def.Module.Assembly), def);
        }

        public AssemblyDefinition GetAssembly(IXamlAssembly asm)
            => ((CecilAssembly) asm).Assembly;
    }
}
