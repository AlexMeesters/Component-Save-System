using System.Collections.Generic;

namespace Lowscope.Saving.Data
{
    /// <summary>
    /// Collection of all data retrieved from components. 
    /// This gets passed on to the savegame from a Saveable.
    /// </summary>
    [System.Serializable]
    public class ISaveableDataCollection
    {
        public List<ISaveableData> saveStructures = new List<ISaveableData>();

        public bool GetData (string identifier, out ISaveableData data)
        {
            for (int i = 0; i < saveStructures.Count; i++)
            {
                if (saveStructures[i].identifier == identifier)
                {
                    data = saveStructures[i];
                    return true;
                }
            }

            data = default(ISaveableData);
            return false;
        }
    }
}