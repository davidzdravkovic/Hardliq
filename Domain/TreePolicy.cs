namespace TaskManager.Domain;

public static class TreePolicy
{
    public const int MaxFolders = 100;
    public const int MaxChildrenPerFolder = 20;
    public const int MaxFolderDepth = 15;

    public static int MaxTasksCeiling => MaxFolders * MaxChildrenPerFolder;
}
