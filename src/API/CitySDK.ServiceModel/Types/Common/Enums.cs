namespace CitySDK.ServiceModel.Types
{
    public enum APITerms
    {
        pois,
        routes,
        events
    }

    public enum Term
    {
        category,
        tag
    };

    public enum AuthorTerms
    {
        primary,
        secondary,
        contributor,
        editor,
        publisher
    };

    public enum TimeTerms
    {
        start,
        end,
        instant,
        open
    };

    /// <summary>
    /// Human-readable labels
    /// </summary>
    public enum LabelTerms
    {
        primary,
        note
    };

    public enum PointTerms
    {
        center,
        navigator_point,
        entrance
    };

    public enum RelationShipTerms
    {
        within,
        contains,
        equals,
        disjoint,
        intersects,
        touches,
        crosses,
        overlaps
    };

    public enum LinkTerms
    {
        alternate,
        canonical,
        capyright,
        describedby,
        edit,
        enclosure,
        icon,
        latest_version,
        license,
        related,
        search,

        parent,
        child,
        historic,
        future
    };

    public enum LicenseTerms
    {
        commom,
        opensource
    };
}
