using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Xml;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlEmitContext : XamlXEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public bool EnableIlVerification { get; }

        public XamlEmitContext(IXamlILEmitter emitter, XamlTransformerConfiguration configuration,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings,
            XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> runtimeContext,
            IXamlLocal contextLocal,
            Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createSubType, IFileSource file, IEnumerable<object> emitters)
            : base(emitter, configuration, emitMappings, runtimeContext,
                contextLocal, createSubType, file, emitters)
        {
            EnableIlVerification = configuration.GetExtra<ILEmitContextSettings>().EnableILVerification;
        }
        
        protected override XamlILNodeEmitResult EmitNode(IXamlAstNode value, IXamlILEmitter codeGen)
        {
            CheckingIlEmitter parent = null;
            CheckingIlEmitter checkedEmitter = null;
            if (EnableIlVerification)
            {
                parent = codeGen as CheckingIlEmitter;

                parent?.Pause();
                checkedEmitter = new CheckingIlEmitter(codeGen);
            }
#if XAMLX_DEBUG
            var res = base.EmitNode(value, checkedEmitter);
#else
            var res = base.EmitNode(value, codeGen);
#endif
            if (EnableIlVerification)
            {
                var expectedBalance = res.ProducedItems - res.ConsumedItems;
                var checkResult =
                    checkedEmitter.Check(res.ProducedItems - res.ConsumedItems, false);
                if (checkResult != null)
                    throw new XamlLoadException($"Error during IL verification: {checkResult}\n{checkedEmitter}\n",
                        value);
                parent?.Resume();
                parent?.ExplicitStack(expectedBalance);
            }

            return res;
        }

        protected override XamlILNodeEmitResult EmitNodeCore(IXamlAstNode value, IXamlILEmitter codeGen, out bool foundEmitter)
        {
            if(File!=null)
                codeGen.InsertSequencePoint(File, value.Line, value.Position);

            return base.EmitNodeCore(value, codeGen, out foundEmitter);
        }

        protected override void EmitConvert(IXamlAstNode value, IXamlILEmitter codeGen, IXamlType expectedType, IXamlType returnedType)
        {
            XamlLocalsPool.PooledLocal local = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            // Value is assigned inside the closure in certain conditions

            ILEmitHelpers.EmitConvert(this, value, returnedType, expectedType, ldaddr =>
            {
                if (ldaddr && returnedType.IsValueType)
                {
                    // We need to store the value to a temporary variable, since *address*
                    // is required (probably for  method call on the value type)
                    local = GetLocal(returnedType);
                    codeGen
                        .Stloc(local.Local)
                        .Ldloca(local.Local);

                }
                // Otherwise do nothing, value is already at the top of the stack
                return codeGen;
            });
            local?.Dispose();
        }

        public override void StoreLocal(XamlAstCompilerLocalNode node, IXamlILEmitter codeGen)
        {
            codeGen.Stloc(TryGetLocalForNode(node, codeGen, false));
        }

        public override void LoadLocalValue(XamlAstCompilerLocalNode node, IXamlILEmitter codeGen)
        {
            codeGen.Stloc(TryGetLocalForNode(node, codeGen, true));
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class ILEmitContextSettings
    {
        public bool EnableILVerification { get; set; }
    }
}
