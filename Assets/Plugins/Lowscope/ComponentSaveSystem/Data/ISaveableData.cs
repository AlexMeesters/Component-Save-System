namespace Lowscope.Saving.Data
{
    /// <summary>
    /// Data that has been retrieved from any ISaveable component
    /// </summary>
    [System.Serializable]
    public struct ISaveableData
    {
        public string identifier;
        public string data;
    }
}