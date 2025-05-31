using Scellecs.Morpeh;
public struct BuildRequest : IRequestData {
    public string buildingName;
}

public struct ToggleGrid : IRequestData {
    public bool activity;
}

public struct BuildUnitRequest : IRequestData {
    public string unitName;
}
