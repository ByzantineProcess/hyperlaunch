using System.Collections.Generic;
using System.Text.Json.Serialization;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Instances.Mods.Modrinth;

public class MiniProject
{
    [JsonPropertyName("project_id")]
    public required string Id { get; set; }

    [JsonPropertyName("organization")]
    public required string OrganisationName { get; set; }

    [JsonPropertyName("organization_id")]
    public required string OrganisationId { get; set; }

    [JsonPropertyName("author")]
    public required string AuthorName { get; set; }

    [JsonPropertyName("author_id")]
    public required string AuthorId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("summary")]
    public required string Summary { get; set; }

    [JsonPropertyName("project_types")]
    public required List<string> ProjectTypes { get; set; } // TODO: Map to enum?

    [JsonPropertyName("all_project_types")]
    public required List<string> AllProjectTypes { get; set; } // TODO: Map to enum?

    [JsonPropertyName("categories")]
    public required List<string> Categories { get; set; }

    [JsonPropertyName("display_categories")]
    public required List<string> DisplayCategories { get; set; }

    [JsonPropertyName("environment")]
    public required List<string> Environment { get; set; }

    [JsonPropertyName("game_versions")]
    public required List<string> GameVersions { get; set; }

    [JsonPropertyName("loaders")]
    public required List<string> Loaders { get; set; }

    [JsonPropertyName("version_id")]
    public required string RecommendedVersionId { get; set; }

    [JsonPropertyName("license")]
    public required string Licence { get; set; }

    [JsonPropertyName("date_created")]
    public required string CreatedDate { get; set; }

    [JsonPropertyName("date_modified")]
    public required string ModifiedDate { get; set; }

    [JsonPropertyName("downloads")]
    public required int Downloads { get; set; }

    [JsonPropertyName("follows")]
    public required int Follows { get; set; }

    [JsonPropertyName("gallery")]
    public required List<string> Gallery { get; set; }

    [JsonPropertyName("dependencies")]
    public required List<Dependency> Dependencies { get; set; }

    #nullable enable

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("organisation")]
    public string? Organisation { get; set; }

    [JsonPropertyName("requested_status")]
    public string? RequestedStatus { get; set; }

    [JsonPropertyName("approved")]
    public string? ApprovedDate { get; set; }

    [JsonPropertyName("queued")]
    public string? Queued { get; set; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("raw_icon_url")]
    public string? RawIconUrl { get; set; }

    [JsonPropertyName("color")]
    public int? Colour { get; set; }

    [JsonPropertyName("issues_url")]
    public string? IssuesUrl { get; set; }

    [JsonPropertyName("source_url")]
    public string? SourceUrl { get; set; }

    [JsonPropertyName("wiki_url")]
    public string? WikiUrl { get; set; }

    [JsonPropertyName("discord_url")]
    public string? DiscordUrl { get; set; }

    [JsonPropertyName("donation_urls")]
    public List<object>? DonationUrls { get; set; } // TODO: Donation Platform object


    public override string ToString()
    {
        return PrettyToString.Generic(this);
    }
}

public class V2Project : FullProjectBase
{
    [JsonPropertyName("team")]
    public required string Team { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("body")]
    public required string Body { get; set; }

    [JsonPropertyName("project_type")]
    public required string ProjectType { get; set; } // TODO: Map to enum?
}

public class FullProject : FullProjectBase
{
    
}

public class FullProjectBase
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; } // TODO: Map to enum?

    [JsonPropertyName("categories")]
    public required List<string> Categories { get; set; }

    [JsonPropertyName("additional_categories")]
    public required List<string> AdditionalCategories { get; set; }

    [JsonPropertyName("environment")]
    public required List<string> Environment { get; set; }

    [JsonPropertyName("game_versions")]
    public required List<string> GameVersions { get; set; }

    [JsonPropertyName("loaders")]
    public required List<string> Loaders { get; set; }

    [JsonPropertyName("versions")]
    public required List<string> Versions { get; set; }

    [JsonPropertyName("license")]
    public required Licence Licence { get; set; } // TODO: Licence object

    [JsonPropertyName("published")]
    public required string PublishedDate { get; set; }

    [JsonPropertyName("updated")]
    public required string UpdatedDate { get; set; }

    [JsonPropertyName("downloads")]
    public required int Downloads { get; set; }

    [JsonPropertyName("followers")]
    public required int Followers { get; set; }

    [JsonPropertyName("gallery")]
    public required List<Image> Gallery { get; set; } // TODO: Image object

    [JsonPropertyName("thread_id")]
    public required string ModerationThreadId { get; set; }

    [JsonPropertyName("monetization_status")]
    public required string MonetisationStatus { get; set; } // TODO: Enum values: monetised, not monetised, demonetised.

    #nullable enable

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("organisation")]
    public string? Organisation { get; set; }

    [JsonPropertyName("requested_status")]
    public string? RequestedStatus { get; set; }

    [JsonPropertyName("approved")]
    public string? ApprovedDate { get; set; }

    [JsonPropertyName("queued")]
    public string? Queued { get; set; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("raw_icon_url")]
    public string? RawIconUrl { get; set; }

    [JsonPropertyName("color")]
    public int? Colour { get; set; }

    [JsonPropertyName("issues_url")]
    public string? IssuesUrl { get; set; }

    [JsonPropertyName("source_url")]
    public string? SourceUrl { get; set; }

    [JsonPropertyName("wiki_url")]
    public string? WikiUrl { get; set; }

    [JsonPropertyName("discord_url")]
    public string? DiscordUrl { get; set; }

    [JsonPropertyName("donation_urls")]
    public List<object>? DonationUrls { get; set; } // TODO: Donation Platform object


    public override string ToString()
    {
        return PrettyToString.Generic(this);
    }
}