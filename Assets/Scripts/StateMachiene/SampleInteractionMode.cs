public static class PlayerInteractionMode
{
    public static bool IsContainerPanelOpen { get; private set; }

    public static void SetContainerPanelOpen(bool open)
    {
        IsContainerPanelOpen = open;
    }
}