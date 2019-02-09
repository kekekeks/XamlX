using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform.Emitters;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlILCompiler
    {
        private readonly XamlTransformerConfiguration _configuration;
        public List<IXamlAstTransformer> Transformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstTransformer> SimplificationTransformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstNodeEmitter> Emitters { get; } = new List<IXamlAstNodeEmitter>();
        public XamlILCompiler(XamlTransformerConfiguration configuration, bool fillWithDefaults)
        {
            _configuration = configuration;
            if (fillWithDefaults)
            {
                Transformers = new List<IXamlAstTransformer>
                {
                    new XamlKnownDirectivesTransformer(),
                    new XamlIntrinsicsTransformer(),
                    new XamlXArgumentsTransformer(),
                    new XamlTypeReferenceResolver(),
                    new XamlPropertyReferenceResolver(),
                    new XamlXStructConvertTransformer(),
                    new XamlNewObjectTransformer(),
                    new XamlXXamlPropertyValueTransformer(),
                    new XamlDeferredContentTransformer(),
                    new XamlTopDownInitializationTransformer(),
                };
                SimplificationTransformers = new List<IXamlAstTransformer>
                {
                    new XamlFlattenTransformer()
                };
                Emitters = new List<IXamlAstNodeEmitter>()
                {
                    new NewObjectEmitter(),
                    new TextNodeEmitter(),
                    new MethodCallEmitter(),
                    new PropertyAssignmentEmitter(),
                    new PropertyValueManipulationEmitter(),
                    new ManipulationGroupEmitter(),
                    new ValueWithManipulationsEmitter(),
                    new MarkupExtensionEmitter(),
                    new ObjectInitializationNodeEmitter()
                };
            }
        }

        public void Transform(XamlDocument doc,
            Dictionary<string, string> namespaceAliases, bool strict = true)
        {
            var ctx = new XamlAstTransformationContext(_configuration, namespaceAliases, strict);

            var root = doc.Root;
            foreach (var transformer in Transformers)
            {
                root = root.Visit(n => transformer.Transform(ctx, n));
                foreach (var simplifier in SimplificationTransformers)
                    root = root.Visit(n => simplifier.Transform(ctx, n));
            }

            doc.Root = root;
        }


        /// <summary>
        ///         /// T Build(IServiceProvider sp); 
        /// </summary>


        XamlEmitContext InitCodeGen(Func<string, IXamlType, IXamlTypeBuilder> createSubType,
            IXamlILEmitter codeGen, XamlContext context, bool needContextLocal)
        {
            IXamlLocal contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.DefineLocal(context.ContextType);
                codeGen
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, context.Constructor)
                    .Emit(OpCodes.Stloc, contextLocal);
            }

            var emitContext = new XamlEmitContext(codeGen, _configuration, context, contextLocal, createSubType, Emitters);
            return emitContext;
        }
        
        void CompileBuild(IXamlAstValueNode rootInstance, Func<string, IXamlType, IXamlTypeBuilder> createSubType,
            IXamlILEmitter codeGen, XamlContext context, IXamlMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
            var emitContext = InitCodeGen(createSubType, codeGen, context, needContextLocal);


            var rv = codeGen.DefineLocal(rootInstance.Type.GetClrType());
            emitContext.Emit(rootInstance, codeGen, rootInstance.Type.GetClrType());
            codeGen
                .Emit(OpCodes.Stloc, rv)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldloc, rv)
                .Emit(OpCodes.Call, compiledPopulate)
                .Emit(OpCodes.Ldloc, rv)
                .Emit(OpCodes.Ret);
        }

        /// <summary>
        /// void Populate(IServiceProvider sp, T target);
        /// </summary>

        void CompilePopulate(IXamlAstManipulationNode manipulation, Func<string, IXamlType, IXamlTypeBuilder> createSubType, IXamlILEmitter codeGen, XamlContext context)
        {
            var emitContext = InitCodeGen(createSubType, codeGen, context, true);

            codeGen
                .Emit(OpCodes.Ldloc, emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.RootObjectField)
                .Emit(OpCodes.Ldarg_1);
            emitContext.Emit(manipulation, codeGen, null);
            codeGen.Emit(OpCodes.Ret);
        }

        public void Compile(IXamlAstNode root, IXamlTypeBuilder typeBuilder, XamlContext contextType,
            string populateMethodName, string createMethodName)
        {
            var rootGrp = (XamlValueWithManipulationNode) root;
            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);

            IXamlTypeBuilder CreateSubType(string name, IXamlType baseType) 
                => typeBuilder.DefineSubType(baseType, name, false);

            CompilePopulate(rootGrp.Manipulation, CreateSubType, populateMethod.Generator, contextType);

            var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
            CompileBuild(rootGrp.Value, CreateSubType, createMethod.Generator, contextType, populateMethod);
        }
    }


    public interface IXamlAstTransformer
    {
        IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node);
    }

    public class XamlNodeEmitResult
    {
        public IXamlType ReturnType { get; set; }
        public bool AllowCast { get; set; }

        public XamlNodeEmitResult(IXamlType returnType = null)
        {
            ReturnType = returnType;
        }
        public static XamlNodeEmitResult Void { get; } = new XamlNodeEmitResult();
        public static XamlNodeEmitResult Type(IXamlType type) => new XamlNodeEmitResult(type);
    }
    
    public interface IXamlAstNodeEmitter
    {
        XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen);
    }

    public interface IXamlAstEmitableNode
    {
        XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen);
    }
    
}