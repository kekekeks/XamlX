using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace XamlX.TypeSystem;

// Cecil doesn't expose these comparers in any way, and only uses them for WinMD stuff internally. So we have to copy paste them.
// https://github.com/jbevain/cecil/blob/56d4409b8a0165830565c6e3f96f41bead2c418b/Mono.Cecil/MethodReferenceComparer.cs
// Also https://github.com/jbevain/cecil/issues/389

public class MethodReferenceEqualityComparer : EqualityComparer<MethodReference>
{
	private readonly CecilTypeComparisonMode _comparisonMode;

	// Initialized lazily for each thread
	[ThreadStatic] static List<MethodReference>? xComparisonStack;

	[ThreadStatic] static List<MethodReference>? yComparisonStack;

	public MethodReferenceEqualityComparer(CecilTypeComparisonMode comparisonMode)
	{
		_comparisonMode = comparisonMode;
	}
	
	public override bool Equals(MethodReference? x, MethodReference? y)
	{
		return AreEqual(x, y, _comparisonMode);
	}

	public override int GetHashCode(MethodReference obj)
	{
		return GetHashCodeFor(obj, _comparisonMode);
	}

	public static bool AreEqual(MethodReference? x, MethodReference? y, CecilTypeComparisonMode comparisonMode)
	{
		if (ReferenceEquals(x, y))
			return true;

		if (x is null || y is null)
			return false;

		if (x.HasThis != y.HasThis)
			return false;

		if (x.HasParameters != y.HasParameters)
			return false;

		if (x.HasGenericParameters != y.HasGenericParameters)
			return false;

		if (x.Parameters.Count != y.Parameters.Count)
			return false;

		if (x.Name != y.Name)
			return false;

		if (!TypeReferenceEqualityComparer.AreEqual(x.DeclaringType, y.DeclaringType, comparisonMode))
			return false;

		var xGeneric = x as GenericInstanceMethod;
		var yGeneric = y as GenericInstanceMethod;
		if (xGeneric != null || yGeneric != null)
		{
			if (xGeneric == null || yGeneric == null)
				return false;

			if (xGeneric.GenericArguments.Count != yGeneric.GenericArguments.Count)
				return false;

			for (int i = 0; i < xGeneric.GenericArguments.Count; i++)
				if (!TypeReferenceEqualityComparer.AreEqual(xGeneric.GenericArguments[i], yGeneric.GenericArguments[i], comparisonMode))
					return false;
		}

		var xResolved = comparisonMode == CecilTypeComparisonMode.Exact ? x.Resolve() : x as MethodDefinition;
		var yResolved = comparisonMode == CecilTypeComparisonMode.Exact ? y.Resolve() : y as MethodDefinition;

		if (xResolved != yResolved)
			return false;

		if (xResolved == null)
		{
			// We couldn't resolve either method. In order for them to be equal, their parameter types _must_ match. But wait, there's a twist!
			// There exists a situation where we might get into a recursive state: parameter type comparison might lead to comparing the same
			// methods again if the parameter types are generic parameters whose owners are these methods. We guard against these by using a
			// thread static list of all our comparisons carried out in the stack so far, and if we're in progress of comparing them already,
			// we'll just say that they match.

			if (xComparisonStack == null)
				xComparisonStack = new List<MethodReference>();

			if (yComparisonStack == null)
				yComparisonStack = new List<MethodReference>();

			for (int i = 0; i < xComparisonStack.Count; i++)
			{
				if (xComparisonStack[i] == x && yComparisonStack[i] == y)
					return true;
			}

			xComparisonStack.Add(x);

			try
			{
				yComparisonStack.Add(y);

				try
				{
					for (int i = 0; i < x.Parameters.Count; i++)
					{
						if (!TypeReferenceEqualityComparer.AreEqual(x.Parameters[i].ParameterType,
							    y.Parameters[i].ParameterType, comparisonMode))
							return false;
					}
				}
				finally
				{
					yComparisonStack.RemoveAt(yComparisonStack.Count - 1);
				}
			}
			finally
			{
				xComparisonStack.RemoveAt(xComparisonStack.Count - 1);
			}
		}

		return true;
	}

	public static bool AreSignaturesEqual(MethodReference x, MethodReference y,
		CecilTypeComparisonMode comparisonMode)
	{
		if (x.HasThis != y.HasThis)
			return false;

		if (x.Parameters.Count != y.Parameters.Count)
			return false;

		if (x.GenericParameters.Count != y.GenericParameters.Count)
			return false;

		for (var i = 0; i < x.Parameters.Count; i++)
			if (!TypeReferenceEqualityComparer.AreEqual(x.Parameters[i].ParameterType, y.Parameters[i].ParameterType,
				    comparisonMode))
				return false;

		if (!TypeReferenceEqualityComparer.AreEqual(x.ReturnType, y.ReturnType, comparisonMode))
			return false;

		return true;
	}

	public static int GetHashCodeFor(MethodReference obj, CecilTypeComparisonMode comparisonMode)
	{
		// a very good prime number
		const int hashCodeMultiplier = 486187739;

		var genericInstanceMethod = obj as GenericInstanceMethod;
		if (genericInstanceMethod != null)
		{
			var hashCode = GetHashCodeFor(genericInstanceMethod.ElementMethod, comparisonMode);
			for (var i = 0; i < genericInstanceMethod.GenericArguments.Count; i++)
				hashCode = hashCode * hashCodeMultiplier +
				           TypeReferenceEqualityComparer.GetHashCodeFor(genericInstanceMethod.GenericArguments[i], comparisonMode);
			return hashCode;
		}

		return TypeReferenceEqualityComparer.GetHashCodeFor(obj.DeclaringType, comparisonMode) * hashCodeMultiplier +
		       obj.Name.GetHashCode();
	}
}