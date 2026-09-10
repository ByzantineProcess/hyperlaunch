using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Hyperlaunch.Instances.Mods.Modrinth;

// i made this entire facet system because the modrinth docs were wrong
// end my suffering
public class Facet
{
    FacetType facetType;
    FacetOps facetOperator;
    string facetValue;
    public Facet(FacetType facetType, FacetOps facetOperator, string facetValue)
    {
        this.facetType = facetType;
        this.facetOperator = facetOperator;
        this.facetValue = facetValue;
    }

    public string Construct()
    {
        return $"{Utilities.StringEnum.Retrieve(facetType)}{Utilities.StringEnum.Retrieve(facetOperator)}{facetValue}";
    }

    public FacetCollection And(Facet facet)
    {
        return new FacetCollection(FacetCollectionType.AND, [this, facet], []);
    }

    public FacetCollection And(FacetCollection facetCollection)
    {
        return new FacetCollection(FacetCollectionType.AND, [this], [facetCollection]);
    }

    public FacetCollection Or(Facet facet)
    {
        return new FacetCollection(FacetCollectionType.OR, [this, facet], []);
    }

    public FacetCollection Or(FacetCollection facetCollection)
    {
        return new FacetCollection(FacetCollectionType.OR, [this], [facetCollection]);
    }

    public FacetCollection ToCollection()
    {
        return new FacetCollection(FacetCollectionType.AND, [this], []);
    }
}

public class FacetCollection
{
    public FacetCollectionType collectionType { get; set; }
    public List<Facet> childFacets { get; set; }
    public List<FacetCollection> childCollections { get; set; }

    public FacetCollection(FacetCollectionType collectionType, List<Facet> childFacets, List<FacetCollection> childCollections)
    {
        this.collectionType = collectionType;
        this.childFacets = childFacets;
        this.childCollections = childCollections;
    }

    public FacetCollection And(Facet facet)
    {
        if (collectionType == FacetCollectionType.AND)
        {
            childFacets.Add(facet);
            return this;
        }
        else // if (collectionType == FacetCollectionType.OR)
        {
            return new FacetCollection(FacetCollectionType.AND, [facet], [this]);
        }
    }

    public FacetCollection And(FacetCollection facetCollection)
    {
        if (collectionType == FacetCollectionType.AND)
        {
            childCollections.Add(facetCollection);
            return this;
        }
        else // if (collectionType == FacetCollectionType.OR)
        {
            return new FacetCollection(FacetCollectionType.AND, [], [this, facetCollection]);
        }
    }

    public FacetCollection Or(Facet facet)
    {
        if (collectionType == FacetCollectionType.OR)
        {
            childFacets.Add(facet);
            return this;
        }
        else // if (collectionType == FacetCollectionType.AND)
        {
            return new FacetCollection(FacetCollectionType.OR, [facet], [this]);
        }
    }

    public FacetCollection Or(FacetCollection facetCollection)
    {
        if (collectionType == FacetCollectionType.OR)
        {
            childCollections.Add(facetCollection);
            return this;
        }
        else // if (collectionType == FacetCollectionType.AND)
        {
            return new FacetCollection(FacetCollectionType.OR, [], [this, facetCollection]);
        }
    }

    public FacetCollection WrapAnd()
    {
        return new FacetCollection(FacetCollectionType.AND, [], [this]);
    }

    public List<object> Construct()
    {
        List<object> res = new List<object>();

        if (childCollections.Count > 0)
        {
            foreach (FacetCollection facetCollection in childCollections)
            {
                res.Add(facetCollection.Construct());
            }
        }
        if (childFacets.Count > 0)
        {
            foreach (Facet facet in childFacets)
            {
                if (collectionType == FacetCollectionType.AND)
                {
                    
                    res.Add(new[] {facet.Construct()}); // thanks i hate it (https://stackoverflow.com/a/30509206)
                }
                else
                {
                    res.Add(facet.Construct());
                }
            }
        }
        return res;
    }

    public string Serialise() // we use normal english here
    {
        JsonSerializerOptions jsonOpts = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // it gets url encoded later don't worry
        };
        return JsonSerializer.Serialize(Construct(), jsonOpts);
    }
}

public enum FacetCollectionType
{
    AND,
    OR
}

public enum FacetType
{
    [Utilities.StringEnum("project_type")]
    ProjectType,

    [Utilities.StringEnum("all_project_types")]
    AllProjectTypes,

    [Utilities.StringEnum("categories")]
    Categories,

    [Utilities.StringEnum("versions")]
    Versions,

    [Utilities.StringEnum("client_side")]
    ClientSide,

    [Utilities.StringEnum("server_side")]
    ServerSide,

    [Utilities.StringEnum("open_source")]
    OpenSource,

    [Utilities.StringEnum("title")]
    Title,

    [Utilities.StringEnum("author")]
    Author,

    [Utilities.StringEnum("follows")]
    Follows,

    [Utilities.StringEnum("project_id")]
    ProjectId,

    [Utilities.StringEnum("license")]
    License,

    [Utilities.StringEnum("downloads")]
    Downloads,

    [Utilities.StringEnum("created_timestamp")]
    CreatedTimestamp,

    [Utilities.StringEnum("modified_timestamp")]
    ModifiedTimestamp
}

public enum FacetOps
{
    [Utilities.StringEnum(":")]
    Is,

    [Utilities.StringEnum("!=")]
    IsNot,

    [Utilities.StringEnum(">=")]
    GreaterThanOrEqual,

    [Utilities.StringEnum(">")]
    GreaterThan,

    [Utilities.StringEnum("<=")]
    LessThanOrEqual,
    
    [Utilities.StringEnum("<")]
    LessThan
}