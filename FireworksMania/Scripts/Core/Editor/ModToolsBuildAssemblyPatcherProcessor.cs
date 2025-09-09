using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UMod.Shared.Resources;
using UnityEngine;

namespace FireworksMania.Core.Editor
{
    // Add a build processor for script assets since we are only interested in scripts that will be compiled by uMod.
    // High priority means that it will run after script compilation so build assemblies collection will already be filled out with compiled assemblies.
    [UModBuildProcessor("")]
    public class ModToolsBuildAssemblyPatcherProcessor : BuildEngineProcessor
    {
        // Methods
        // Called by the build engine just after compilation if the mod includes any script content, passing all script assets as a parameter (Not interested in those).
        public override void ProcessAssetBatch(BuildContext context, IEnumerable<BuildPipelineAsset> assets)
        {
            // get assemblies
            ModAssemblyEntry[] entries = context.BuildAssemblies.Assemblies;

            // Patch all
            foreach (ModAssemblyEntry entry in entries)
            {
                Debug.Log($"Netcode for Gameobject CodeGen patching assembly: '{entry.AssemblyName}'");
                PatchAssembly(context, entry);
            }
        }

        private void PatchAssembly(BuildContext context, ModAssemblyEntry entry)
        {
            //Note: We can't use entry.AssemblySymbolsImage as it set wrong by Umod it seems. It's the same as the AssemblyImage. Therefore we have to read the PDB file manually.
            var umodAssemblyPdbAsBytes = File.ReadAllBytes($"{entry.AssemblyName}.pdb");
            var codeGenAssembly        = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unity.Netcode.Editor.CodeGen");

            if (codeGenAssembly == null)
                throw new Exception("Cannot find 'Unity.Netcode.Editor.CodeGen' assembly.");

            Type ilppType = codeGenAssembly.GetTypes().FirstOrDefault(t => t.Name == "NetworkBehaviourILPP");
            if (ilppType == null)
                throw new Exception("Cannot find NetworkBehaviourILPP type.");

            MethodInfo processMethod = ilppType.GetMethod("Process", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (processMethod == null)
                throw new Exception("Cannot find Process method.");

            var ilppInstance = Activator.CreateInstance(ilppType);

            // Load compiled assembly shim (ICompiledAssembly proxy)
            var compiledAssemblyProxy = LoadCompiledAssembly(entry.AssemblyName, entry.AssemblyImage, umodAssemblyPdbAsBytes);
            
            object resultObj;
            try
            {
                resultObj = processMethod.Invoke(ilppInstance, new object[] { compiledAssemblyProxy });
            }
            catch (TargetInvocationException tie)
            {
                Debug.LogException(tie.InnerException ?? tie);
                throw; // so you see the inner stack
            }

            if (resultObj == null)
            {
                Debug.Log("ILPP returned null (likely WillProcess == false). No changes were applied.");
                return;
            }

            // ---- Read ILPostProcessResult via reflection ----
            var resultType = resultObj.GetType();
            var inMemProp  = resultType.GetProperty("InMemoryAssembly");
            var diagsProp  = resultType.GetProperty("Diagnostics");

            var inMemObj = inMemProp?.GetValue(resultObj);
            if (inMemObj == null)
            {
                Console.WriteLine("ILPP returned a result but InMemoryAssembly is null. No output written.");
                DumpDiagnostics(diagsProp?.GetValue(resultObj));
                return;
            }


            // InMemoryAssembly.PeData / PdbData
            var inMemType   = inMemObj.GetType();
            var peDataProp  = inMemType.GetProperty("PeData");
            var pdbDataProp = inMemType.GetProperty("PdbData");

            var patchedUmodAssemblyAsBytes = peDataProp?.GetValue(inMemObj) as byte[];
            var pdbBytes = pdbDataProp?.GetValue(inMemObj) as byte[];

            // Check for patched
            if (patchedUmodAssemblyAsBytes == null || patchedUmodAssemblyAsBytes.Length == 0)
            {
                Console.WriteLine("InMemoryAssembly.PeData is empty. No output written.");
                DumpDiagnostics(diagsProp?.GetValue(resultObj));
                return;
            }

            // Remove original assembly
            context.BuildAssemblies.RemoveFromBuild(entry);

            // Register new assembly
            if (context.BuildAssemblies.RegisterAssemblyForBuild(patchedUmodAssemblyAsBytes, true) == false)
                Debug.LogError("Failed to register patches assembly for build! : " + entry.AssemblyName);
        }

        private static void DumpDiagnostics(object diagnosticsObj)
        {
            if (diagnosticsObj == null) return;

            // Diagnostics is IEnumerable<DiagnosticMessage>; reflect minimally
            var enumerable = diagnosticsObj as System.Collections.IEnumerable;
            if (enumerable == null) return;

            Console.WriteLine("Diagnostics from ILPP:");
            foreach (var d in enumerable)
            {
                var dt = d.GetType();
                var diagType = dt.GetProperty("DiagnosticType")?.GetValue(d)?.ToString();
                var message = dt.GetProperty("MessageData")?.GetValue(d)?.ToString()
                              ?? dt.GetProperty("Message")?.GetValue(d)?.ToString();
                Console.WriteLine($"- [{diagType}] {message}");
            }
        }

        private static object LoadCompiledAssembly(string umodAssemblyName, byte[] umodAssemblyAsByteArray, byte[] umodAssemblySymbolAsByteArray = null)
        {
            // Create the shim with DLL + optional PDB
            var shim           = new CompiledAssemblyShim(umodAssemblyName, umodAssemblyAsByteArray, umodAssemblySymbolAsByteArray);
            var commonAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unity.CompilationPipeline.Common");
            if (commonAssembly == null)
                throw new Exception("Unity.CompilationPipeline.Common.dll not loaded");

            // Find ICompiledAssembly interface
            Type interfaceType = commonAssembly.GetTypes().FirstOrDefault(t => t.FullName == "Unity.CompilationPipeline.Common.ILPostProcessing.ICompiledAssembly");
            if (interfaceType == null)
                throw new Exception("Cannot find ICompiledAssembly type in Unity.CompilationPipeline.Common.dll");
            
            // Create a proxy implementing ICompiledAssembly
            var proxy = CompiledAssemblyShimProxy.Create(shim, interfaceType);

            Console.WriteLine($"Loaded ICompiledAssembly proxy for {shim.Name}");
            return proxy;
        }

        public class CompiledAssemblyShim
        {
            public string Name         { get; }
            public byte[] Bytes        { get; }
            public string[] References { get; } = Array.Empty<string>();
            public string[] Defines    { get; } = Array.Empty<string>(); //Note: This is not referenced directly, but it have to be here for the interface to match. Same goes for all the properties here.

            // Unity type loaded at runtime
            public object InMemoryAssembly { get; }

            public CompiledAssemblyShim(string umodAssemblyName, byte[] umodAssemblyBytes, byte[] umodAssemblySymbolsAsBytes = null)
            {
                if (umodAssemblyBytes == null || umodAssemblyBytes.Length == 0)
                    throw new ArgumentNullException(nameof(umodAssemblyBytes));

                Bytes = umodAssemblyBytes;
                Name  = umodAssemblyName;

                var umodAssembly = Assembly.Load(umodAssemblyBytes);
                var referencedAssemblyLocations = new List<string>();
                foreach (var referencedAssemblyName in umodAssembly.GetReferencedAssemblies())
                {
                    var referencedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == referencedAssemblyName.Name);
                    if (referencedAssembly == null)
                        throw new InvalidOperationException($"Referenced assembly '{referencedAssemblyName.Name}' not found in AppDomain.");

                    //Debug.Log($"Found referenced assembly '{referencedAssemblyName.Name}'");
                    referencedAssemblyLocations.Add(referencedAssembly.Location);
                }

                //Note: Not 100% sure this is needed, but adding it just in case.
                var mscorlib = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "mscorlib");
                if (mscorlib != null)
                    referencedAssemblyLocations.Add(mscorlib.Location);

                References = referencedAssemblyLocations.ToArray();

                var commonAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unity.CompilationPipeline.Common");

                if (commonAssembly == null)
                    throw new InvalidOperationException("Required assembly 'Unity.CompilationPipeline.Common' not found in AppDomain.");

                var inMemoryAsmType = commonAssembly.GetType("Unity.CompilationPipeline.Common.ILPostProcessing.InMemoryAssembly", true);

                InMemoryAssembly = Activator.CreateInstance(inMemoryAsmType, umodAssemblyBytes, umodAssemblySymbolsAsBytes) ?? throw new Exception("Failed to create InMemoryAssembly instance.");
            }
        }

        public static class CompiledAssemblyShimProxy
        {
            private static readonly ModuleBuilder ModuleBuilder;

            static CompiledAssemblyShimProxy()
            {
                var asmName = new AssemblyName("DynamicCompiledAssemblyProxy");
                var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
                ModuleBuilder = asmBuilder.DefineDynamicModule("MainModule");
            }

            public static object Create(CompiledAssemblyShim shim, Type iCompiledAssemblyType)
            {
                if (shim == null) throw new ArgumentNullException(nameof(shim));
                if (iCompiledAssemblyType == null) throw new ArgumentNullException(nameof(iCompiledAssemblyType));

                string typeName = $"CompiledAssemblyProxy_{shim.Name}";
                var tb = ModuleBuilder.DefineType(
                    typeName,
                    TypeAttributes.Public | TypeAttributes.Class,
                    null,
                    new[] { iCompiledAssemblyType }
                );

                // Define a private field to hold the shim
                var shimField = tb.DefineField("_shim", typeof(CompiledAssemblyShim), FieldAttributes.Private);

                // Constructor: (CompiledAssemblyShim shim)
                var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(CompiledAssemblyShim) });
                var ilCtor = ctor.GetILGenerator();
                // base constructor
                ilCtor.Emit(OpCodes.Ldarg_0);
                ilCtor.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
                // store shim
                ilCtor.Emit(OpCodes.Ldarg_0);
                ilCtor.Emit(OpCodes.Ldarg_1);
                ilCtor.Emit(OpCodes.Stfld, shimField);
                ilCtor.Emit(OpCodes.Ret);

                // Implement all interface properties by forwarding to the shim
                foreach (var prop in iCompiledAssemblyType.GetProperties())
                {
                    var pb = tb.DefineProperty(prop.Name, PropertyAttributes.None, prop.PropertyType, null);
                    var getter = tb.DefineMethod(
                        "get_" + prop.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                        prop.PropertyType,
                        Type.EmptyTypes
                    );

                    var il = getter.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, shimField);

                    var shimProp = typeof(CompiledAssemblyShim).GetProperty(prop.Name);
                    if (shimProp == null)
                        throw new Exception($"Shim is missing property {prop.Name}");

                    il.Emit(OpCodes.Callvirt, shimProp.GetGetMethod());
                    il.Emit(OpCodes.Ret);

                    pb.SetGetMethod(getter);
                    tb.DefineMethodOverride(getter, prop.GetGetMethod());
                }

                var proxyType = tb.CreateType();
                return Activator.CreateInstance(proxyType, shim);
            }
        }
    }
}
