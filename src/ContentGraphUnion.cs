using Graphify.EPiServer.Core;
using Graphify.EPiServer.Core.Attributes;
using Graphify.EPiServer.Core.Factory;
using Graphify.EPiServer.Core.Loader;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Graphify.EPiServer.Cms
{
    [ServiceConfiguration(typeof(IEPiServerGraphUnion), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ContentGraphUnion : UnionGraphType, IEPiServerGraphUnion
    {
        public const string NONE_RESOLVED_GRAPH_NAME = "NoneResolvedType";

        private readonly IInterfaceGraphType _contentInterface;
        private readonly IInterfaceGraphType _localizableInterface;
        private readonly ObjectGraphTypeFactory _objectGraphTypeFactory;

        public ContentGraphUnion(IContentTypeRepository contentTypeRepository, IServiceLocator serviceLocator)
        {
            Name = "ContentUnion";

            _contentInterface = GraphTypeLoader.GetGraphInterface<IContent>(serviceLocator);
            _localizableInterface = GraphTypeLoader.GetGraphInterface<ILocalizable>(serviceLocator);
            _objectGraphTypeFactory = new ObjectGraphTypeFactory();

            var availableTypes = ContentTypeLoader.GetAvailableEpiContentTypes(contentTypeRepository);

            var blockTypes = availableTypes.Where(IsBlockType);
            var otherTypes = availableTypes.Where(x => IsBlockType(x) == false);

            // Create graphs of type Block
            var blockGraphs = CreateGraphs(blockTypes);

            // Create a dummy content Type for none resolved graphs
            var dummyContentType = new ContentType { Name = NONE_RESOLVED_GRAPH_NAME };
            var dummyGraphType = _objectGraphTypeFactory
                .CreateGraphFromType(
                    dummyContentType,
                    new[] { _contentInterface },
                    (target) => IsTypeOf(target, dummyContentType)
                );
            AddPossibleType(dummyGraphType);

            var otherGraphs =
                CreateGraphs(otherTypes)
                .Concat(new[] { dummyGraphType });
        }

        private static bool IsBlockType(ContentType contentType)
            => typeof(BlockData).IsAssignableFrom(contentType.ModelType);

        private bool IsTypeOf(object target, ContentType contentType)
        {
            bool isTypeOf = target.GetOriginalType() == contentType.ModelType;

            if (!isTypeOf && contentType.ModelType == null)
            {
                bool hasAnyType = base.PossibleTypes
                    .Any(x =>
                        x.HasMetadata("type") &&
                        x.GetMetadata<Type>("type").Equals(target.GetOriginalType())
                    );

                if (hasAnyType == false)
                    return true;
            }

            return isTypeOf;
        }

        private void FallbackSetFields(ref ObjectGraphType objectGraph, (PropertyInfo propertyInfo, string description) tuple)
        {
            var propType = tuple.propertyInfo.PropertyType;
            var description = tuple.description;

            var hasAttributeNotHide = tuple.propertyInfo.HasAttributeWithConditionOrTrue<GraphPropertyAttribute>(x => x.Hide == false);

            // Check if it's a Block (IContentData) type
            if (typeof(IContentData).IsAssignableFrom(propType) && hasAttributeNotHide)
            {
                // NOTE! Assumes that all blocks that could be local blocks are already processed and resolved and inserted into the "PossibleTypes"
                var resolvedBlockGraphType = base.PossibleTypes
                    .FirstOrDefault(x =>
                        x.HasMetadata("type") &&
                        ((System.Type)x.Metadata["type"]).Equals(propType)
                    );                

                objectGraph.AddField(
                    new FieldType
                    {
                        Name = ObjectGraphTypeFactory.getPropertyName(tuple.propertyInfo),
                        Description = description,
                        ResolvedType = resolvedBlockGraphType
                    });
            }

            // Otherwise do nothing
        }

        /// <summary>
        /// NOTE! Converts block types first so they can be used as local blocks
        /// </summary>
        /// <returns></returns>
        private IEnumerable<ObjectGraphType> CreateGraphs(IEnumerable<ContentType> contentTypes)
        {
            // First step create all the graph types with interface properties
            var graphs = contentTypes.Select(contentType =>
            {
                var interfaces = new List<IInterfaceGraphType> { _contentInterface };
                if (contentType.ModelType != null && typeof(PageData).IsAssignableFrom(contentType.ModelType))
                {
                    interfaces.Add(_localizableInterface);
                }

                var graph = _objectGraphTypeFactory.CreateGraphFromType(
                    contentType,
                    interfaces,
                    (target) => IsTypeOf(target, contentType)
                );

                // Add type so we can utilize it on other types later
                AddPossibleType(graph);
                return graph;
            })
            .ToArray(); // Run to array to force running above code. Needed for reference below
            
            // Create all properties on graph
            for (int i = 0; i < graphs.Length; i++)
            {
                _objectGraphTypeFactory
                    .AddPropertiesToGraph(
                        ref graphs[i],
                        contentTypes.ElementAt(i),
                        FallbackSetFields
                    );
            }

            return graphs;
        }
    }
}
