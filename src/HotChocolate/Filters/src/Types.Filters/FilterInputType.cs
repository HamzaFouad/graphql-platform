using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Internal.FieldInitHelper;

namespace HotChocolate.Types.Filters;

[Obsolete("Use HotChocolate.Data.")]
public class FilterInputType<T>
    : InputObjectType
        , IFilterInputType
{
    private readonly Action<IFilterInputTypeDescriptor<T>> _configure;

    public FilterInputType()
    {
        _configure = Configure;
    }

    public FilterInputType(
        Action<IFilterInputTypeDescriptor<T>> configure)
    {
        _configure = configure
            ?? throw new ArgumentNullException(nameof(configure));
    }

    public Type EntityType { get; private set; } = typeof(object);

    protected override InputObjectTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        var descriptor = FilterInputTypeDescriptor<T>.New(
            context.DescriptorContext,
            typeof(T));
        _configure(descriptor);
        return descriptor.CreateDefinition();
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InputObjectTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);

        SetTypeIdentity(typeof(FilterInputType<>));
    }

    protected virtual void Configure(
        IFilterInputTypeDescriptor<T> descriptor)
    {
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        if (definition is FilterInputTypeDefinition ft &&
            ft.EntityType is { })
        {
            EntityType = ft.EntityType;
        }
    }

    protected override FieldCollection<InputField> OnCompleteFields(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        var index = 0;
        var fields = new InputField[definition.Fields.Count + 2];

        fields[index] = new AndField(context.DescriptorContext, index);
        index++;

        fields[index] = new OrField(context.DescriptorContext, index);
        index++;

        foreach (var fieldDefinition in
            definition.Fields.OfType<FilterOperationDefinition>())
        {
            fields[index] = new FilterOperationField(fieldDefinition, index);
            index++;
        }

        return CompleteFields(context, this, fields);
    }

    // we are disabling the default configure method so
    // that this does not lead to confusion.
    protected sealed override void Configure(
        IInputObjectTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }

}
