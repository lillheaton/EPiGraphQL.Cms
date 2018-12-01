using Eols.EPiGraphQL.Core;
using Eols.EPiGraphQL.Core.Attributes;
using Eols.EPiGraphQL.Core.Factory;
using Eols.EPiGraphQL.Core.Loader;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Eols.EPiGraphQL.Cms
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

            // Add types so we can utilize them on other types (PageData)
            foreach (var graph in blockGraphs)
            {
                AddPossibleType(graph);
            }

            // Create a dummy content Type for none resolved graphs
            var dummyContentType = new ContentType { Name = NONE_RESOLVED_GRAPH_NAME };

            var otherGraphs = 
                CreateGraphs(otherTypes)
                .Concat(
                    new[] 
                    {
                        _objectGraphTypeFactory.CreateGraphFromType(
                            dummyContentType,
                            new[] { _contentInterface },
                            (target) => IsTypeOf(target, dummyContentType),
                            FallbackSetFields
                        )
                    }
                );
            
            foreach (var graph in otherGraphs)
            {                
                AddPossibleType(graph);
            }
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

        private void FallbackSetFields(ref ObjectGraphType objectGraph, (PropertyInfo propertyInfo, DisplayAttribute attribute) tuple)
        {
            var propType = tuple.propertyInfo.PropertyType;
            var displayAttribute = tuple.attribute;

            // Check if it's a Block (IContentData) type
            if (typeof(IContentData).IsAssignableFrom(propType) && propType.HasAttribute<GraphHideAttribute>() == false)
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
                        Name = tuple.propertyInfo.Name,
                        Description = displayAttribute.Description,
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
            return contentTypes.Select(contentType =>
            {
                var interfaces = new List<IInterfaceGraphType> { _contentInterface };
                if (contentType.ModelType != null && typeof(PageData).IsAssignableFrom(contentType.ModelType))
                {
                    interfaces.Add(_localizableInterface);
                }
                
                return _objectGraphTypeFactory.CreateGraphFromType(
                    contentType,
                    interfaces,
                    (target) => IsTypeOf(target, contentType),
                    FallbackSetFields
                );
            });
        }        
    }
}