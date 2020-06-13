using System;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    static class CecilHelpers
    {
        [ThreadStatic] private static int _recursionDepth;
        public static TypeReference TransformGeneric(this TypeReference reference, TypeReference declaringType)
        {
            try
            {
                _recursionDepth++;
                if (_recursionDepth > 50)
                    throw new StackOverflowException();
                // I'm most likely missing something here...
                if (declaringType is GenericInstanceType inst)
                {
                    if (reference is GenericParameter g)
                    {
                        var ga = inst.GenericArguments[g.Position];
                        if (ga == g)
                            return ga;
                        return ga.TransformGeneric(declaringType);
                    }
                    else if (reference is GenericInstanceType gref)
                    {
                        GenericInstanceType clone = null;
                        for (var c = 0; c < gref.GenericArguments.Count; c++)
                        {
                            var arg = gref.GenericArguments[c];
                             if (arg is GenericParameter genericParameter
                                && CecilHelpers.Equals(genericParameter.DeclaringType, inst.ElementType))
                            {
                                if (clone == null)
                                {
                                    clone = new GenericInstanceType(gref.ElementType);
                                    foreach (var orig in gref.GenericArguments)
                                        clone.GenericArguments.Add(orig);
                                }

                                clone.GenericArguments[c] = inst.GenericArguments[genericParameter.Position];

                            }
                        }

                        return clone ?? gref;
                    }
                }

                return reference;
            }

            finally
            {
                _recursionDepth--;
            }
        }

        [ThreadStatic]
        private static int _depth;
        public static bool Equals(TypeReference left, TypeReference right)
        {
            _depth++;
            if(_depth>200)
                throw new StackOverflowException();
            var res = EqualsCore(left, right);
            _depth--;
            return res;

        }
        static bool EqualsCore(TypeReference left, TypeReference right)
        {
            if (left == null && right == null)
                return true;
            if (left == null || right == null)
                return false;


            if (left.GetType() == typeof(TypeReference))
                left = left.Resolve();
            if (right.GetType() == typeof(TypeReference))
                right = right.Resolve();
            
            if (left.GetType() != right.GetType())
                return false;
            // Direct
            if (left is TypeDefinition)
                return left == right;
            if (left is GenericParameter leftGp && right is GenericParameter rightGp)
            {
                return
                    leftGp.Name == rightGp.Name
                    && leftGp.DeclaringType?.Resolve() == rightGp.DeclaringType?.Resolve()
                    && leftGp.DeclaringMethod == rightGp.DeclaringMethod;
            }
            if (left is GenericInstanceType leftGi && right is GenericInstanceType rightGi)
            {
                for(var c=0; c<leftGi.GenericArguments.Count;c++)
                    if (!Equals(leftGi.GenericArguments[c], rightGi.GenericArguments[c]))
                        return false;
                return true;
            }
            if (!Equals(left.GetElementType(), right.GetElementType()))
                return false;
            if (left is ArrayType leftA && right is ArrayType rightA)
                return leftA.Rank == rightA.Rank;
            if (left is FunctionPointerType leftFp && right is FunctionPointerType rightFp)
                return leftFp.Namespace == rightFp.Namespace && leftFp.Name == rightFp.Name;
            if (left.IsPointer || left.IsByReference)
                return true;



            // No idea what to do with the rest
            return false;
        }
    }
}