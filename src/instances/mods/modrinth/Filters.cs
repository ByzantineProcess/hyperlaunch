
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Instances.Mods.Modrinth;

public class Filter
{
    FilterType FilterType;
    FilterOps FilterOperator;
    string FilterValue;
    bool? Not;
    public Filter(FilterType FilterType, FilterOps FilterOperator, string FilterValue, bool? Not = false)
    {
        this.FilterType = FilterType;
        this.FilterOperator = FilterOperator;
        this.FilterValue = FilterValue;
        this.Not = Not;
    }

    public string Construct()
    {
        string res = $"{StringEnum.Retrieve(FilterType)} {StringEnum.Retrieve(FilterOperator)} `{FilterValue}`";
        if (Not == true)
        {
            res = "NOT " + res;
        }
        return res;
    }

    public FilterCollection And(Filter Filter)
    {
        return new FilterCollection(FilterCollectionType.AND, [this, Filter], []);
    }

    public FilterCollection And(FilterCollection FilterCollection)
    {
        return new FilterCollection(FilterCollectionType.AND, [this], [FilterCollection]);
    }

    public FilterCollection Or(Filter Filter)
    {
        return new FilterCollection(FilterCollectionType.OR, [this, Filter], []);
    }

    public FilterCollection Or(FilterCollection FilterCollection)
    {
        return new FilterCollection(FilterCollectionType.OR, [this], [FilterCollection]);
    }

    public FilterCollection ToCollection()
    {
        return new FilterCollection(FilterCollectionType.AND, [this], []);
    }
}

public class FilterCollection
{
    public FilterCollectionType collectionType { get; set; }
    public List<Filter> childFilters { get; set; }
    public List<FilterCollection> childCollections { get; set; }

    public FilterCollection(FilterCollectionType collectionType, List<Filter> childFilters, List<FilterCollection> childCollections)
    {
        this.collectionType = collectionType;
        this.childFilters = childFilters;
        this.childCollections = childCollections;
    }

    public FilterCollection And(Filter Filter)
    {
        if (collectionType == FilterCollectionType.AND)
        {
            childFilters.Add(Filter);
            return this;
        }
        else // if (collectionType == FilterCollectionType.OR)
        {
            return new FilterCollection(FilterCollectionType.AND, [Filter], [this]);
        }
    }

    public FilterCollection And(FilterCollection FilterCollection)
    {
        if (collectionType == FilterCollectionType.AND)
        {
            childCollections.Add(FilterCollection);
            return this;
        }
        else // if (collectionType == FilterCollectionType.OR)
        {
            return new FilterCollection(FilterCollectionType.AND, [], [this, FilterCollection]);
        }
    }

    public FilterCollection Or(Filter Filter)
    {
        if (collectionType == FilterCollectionType.OR)
        {
            childFilters.Add(Filter);
            return this;
        }
        else // if (collectionType == FilterCollectionType.AND)
        {
            return new FilterCollection(FilterCollectionType.OR, [Filter], [this]);
        }
    }

    public FilterCollection Or(FilterCollection FilterCollection)
    {
        if (collectionType == FilterCollectionType.OR)
        {
            childCollections.Add(FilterCollection);
            return this;
        }
        else // if (collectionType == FilterCollectionType.AND)
        {
            return new FilterCollection(FilterCollectionType.OR, [], [this, FilterCollection]);
        }
    }

    public FilterCollection WrapAnd()
    {
        return new FilterCollection(FilterCollectionType.AND, [], [this]);
    }

    public string Construct()
    {
        string res = "";

        if (childCollections.Count > 0)
        {
            foreach (FilterCollection FilterCollection in childCollections)
            {
                res += $"({FilterCollection.Construct()})";
                if (childCollections.IndexOf(FilterCollection) != childCollections.Count-1 || childFilters.Count > 0)
                {
                    res += $" {StringEnum.Retrieve(collectionType)} ";
                }
            }
        }
        if (childFilters.Count > 0)
        {
            foreach (Filter Filter in childFilters)
            {
                res += Filter.Construct();
                if (childFilters.IndexOf(Filter) != childFilters.Count-1)
                {
                    res += $" {StringEnum.Retrieve(collectionType)} ";
                }
            }
        }
        return res;
    }
}

public enum FilterCollectionType
{
    [StringEnum("AND")]
    AND,

    [StringEnum("OR")]
    OR
}

public enum FilterType
{
    [StringEnum("project_type")]
    ProjectType,

    [StringEnum("all_project_types")]
    AllProjectTypes,

    [StringEnum("categories")]
    Categories,

    [StringEnum("game_versions")]
    Versions,

    [StringEnum("client_side")]
    ClientSide,

    [StringEnum("server_side")]
    ServerSide,

    [StringEnum("open_source")]
    OpenSource,

    [StringEnum("title")]
    Title,

    [StringEnum("author")]
    Author,

    [StringEnum("follows")]
    Follows,

    [StringEnum("project_id")]
    ProjectId,

    [StringEnum("license")]
    License,

    [StringEnum("downloads")]
    Downloads,

    [StringEnum("created_timestamp")]
    CreatedTimestamp,

    [StringEnum("modified_timestamp")]
    ModifiedTimestamp
}

public enum FilterOps
{
    [StringEnum("=")]
    Is,

    [StringEnum("!=")]
    IsNot,

    [StringEnum(">=")]
    GreaterThanOrEqual,

    [StringEnum(">")]
    GreaterThan,

    [StringEnum("<=")]
    LessThanOrEqual,
    
    [StringEnum("<")]
    LessThan
}