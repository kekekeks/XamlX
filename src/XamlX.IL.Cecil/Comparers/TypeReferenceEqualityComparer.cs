using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace XamlX.TypeSystem;

// Cecil doesn't expose these comparers in any way, and only uses them for WinMD stuff internally. So we have to copy paste them.
// https://github.com/jbevain/cecil/blob/56d4409b8a0165830565c6e3f96f41bead2c418b/Mono.Cecil/TypeReferenceEqualityComparer.cs
// Also https://github.com/jbevain/cecil/issues/389

internal sealed class TypeReferenceEqualityComparer : EqualityComparer<TypeReference>
{
	private readonly CecilTypeComparisonMode _comparisonMode;

	public TypeReferenceEqualityComparer(CecilTypeComparisonMode comparisonMode)
	{
		_comparisonMode = comparisonMode;
	}

	public override bool Equals(TypeReference? x, TypeReference? y)
	{
		return AreEqual(x, y, _comparisonMode);
	}

	public override int GetHashCode(TypeReference obj)
	{
		return GetHashCodeFor(obj, _comparisonMode);
	}

	public static bool AreEqual(TypeReference? a, TypeReference? b,
		CecilTypeComparisonMode comparisonMode)
	{
		if (ReferenceEquals(a, b))
			return true;

		if (a == null || b == null)
			return false;

		var aMetadataType = a.MetadataType;
		var bMetadataType = b.MetadataType;

		if (aMetadataType == MetadataType.GenericInstance || bMetadataType == MetadataType.GenericInstance)
		{
			if (aMetadataType != bMetadataType)
				return false;

			return AreEqual((GenericInstanceType)a, (GenericInstanceType)b, comparisonMode);
		}

		if (aMetadataType == MetadataType.Array || bMetadataType == MetadataType.Array)
		{
			if (aMetadataType != bMetadataType)
				return false;

			var a1 = (ArrayType)a;
			var b1 = (ArrayType)b;
			if (a1.Rank != b1.Rank)
				return false;

			return AreEqual(a1.ElementType, b1.ElementType, comparisonMode);
		}

		if (aMetadataType == MetadataType.Var || bMetadataType == MetadataType.Var)
		{
			if (aMetadataType != bMetadataType)
				return false;

			return AreEqual((GenericParameter)a, (GenericParameter)b, comparisonMode);
		}

		if (aMetadataType == MetadataType.MVar || bMetadataType == MetadataType.MVar)
		{
			if (aMetadataType != bMetadataType)
				return false;

			return AreEqual((GenericParameter)a, (GenericParameter)b, comparisonMode);
		}

		if (aMetadataType == MetadataType.ByReference || bMetadataType == MetadataType.ByReference)
		{
			if (aMetadataType != bMetadataType)
				return false;

			return AreEqual(((ByReferenceType)a).ElementType, ((ByReferenceType)b).ElementType, comparisonMode);
		}

		if (aMetadataType == MetadataType.Pointer || bMetadataType == MetadataType.Pointer)
		{
			if (aMetadataType != bMetadataType)
				return false;

			return AreEqual(((PointerType)a).ElementType, ((PointerType)b).ElementType, comparisonMode);
		}

		if (aMetadataType == MetadataType.RequiredModifier || bMetadataType == MetadataType.RequiredModifier)
		{
			if (aMetadataType != bMetadataType)
				return false;

			var a1 = (RequiredModifierType)a;
			var b1 = (RequiredModifierType)b;

			return AreEqual(a1.ModifierType, b1.ModifierType, comparisonMode) &&
			       AreEqual(a1.ElementType, b1.ElementType, comparisonMode);
		}

		if (aMetadataType == MetadataType.OptionalModifier || bMetadataType == MetadataType.OptionalModifier)
		{
			if (aMetadataType != bMetadataType)
				return false;

			var a1 = (OptionalModifierType)a;
			var b1 = (OptionalModifierType)b;

			return AreEqual(a1.ModifierType, b1.ModifierType, comparisonMode) &&
			       AreEqual(a1.ElementType, b1.ElementType, comparisonMode);
		}

		if (aMetadataType == MetadataType.Pinned || bMetadataType == MetadataType.Pinned)
		{
			if (aMetadataType != bMetadataType)
				return false;

			return AreEqual(((PinnedType)a).ElementType, ((PinnedType)b).ElementType, comparisonMode);
		}

		if (aMetadataType == MetadataType.Sentinel || bMetadataType == MetadataType.Sentinel)
		{
			if (aMetadataType != bMetadataType)
				return false;

			return AreEqual(((SentinelType)a).ElementType, ((SentinelType)b).ElementType, comparisonMode);
		}

		if (!a.Name.Equals(b.Name, StringComparison.Ordinal) ||
			!a.Namespace.Equals(b.Namespace, StringComparison.Ordinal))
			return false;

		var xDefinition = comparisonMode == CecilTypeComparisonMode.Exact ? a.Resolve() : a as TypeDefinition;
		var yDefinition = comparisonMode == CecilTypeComparisonMode.Exact ? b.Resolve() : b as TypeDefinition;

		if (xDefinition is not null && yDefinition is not null)
		{
			if (xDefinition.Module.Name != yDefinition.Module.Name)
				return false;

			if (xDefinition.Module.Assembly.Name.Name != yDefinition.Module.Assembly.Name.Name)
				return false;

			return xDefinition.FullName == yDefinition.FullName;
		}

		return true;
	}

	static bool AreEqual(GenericParameter a, GenericParameter b,
		CecilTypeComparisonMode comparisonMode)
	{
		if (ReferenceEquals(a, b))
			return true;

		if (a.Position != b.Position)
			return false;

		if (a.Type != b.Type)
			return false;

		var aOwnerType = a.Owner as TypeReference;
		if (aOwnerType != null && AreEqual(aOwnerType, b.Owner as TypeReference, comparisonMode))
			return true;

		var aOwnerMethod = a.Owner as MethodReference;
		if (aOwnerMethod != null && comparisonMode != CecilTypeComparisonMode.SignatureOnlyLoose &&
		    MethodReferenceEqualityComparer.AreEqual(aOwnerMethod, b.Owner as MethodReference, comparisonMode))
			return true;

		return comparisonMode == CecilTypeComparisonMode.SignatureOnly ||
		       comparisonMode == CecilTypeComparisonMode.SignatureOnlyLoose;
	}

	static bool AreEqual(GenericInstanceType a, GenericInstanceType b,
		CecilTypeComparisonMode comparisonMode)
	{
		if (ReferenceEquals(a, b))
			return true;

		var aGenericArgumentsCount = a.GenericArguments.Count;
		if (aGenericArgumentsCount != b.GenericArguments.Count)
			return false;

		if (!AreEqual(a.ElementType, b.ElementType, comparisonMode))
			return false;

		for (int i = 0; i < aGenericArgumentsCount; i++)
			if (!AreEqual(a.GenericArguments[i], b.GenericArguments[i], comparisonMode))
				return false;

		return true;
	}

	public static int GetHashCodeFor(TypeReference obj, CecilTypeComparisonMode comparisonMode)
	{
		// a very good prime number
		const int hashCodeMultiplier = 486187739;
		// prime numbers
		const int genericInstanceTypeMultiplier = 31;
		const int byReferenceMultiplier = 37;
		const int pointerMultiplier = 41;
		const int requiredModifierMultiplier = 43;
		const int optionalModifierMultiplier = 47;
		const int pinnedMultiplier = 53;
		const int sentinelMultiplier = 59;
		const int moduleMultiplier = 61;
		const int assemblyMultiplier = 67;

		var metadataType = obj.MetadataType;

		if (metadataType == MetadataType.GenericInstance)
		{
			var genericInstanceType = (GenericInstanceType)obj;
			var hashCode = GetHashCodeFor(genericInstanceType.ElementType, comparisonMode) * hashCodeMultiplier +
			               genericInstanceTypeMultiplier;
			for (var i = 0; i < genericInstanceType.GenericArguments.Count; i++)
				hashCode = hashCode * hashCodeMultiplier + GetHashCodeFor(genericInstanceType.GenericArguments[i], comparisonMode);
			return hashCode;
		}

		if (metadataType == MetadataType.Array)
		{
			var arrayType = (ArrayType)obj;
			return GetHashCodeFor(arrayType.ElementType, comparisonMode) * hashCodeMultiplier + arrayType.Rank.GetHashCode();
		}

		if (metadataType == MetadataType.Var || metadataType == MetadataType.MVar)
		{
			var genericParameter = (GenericParameter)obj;
			var hashCode = genericParameter.Position.GetHashCode() * hashCodeMultiplier +
			               ((int)metadataType).GetHashCode();

			var ownerTypeReference = genericParameter.Owner as TypeReference;
			if (ownerTypeReference != null)
				return hashCode * hashCodeMultiplier + GetHashCodeFor(ownerTypeReference, comparisonMode);

			var ownerMethodReference = genericParameter.Owner as MethodReference;
			if (ownerMethodReference != null)
				return hashCode * hashCodeMultiplier + MethodReferenceEqualityComparer.GetHashCodeFor(ownerMethodReference, comparisonMode);

			throw new InvalidOperationException("Generic parameter encountered with invalid owner");
		}

		if (metadataType == MetadataType.ByReference)
		{
			var byReferenceType = (ByReferenceType)obj;
			return GetHashCodeFor(byReferenceType.ElementType, comparisonMode) * hashCodeMultiplier * byReferenceMultiplier;
		}

		if (metadataType == MetadataType.Pointer)
		{
			var pointerType = (PointerType)obj;
			return GetHashCodeFor(pointerType.ElementType, comparisonMode) * hashCodeMultiplier * pointerMultiplier;
		}

		if (metadataType == MetadataType.RequiredModifier)
		{
			var requiredModifierType = (RequiredModifierType)obj;
			var hashCode = GetHashCodeFor(requiredModifierType.ElementType, comparisonMode) * requiredModifierMultiplier;
			hashCode = hashCode * hashCodeMultiplier + GetHashCodeFor(requiredModifierType.ModifierType, comparisonMode);
			return hashCode;
		}

		if (metadataType == MetadataType.OptionalModifier)
		{
			var optionalModifierType = (OptionalModifierType)obj;
			var hashCode = GetHashCodeFor(optionalModifierType.ElementType, comparisonMode) * optionalModifierMultiplier;
			hashCode = hashCode * hashCodeMultiplier + GetHashCodeFor(optionalModifierType.ModifierType, comparisonMode);
			return hashCode;
		}

		if (metadataType == MetadataType.Pinned)
		{
			var pinnedType = (PinnedType)obj;
			return GetHashCodeFor(pinnedType.ElementType, comparisonMode) * hashCodeMultiplier * pinnedMultiplier;
		}

		if (metadataType == MetadataType.Sentinel)
		{
			var sentinelType = (SentinelType)obj;
			return GetHashCodeFor(sentinelType.ElementType, comparisonMode) * hashCodeMultiplier * sentinelMultiplier;
		}

		if (metadataType == MetadataType.FunctionPointer)
		{
			throw new NotImplementedException("We currently don't handle function pointer types.");
		}

		var def = comparisonMode == CecilTypeComparisonMode.Exact ? obj.Resolve() : obj as TypeDefinition;

		if (def is not null)
		{
			return def.Module.Name.GetHashCode() * moduleMultiplier
			       + def.Module.Assembly.Name.Name.GetHashCode() * assemblyMultiplier
			       + obj.Namespace.GetHashCode() * hashCodeMultiplier
			       + obj.FullName.GetHashCode();
		}
		else
		{
			return obj.Namespace.GetHashCode() * hashCodeMultiplier
			       + obj.FullName.GetHashCode();
		}
	}
}
