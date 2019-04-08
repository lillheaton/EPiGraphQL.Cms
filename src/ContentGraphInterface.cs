using EPiGraphQL.Core;
using EPiGraphQL.Core.Factory;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using GraphQL.Types;

namespace EPiGraphQL.Cms
{
    [ServiceConfiguration(typeof(IInterfaceGraphType), Lifecycle = ServiceInstanceScope.Singleton)]    
    public class ContentGraphInterface : InterfaceGraphType<IContent>
    {
        public ContentGraphInterface(
            IContentLoader contentLoader,            
            IContentTypeRepository contentTypeRepository)
        {            
            Name = "ContentInterface";
            Field(x => x.Name);
            Field(x => x.ContentTypeID);
            Field<IntGraphType>("Id", resolve: x => x.Source.ContentLink.ID);
            Field<IntGraphType>("ParentId", resolve: x => x.Source.ParentLink.ID);
            Field<StringGraphType>("Type", resolve: x => x.Source.GetOriginalType().Name);
            Field<StringGraphType>("CsharpClassName", resolve: x => x.Source.GetOriginalType().FullName);
            Field<StringGraphType>(
                "Path",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType>
                    {
                        DefaultValue = false,
                        Name = Constants.Arguments.ARGUMENT_ABSOLUTE_URL
                    },
                    new QueryArgument<StringGraphType>
                    {
                        DefaultValue = Constants.Value.DefaultLocale,
                        Name = Constants.Arguments.ARGUMENT_LOCALE
                    }
                ),
                resolve: x =>
                {
                    var locale = x.GetLocaleFromArgument();

                    return x.Source.ContentLink.GetUrl(
                        locale.Name, x.GetArgument<bool>(Constants.Arguments.ARGUMENT_ABSOLUTE_URL)
                    );
                });
            
            Connection<ContentGraphInterface>()
                .Name("Children")
                .PageSize(10)
                .Argument<BooleanGraphType, bool>(Constants.Arguments.ARGUMENT_ALLOWFALLBACK_LANG, "Allow Fallback Language", true)
                .Resolve(context =>
                {
                    var loaderOptions = context.CreateLoaderOptionsFromAgruments();
                    return new ContentConnectionFactory(contentLoader).CreateIContentConnection(context, loaderOptions, 10);                    
                });   
        }
    }
}