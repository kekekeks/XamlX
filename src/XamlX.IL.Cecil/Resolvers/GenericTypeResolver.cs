using System;
using Mono.Cecil;

namespace XamlX.TypeSystem;

internal class GenericTypeResolver
{
	private readonly IGenericInstance? _typeDefinitionContext;
	private readonly IGenericInstance? _methodDefinitionContext;

	public GenericTypeResolver()
	{

	}

	public GenericTypeResolver(GenericInstanceType? typeDefinitionContext)
	{
		_typeDefinitionContext = typeDefinitionContext;
	}

	public GenericTypeResolver(GenericInstanceType? typeDefinitionContext, GenericInstanceMethod? methodDefinitionContext)
	{
		_typeDefinitionContext = typeDefinitionContext;
		_methodDefinitionContext = methodDefinitionContext;
	}

	public MethodReference Resolve(MethodReference method, bool transformGenerics = true)
	{
		var methodReference = method;
		if (IsDummy())
			return methodReference;

		var declaringType = Resolve(method.DeclaringType);

		var genericInstanceMethod = method as GenericInstanceMethod;
		if (genericInstanceMethod != null)
		{
			methodReference = new MethodReference(method.Name, method.ReturnType, declaringType);

			foreach (var p in method.Parameters)
				methodReference.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));

			foreach (var gp in genericInstanceMethod.ElementMethod.GenericParameters)
				methodReference.GenericParameters.Add(new GenericParameter(gp.Name, methodReference));

			methodReference.HasThis = method.HasThis;

			var m = new GenericInstanceMethod(methodReference);
			foreach (var ga in genericInstanceMethod.GenericArguments)
			{
				m.GenericArguments.Add(transformGenerics ? Resolve(ga) : ga);
			}

			methodReference = m;
		}
		else
		{
			methodReference = new MethodReference(method.Name, method.ReturnType, declaringType);

			foreach (var gp in method.GenericParameters)
				methodReference.GenericParameters.Add(new GenericParameter(gp.Name, methodReference));

			foreach (var p in method.Parameters)
				methodReference.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));

			methodReference.HasThis = method.HasThis;
		}


		return methodReference;
	}

	public FieldReference Resolve(FieldReference field)
	{
		if (IsDummy())
			return field;

		return new FieldReference(field.Name, field.FieldType, Resolve(field.DeclaringType));
	}

	public TypeReference Resolve(TypeReference typeReference)
	{
		return Resolve(typeReference, true);
	}

	public TypeReference Resolve(TypeReference typeReference, bool includeTypeDefinitions)
	{
		if (IsDummy())
			return typeReference;

		if (_typeDefinitionContext == typeReference)
			return typeReference;

		if (_typeDefinitionContext != null && _typeDefinitionContext.GenericArguments.Contains(typeReference))
			return typeReference;
		if (_methodDefinitionContext != null && _methodDefinitionContext.GenericArguments.Contains(typeReference))
			return typeReference;

		var genericParameter = typeReference as GenericParameter;
		if (genericParameter != null)
		{
			if (_typeDefinitionContext != null && _typeDefinitionContext.GenericArguments.Contains(genericParameter))
				return genericParameter;
			if (_methodDefinitionContext != null &&
			    _methodDefinitionContext.GenericArguments.Contains(genericParameter))
				return genericParameter;
			return ResolveGenericParameter(genericParameter);
		}

		var arrayType = typeReference as ArrayType;
		if (arrayType != null)
			return new ArrayType(Resolve(arrayType.ElementType), arrayType.Rank);

		var pointerType = typeReference as PointerType;
		if (pointerType != null)
			return new PointerType(Resolve(pointerType.ElementType));

		var byReferenceType = typeReference as ByReferenceType;
		if (byReferenceType != null)
			return new ByReferenceType(Resolve(byReferenceType.ElementType));

		var pinnedType = typeReference as PinnedType;
		if (pinnedType != null)
			return new PinnedType(Resolve(pinnedType.ElementType));

		var genericInstanceType = typeReference as GenericInstanceType;
		if (genericInstanceType != null)
		{
			var newGenericInstanceType = new GenericInstanceType(genericInstanceType.ElementType);
			foreach (var genericArgument in genericInstanceType.GenericArguments)
				newGenericInstanceType.GenericArguments.Add(Resolve(genericArgument));
			return newGenericInstanceType;
		}

		var requiredModType = typeReference as RequiredModifierType;
		if (requiredModType != null)
			return Resolve(requiredModType.ElementType, includeTypeDefinitions);


		if (includeTypeDefinitions)
		{
			var typeDefinition = typeReference as TypeDefinition;
			if (typeDefinition != null && typeDefinition.HasGenericParameters)
			{
				var newGenericInstanceType = new GenericInstanceType(typeDefinition);
				foreach (var gp in typeDefinition.GenericParameters)
					newGenericInstanceType.GenericArguments.Add(Resolve(gp));
				return newGenericInstanceType;
			}
		}

		if (typeReference is TypeSpecification)
			throw new NotSupportedException(string.Format("The type {0} cannot be resolved correctly.",
				typeReference.FullName));

		return typeReference;
	}

	internal GenericTypeResolver Nested(TypeReference? typeReference, MethodReference? methodReference)
	{
		// If none of parameters has changed - return current instance.
		var (oldType, oldMethod) = (_typeDefinitionContext, _methodDefinitionContext);
		var newType = typeReference is not null ? typeReference as GenericInstanceType : oldType;
		var newMethod = methodReference is not null ? methodReference as GenericInstanceMethod : oldMethod;

		if (oldType != newType || oldMethod != newMethod)
			return new GenericTypeResolver(newType as GenericInstanceType, newMethod as GenericInstanceMethod);
		return this;
	}

	private TypeReference ResolveGenericParameter(GenericParameter genericParameter)
	{
		if (genericParameter.Owner == null)
			return HandleOwnerlessInvalidILCode(genericParameter);

		var memberReference = genericParameter.Owner as MemberReference;
		if (memberReference == null)
			throw new NotSupportedException();

		return genericParameter.Type == GenericParameterType.Type
			? _typeDefinitionContext!.GenericArguments[genericParameter.Position]
			: (_methodDefinitionContext != null
				? _methodDefinitionContext.GenericArguments[genericParameter.Position]
				: genericParameter);
	}

	private TypeReference HandleOwnerlessInvalidILCode(GenericParameter genericParameter)
	{
		// NOTE: If owner is null and we have a method parameter, then we'll assume that the method parameter
		// is actually a type parameter, and we'll use the type parameter from the corresponding position. I think
		// this assumption is valid, but if you're visiting this code then I might have been proven wrong.
		if (genericParameter.Type == GenericParameterType.Method && (_typeDefinitionContext != null &&
		                                                             genericParameter.Position <
		                                                             _typeDefinitionContext.GenericArguments.Count))
			return _typeDefinitionContext.GenericArguments[genericParameter.Position];

		// NOTE: Owner cannot be null, but sometimes the Mono compiler generates invalid IL and we
		// end up in this situation.
		// When we do, we assume that the runtime doesn't care about the resolved type of the GenericParameter,
		// thus we return a reference to System.Object.
		return genericParameter.Module.TypeSystem.Object;
	}

	private bool IsDummy()
	{
		return _typeDefinitionContext == null && _methodDefinitionContext == null;
	}
}